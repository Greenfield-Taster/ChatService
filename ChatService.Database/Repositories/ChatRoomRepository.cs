using System.Collections.Generic;
using System.Threading.Tasks;
using ChatService.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Database.Repositories
{
    public interface IChatRoomRepository
    {
        Task<List<ChatRoom>> GetAllRoomsAsync();
        Task<ChatRoom?> GetRoomByIdAsync(string id);
        Task<List<ChatRoom>> GetRoomsByUserIdAsync(string userId);
        Task<List<ChatRoom>> GetRoomsByAdminIdAsync(string adminId);
        Task<ChatRoom> AddRoomAsync(ChatRoom room);
        Task<ChatRoom> UpdateRoomAsync(ChatRoom room);
        Task DeleteRoomAsync(string id);
    }

    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly ChatDbContext _context;

        public ChatRoomRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatRoom>> GetAllRoomsAsync()
        {
            return await _context.ChatRooms
                .Include(r => r.Admin)
                .Include(r => r.RegularUser)
                .Include(r => r.Messages)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ChatRoom?> GetRoomByIdAsync(string id)
        {
            return await _context.ChatRooms
                .Include(r => r.Admin)
                .Include(r => r.RegularUser)
                .Include(r => r.Messages)
                    .ThenInclude(m => m.Sender)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<ChatRoom>> GetRoomsByUserIdAsync(string userId)
        {
            return await _context.ChatRooms
                .Include(r => r.Admin)
                .Include(r => r.RegularUser)
                .Include(r => r.Messages)
                .Where(r => r.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<ChatRoom>> GetRoomsByAdminIdAsync(string adminId)
        {
            return await _context.ChatRooms
                .Include(r => r.Admin)
                .Include(r => r.RegularUser)
                .Include(r => r.Messages)
                .Where(r => r.AdminId == adminId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ChatRoom> AddRoomAsync(ChatRoom room)
        {
            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task<ChatRoom> UpdateRoomAsync(ChatRoom room)
        {
            _context.Entry(room).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task DeleteRoomAsync(string id)
        {
            var room = await _context.ChatRooms.FindAsync(id);
            if (room != null)
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.ChatRoomId == id)
                    .ToListAsync();

                _context.ChatMessages.RemoveRange(messages);
                _context.ChatRooms.Remove(room);
                await _context.SaveChangesAsync();
            }
        }
    }
}