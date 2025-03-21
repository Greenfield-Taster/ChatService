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
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Role).IsRequired();
 
            entity.HasIndex(e => e.Email).IsUnique();
        });
         
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
             
            entity.HasOne(e => e.Admin)
                  .WithMany()
                  .HasForeignKey(e => e.AdminId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RegularUser)
                  .WithMany(u => u.ChatRooms)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
         
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Status).IsRequired();
             
            entity.HasOne(e => e.ChatRoom)
                  .WithMany(r => r.Messages)
                  .HasForeignKey(e => e.ChatRoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                  .WithMany(u => u.Messages)
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
             
            entity.HasIndex(e => e.ChatRoomId);
             
            entity.HasIndex(e => e.Timestamp);
        });
    }
}