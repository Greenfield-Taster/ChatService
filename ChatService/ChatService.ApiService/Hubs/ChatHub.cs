using ChatService.ApiService.Models;
using ChatService.DataService;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.ApiService.Hubs;

public class ChatHub(SharedDb sharedDB) : Hub
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
		if (sharedDB.connections.TryGetValue(Context.ConnectionId, out UserConnection conn))
		{
			await Clients.Group(conn.ChatRoom).SendAsync("ReceiveSpecificMessage", conn.Username, msg);
		}
	}
}