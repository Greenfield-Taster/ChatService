using ChatService.ApiService.Models;
using ChatService.Database.Models;
using ChatService.Database.Repositories;
using ChatService.DataService;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.ApiService.Hubs;

public class ChatHub(SharedDb sharedDB, IChatRepository chatRepository) : Hub
{
	public async Task JoinChat(UserConnection conn)
	{
		await Clients.All.SendAsync("ReceiveMessage", "admin", $"{conn.Username} has joined");
	}

	public async Task JoinSpecificChatRoom(UserConnection conn)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, conn.ChatRoom);

		sharedDB.connections[Context.ConnectionId] = conn;

		await Clients.Group(conn.ChatRoom).SendAsync("ReceiveMessage", "admin", $"{conn.Username} has joined {conn.ChatRoom}");
	}

	public async Task SendMessage(string msg)
	{
		var chatMessage = new ChatMessage
		{
			SenderId = Guid.NewGuid(), // This should be the actual sender's ID
			RoomId = Guid.NewGuid(), // This should be the actual room's ID
			Content = msg
		};

		await chatRepository.StoreMessageAsync(chatMessage);

		if (sharedDB.connections.TryGetValue(Context.ConnectionId, out UserConnection conn))
		{
			await Clients.Group(conn.ChatRoom).SendAsync("ReceiveSpecificMessage", conn.Username, msg);
		}
	}
}