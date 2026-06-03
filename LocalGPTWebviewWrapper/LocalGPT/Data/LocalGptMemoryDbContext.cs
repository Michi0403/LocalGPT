using LocalGPT.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Data
{
    public class LocalGptMemoryDbContext(DbContextOptions<LocalGptMemoryDbContext> options) : DbContext(options)
    {
        public DbSet<ChatMemoryConversation> Conversations => Set<ChatMemoryConversation>();
        public DbSet<ChatMemoryMessage> Messages => Set<ChatMemoryMessage>();
        public DbSet<ApplicationLogEntry> ApplicationLogs => Set<ApplicationLogEntry>();
        public DbSet<CouncilKnowledgeEntry> CouncilKnowledgeEntries => Set<CouncilKnowledgeEntry>();
        public DbSet<NativeCommandLogEntry> NativeCommandLogs => Set<NativeCommandLogEntry>();

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

            modelBuilder.Entity<CouncilKnowledgeEntry>(entity =>
            {
                entity.ToTable("CouncilKnowledgeEntries");
                entity.HasKey(entry => entry.Id);
                entity.Property(entry => entry.Topic).HasMaxLength(240).IsRequired();
                entity.Property(entry => entry.Scope).HasMaxLength(120).IsRequired();
                entity.Property(entry => entry.Content).IsRequired();
                entity.Property(entry => entry.Source).HasMaxLength(240).IsRequired();
                entity.Property(entry => entry.HelpfulSources).IsRequired();
                entity.Property(entry => entry.Tags).HasMaxLength(400).IsRequired();
                entity.Property(entry => entry.VerificationStatus).HasMaxLength(80).IsRequired();
                entity.Property(entry => entry.ReviewStatus).HasMaxLength(80).IsRequired();
                entity.Property(entry => entry.StalenessReason).HasMaxLength(500).IsRequired();
                entity.Property(entry => entry.StalenessDetectedBy).HasMaxLength(160).IsRequired();
                entity.Property(entry => entry.SourceHash).HasMaxLength(128).IsRequired();
                entity.HasIndex(entry => entry.UpdatedAtUtc);
                entity.HasIndex(entry => new { entry.IsUserApproved, entry.UpdatedAtUtc });
                entity.HasIndex(entry => new { entry.IsPinned, entry.UpdatedAtUtc });
                entity.HasIndex(entry => entry.VerificationStatus);
                entity.HasIndex(entry => entry.ReviewStatus);
                entity.HasIndex(entry => entry.ExpiresAtUtc);
                entity.HasIndex(entry => entry.LastVerifiedAtUtc);
                entity.HasIndex(entry => entry.LastUsedAtUtc);
                entity.HasIndex(entry => entry.SupersededByKnowledgeId);
                entity.HasIndex(entry => entry.Scope);
            });

            modelBuilder.Entity<NativeCommandLogEntry>(entity =>
            {
                entity.ToTable("NativeCommandLogs");
                entity.HasKey(entry => entry.Id);
                entity.Property(entry => entry.FeatureName).HasMaxLength(120).IsRequired();
                entity.Property(entry => entry.RequestedBy).HasMaxLength(120).IsRequired();
                entity.Property(entry => entry.CommandProfile).HasMaxLength(120).IsRequired();
                entity.Property(entry => entry.Executable).HasMaxLength(260).IsRequired();
                entity.Property(entry => entry.Arguments).IsRequired();
                entity.Property(entry => entry.WorkingDirectory).HasMaxLength(1024).IsRequired();
                entity.Property(entry => entry.StdoutPath).HasMaxLength(1024).IsRequired();
                entity.Property(entry => entry.StderrPath).HasMaxLength(1024).IsRequired();
                entity.Property(entry => entry.PolicyDecision).HasMaxLength(80).IsRequired();
                entity.Property(entry => entry.PolicyReason).HasMaxLength(500).IsRequired();
                entity.HasIndex(entry => entry.StartedAtUtc);
                entity.HasIndex(entry => entry.Executable);
                entity.HasIndex(entry => entry.PolicyDecision);
            });
        }
    }
}
