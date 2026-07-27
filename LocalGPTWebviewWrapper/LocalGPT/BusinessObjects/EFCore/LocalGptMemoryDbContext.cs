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
        public DbSet<LocalGptProjectRevision> LocalGptProjectRevisions => Set<LocalGptProjectRevision>();
        public DbSet<LocalGptProjectRequirement> LocalGptProjectRequirements => Set<LocalGptProjectRequirement>();
        public DbSet<LocalGptProjectRequirementLink> LocalGptProjectRequirementLinks => Set<LocalGptProjectRequirementLink>();
        public DbSet<LocalGptProjectArtifact> LocalGptProjectArtifacts => Set<LocalGptProjectArtifact>();
        public DbSet<ProjectDocumentImport> ProjectDocumentImports => Set<ProjectDocumentImport>();
        public DbSet<CouncilModelPreset> CouncilModelPresets => Set<CouncilModelPreset>();
        public DbSet<SqliteEditorFieldOverride> SqliteEditorFieldOverrides => Set<SqliteEditorFieldOverride>();
        public DbSet<CouncilKnowledgeUserRating> CouncilKnowledgeUserRatings => Set<CouncilKnowledgeUserRating>();
        public DbSet<OrganicSkillDefinition> OrganicSkills => Set<OrganicSkillDefinition>();
        public DbSet<ProjectOrganicSkillLink> ProjectOrganicSkillLinks => Set<ProjectOrganicSkillLink>();
        public DbSet<CouncilMemberOrganicSkillLink> CouncilMemberOrganicSkillLinks => Set<CouncilMemberOrganicSkillLink>();
        public DbSet<CouncilTeamConfiguration> CouncilTeamConfigurations => Set<CouncilTeamConfiguration>();
        public DbSet<ProjectWorkspaceRoot> ProjectWorkspaceRoots => Set<ProjectWorkspaceRoot>();
        public DbSet<ProjectCompilerInstallation> ProjectCompilerInstallations => Set<ProjectCompilerInstallation>();
        public DbSet<LocalGptProjectTrackedFile> LocalGptProjectTrackedFiles => Set<LocalGptProjectTrackedFile>();
        public DbSet<ProjectBuildVerification> ProjectBuildVerifications => Set<ProjectBuildVerification>();

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
                entity.Property(project => project.RootPath).HasMaxLength(2048).IsRequired();
                entity.Property(project => project.ProjectType).HasMaxLength(120).IsRequired();
                entity.Property(project => project.SolutionPath).HasMaxLength(2048).IsRequired();
                entity.Property(project => project.SolutionSearchPattern).IsRequired();
                entity.Property(project => project.FileIncludePattern).IsRequired();
                entity.Property(project => project.FileExcludePattern).IsRequired();
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


            modelBuilder.Entity<LocalGptProjectRevision>(entity =>
            {
                entity.ToTable("LocalGptProjectRevisions");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.BranchName).HasMaxLength(160).IsRequired();
                entity.Property(item => item.RevisionName).HasMaxLength(160).IsRequired();
                entity.Property(item => item.Summary).IsRequired();
                entity.Property(item => item.ProjectStructureJson).IsRequired();
                entity.Property(item => item.CreatedBy).HasMaxLength(120).IsRequired();
                entity.Property(item => item.SourceSnapshotHash).HasMaxLength(128).IsRequired();
                entity.Property(item => item.SnapshotArchivePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.SourceRootPath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.SolutionPath).HasMaxLength(2048).IsRequired();
                entity.HasIndex(item => new { item.ProjectId, item.BranchName, item.RevisionName }).IsUnique();
                entity.HasIndex(item => new { item.ProjectId, item.IsCurrent, item.UpdatedAtUtc });
                entity.HasOne(item => item.Project).WithMany(project => project.Revisions)
                    .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.ParentRevision).WithMany(item => item.ChildRevisions)
                    .HasForeignKey(item => item.ParentRevisionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LocalGptProjectRequirement>(entity =>
            {
                entity.ToTable("LocalGptProjectRequirements");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Name).HasMaxLength(240).IsRequired();
                entity.Property(item => item.Description).IsRequired();
                entity.Property(item => item.RequirementType).HasMaxLength(80).IsRequired();
                entity.Property(item => item.Status).HasMaxLength(80).IsRequired();
                entity.Property(item => item.Priority).HasMaxLength(40).IsRequired();
                entity.Property(item => item.RequiredCapability).HasMaxLength(240).IsRequired();
                entity.Property(item => item.SourceKind).HasMaxLength(160).IsRequired();
                entity.HasIndex(item => new { item.ProjectId, item.Status, item.Priority });
                entity.HasIndex(item => new { item.ProjectId, item.Name }).IsUnique();
                entity.HasOne(item => item.Project).WithMany(project => project.Requirements)
                    .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Revision).WithMany()
                    .HasForeignKey(item => item.RevisionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LocalGptProjectRequirementLink>(entity =>
            {
                entity.ToTable("LocalGptProjectRequirementLinks");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.TargetKind).HasMaxLength(80).IsRequired();
                entity.Property(item => item.TargetName).HasMaxLength(240).IsRequired();
                entity.Property(item => item.TargetId).HasMaxLength(160).IsRequired();
                entity.Property(item => item.TargetTable).HasMaxLength(160).IsRequired();
                entity.Property(item => item.LinkPurpose).HasMaxLength(1000).IsRequired();
                entity.Property(item => item.CouncilReviewStatus).HasMaxLength(80).IsRequired();
                entity.HasIndex(item => new { item.RequirementId, item.TargetKind, item.TargetName }).IsUnique();
                entity.HasOne(item => item.Requirement).WithMany(requirement => requirement.Links)
                    .HasForeignKey(item => item.RequirementId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LocalGptProjectArtifact>(entity =>
            {
                entity.ToTable("LocalGptProjectArtifacts");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ArtifactKind).HasMaxLength(80).IsRequired();
                entity.Property(item => item.Name).HasMaxLength(240).IsRequired();
                entity.Property(item => item.Value).IsRequired();
                entity.Property(item => item.DataType).HasMaxLength(120).IsRequired();
                entity.Property(item => item.Flags).HasMaxLength(160).IsRequired();
                entity.Property(item => item.Description).HasMaxLength(2000).IsRequired();
                entity.Property(item => item.CouncilReviewStatus).HasMaxLength(80).IsRequired();
                entity.HasIndex(item => new { item.ProjectId, item.ArtifactKind, item.Name }).IsUnique();
                entity.HasIndex(item => new { item.ProjectId, item.IsUserApproved, item.UpdatedAtUtc });
                entity.HasOne(item => item.Project).WithMany(project => project.Artifacts)
                    .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Revision).WithMany()
                    .HasForeignKey(item => item.RevisionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Requirement).WithMany()
                    .HasForeignKey(item => item.RequirementId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProjectDocumentImport>(entity =>
            {
                entity.ToTable("ProjectDocumentImports");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.SourceName).HasMaxLength(260).IsRequired();
                entity.Property(item => item.SourceUri).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.ContentHash).HasMaxLength(128).IsRequired();
                entity.Property(item => item.ContentType).HasMaxLength(120).IsRequired();
                entity.Property(item => item.EncodingName).HasMaxLength(80).IsRequired();
                entity.Property(item => item.ExtractedText).IsRequired();
                entity.Property(item => item.Status).HasMaxLength(80).IsRequired();
                entity.Property(item => item.SafetyNotes).HasMaxLength(2000).IsRequired();
                entity.HasIndex(item => new { item.ProjectId, item.ContentHash }).IsUnique();
                entity.HasOne(item => item.Project).WithMany()
                    .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Revision).WithMany()
                    .HasForeignKey(item => item.RevisionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CouncilModelPreset>(entity =>
            {
                entity.ToTable("CouncilModelPresets");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
                entity.Property(item => item.Description).HasMaxLength(1000).IsRequired();
                entity.Property(item => item.ModelNamesJson).IsRequired();
                entity.Property(item => item.ModelRoutesJson).IsRequired();
                entity.HasIndex(item => item.Name).IsUnique();
                entity.HasIndex(item => new { item.IsArchived, item.IsDefault, item.UpdatedAtUtc });
            });

            modelBuilder.Entity<OrganicSkillDefinition>(entity =>
            {
                entity.ToTable("OrganicSkills");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Key).HasMaxLength(200).IsRequired();
                entity.Property(item => item.DisplayName).HasMaxLength(240).IsRequired();
                entity.Property(item => item.Description).HasMaxLength(2000).IsRequired();
                entity.Property(item => item.SourcePeerId).HasMaxLength(240).IsRequired();
                entity.Property(item => item.OrgansJson).IsRequired();
                entity.Property(item => item.CapabilityKeysJson).IsRequired();
                entity.Property(item => item.UiActivationKeysJson).IsRequired();
                entity.HasIndex(item => item.Key).IsUnique();
                entity.HasIndex(item => new { item.IsEnabled, item.IsOnline, item.UpdatedAtUtc });
            });

            modelBuilder.Entity<ProjectOrganicSkillLink>(entity =>
            {
                entity.ToTable("ProjectOrganicSkillLinks");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Notes).HasMaxLength(2000).IsRequired();
                entity.HasIndex(item => new { item.ProjectId, item.SkillId }).IsUnique();
                entity.HasIndex(item => new { item.ProjectId, item.IsEnabled, item.IsRequired });
                entity.HasOne(item => item.Project).WithMany()
                    .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.Skill).WithMany(skill => skill.ProjectLinks)
                    .HasForeignKey(item => item.SkillId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CouncilMemberOrganicSkillLink>(entity =>
            {
                entity.ToTable("CouncilMemberOrganicSkillLinks");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.MemberKey).HasMaxLength(240).IsRequired();
                entity.Property(item => item.Evidence).HasMaxLength(4000).IsRequired();
                entity.Property(item => item.DxFunctionsJson).IsRequired();
                entity.Property(item => item.ControllerMethodsJson).IsRequired();
                entity.Property(item => item.OrganicCapabilitiesJson).IsRequired();
                entity.HasIndex(item => new { item.MemberKey, item.SkillId }).IsUnique();
                entity.HasIndex(item => new { item.MemberKey, item.IsEnabled, item.Proficiency });
                entity.HasOne(item => item.Skill).WithMany(skill => skill.MemberLinks)
                    .HasForeignKey(item => item.SkillId).OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<CouncilTeamConfiguration>(entity =>
            {
                entity.ToTable("CouncilTeamConfigurations");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Key).HasMaxLength(160).IsRequired();
                entity.Property(item => item.DisplayName).HasMaxLength(240).IsRequired();
                entity.Property(item => item.Purpose).HasMaxLength(4000).IsRequired();
                entity.Property(item => item.RolesJson).IsRequired();
                entity.Property(item => item.PreferredCapabilitiesJson).IsRequired();
                entity.Property(item => item.ArchitectureContractsJson).IsRequired();
                entity.Property(item => item.WorkflowStepsJson).IsRequired();
                entity.Property(item => item.ExpertPreparationPromptTemplate).IsRequired();
                entity.Property(item => item.LeaderSynthesisPromptTemplate).IsRequired();
                entity.Property(item => item.MainRoundInstructionTemplate).IsRequired();
                entity.HasIndex(item => item.Key).IsUnique();
                entity.HasIndex(item => new { item.IsEnabled, item.UpdatedAtUtc });
            });

            modelBuilder.Entity<SqliteEditorFieldOverride>(entity =>
            {
                entity.ToTable("SqliteEditorFieldOverrides");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.TableName).HasMaxLength(160).IsRequired();
                entity.Property(item => item.ColumnName).HasMaxLength(160).IsRequired();
                entity.Property(item => item.EditorKind).HasMaxLength(40).IsRequired();
                entity.Property(item => item.InputMask).HasMaxLength(240).IsRequired();
                entity.Property(item => item.FormatString).HasMaxLength(160).IsRequired();
                entity.Property(item => item.NullText).HasMaxLength(120).IsRequired();
                entity.HasIndex(item => new { item.TableName, item.ColumnName }).IsUnique();
            });

            modelBuilder.Entity<CouncilKnowledgeUserRating>(entity =>
            {
                entity.ToTable("CouncilKnowledgeUserRatings");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.AccuracyStatus).HasMaxLength(80).IsRequired();
                entity.Property(item => item.Notes).HasMaxLength(4000).IsRequired();
                entity.Property(item => item.RatedBy).HasMaxLength(120).IsRequired();
                entity.HasIndex(item => new { item.KnowledgeEntryId, item.UpdatedAtUtc });
                entity.HasOne(item => item.KnowledgeEntry).WithMany()
                    .HasForeignKey(item => item.KnowledgeEntryId).OnDelete(DeleteBehavior.Restrict);
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
                entity.HasIndex(review => review.ProjectRevisionId);
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

            modelBuilder.Entity<ProjectWorkspaceRoot>(entity =>
            {
                entity.ToTable("ProjectWorkspaceRoots");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
                entity.Property(item => item.RootPath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.ScopeKind).HasMaxLength(40).IsRequired();
                entity.Property(item => item.ProjectTypePattern).HasMaxLength(240).IsRequired();
                entity.Property(item => item.SolutionPattern).HasMaxLength(1000).IsRequired();
                entity.Property(item => item.LastResolutionStatus).HasMaxLength(80).IsRequired();
                entity.HasIndex(item => new { item.ScopeKind, item.ProjectId, item.Priority });
                entity.HasIndex(item => item.RootPath);
                entity.HasOne(item => item.Project).WithMany(project => project.WorkspaceRoots).HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProjectCompilerInstallation>(entity =>
            {
                entity.ToTable("ProjectCompilerInstallations");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
                entity.Property(item => item.Language).HasMaxLength(80).IsRequired();
                entity.Property(item => item.ExecutablePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.CompilerHomePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.Version).HasMaxLength(160).IsRequired();
                entity.Property(item => item.Architecture).HasMaxLength(80).IsRequired();
                entity.Property(item => item.DiscoverySource).HasMaxLength(80).IsRequired();
                entity.Property(item => item.ValidationArguments).HasMaxLength(500).IsRequired();
                entity.Property(item => item.EnvironmentVariablesJson).IsRequired();
                entity.Property(item => item.LastValidationMessage).HasMaxLength(4000).IsRequired();
                entity.HasIndex(item => item.ExecutablePath).IsUnique();
                entity.HasIndex(item => new { item.Language, item.IsDefaultForLanguage, item.IsEnabled });
            });

            modelBuilder.Entity<LocalGptProjectTrackedFile>(entity =>
            {
                entity.ToTable("LocalGptProjectTrackedFiles");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.StableFileKey).HasMaxLength(128).IsRequired();
                entity.Property(item => item.AbsolutePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.ProjectRelativePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.WorkspaceRelativePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.SolutionPath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.ProjectFilePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.FileName).HasMaxLength(260).IsRequired();
                entity.Property(item => item.Extension).HasMaxLength(40).IsRequired();
                entity.Property(item => item.ContentType).HasMaxLength(120).IsRequired();
                entity.Property(item => item.EncodingName).HasMaxLength(80).IsRequired();
                entity.Property(item => item.FileRole).HasMaxLength(120).IsRequired();
                entity.Property(item => item.StructureRegex).IsRequired();
                entity.Property(item => item.ContentFormatRegex).IsRequired();
                entity.Property(item => item.ContentHash).HasMaxLength(128).IsRequired();
                entity.HasIndex(item => new { item.ProjectId, item.RevisionId, item.ProjectRelativePath }).IsUnique();
                entity.HasIndex(item => new { item.ProjectId, item.RevisionId, item.Exists });
                entity.HasOne(item => item.Project).WithMany(project => project.TrackedFiles).HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Revision).WithMany(revision => revision.TrackedFiles).HasForeignKey(item => item.RevisionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProjectBuildVerification>(entity =>
            {
                entity.ToTable("ProjectBuildVerifications");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Configuration).HasMaxLength(80).IsRequired();
                entity.Property(item => item.TargetFramework).HasMaxLength(160).IsRequired();
                entity.Property(item => item.RuntimeIdentifier).HasMaxLength(80).IsRequired();
                entity.Property(item => item.ExecutablePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.Arguments).IsRequired();
                entity.Property(item => item.WorkingDirectory).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.OutputLogPath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.EvidenceManifestPath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.OutputHash).HasMaxLength(128).IsRequired();
                entity.Property(item => item.SourceSnapshotHash).HasMaxLength(128).IsRequired();
                entity.Property(item => item.SnapshotArchivePath).HasMaxLength(2048).IsRequired();
                entity.Property(item => item.CouncilReviewSummary).IsRequired();
                entity.Property(item => item.Summary).IsRequired();
                entity.HasIndex(item => new { item.ProjectId, item.RevisionId, item.CompletedAtUtc });
                entity.HasOne(item => item.Project).WithMany(project => project.BuildVerifications).HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Revision).WithMany(revision => revision.BuildVerifications).HasForeignKey(item => item.RevisionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.CompilerInstallation).WithMany().HasForeignKey(item => item.CompilerInstallationId).OnDelete(DeleteBehavior.Restrict);
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
