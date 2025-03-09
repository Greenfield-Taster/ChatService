using ChatService.Database;
using ChatService.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Database.Repositories;

public interface IChatRepository
{
	Task<IEnumerable<ChatMessage>> GetMessagesAsync(Guid roomId);
	Task StoreMessageAsync(ChatMessage message);

	Task<IEnumerable<ChatUser>> GetUsersInRoomAsync(Guid roomId);

	Task CreateRoomAsync(ChatRoom room);
	Task DeleteRoomAsync(Guid roomId);
}

public class ChatRepository(ChatDbContext chatDbContext) : IChatRepository
{
	public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(Guid roomId)
	{
		return await chatDbContext.ChatMessages
			.Where(message => message.RoomId == roomId)
			.ToListAsync();
	}

	public async Task StoreMessageAsync(ChatMessage message)
	{
		await chatDbContext.ChatMessages.AddAsync(message);
		await chatDbContext.SaveChangesAsync();
	}

	public async Task<IEnumerable<ChatUser>> GetUsersInRoomAsync(Guid roomId)
	{
		throw new NotImplementedException();
	}

	public async Task CreateRoomAsync(ChatRoom room)
	{
		await chatDbContext.ChatRooms.AddAsync(room);
		await chatDbContext.SaveChangesAsync();
	}

	public async Task DeleteRoomAsync(Guid roomId)
	{
		var room = await chatDbContext.ChatRooms.FindAsync(roomId);
		if (room != null)
		{
			chatDbContext.ChatRooms.Remove(room);
			await chatDbContext.SaveChangesAsync();
		}
	}
}