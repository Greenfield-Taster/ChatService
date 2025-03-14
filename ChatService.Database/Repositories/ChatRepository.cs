using ChatService.Database;
using ChatService.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Database.Repositories;

public interface IChatRepository
{
    Task<List<ChatMessage>> GetMessagesByChatRoomIdAsync(string chatRoomId, int limit = 50, int offset = 0);
    Task<ChatMessage> SaveMessageAsync(ChatMessage message);
    Task<ChatMessage> UpdateMessageStatusAsync(string messageId, MessageStatus status);
}

public class ChatRepository : IChatRepository
{
    private readonly ChatDbContext _context;

    public ChatRepository(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatMessage>> GetMessagesByChatRoomIdAsync(string chatRoomId, int limit = 50, int offset = 0)
    {
        return await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.ChatRoomId == chatRoomId)
            .OrderByDescending(m => m.Timestamp)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
    {
        message.Id = Guid.NewGuid().ToString();
        message.Timestamp = DateTime.UtcNow;
        message.Status = MessageStatus.Sent;

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<ChatMessage> UpdateMessageStatusAsync(string messageId, MessageStatus status)
    {
        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = status;
            await _context.SaveChangesAsync();
        }
        return message;
    }
}