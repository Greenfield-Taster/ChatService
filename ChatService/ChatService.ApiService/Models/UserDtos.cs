using System.ComponentModel.DataAnnotations;

namespace ChatService.ApiService.Models
{ 
    public class CreateUserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; } 
        public string Role { get; set; } = "user"; 
    }
     
    public class UpdateUserDto
    { 
        public string Email { get; set; }

        public string Name { get; set; }

        public string Nickname { get; set; }

        public string Role { get; set; }
    }
     
    public class UserAuthDto
    { 
        public string Id { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public string Nickname { get; set; }

        public string Role { get; set; }
    }
     
    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public string Role { get; set; }
    }
}