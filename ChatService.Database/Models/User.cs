using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatService.Database.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public string Role { get; set; }  
         
        [JsonIgnore]
        public List<ChatRoom> ChatRooms { get; set; }

        [JsonIgnore]
        public List<ChatMessage> Messages { get; set; }
    }
}