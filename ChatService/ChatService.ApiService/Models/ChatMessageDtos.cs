using System;
using System.ComponentModel.DataAnnotations;

namespace ChatService.ApiService.Models
{ 
    public class CreateMessageDto
    {
        [Required]
        public string Message { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string RoomId { get; set; }
    }
     
    public class MessageDto
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string RoomId { get; set; }
    }
      
    public class UpdateStatusDto
    {
        [Required]
        public string Status { get; set; }

        [Required]
        public string UserId { get; set; }
    }
}