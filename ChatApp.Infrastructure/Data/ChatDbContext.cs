using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace ChatApp.Infrastructure.Data;

public class ChatDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            //e.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            e.Property(u => u.OtherName).HasMaxLength(50).IsRequired();
        });

        builder.Entity<Conversation>(e =>
        {
            e.Property(c => c.Title).HasMaxLength(150);
        });

        builder.Entity<ConversationParticipant>(e =>
        {
            e.HasKey(p => new { p.ConversationId, p.UserId });

            e.HasOne(p => p.Conversation)
             .WithMany(c => c.Participants)
             .HasForeignKey(p => p.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.User)
             .WithMany(u => u.Conversations)
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Message>(e =>
        {
            e.Property(m => m.Content).HasMaxLength(4000).IsRequired();

            e.HasOne(m => m.Conversation)
             .WithMany(c => c.Messages)
             .HasForeignKey(m => m.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Sender)
             .WithMany()
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            // Fast loading of a conversation's message history
            e.HasIndex(m => new { m.ConversationId, m.SentAt });
        });
    }
}