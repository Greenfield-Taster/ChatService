using ChatService.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Database;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
	public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
	public DbSet<ChatUser> ChatUsers => Set<ChatUser>();
	public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
}