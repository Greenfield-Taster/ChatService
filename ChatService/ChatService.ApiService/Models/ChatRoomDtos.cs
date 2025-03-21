using System;
using System.ComponentModel.DataAnnotations;

namespace ChatService.ApiService.Models
{ 
    public class CreateChatRoomDto
    {
        public string Name { get; set; }

        [Required]
        public string AdminId { get; set; }

        [Required]
        public string UserId { get; set; }
    }
     
    public class ChatRoomDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AdminId { get; set; }
        public string AdminName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime LastMessageTimestamp { get; set; }
        public int UnreadCount { get; set; }
    }
     
    public class UpdateChatRoomDto
    {
        public string Name { get; set; }
    }
}