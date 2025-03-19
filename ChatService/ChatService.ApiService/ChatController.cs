using ChatService.ApiService.Models;
using ChatService.Database.Models;
using ChatService.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatService.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository _userRepository;

        public ChatController(
            IChatRepository chatRepository,
            IChatRoomRepository chatRoomRepository,
            IUserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _chatRoomRepository = chatRoomRepository;
            _userRepository = userRepository;
        }

        [HttpGet("rooms/{userId}")]
        public async Task<ActionResult<List<ChatRoomDto>>> GetUserChatRooms(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            List<ChatRoom> chatRooms;

            if (user.Role == "admin")
            {
                // Admins can see all chat rooms they're part of
                chatRooms = await _chatRoomRepository.GetByAdminIdAsync(userId);
            }
            else
            {
                // Regular users can only see their own chat rooms
                chatRooms = await _chatRoomRepository.GetByUserIdAsync(userId);
            }

            var chatRoomDtos = new List<ChatRoomDto>();

            foreach (var room in chatRooms)
            {
                // Get the last message for each room
                var messages = await _chatRepository.GetMessagesByChatRoomIdAsync(room.Id, 1, 0);
                ChatMessageDto lastMessage = null;

                if (messages.Any())
                {
                    var msg = messages.First();
                    var sender = await _userRepository.GetByIdAsync(msg.SenderId);

                    if (sender != null)
                    {
                        lastMessage = new ChatMessageDto
                        {
                            Id = msg.Id,
                            Message = msg.Message,
                            Timestamp = msg.Timestamp,
                            SenderId = msg.SenderId,
                            SenderName = sender.Name,
                            SenderRole = sender.Role,
                            Status = msg.Status.ToString()
                        };
                    }
                    else
                    {
                        // If sender not found, provide message without sender details
                        lastMessage = new ChatMessageDto
                        {
                            Id = msg.Id,
                            Message = msg.Message,
                            Timestamp = msg.Timestamp,
                            SenderId = msg.SenderId,
                            SenderName = "Unknown User",
                            SenderRole = "unknown",
                            Status = msg.Status.ToString()
                        };
                    }
                }

                // Get admin and user details
                var admin = await _userRepository.GetByIdAsync(room.AdminId);
                var regularUser = await _userRepository.GetByIdAsync(room.UserId);

                // Skip rooms with missing users
                if (admin == null || regularUser == null)
                {
                    continue;
                }

                chatRoomDtos.Add(new ChatRoomDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    CreatedAt = room.CreatedAt,
                    Admin = new UserDto
                    {
                        Id = admin.Id,
                        Email = admin.Email,
                        Name = admin.Name,
                        Nickname = admin.Nickname,
                        Role = admin.Role
                    },
                    User = new UserDto
                    {
                        Id = regularUser.Id,
                        Email = regularUser.Email,
                        Name = regularUser.Name,
                        Nickname = regularUser.Nickname,
                        Role = regularUser.Role
                    },
                    LastMessage = lastMessage
                });
            }

            return chatRoomDtos;
        }

        [HttpGet("room/{id}")]
        public async Task<ActionResult<ChatRoomDto>> GetChatRoom(string id)
        {
            var chatRoom = await _chatRoomRepository.GetByIdAsync(id);

            if (chatRoom == null)
            {
                return NotFound("Chat room not found");
            }

            // Get admin and user details
            var admin = await _userRepository.GetByIdAsync(chatRoom.AdminId);
            var regularUser = await _userRepository.GetByIdAsync(chatRoom.UserId);

            if (admin == null || regularUser == null)
            {
                return NotFound("One or more users associated with this chat room not found");
            }

            return new ChatRoomDto
            {
                Id = chatRoom.Id,
                Name = chatRoom.Name,
                CreatedAt = chatRoom.CreatedAt,
                Admin = new UserDto
                {
                    Id = admin.Id,
                    Email = admin.Email,
                    Name = admin.Name,
                    Nickname = admin.Nickname,
                    Role = admin.Role
                },
                User = new UserDto
                {
                    Id = regularUser.Id,
                    Email = regularUser.Email,
                    Name = regularUser.Name,
                    Nickname = regularUser.Nickname,
                    Role = regularUser.Role
                }
            };
        }

        [HttpPost("room")]
        public async Task<ActionResult<ChatRoomDto>> CreateChatRoom(CreateChatRoomRequest request)
        {
            // Check if users exist
            var admin = await _userRepository.GetByIdAsync(request.AdminId);
            if (admin == null)
            {
                return NotFound($"Admin with ID {request.AdminId} not found");
            }

            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return NotFound($"User with ID {request.UserId} not found");
            }

            // Check if a chat room already exists between these users
            var chatRoomExists = await _chatRoomRepository.ChatRoomExistsAsync(request.AdminId, request.UserId);
            if (chatRoomExists)
            {
                return BadRequest("A chat room already exists between these users");
            }

            // Generate room name based on user's nickname
            string roomName = $"Chat with {user.Nickname}";

            var chatRoom = new ChatRoom
            {
                Name = roomName, // Automatically set room name
                AdminId = request.AdminId,
                UserId = request.UserId
            };

            await _chatRoomRepository.CreateChatRoomAsync(chatRoom);

            return new ChatRoomDto
            {
                Id = chatRoom.Id,
                Name = chatRoom.Name,
                CreatedAt = chatRoom.CreatedAt,
                Admin = new UserDto
                {
                    Id = admin.Id,
                    Email = admin.Email,
                    Name = admin.Name,
                    Nickname = admin.Nickname,
                    Role = admin.Role
                },
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Nickname = user.Nickname,
                    Role = user.Role
                }
            };
        }

        [HttpGet("messages/{chatRoomId}")]
        public async Task<ActionResult<List<ChatMessageDto>>> GetMessages(
            string chatRoomId,
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0)
        {
            var chatRoom = await _chatRoomRepository.GetByIdAsync(chatRoomId);

            if (chatRoom == null)
            {
                return NotFound("Chat room not found");
            }

            var messages = await _chatRepository.GetMessagesByChatRoomIdAsync(chatRoomId, limit, offset);
            var messageDtos = new List<ChatMessageDto>();

            foreach (var message in messages)
            {
                var sender = await _userRepository.GetByIdAsync(message.SenderId);

                if (sender != null)
                {
                    messageDtos.Add(new ChatMessageDto
                    {
                        Id = message.Id,
                        Message = message.Message,
                        Timestamp = message.Timestamp,
                        SenderId = message.SenderId,
                        SenderName = sender.Name,
                        SenderRole = sender.Role,
                        Status = message.Status.ToString()
                    });
                }
                else
                {
                    // Handle messages from users that no longer exist
                    messageDtos.Add(new ChatMessageDto
                    {
                        Id = message.Id,
                        Message = message.Message,
                        Timestamp = message.Timestamp,
                        SenderId = message.SenderId,
                        SenderName = "Unknown User",
                        SenderRole = "unknown",
                        Status = message.Status.ToString()
                    });
                }
            }

            return messageDtos;
        }

        [HttpPost("message")]
        public async Task<ActionResult<ChatMessageDto>> SendMessage(SendMessageRequest request)
        {
            // Check if chat room exists
            var chatRoom = await _chatRoomRepository.GetByIdAsync(request.ChatRoomId);
            if (chatRoom == null)
            {
                return NotFound($"Chat room with ID {request.ChatRoomId} not found");
            }

            // Check if sender exists
            var sender = await _userRepository.GetByIdAsync(request.SenderId);
            if (sender == null)
            {
                return NotFound($"User with ID {request.SenderId} not found");
            }

            // Check if the sender has access to this chat room
            if (chatRoom.UserId != request.SenderId && chatRoom.AdminId != request.SenderId)
            {
                return Forbid("You don't have access to this chat room");
            }

            // Create and save the message
            var message = new ChatMessage
            {
                ChatRoomId = request.ChatRoomId,
                SenderId = request.SenderId,
                Message = request.Message
            };

            await _chatRepository.SaveMessageAsync(message);

            return new ChatMessageDto
            {
                Id = message.Id,
                Message = message.Message,
                Timestamp = message.Timestamp,
                SenderId = message.SenderId,
                SenderName = sender.Name,
                SenderRole = sender.Role,
                Status = message.Status.ToString()
            };
        }

        [HttpPost("message/status")]
        public async Task<ActionResult> UpdateMessageStatus(UpdateMessageStatusRequest request)
        {
            // Parse the status string to enum
            if (!Enum.TryParse<MessageStatus>(request.Status, true, out var messageStatus))
            {
                return BadRequest($"Invalid message status: {request.Status}");
            }

            // Update the message status in the database
            var message = await _chatRepository.UpdateMessageStatusAsync(request.MessageId, messageStatus);
            if (message == null)
            {
                return NotFound($"Message with ID {request.MessageId} not found");
            }

            return Ok();
        }

        [HttpPost("user")]
        public async Task<ActionResult<UserDto>> RegisterOrUpdateUser(UserInfoRequest request)
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByIdAsync(request.Id);

            User user;
            if (existingUser != null)
            {
                // Update existing user
                // Update existing user's properties
                existingUser.Email = request.Email;
                existingUser.Name = request.Name;
                existingUser.Nickname = request.Nickname;
                existingUser.Role = request.Role;
                user = existingUser;

                // Create the user again to update it (since there's no UpdateUserAsync method)
                await _userRepository.CreateUserAsync(user);
            }
            else
            {
                // Create new user
                user = new User
                {
                    Id = request.Id,
                    Email = request.Email,
                    Name = request.Name,
                    Nickname = request.Nickname,
                    Role = request.Role
                };
                await _userRepository.CreateUserAsync(user);
            }

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Nickname = user.Nickname,
                Role = user.Role
            };
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Nickname = u.Nickname,
                Role = u.Role
            }).ToList();
        }
    }
}