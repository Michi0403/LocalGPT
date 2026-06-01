using LocalGPT.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Data
{
    public class LocalGptMemoryDbContext(DbContextOptions<LocalGptMemoryDbContext> options) : DbContext(options)
    {
        public DbSet<ChatMemoryConversation> Conversations => Set<ChatMemoryConversation>();
        public DbSet<ChatMemoryMessage> Messages => Set<ChatMemoryMessage>();
        public DbSet<ApplicationLogEntry> ApplicationLogs => Set<ApplicationLogEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatMemoryConversation>(entity =>
            {
                entity.ToTable("ChatMemoryConversations");
                entity.HasKey(conversation => conversation.Id);
                entity.Property(conversation => conversation.Title).HasMaxLength(240).IsRequired();
                entity.Property(conversation => conversation.ProviderName).HasMaxLength(160).IsRequired();
                entity.HasIndex(conversation => conversation.UpdatedAtUtc);
                entity.HasMany(conversation => conversation.Messages)
                    .WithOne(message => message.Conversation)
                    .HasForeignKey(message => message.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ChatMemoryMessage>(entity =>
            {
                entity.ToTable("ChatMemoryMessages");
                entity.HasKey(message => message.Id);
                entity.Property(message => message.Role).HasMaxLength(40).IsRequired();
                entity.Property(message => message.Content).IsRequired();
                entity.HasIndex(message => new { message.ConversationId, message.SortOrder }).IsUnique();
                entity.HasIndex(message => message.CreatedAtUtc);
            });

            modelBuilder.Entity<ApplicationLogEntry>(entity =>
            {
                entity.ToTable("ApplicationLogs");
                entity.HasKey(log => log.Id);
                entity.Property(log => log.Level).HasMaxLength(32).IsRequired();
                entity.Property(log => log.Category).HasMaxLength(300).IsRequired();
                entity.Property(log => log.EventName).HasMaxLength(200);
                entity.Property(log => log.Message).IsRequired();
                entity.Property(log => log.MachineName).HasMaxLength(120).IsRequired();
                entity.HasIndex(log => log.TimestampUtc);
                entity.HasIndex(log => log.LogLevelValue);
                entity.HasIndex(log => new { log.LogLevelValue, log.TimestampUtc });
            });
        }
    }
}
