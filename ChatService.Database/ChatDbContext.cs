using ChatService.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Database;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
	public DbSet<User> Users => Set<User>();
	public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
	public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure User
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configure ChatRoom
        modelBuilder.Entity<ChatRoom>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<ChatRoom>()
            .HasOne(r => r.Admin)
            .WithMany()
            .HasForeignKey(r => r.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatRoom>()
            .HasOne(r => r.RegularUser)
            .WithMany(u => u.ChatRooms)
            .HasForeignKey(r => r.UserId);

        // Configure ChatMessage
        modelBuilder.Entity<ChatMessage>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.ChatRoom)
            .WithMany(r => r.Messages)
            .HasForeignKey(m => m.ChatRoomId);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.Messages)
            .HasForeignKey(m => m.SenderId);
    }
}