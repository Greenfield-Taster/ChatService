using System;

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

        // Foreign keys
        public string ChatRoomId { get; set; }
        public string SenderId { get; set; }

        // Navigation properties
        public ChatRoom ChatRoom { get; set; }
        public User Sender { get; set; }
    }
}