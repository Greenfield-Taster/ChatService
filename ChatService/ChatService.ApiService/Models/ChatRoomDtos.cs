using System;
using System.ComponentModel.DataAnnotations;

namespace ChatService.ApiService.Models
{ 
    public class CreateChatRoomDto
    {
        public string Name { get; set; }
        public string AdminId { get; set; }
        public string UserId { get; set; }
    }
     
    public class ChatRoomDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto Admin { get; set; }
        public UserDto User { get; set; }
        public DateTime LastMessageTimestamp { get; set; }
        public int UnreadCount { get; set; }
    }
     
    public class UpdateChatRoomDto
    {
        public string Name { get; set; }
    }
}