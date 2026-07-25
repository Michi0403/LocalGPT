using LocalGPT.BusinessObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.BusinessObjects.EFCore
{
    public class LocalGptMemoryDbContext(DbContextOptions<LocalGptMemoryDbContext> options) : DbContext(options)
    {
        public DbSet<ChatMemoryConversation> Conversations => Set<ChatMemoryConversation>();
        public DbSet<ChatMemoryMessage> Messages => Set<ChatMemoryMessage>();
        public DbSet<ApplicationLogEntry> ApplicationLogs => Set<ApplicationLogEntry>();
        public DbSet<CouncilKnowledgeEntry> CouncilKnowledgeEntries => Set<CouncilKnowledgeEntry>();
        public DbSet<NativeCommandLogEntry> NativeCommandLogs => Set<NativeCommandLogEntry>();
        public DbSet<RegexPattern> RegexPatterns { get; set; }
        public DbSet<PromptConfig> Prompts { get; set; }
        public DbSet<SystemVariable> SystemVariables { get; set; }
        public DbSet<LocalGptProject> LocalGptProjects => Set<LocalGptProject>();
        public DbSet<LocalGptProjectTopic> LocalGptProjectTopics => Set<LocalGptProjectTopic>();
        public DbSet<LocalGptProjectVersion> LocalGptProjectVersions => Set<LocalGptProjectVersion>();
        public DbSet<LocalGptProjectTopicKnowledgeLink> LocalGptProjectTopicKnowledgeLinks => Set<LocalGptProjectTopicKnowledgeLink>();
        public DbSet<CodeGenerationChangeReview> CodeGenerationChangeReviews => Set<CodeGenerationChangeReview>();
        public DbSet<HumanCollaborationRequest> HumanCollaborationRequests => Set<HumanCollaborationRequest>();
        public DbSet<HumanCouncilParticipantProfile> HumanCouncilParticipantProfiles => Set<HumanCouncilParticipantProfile>();
        public DbSet<HumanCouncilContribution> HumanCouncilContributions => Set<HumanCouncilContribution>();
        public DbSet<DeferredDxAiInvocation> DeferredDxAiInvocations => Set<DeferredDxAiInvocation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure unique indexes and properties
            modelBuilder.Entity<RegexPattern>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).IsRequired().HasMaxLength(128);
                e.Property(p => p.Pattern).IsRequired();
                e.Property(p => p.Flags).HasMaxLength(32);
                e.Property(p => p.CreatedOn).IsRequired();
                e.Property(p => p.UpdatedOn).IsRequired();
                e.HasIndex(p => p.Name).IsUnique();
            });


            modelBuilder.Entity<PromptConfig>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Key).IsRequired().HasMaxLength(128);
                e.Property(p => p.Language).HasMaxLength(10);
                e.Property(p => p.Text).IsRequired();
                e.Property(p => p.LastUpdated).IsRequired();
                e.HasIndex(pc => new { pc.Key, pc.Language }).IsUnique();
            });

            modelBuilder.Entity<SystemVariable>(e =>
            {
                e.HasKey(sv => sv.Id);
                e.Property(sv => sv.Name).IsRequired().HasMaxLength(128);
                e.Property(sv => sv.ValueString).IsRequired();
                e.Property(sv => sv.DataType).HasMaxLength(32);
                e.Property(sv => sv.LastUpdated).IsRequired();
                e.HasIndex(sv => sv.Name).IsUnique();
            });

            modelBuilder.Entity<ChatMemoryConversation>(entity =>
            {
                entity.ToTable("ChatMemoryConversations");
                entity.HasKey(conversation => conversation.Id);
                entity.Property(conversation => conversation.Title).HasMaxLength(240).IsRequired();
                entity.Property(conversation => conversation.ProviderName).HasMaxLength(160).IsRequired();
                entity.Property(conversation => conversation.ApplicationVersion).HasMaxLength(120).IsRequired();
                entity.HasIndex(conversation => conversation.UpdatedAtUtc);
                entity.HasIndex(conversation => new { conversation.ProjectId, conversation.ProjectVersionId, conversation.UpdatedAtUtc });
                entity.HasMany(conversation => conversation.Messages)
                    .WithOne(message => message.Conversation)
                    .HasForeignKey(message => message.ConversationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ChatMemoryMessage>(entity =>
            {
                entity.ToTable("ChatMemoryMessages");
                entity.HasKey(message => message.Id);
                entity.Property(message => message.Role).HasMaxLength(40).IsRequired();
                entity.Property(message => message.Content).IsRequired();
                entity.Property(message => message.FeedbackComment).HasMaxLength(4000).IsRequired();
                entity.HasIndex(message => new { message.ConversationId, message.SortOrder }).IsUnique();
                entity.HasIndex(message => new { message.IsPositiveFeedback, message.FeedbackUpdatedAtUtc });
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
                entity.Property(entry => entry.Content).IsRequired() ;
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


            modelBuilder.Entity<LocalGptProject>(entity =>
            {
                entity.ToTable("LocalGptProjects");
                entity.HasKey(project => project.Id);
                entity.Property(project => project.Name).HasMaxLength(200).IsRequired();
                entity.Property(project => project.Purpose).IsRequired();
                entity.Property(project => project.RootPath).HasMaxLength(1024).IsRequired();
                entity.Property(project => project.CurrentVersion).HasMaxLength(120).IsRequired();
                entity.Property(project => project.Status).HasMaxLength(80).IsRequired();
                entity.HasIndex(project => project.Name);
                entity.HasIndex(project => new { project.IsArchived, project.UpdatedAtUtc });
            });

            modelBuilder.Entity<LocalGptProjectTopic>(entity =>
            {
                entity.ToTable("LocalGptProjectTopics");
                entity.HasKey(topic => topic.Id);
                entity.Property(topic => topic.Name).HasMaxLength(240).IsRequired();
                entity.Property(topic => topic.Description).IsRequired();
                entity.Property(topic => topic.Status).HasMaxLength(80).IsRequired();
                entity.HasIndex(topic => new { topic.ProjectId, topic.Name }).IsUnique();
                entity.HasIndex(topic => new { topic.ProjectId, topic.Status });
                entity.HasOne(topic => topic.Project)
                    .WithMany(project => project.Topics)
                    .HasForeignKey(topic => topic.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LocalGptProjectVersion>(entity =>
            {
                entity.ToTable("LocalGptProjectVersions");
                entity.HasKey(version => version.Id);
                entity.Property(version => version.Version).HasMaxLength(120).IsRequired();
                entity.Property(version => version.Notes).IsRequired();
                entity.Property(version => version.PathSnapshot).HasMaxLength(1024).IsRequired();
                entity.HasIndex(version => new { version.ProjectId, version.Version }).IsUnique();
                entity.HasIndex(version => new { version.ProjectId, version.IsCurrent });
                entity.HasOne(version => version.Project)
                    .WithMany(project => project.Versions)
                    .HasForeignKey(version => version.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LocalGptProjectTopicKnowledgeLink>(entity =>
            {
                entity.ToTable("LocalGptProjectTopicKnowledgeLinks");
                entity.HasKey(link => new { link.ProjectTopicId, link.KnowledgeEntryId });
                entity.Property(link => link.LinkReason).HasMaxLength(500).IsRequired();
                entity.HasIndex(link => link.KnowledgeEntryId);
                entity.HasIndex(link => link.LinkedAtUtc);
                entity.HasOne(link => link.ProjectTopic)
                    .WithMany(topic => topic.KnowledgeLinks)
                    .HasForeignKey(link => link.ProjectTopicId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(link => link.KnowledgeEntry)
                    .WithMany()
                    .HasForeignKey(link => link.KnowledgeEntryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<CodeGenerationChangeReview>(entity =>
            {
                entity.ToTable("CodeGenerationChangeReviews");
                entity.HasKey(review => review.Id);
                entity.Property(review => review.Title).HasMaxLength(240).IsRequired();
                entity.Property(review => review.Goal).IsRequired();
                entity.Property(review => review.CurrentProjectState).IsRequired();
                entity.Property(review => review.CouncilSummary).IsRequired();
                entity.Property(review => review.ChangeSummary).IsRequired();
                entity.Property(review => review.SafetySummary).IsRequired();
                entity.Property(review => review.PayloadJson).IsRequired();
                entity.Property(review => review.ReviewHash).HasMaxLength(128).IsRequired();
                entity.Property(review => review.Status).HasMaxLength(80).IsRequired();
                entity.Property(review => review.DecisionNote).HasMaxLength(2000).IsRequired();
                entity.Property(review => review.WorkspaceName).HasMaxLength(260).IsRequired();
                entity.Property(review => review.ZipFileName).HasMaxLength(260).IsRequired();
                entity.Property(review => review.BuildStatus).HasMaxLength(1000).IsRequired();
                entity.HasIndex(review => review.UpdatedAtUtc);
                entity.HasIndex(review => new { review.ProjectId, review.Status, review.UpdatedAtUtc });
                entity.HasIndex(review => review.CouncilRunId);
                entity.HasIndex(review => review.ReviewHash);
            });

            modelBuilder.Entity<HumanCollaborationRequest>(entity =>
            {
                entity.ToTable("HumanCollaborationRequests");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.CorrelationId).HasMaxLength(180).IsRequired();
                entity.Property(item => item.OperationKey).HasMaxLength(180).IsRequired();
                entity.Property(item => item.ParameterFingerprint).HasMaxLength(128).IsRequired();
                entity.Property(item => item.RequestKind).HasMaxLength(40).IsRequired();
                entity.Property(item => item.Title).HasMaxLength(240).IsRequired();
                entity.Property(item => item.Description).HasMaxLength(2000).IsRequired();
                entity.Property(item => item.RiskLevel).HasMaxLength(40).IsRequired();
                entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
                entity.Property(item => item.Source).HasMaxLength(160).IsRequired();
                entity.Property(item => item.RequestedBy).HasMaxLength(160).IsRequired();
                entity.Property(item => item.RequestedRole).HasMaxLength(160).IsRequired();
                entity.Property(item => item.SuggestedResponsesText).HasMaxLength(1600).IsRequired();
                entity.Property(item => item.ResponsePrompt).HasMaxLength(500).IsRequired();
                entity.Property(item => item.PrefillText).HasMaxLength(2000).IsRequired();
                entity.Property(item => item.UserResponse).HasMaxLength(4000).IsRequired();
                entity.Property(item => item.DecisionReason).HasMaxLength(2000).IsRequired();
                entity.Property(item => item.DecisionBy).HasMaxLength(120).IsRequired();
                entity.HasIndex(item => new { item.CorrelationId, item.OperationKey, item.RequestedAtUtc });
                entity.HasIndex(item => new { item.Status, item.UpdatedAtUtc });
                entity.HasIndex(item => new { item.CouncilRunId, item.Status });
            });

            modelBuilder.Entity<HumanCouncilParticipantProfile>(entity =>
            {
                entity.ToTable("HumanCouncilParticipantProfiles");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.DisplayName).HasMaxLength(120).IsRequired();
                entity.Property(item => item.RoleName).HasMaxLength(180).IsRequired();
                entity.Property(item => item.Expertise).HasMaxLength(2000).IsRequired();
                entity.Property(item => item.WorkingStyle).HasMaxLength(1200).IsRequired();
                entity.Property(item => item.UpdatedBy).HasMaxLength(120).IsRequired();
            });

            modelBuilder.Entity<HumanCouncilContribution>(entity =>
            {
                entity.ToTable("HumanCouncilContributions");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.HumanDisplayName).HasMaxLength(120).IsRequired();
                entity.Property(item => item.HumanRole).HasMaxLength(180).IsRequired();
                entity.Property(item => item.Content).HasMaxLength(4000).IsRequired();
                entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
                entity.Property(item => item.Evaluation).HasMaxLength(4000).IsRequired();
                entity.Property(item => item.EvaluationVerdict).HasMaxLength(40).IsRequired();
                entity.HasIndex(item => new { item.CouncilRunId, item.Status, item.EarliestCouncilRound });
            });

            modelBuilder.Entity<DeferredDxAiInvocation>(entity =>
            {
                entity.ToTable("DeferredDxAiInvocations");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.CorrelationId).HasMaxLength(180).IsRequired();
                entity.Property(item => item.FunctionName).HasMaxLength(180).IsRequired();
                entity.Property(item => item.ParametersJson).HasMaxLength(64000).IsRequired();
                entity.Property(item => item.ConfirmationSummaryHash).HasMaxLength(180).IsRequired();
                entity.Property(item => item.RequestedBy).HasMaxLength(160).IsRequired();
                entity.Property(item => item.ApplicationVersion).HasMaxLength(80).IsRequired();
                entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
                entity.Property(item => item.ResultStatus).HasMaxLength(80).IsRequired();
                entity.Property(item => item.ResultSummary).HasMaxLength(8000).IsRequired();
                entity.HasIndex(item => item.ApprovalRequestId).IsUnique();
                entity.HasIndex(item => new { item.CouncilRunId, item.Status, item.CreatedAtUtc });
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
