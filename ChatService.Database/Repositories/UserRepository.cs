using ChatService.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Database.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(string id);
        Task<User> GetByEmailAsync(string email);
        Task<List<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(User user);
        Task<bool> UserExistsAsync(string id);
    }

    public class UserRepository : IUserRepository
    {
        private readonly ChatDbContext _context;

        public UserRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByIdAsync(string id)
        {   
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Check if the user already exists by Id
            var existingUser = await _context.Users.FindAsync(user.Id);

            if (existingUser != null)
            {
                // Update the existing user with new information
                _context.Entry(existingUser).CurrentValues.SetValues(user);
            }
            else
            {
                // Create a new user
                await _context.Users.AddAsync(user);
            }

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UserExistsAsync(string id)
        {   
            return await _context.Users.AnyAsync(u => u.Id == id);
        }
    }
}