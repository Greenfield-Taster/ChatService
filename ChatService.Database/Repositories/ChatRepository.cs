using System.Collections.Generic;
using System.Threading.Tasks;
using ChatService.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Database.Repositories
{
    public interface IChatRepository
    {
        Task<List<ChatMessage>> GetAllMessagesAsync();
        Task<ChatMessage?> GetMessageByIdAsync(string id);
        Task<List<ChatMessage>> GetMessagesByRoomIdAsync(string roomId);
        Task<List<ChatMessage>> GetPaginatedMessagesByRoomIdAsync(string roomId, int pageNumber, int pageSize);
        Task<List<ChatMessage>> GetUnreadMessagesAsync(string roomId, string userId);
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
        Task<ChatMessage> UpdateMessageAsync(ChatMessage message);
        Task DeleteMessageAsync(string id);
    }

    public class ChatRepository : IChatRepository
    {
        private readonly ChatDbContext _context;

        public ChatRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatMessage>> GetAllMessagesAsync()
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.ChatRoom)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ChatMessage?> GetMessageByIdAsync(string id)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.ChatRoom)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<ChatMessage>> GetMessagesByRoomIdAsync(string roomId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ChatRoomId == roomId)
                .OrderBy(m => m.Timestamp)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetPaginatedMessagesByRoomIdAsync(string roomId, int pageNumber, int pageSize)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ChatRoomId == roomId)
                .OrderByDescending(m => m.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetUnreadMessagesAsync(string roomId, string userId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ChatRoomId == roomId &&
                            m.SenderId != userId &&
                            m.Status != MessageStatus.Read)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<ChatMessage> UpdateMessageAsync(ChatMessage message)
        {
            _context.Entry(message).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task DeleteMessageAsync(string id)
        {
            var message = await _context.ChatMessages.FindAsync(id);
            if (message != null)
            {
                _context.ChatMessages.Remove(message);
                await _context.SaveChangesAsync();
            }
        }
    }
}