using System;
using System.Collections.Generic;

namespace ChatService.ApiService.Models
{
    public class ChatRoomDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto Admin { get; set; }
        public UserDto User { get; set; }
        public ChatMessageDto LastMessage { get; set; }
    }

    public class CreateChatRoomRequest
    {
        public string AdminId { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
    }
}
