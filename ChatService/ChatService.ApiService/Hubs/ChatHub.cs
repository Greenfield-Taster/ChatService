using ChatService.Database.Models;
using ChatService.Database.Repositories;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatService.ApiService.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatRepository _chatRepository;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository _userRepository;

        // Словарь для хранения соответствия ConnectionId и UserId
        private static Dictionary<string, string> _connectionToUser = new Dictionary<string, string>();

        public ChatHub(
            IChatRepository chatRepository,
            IChatRoomRepository chatRoomRepository,
            IUserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _chatRoomRepository = chatRoomRepository;
            _userRepository = userRepository;
        }

        // Подключение пользователя
        public async Task ConnectUser(string userId)
        {
            try
            {
                // Проверка существования пользователя
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // Сохраняем соответствие ConnectionId и UserId
                _connectionToUser[Context.ConnectionId] = userId;

                // Добавляем пользователя в его личную группу
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                // Если пользователь - админ, добавляем его в группу админов
                if (user.Role == "admin")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
                }

                // Присоединение ко всем комнатам пользователя
                var rooms = await _chatRoomRepository.GetRoomsByUserIdAsync(userId);
                foreach (var room in rooms)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{room.Id}");
                }

                // Отправка уведомления о статусе пользователя
                await Clients.Group("admins").SendAsync("UserOnline", userId, user.Name);

                // Отправляем список чатов, если пользователь - админ
                if (user.Role == "admin")
                {
                    var allRooms = await _chatRoomRepository.GetAllRoomsAsync();
                    await Clients.Caller.SendAsync("ReceiveAllRooms", allRooms);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Отправка сообщения
        public async Task SendMessage(string roomId, string message)
        {
            try
            {
                if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var senderId))
                {
                    throw new Exception("User not authenticated");
                }

                var room = await _chatRoomRepository.GetRoomByIdAsync(roomId);
                if (room == null)
                {
                    throw new Exception("Chat room not found");
                }

                // Проверка права на отправку сообщений в эту комнату
                var sender = await _userRepository.GetUserByIdAsync(senderId);
                if (sender.Role != "admin" && room.UserId != senderId)
                {
                    throw new Exception("Not authorized to send messages to this room");
                }

                // Создание и сохранение сообщения
                var chatMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    Status = MessageStatus.Sent,
                    ChatRoomId = roomId,
                    SenderId = senderId
                };

                await _chatRepository.AddMessageAsync(chatMessage);

                // Отправка сообщения всем участникам комнаты
                await Clients.Group($"room_{roomId}").SendAsync("ReceiveMessage", new
                {
                    Id = chatMessage.Id,
                    Message = chatMessage.Message,
                    Timestamp = chatMessage.Timestamp,
                    Status = chatMessage.Status.ToString(),
                    RoomId = chatMessage.ChatRoomId,
                    SenderId = chatMessage.SenderId,
                    SenderName = sender.Name
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Создание новой комнаты чата
        public async Task CreateRoom(string userId)
        {
            try
            {
                if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var currentUserId))
                {
                    throw new Exception("User not authenticated");
                }

                // Если пользователь не админ и пытается создать комнату не для себя
                var currentUser = await _userRepository.GetUserByIdAsync(currentUserId);
                if (currentUser.Role != "admin" && currentUserId != userId)
                {
                    throw new Exception("Not authorized to create room for another user");
                }

                // Проверяем существование пользователя
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // Ищем существующую комнату между админом и пользователем
                var existingRoom = await _chatRoomRepository.GetRoomsByUserIdAsync(userId);
                if (existingRoom.Any())
                {
                    // Если комната уже существует, возвращаем ее ID
                    var existingRoomId = existingRoom.First().Id;
                    await Clients.Caller.SendAsync("RoomCreated", existingRoomId);
                    return;
                }

                // Назначаем админа (если текущий пользователь - админ, используем его, иначе берем первого админа)
                string adminId;
                if (currentUser.Role == "admin")
                {
                    adminId = currentUserId;
                }
                else
                {
                    var admin = await _userRepository.GetAdminUsersAsync();
                    if (!admin.Any())
                    {
                        throw new Exception("No admin users found");
                    }
                    adminId = admin.First().Id;
                }

                // Создаем новую комнату
                var roomId = Guid.NewGuid().ToString();
                var room = new ChatRoom
                {
                    Id = roomId,
                    Name = $"Support chat for {user.Name}",
                    CreatedAt = DateTime.UtcNow,
                    AdminId = adminId,
                    UserId = userId,
                    Messages = new List<ChatMessage>()
                };

                await _chatRoomRepository.AddRoomAsync(room);

                // Присоединяем текущего пользователя к комнате
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");

                // Уведомляем админов о новой комнате
                await Clients.Group("admins").SendAsync("NewRoomCreated", new
                {
                    Id = room.Id,
                    Name = room.Name,
                    CreatedAt = room.CreatedAt,
                    UserId = room.UserId,
                    UserName = user.Name,
                    AdminId = room.AdminId
                });

                // Отправляем подтверждение создания комнаты
                await Clients.Caller.SendAsync("RoomCreated", roomId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Присоединение к комнате
        public async Task JoinRoom(string roomId)
        {
            try
            {
                if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var userId))
                {
                    throw new Exception("User not authenticated");
                }

                var room = await _chatRoomRepository.GetRoomByIdAsync(roomId);
                if (room == null)
                {
                    throw new Exception("Chat room not found");
                }

                // Проверка права на доступ к комнате
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user.Role != "admin" && room.UserId != userId)
                {
                    throw new Exception("Not authorized to join this room");
                }

                // Присоединение к группе комнаты
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");

                // Загрузка истории сообщений
                var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId);

                // Преобразуем сообщения в анонимные объекты для отправки
                var messageList = messages.Select(m => new
                {
                    Id = m.Id,
                    Message = m.Message,
                    Timestamp = m.Timestamp,
                    Status = m.Status.ToString(),
                    RoomId = m.ChatRoomId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Name
                }).ToList();

                // Отправка истории сообщений
                await Clients.Caller.SendAsync("ReceiveMessageHistory", roomId, messageList);

                // Обновление статуса сообщений на "прочитано"
                await UpdateMessagesStatusAsync(roomId, userId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Покинуть комнату
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
        }

        // Получить все чаты (для админов)
        public async Task GetAllChats()
        {
            try
            {
                if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var userId))
                {
                    throw new Exception("User not authenticated");
                }

                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user.Role != "admin")
                {
                    throw new Exception("Only admins can view all chats");
                }

                var rooms = await _chatRoomRepository.GetAllRoomsAsync();

                // Преобразуем результаты для отправки
                var roomsList = rooms.Select(r => new
                {
                    Id = r.Id,
                    Name = r.Name,
                    CreatedAt = r.CreatedAt,
                    UserId = r.UserId,
                    UserName = r.RegularUser?.Name ?? "Unknown User",
                    AdminId = r.AdminId,
                    AdminName = r.Admin?.Name ?? "Unknown Admin",
                    LastMessage = r.Messages.OrderByDescending(m => m.Timestamp).FirstOrDefault()?.Message ?? "",
                    UnreadCount = r.Messages.Count(m => m.Status != MessageStatus.Read && m.SenderId != userId)
                }).ToList();

                await Clients.Caller.SendAsync("ReceiveAllChats", roomsList);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Метод для обновления статуса сообщений
        private async Task UpdateMessagesStatusAsync(string roomId, string userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId);

                // Обновляем статус только для сообщений, которые отправлены не этим пользователем
                var messagesToUpdate = messages.Where(m => m.SenderId != userId && m.Status != MessageStatus.Read).ToList();

                foreach (var message in messagesToUpdate)
                {
                    message.Status = MessageStatus.Read;
                    await _chatRepository.UpdateMessageAsync(message);
                }

                // Уведомляем о прочтении сообщений
                if (messagesToUpdate.Any())
                {
                    await Clients.Group($"room_{roomId}").SendAsync("MessagesRead",
                        roomId,
                        messagesToUpdate.Select(m => m.Id).ToList(),
                        userId);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Отправка статуса "печатает..."
        public async Task SendTypingStatus(string roomId, bool isTyping)
        {
            try
            {
                if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var userId))
                {
                    throw new Exception("User not authenticated");
                }

                var user = await _userRepository.GetUserByIdAsync(userId);

                await Clients.Group($"room_{roomId}").SendAsync("UserTyping", roomId, userId, user.Name, isTyping);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Обработка отключения пользователя
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_connectionToUser.TryGetValue(Context.ConnectionId, out var userId))
            {
                var user = await _userRepository.GetUserByIdAsync(userId);

                // Уведомляем админов о выходе пользователя
                if (user != null)
                {
                    await Clients.Group("admins").SendAsync("UserOffline", userId, user.Name);
                }

                // Удаляем запись о подключении
                _connectionToUser.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}