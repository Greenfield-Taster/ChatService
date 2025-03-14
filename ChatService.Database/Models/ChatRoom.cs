using System;
using System.Collections.Generic;

namespace ChatService.Database.Models
{
    public class ChatRoom
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        // Foreign keys
        public string AdminId { get; set; }
        public string UserId { get; set; }

        // Navigation properties
        public User Admin { get; set; }
        public User RegularUser { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }
}