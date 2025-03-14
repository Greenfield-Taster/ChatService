using System;
using System.Collections.Generic;

namespace ChatService.Database.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; } 
        public string Nickname { get; set; } 
        public string Role { get; set; } // "admin" or "user"

        // Navigation properties
        public List<ChatRoom> ChatRooms { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }
}