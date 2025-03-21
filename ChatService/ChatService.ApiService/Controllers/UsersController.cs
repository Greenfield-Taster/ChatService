using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatService.ApiService.Models;
using ChatService.Database.Models;
using ChatService.Database.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
         
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userRepository.GetAllUsersAsync();
            var userDtos = users.Select(MapToUserDto).ToList();
            return Ok(userDtos);
        }
         
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(MapToUserDto(user));
        }
         
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        { 
            var existingUser = await _userRepository.GetUserByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                return BadRequest("User with this email already exists");
            }

            var user = new User
            {
                Id = createUserDto.Id,
                Email = createUserDto.Email,
                Name = createUserDto.Name,
                Nickname = createUserDto.Nickname ,
                Role = createUserDto.Role,
                ChatRooms = new List<ChatRoom>(),
                Messages = new List<ChatMessage>()
            };

            await _userRepository.AddUserAsync(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, MapToUserDto(user));
        }
         
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto updateUserDto)
        {
            var existingUser = await _userRepository.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updateUserDto.Email))
            {
                existingUser.Email = updateUserDto.Email;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Name))
            {
                existingUser.Name = updateUserDto.Name;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Nickname))
            {
                existingUser.Nickname = updateUserDto.Nickname;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Role))
            {
                existingUser.Role = updateUserDto.Role;
            }

            await _userRepository.UpdateUserAsync(existingUser);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var existingUser = await _userRepository.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            await _userRepository.DeleteUserAsync(id);

            return NoContent();
        }
         
        [HttpPost("auth")]
        public async Task<ActionResult<UserDto>> AuthenticateUser([FromBody] UserAuthDto authDto)
        { 
            var user = await _userRepository.GetUserByEmailAsync(authDto.Email);
             
            if (user == null)
            {
                user = new User
                {
                    Id = authDto.Id,
                    Email = authDto.Email,
                    Name = authDto.Name,
                    Nickname = authDto.Nickname,   
                    Role = authDto.Role,  
                    ChatRooms = new List<ChatRoom>(),
                    Messages = new List<ChatMessage>()
                };

                await _userRepository.AddUserAsync(user);
            }

            return Ok(MapToUserDto(user));
        }
         
        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Nickname = user.Nickname,
                Role = user.Role
            };
        }
    }
}