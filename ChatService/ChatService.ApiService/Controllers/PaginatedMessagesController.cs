using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatService.Database.Models;
using ChatService.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using ChatService.ApiService.Models;

namespace ChatService.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaginatedMessagesController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository _userRepository;

        public PaginatedMessagesController(
            IChatRepository chatRepository,
            IChatRoomRepository chatRoomRepository,
            IUserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _chatRoomRepository = chatRoomRepository;
            _userRepository = userRepository;
        }
         
        [HttpGet("room/{roomId}")]
        public async Task<ActionResult<PaginatedResult<MessageDto>>> GetPaginatedMessages(
            string roomId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        { 
            var room = await _chatRoomRepository.GetRoomByIdAsync(roomId);
            if (room == null)
            {
                return NotFound("Chat room not found");
            }
             
            var messages = await _chatRepository.GetPaginatedMessagesByRoomIdAsync(roomId, page, pageSize);
             
            var totalMessages = room.Messages.Count;
             
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
             
            var result = new PaginatedResult<MessageDto>
            {
                Items = messageDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalMessages,
                TotalPages = (totalMessages + pageSize - 1) / pageSize
            };

            return Ok(result);
        }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}