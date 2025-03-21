using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatService.ApiService.Models;
using ChatService.Database.Models;
using ChatService.Database.Repositories;
using Microsoft.AspNetCore.Mvc; 

namespace ChatService.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatRoomsController : ControllerBase
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository _userRepository;

        public ChatRoomsController(
            IChatRoomRepository chatRoomRepository,
            IUserRepository userRepository)
        {
            _chatRoomRepository = chatRoomRepository;
            _userRepository = userRepository;
        }

        // Получение всех чат-комнат
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChatRoomDto>>> GetChatRooms()
        {
            var rooms = await _chatRoomRepository.GetAllRoomsAsync();

            var roomDtos = rooms.Select(r => new ChatRoomDto
            {
                Id = r.Id,
                Name = r.Name,
                CreatedAt = r.CreatedAt,
                AdminId = r.AdminId,
                AdminName = r.Admin?.Name,
                UserId = r.UserId,
                UserName = r.RegularUser?.Name,
                LastMessageTimestamp = r.Messages.OrderByDescending(m => m.Timestamp)
                                       .FirstOrDefault()?.Timestamp ?? r.CreatedAt,
                UnreadCount = r.Messages.Count(m => m.Status != MessageStatus.Read)
            }).ToList();

            return Ok(roomDtos);
        }
         
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ChatRoomDto>>> GetUserChatRooms(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var rooms = await _chatRoomRepository.GetRoomsByUserIdAsync(userId);
             
            if (user.Role == "admin")
            {
                rooms = await _chatRoomRepository.GetAllRoomsAsync();
            }

            var roomDtos = rooms.Select(r => new ChatRoomDto
            {
                Id = r.Id,
                Name = r.Name,
                CreatedAt = r.CreatedAt,
                AdminId = r.AdminId,
                AdminName = r.Admin?.Name,
                UserId = r.UserId,
                UserName = r.RegularUser?.Name,
                LastMessageTimestamp = r.Messages.OrderByDescending(m => m.Timestamp)
                                      .FirstOrDefault()?.Timestamp ?? r.CreatedAt,
                UnreadCount = r.Messages.Count(m => m.Status != MessageStatus.Read && m.SenderId != userId)
            }).ToList();

            return Ok(roomDtos);
        }
         
        [HttpGet("{id}")]
        public async Task<ActionResult<ChatRoomDto>> GetChatRoom(string id)
        {
            var room = await _chatRoomRepository.GetRoomByIdAsync(id);

            if (room == null)
            {
                return NotFound();
            }

            var roomDto = new ChatRoomDto
            {
                Id = room.Id,
                Name = room.Name,
                CreatedAt = room.CreatedAt,
                AdminId = room.AdminId,
                AdminName = room.Admin?.Name ,
                UserId = room.UserId,
                UserName = room.RegularUser?.Name ,
                LastMessageTimestamp = room.Messages.OrderByDescending(m => m.Timestamp)
                                       .FirstOrDefault()?.Timestamp ?? room.CreatedAt,
                UnreadCount = room.Messages.Count(m => m.Status != MessageStatus.Read)
            };

            return Ok(roomDto);
        }
         
        [HttpPost]
        public async Task<ActionResult<ChatRoom>> CreateChatRoom(CreateChatRoomDto createRoomDto)
        { 
            var user = await _userRepository.GetUserByIdAsync(createRoomDto.UserId);
            if (user == null)
            {
                return BadRequest("User not found");
            }
             
            var admin = await _userRepository.GetUserByIdAsync(createRoomDto.AdminId);
            if (admin == null)
            {
                return BadRequest("Admin not found");
            }
             
            if (admin.Role != "admin")
            {
                return BadRequest("Specified user is not an admin");
            }
             
            var existingRooms = await _chatRoomRepository.GetRoomsByUserIdAsync(createRoomDto.UserId);
            if (existingRooms.Any(r => r.AdminId == createRoomDto.AdminId))
            {
                return BadRequest("Chat room already exists for this user and admin");
            }

            var chatRoom = new ChatRoom
            {
                Id = Guid.NewGuid().ToString(),
                Name = createRoomDto.Name ?? $"Support chat for {user.Name}",
                CreatedAt = DateTime.UtcNow,
                AdminId = createRoomDto.AdminId,
                UserId = createRoomDto.UserId,
                Messages = new List<ChatMessage>()
            };

            await _chatRoomRepository.AddRoomAsync(chatRoom);

            return CreatedAtAction(nameof(GetChatRoom), new { id = chatRoom.Id }, chatRoom);
        }
         
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatRoom(string id)
        {
            var room = await _chatRoomRepository.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            await _chatRoomRepository.DeleteRoomAsync(id);

            return NoContent();
        }
    }
}