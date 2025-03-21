using System;
using System.Text.Json.Serialization;

namespace ChatService.Database.Models
{
    public enum MessageStatus
    {
        Sent,
        Delivered,
        Read
    }

    public class ChatMessage
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public MessageStatus Status { get; set; } 
        public string ChatRoomId { get; set; }
        public string SenderId { get; set; }
         
        [JsonIgnore]
        public ChatRoom ChatRoom { get; set; }

        [JsonIgnore]
        public User Sender { get; set; }
    }
}