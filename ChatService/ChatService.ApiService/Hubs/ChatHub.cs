using ChatService.ApiService.Models;
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
        private readonly static Dictionary<string, UserConnection> _connections = new Dictionary<string, UserConnection>();
        private readonly IChatRepository _chatRepository;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository _userRepository;

        public ChatHub(IChatRepository chatRepository, IChatRoomRepository chatRoomRepository, IUserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _chatRoomRepository = chatRoomRepository;
            _userRepository = userRepository;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _connections.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterConnection(string userId)
        {
            var connectionId = Context.ConnectionId;

            // Ensure user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new HubException($"User with ID {userId} not found");
            }

            // Register connection
            _connections[connectionId] = new UserConnection
            {
                UserId = userId,
                ConnectionId = connectionId
            };

            await Task.CompletedTask;
        }

        public async Task JoinChatRoom(string userId, string chatRoomId)
        {
            var connectionId = Context.ConnectionId;

            // Ensure user and chat room exist
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new HubException($"User with ID {userId} not found");
            }

            // Check if the user has access to this chat room
            var chatRoom = await _chatRoomRepository.GetByIdAsync(chatRoomId);
            if (chatRoom == null)
            {
                throw new HubException($"Chat room with ID {chatRoomId} not found");
            }

            if (chatRoom.UserId != userId && chatRoom.AdminId != userId)
            {
                throw new HubException("You don't have access to this chat room");
            }

            // Add user to the SignalR group for this chat room
            await Groups.AddToGroupAsync(connectionId, chatRoomId.ToString());

            // Update connection information
            if (_connections.ContainsKey(connectionId))
            {
                _connections[connectionId].ChatRoomId = chatRoomId;
            }
            else
            {
                _connections[connectionId] = new UserConnection
                {
                    UserId = userId,
                    ChatRoomId = chatRoomId,
                    ConnectionId = connectionId
                };
            }

            // Get recent messages
            var messages = await _chatRepository.GetMessagesByChatRoomIdAsync(chatRoomId, 50, 0);
            var messageDtos = new List<ChatMessageDto>();

            foreach (var m in messages)
            {
                var sender = await _userRepository.GetByIdAsync(m.SenderId);

                if (sender != null)
                {
                    messageDtos.Add(new ChatMessageDto
                    {
                        Id = m.Id,
                        Message = m.Message,
                        Timestamp = m.Timestamp,
                        SenderId = m.SenderId,
                        SenderName = sender.Name,
                        SenderRole = sender.Role,
                        Status = m.Status.ToString()
                    });
                }
                else
                {
                    // Handle messages from users that no longer exist
                    messageDtos.Add(new ChatMessageDto
                    {
                        Id = m.Id,
                        Message = m.Message,
                        Timestamp = m.Timestamp,
                        SenderId = m.SenderId,
                        SenderName = "Unknown User",
                        SenderRole = "unknown",
                        Status = m.Status.ToString()
                    });
                }
            }

            await Clients.Caller.SendAsync("ReceiveRecentMessages", messageDtos);
        }

        public async Task SendMessage(string userId, string chatRoomId, string message)
        {
            var connectionId = Context.ConnectionId;

            // Ensure user and chat room exist
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new HubException($"User with ID {userId} not found");
            }

            // Check if the user has access to this chat room
            var chatRoom = await _chatRoomRepository.GetByIdAsync(chatRoomId);
            if (chatRoom == null)
            {
                throw new HubException($"Chat room with ID {chatRoomId} not found");
            }

            if (chatRoom.UserId != userId && chatRoom.AdminId != userId)
            {
                throw new HubException("You don't have access to this chat room");
            }

            // Create and save the message
            var chatMessage = new ChatMessage
            {
                Message = message,
                ChatRoomId = chatRoomId,
                SenderId = userId
            };

            await _chatRepository.SaveMessageAsync(chatMessage);

            // Notify clients in the chat room
            var messageDto = new ChatMessageDto
            {
                Id = chatMessage.Id,
                Message = chatMessage.Message,
                Timestamp = chatMessage.Timestamp,
                SenderId = chatMessage.SenderId,
                SenderName = user.Name,
                SenderRole = user.Role,
                Status = chatMessage.Status.ToString()
            };

            await Clients.Group(chatRoomId.ToString()).SendAsync("ReceiveMessage", messageDto);
        }

        public async Task UpdateMessageStatus(string messageId, string status)
        {
            // Parse the status string to enum
            if (!Enum.TryParse<MessageStatus>(status, true, out var messageStatus))
            {
                throw new HubException($"Invalid message status: {status}");
            }

            // Update the message status in the database
            var message = await _chatRepository.UpdateMessageStatusAsync(messageId, messageStatus);
            if (message == null)
            {
                throw new HubException($"Message with ID {messageId} not found");
            }

            // Notify clients in the chat room
            await Clients.Group(message.ChatRoomId.ToString()).SendAsync("MessageStatusUpdated", messageId, status);
        }

        public async Task LoadMoreMessages(string userId, string chatRoomId, int offset)
        {
            // Ensure user and chat room exist
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new HubException($"User with ID {userId} not found");
            }

            // Check if the user has access to this chat room
            var chatRoom = await _chatRoomRepository.GetByIdAsync(chatRoomId);
            if (chatRoom == null)
            {
                throw new HubException($"Chat room with ID {chatRoomId} not found");
            }

            if (chatRoom.UserId != userId && chatRoom.AdminId != userId)
            {
                throw new HubException("You don't have access to this chat room");
            }

            // Get messages with pagination
            var messages = await _chatRepository.GetMessagesByChatRoomIdAsync(chatRoomId, 50, offset);
            var messageDtos = new List<ChatMessageDto>();

            foreach (var m in messages)
            {
                var sender = await _userRepository.GetByIdAsync(m.SenderId);

                if (sender != null)
                {
                    messageDtos.Add(new ChatMessageDto
                    {
                        Id = m.Id,
                        Message = m.Message,
                        Timestamp = m.Timestamp,
                        SenderId = m.SenderId,
                        SenderName = sender.Name,
                        SenderRole = sender.Role,
                        Status = m.Status.ToString()
                    });
                }
                else
                {
                    // Handle messages from users that no longer exist
                    messageDtos.Add(new ChatMessageDto
                    {
                        Id = m.Id,
                        Message = m.Message,
                        Timestamp = m.Timestamp,
                        SenderId = m.SenderId,
                        SenderName = "Unknown User",
                        SenderRole = "unknown",
                        Status = m.Status.ToString()
                    });
                }
            }

            await Clients.Caller.SendAsync("ReceiveMoreMessages", messageDtos);
        }
    }
}