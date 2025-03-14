using System;

namespace ChatService.ApiService.Models
{
    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; } 
        public string Nickname { get; set; }
        public string Role { get; set; }
    }

    public class UserInfoRequest
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; } 
        public string Nickname { get; set; }
        public string Role { get; set; } 
    }
}
