using Microsoft.EntityFrameworkCore;
using TerminalChatApp.Backend.Models;

namespace TerminalChatApp.Backend.Data;

public class ChatContext : DbContext
{
    public ChatContext(DbContextOptions<ChatContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<ChatInvitation> ChatInvitations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // Chat configuration
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(c => c.Id);
        });

        // ChatParticipant configuration
        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.HasKey(cp => cp.Id);
            
            entity.HasOne(cp => cp.User)
                .WithMany(u => u.ChatParticipants)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(cp => cp.Chat)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Ensure a user can only be in a chat once
            entity.HasIndex(cp => new { cp.UserId, cp.ChatId }).IsUnique();
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            
            entity.HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ChatInvitation configuration
        modelBuilder.Entity<ChatInvitation>(entity =>
        {
            entity.HasKey(i => i.Id);
            
            entity.HasOne(i => i.Sender)
                .WithMany()
                .HasForeignKey(i => i.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete issues
            
            entity.HasOne(i => i.Receiver)
                .WithMany()
                .HasForeignKey(i => i.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(i => i.Chat)
                .WithMany()
                .HasForeignKey(i => i.ChatId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Index for efficient queries
            entity.HasIndex(i => new { i.ReceiverId, i.Status });
            entity.HasIndex(i => new { i.SenderId, i.Status });
        });

        base.OnModelCreating(modelBuilder);
    }
}