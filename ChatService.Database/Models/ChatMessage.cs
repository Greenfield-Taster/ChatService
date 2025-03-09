namespace ChatService.Database.Models;

public class ChatMessage
{
	public Guid Id { get; set; }
	public Guid SenderId { get; set; }
	public Guid RoomId { get; set; }
	public string Content { get; set; } = string.Empty;
}

public class ChatUser
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
}

public class ChatRoom
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
}