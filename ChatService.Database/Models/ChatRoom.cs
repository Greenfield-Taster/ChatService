using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatService.Database.Models
{
    public class ChatRoom
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } 
        public string AdminId { get; set; }
        public string UserId { get; set; }
         
        [JsonIgnore]
        public User Admin { get; set; }

        [JsonIgnore]
        public User RegularUser { get; set; }

        [JsonIgnore]
        public List<ChatMessage> Messages { get; set; }
    }
}