using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatService.ApiService.Models;
using ChatService.Database.Models;
using ChatService.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ChatService.ApiService.Hubs;

namespace ChatService.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatMessagesController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatMessagesController(
            IChatRepository chatRepository,
            IChatRoomRepository chatRoomRepository,
            IUserRepository userRepository,
            IHubContext<ChatHub> hubContext)
        {
            _chatRepository = chatRepository;
            _chatRoomRepository = chatRoomRepository;
            _userRepository = userRepository;
            _hubContext = hubContext;
        }
         
        [HttpGet("room/{roomId}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetRoomMessages(string roomId)
        {
            var room = await _chatRoomRepository.GetRoomByIdAsync(roomId);
            if (room == null)
            {
                return NotFound("Chat room not found");
            }

            var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId);

            var messageDtos = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                Message = m.Message,
                Timestamp = m.Timestamp,
                Status = m.Status.ToString(),
                SenderId = m.SenderId,
                SenderName = m.Sender?.Name ?? "Unknown",
                RoomId = m.ChatRoomId
            }).OrderBy(m => m.Timestamp).ToList();

            return Ok(messageDtos);
        }
         
        [HttpPost]
        public async Task<ActionResult<MessageDto>> SendMessage(CreateMessageDto createMessageDto)
        { 
            var room = await _chatRoomRepository.GetRoomByIdAsync(createMessageDto.RoomId);
            if (room == null)
            {
                return NotFound("Chat room not found");
            }
             
            var sender = await _userRepository.GetUserByIdAsync(createMessageDto.SenderId);
            if (sender == null)
            {
                return NotFound("Sender not found");
            }
             
            if (sender.Role != "admin" && room.UserId != createMessageDto.SenderId)
            {
                return Forbid("Not authorized to send messages to this room");
            }

            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Message = createMessageDto.Message,
                Timestamp = DateTime.UtcNow,
                Status = MessageStatus.Sent,
                ChatRoomId = createMessageDto.RoomId,
                SenderId = createMessageDto.SenderId
            };

            await _chatRepository.AddMessageAsync(chatMessage);
             
            await _hubContext.Clients.Group($"room_{createMessageDto.RoomId}").SendAsync("ReceiveMessage", new
            {
                Id = chatMessage.Id,
                Message = chatMessage.Message,
                Timestamp = chatMessage.Timestamp,
                Status = chatMessage.Status.ToString(),
                RoomId = chatMessage.ChatRoomId,
                SenderId = chatMessage.SenderId,
                SenderName = sender.Name
            });

            var resultDto = new MessageDto
            {
                Id = chatMessage.Id,
                Message = chatMessage.Message,
                Timestamp = chatMessage.Timestamp,
                Status = chatMessage.Status.ToString(),
                SenderId = chatMessage.SenderId,
                SenderName = sender.Name,
                RoomId = chatMessage.ChatRoomId
            };

            return CreatedAtAction(nameof(GetMessage), new { id = chatMessage.Id }, resultDto);
        }
         
        [HttpGet("{id}")]
        public async Task<ActionResult<MessageDto>> GetMessage(string id)
        {
            var message = await _chatRepository.GetMessageByIdAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            var messageDto = new MessageDto
            {
                Id = message.Id,
                Message = message.Message,
                Timestamp = message.Timestamp,
                Status = message.Status.ToString(),
                SenderId = message.SenderId,
                SenderName = message.Sender?.Name ?? "Unknown",
                RoomId = message.ChatRoomId
            };

            return Ok(messageDto);
        }
         
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateMessageStatus(string id, UpdateStatusDto updateStatusDto)
        {
            var message = await _chatRepository.GetMessageByIdAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            if (!Enum.TryParse<MessageStatus>(updateStatusDto.Status, out var newStatus))
            {
                return BadRequest("Invalid status value");
            }

            message.Status = newStatus;
            await _chatRepository.UpdateMessageAsync(message);
             
            await _hubContext.Clients.Group($"room_{message.ChatRoomId}").SendAsync("MessageStatusUpdated",
                id,
                newStatus.ToString(),
                updateStatusDto.UserId);

            return NoContent();
        }
         
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(string id)
        {
            var message = await _chatRepository.GetMessageByIdAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            await _chatRepository.DeleteMessageAsync(id);
             
            await _hubContext.Clients.Group($"room_{message.ChatRoomId}").SendAsync("MessageDeleted", id);

            return NoContent();
        }
         
        [HttpPut("room/{roomId}/status")]
        public async Task<IActionResult> UpdateRoomMessagesStatus(string roomId, UpdateStatusDto updateStatusDto)
        {
            var room = await _chatRoomRepository.GetRoomByIdAsync(roomId);
            if (room == null)
            {
                return NotFound("Chat room not found");
            }

            if (!Enum.TryParse<MessageStatus>(updateStatusDto.Status, out var newStatus))
            {
                return BadRequest("Invalid status value");
            }

            var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId);
             
            var messagesToUpdate = messages.Where(m => m.SenderId != updateStatusDto.UserId && m.Status != newStatus).ToList();

            foreach (var message in messagesToUpdate)
            {
                message.Status = newStatus;
                await _chatRepository.UpdateMessageAsync(message);
            }
             
            if (messagesToUpdate.Any())
            {
                await _hubContext.Clients.Group($"room_{roomId}").SendAsync("MessagesStatusUpdated",
                    roomId,
                    messagesToUpdate.Select(m => m.Id).ToList(),
                    newStatus.ToString(),
                    updateStatusDto.UserId);
            }

            return NoContent();
        }
    }
}