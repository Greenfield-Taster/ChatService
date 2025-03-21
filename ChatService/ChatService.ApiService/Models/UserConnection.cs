using System;

namespace ChatService.ApiService.Models;

public class UserConnection
{
    public   string UserId { get; set; }
    public string? ChatRoomId { get; set; }
    public   string ConnectionId { get; set; }
}