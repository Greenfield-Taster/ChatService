using ChatService.Database.Models;
using System;

namespace ChatService.ApiService.Models
{
    public class ChatMessageDto
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderRole { get; set; }
        public string Status { get; set; }
    }

    public class SendMessageRequest
    {
        public string ChatRoomId { get; set; }
        public string SenderId { get; set; }
        public string Message { get; set; }
    }

    public class GetMessagesRequest
    {
        public string ChatRoomId { get; set; }
        public int Limit { get; set; } = 50;
        public int Offset { get; set; } = 0;
    }

    public class UpdateMessageStatusRequest
    {
        public string MessageId { get; set; }
        public string Status { get; set; }
    }
}
