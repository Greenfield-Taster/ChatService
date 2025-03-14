using ChatService.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Database.Repositories
{
    public interface IChatRoomRepository
    {
        Task<ChatRoom> GetByIdAsync(string id);
        Task<List<ChatRoom>> GetByAdminIdAsync(string adminId);
        Task<List<ChatRoom>> GetByUserIdAsync(string userId);
        Task<ChatRoom> CreateChatRoomAsync(ChatRoom chatRoom);
        Task<bool> ChatRoomExistsAsync(string adminId, string userId);
    }

    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly ChatDbContext _context;

        public ChatRoomRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<ChatRoom> GetByIdAsync(string id)
        {
            return await _context.ChatRooms
                .Include(r => r.Admin)
                .Include(r => r.RegularUser)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<ChatRoom>> GetByAdminIdAsync(string adminId)
        {
            return await _context.ChatRooms
                .Include(r => r.RegularUser)
                .Where(r => r.AdminId == adminId)
                .ToListAsync();
        }

        public async Task<List<ChatRoom>> GetByUserIdAsync(string userId)
        {
            return await _context.ChatRooms
                .Include(r => r.Admin)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<ChatRoom> CreateChatRoomAsync(ChatRoom chatRoom)
        {
            chatRoom.Id = Guid.NewGuid().ToString();
            chatRoom.CreatedAt = DateTime.UtcNow;

            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();
            return chatRoom;
        }

        public async Task<bool> ChatRoomExistsAsync(string adminId, string userId)
        {
            return await _context.ChatRooms
                .AnyAsync(r => r.AdminId == adminId && r.UserId == userId);
        }
    }

}
