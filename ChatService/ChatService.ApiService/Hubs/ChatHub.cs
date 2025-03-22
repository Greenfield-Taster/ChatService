using ChatService.Database.Models;
using ChatService.Database.Repositories;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatService.ApiService.Models;  

namespace ChatService.ApiService.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatRepository _chatRepository;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository _userRepository;
         
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
         
        public async Task ConnectUser(string userId)
        {
            try
            { 
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }
                 
                _connectionToUser[Context.ConnectionId] = userId;
                 
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                 
                if (user.Role == "admin")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
                }
                 
                var rooms = await _chatRoomRepository.GetRoomsByUserIdAsync(userId);
                foreach (var room in rooms)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{room.Id}");
                }
                 
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Nickname = user.Nickname,
                    Role = user.Role
                };

                await Clients.Group("admins").SendAsync("UserOnline", userDto);
                 
                if (user.Role == "admin")
                {
                    var allRooms = await _chatRoomRepository.GetAllRoomsAsync();
                    // Преобразуем в DTO перед отправкой
                    var roomDtos = allRooms.Select(r => new ChatRoomDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        CreatedAt = r.CreatedAt,
                        Admin = r.Admin != null ? new UserDto
                        {
                            Id = r.AdminId,
                            Email = r.Admin.Email,
                            Name = r.Admin.Name,
                            Nickname = r.Admin.Nickname,
                            Role = r.Admin.Role
                        } : null,
                        User = r.RegularUser != null ? new UserDto
                        {
                            Id = r.UserId,
                            Email = r.RegularUser.Email,
                            Name = r.RegularUser.Name,
                            Nickname = r.RegularUser.Nickname,
                            Role = r.RegularUser.Role
                        } : null,
                        LastMessageTimestamp = r.Messages.OrderByDescending(m => m.Timestamp)
                                              .FirstOrDefault()?.Timestamp ?? r.CreatedAt,
                        UnreadCount = r.Messages.Count(m => m.Status != MessageStatus.Read)
                    }).ToList();

                    await Clients.Caller.SendAsync("ReceiveAllRooms", roomDtos);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }
         
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
                 
                var sender = await _userRepository.GetUserByIdAsync(senderId);
                if (sender.Role != "admin" && room.UserId != senderId)
                {
                    throw new Exception("Not authorized to send messages to this room");
                }
                 
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
                 
                var senderDto = new UserDto
                {
                    Id = sender.Id,
                    Email = sender.Email,
                    Name = sender.Name,
                    Nickname = sender.Nickname,
                    Role = sender.Role
                };
                 
                await Clients.Group($"room_{roomId}").SendAsync("ReceiveMessage", new
                {
                    Id = chatMessage.Id,
                    Message = chatMessage.Message,
                    Timestamp = chatMessage.Timestamp,
                    Status = chatMessage.Status.ToString(),
                    RoomId = chatMessage.ChatRoomId,
                    Sender = senderDto
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }
         
        public async Task CreateRoom(string userId)
        {
            try
            {
                if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var currentUserId))
                {
                    throw new Exception("User not authenticated");
                }
                 
                var currentUser = await _userRepository.GetUserByIdAsync(currentUserId);
                if (currentUser.Role != "admin" && currentUserId != userId)
                {
                    throw new Exception("Not authorized to create room for another user");
                }
                 
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }
                 
                var existingRoom = await _chatRoomRepository.GetRoomsByUserIdAsync(userId);
                if (existingRoom.Any())
                { 
                    var existingRoomId = existingRoom.First().Id;
                    await Clients.Caller.SendAsync("RoomCreated", existingRoomId);
                    return;
                }
                 
                string adminId;
                User adminUser;

                if (currentUser.Role == "admin")
                {
                    adminId = currentUserId;
                    adminUser = currentUser;
                }
                else
                {
                    var admins = await _userRepository.GetAdminUsersAsync();
                    if (!admins.Any())
                    {
                        throw new Exception("No admin users found");
                    }
                    adminUser = admins.First();
                    adminId = adminUser.Id;
                }
                 
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
                 
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
                 
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Nickname = user.Nickname,
                    Role = user.Role
                };

                var adminDto = new UserDto
                {
                    Id = adminUser.Id,
                    Email = adminUser.Email,
                    Name = adminUser.Name,
                    Nickname = adminUser.Nickname,
                    Role = adminUser.Role
                };
                 
                var roomDto = new ChatRoomDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    CreatedAt = room.CreatedAt,
                    Admin = adminDto,
                    User = userDto,
                    LastMessageTimestamp = room.CreatedAt,
                    UnreadCount = 0
                };
                 
                await Clients.Group("admins").SendAsync("NewRoomCreated", roomDto);
                 
                await Clients.Caller.SendAsync("RoomCreated", roomId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }
         
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
                 
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user.Role != "admin" && room.UserId != userId)
                {
                    throw new Exception("Not authorized to join this room");
                }
                 
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
                 
                var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId);
                 
                var messageList = messages.Select(m => new
                {
                    Id = m.Id,
                    Message = m.Message,
                    Timestamp = m.Timestamp,
                    Status = m.Status.ToString(),
                    RoomId = m.ChatRoomId,
                    Sender = new UserDto
                    {
                        Id = m.SenderId,
                        Email = m.Sender.Email,
                        Name = m.Sender.Name,
                        Nickname = m.Sender.Nickname,
                        Role = m.Sender.Role
                    }
                }).ToList();
                 
                await Clients.Caller.SendAsync("ReceiveMessageHistory", roomId, messageList);
                 
                await UpdateMessagesStatusAsync(roomId, userId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }
         
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
        }
         
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
                 
                var roomsList = rooms.Select(r => new ChatRoomDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    CreatedAt = r.CreatedAt,
                    Admin = r.Admin != null ? new UserDto
                    {
                        Id = r.AdminId,
                        Email = r.Admin.Email,
                        Name = r.Admin.Name,
                        Nickname = r.Admin.Nickname,
                        Role = r.Admin.Role
                    } : null,
                    User = r.RegularUser != null ? new UserDto
                    {
                        Id = r.UserId,
                        Email = r.RegularUser.Email,
                        Name = r.RegularUser.Name,
                        Nickname = r.RegularUser.Nickname,
                        Role = r.RegularUser.Role
                    } : null,
                    LastMessageTimestamp = r.Messages.OrderByDescending(m => m.Timestamp)
                                          .FirstOrDefault()?.Timestamp ?? r.CreatedAt,
                    UnreadCount = r.Messages.Count(m => m.Status != MessageStatus.Read && m.SenderId != userId)
                }).ToList();

                await Clients.Caller.SendAsync("ReceiveAllChats", roomsList);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }
         
        private async Task UpdateMessagesStatusAsync(string roomId, string userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId);
                 
                var messagesToUpdate = messages.Where(m => m.SenderId != userId && m.Status != MessageStatus.Read).ToList();

                foreach (var message in messagesToUpdate)
                {
                    message.Status = MessageStatus.Read;
                    await _chatRepository.UpdateMessageAsync(message);
                }
                 
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
         
        public async Task SendTypingStatus(string roomId, bool isTyping)
        {
            try
            {
                if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var userId))
                {
                    throw new Exception("User not authenticated");
                }

                var user = await _userRepository.GetUserByIdAsync(userId);
                 
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Nickname = user.Nickname,
                    Role = user.Role
                };

                await Clients.Group($"room_{roomId}").SendAsync("UserTyping", roomId, userDto, isTyping);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        } 
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_connectionToUser.TryGetValue(Context.ConnectionId, out var userId))
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                 
                if (user != null)
                {
                    var userDto = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Nickname = user.Nickname,
                        Role = user.Role
                    };

                    await Clients.Group("admins").SendAsync("UserOffline", userDto);
                }
                 
                _connectionToUser.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}