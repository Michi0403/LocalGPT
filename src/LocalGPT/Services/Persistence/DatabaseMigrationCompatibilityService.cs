using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Owns legacy SQLite schema adoption and compatibility backup logic.
/// This responsibility is isolated from database seeding so migration reconciliation is testable,
/// logged, and represented in bounded LocalGPT short-term operational memory.
/// </summary>
public sealed partial class DatabaseMigrationCompatibilityService : IDatabaseMigrationCompatibilityService
{
    /// <summary>
    /// Stores the database file health service dependency used by <see cref="DatabaseMigrationCompatibilityService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDatabaseFileHealthService databaseFileHealth;
    /// <summary>
    /// Stores the service activity service dependency used by <see cref="DatabaseMigrationCompatibilityService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IServiceActivityService serviceActivity;
    /// <summary>
    /// Stores the logger used by <see cref="DatabaseMigrationCompatibilityService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<DatabaseMigrationCompatibilityService> logger;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to abandoned migration lock age state owned by <see cref="DatabaseMigrationCompatibilityService"/>.
    /// </summary>
    private readonly TimeSpan abandonedMigrationLockAge = TimeSpan.FromMinutes(10);
    /// <summary>
    /// Stores the internal legacy migration signatures state used by <see cref="DatabaseMigrationCompatibilityService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly DatabaseMigrationSignature[] legacyMigrationSignatures;

    /// <summary>
    /// Initializes a new <see cref="DatabaseMigrationCompatibilityService"/> instance and captures the dependencies or initial state required by its database migration compatibility workflow.
    /// </summary>
    /// <param name="databaseFileHealth">Database file health service dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <param name="serviceActivity">Service activity service dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public DatabaseMigrationCompatibilityService(
        IDatabaseFileHealthService databaseFileHealth,
        IServiceActivityService serviceActivity,
        ILogger<DatabaseMigrationCompatibilityService> logger)
    {
        this.databaseFileHealth = databaseFileHealth ?? throw new ArgumentNullException(nameof(databaseFileHealth));
        this.serviceActivity = serviceActivity ?? throw new ArgumentNullException(nameof(serviceActivity));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        legacyMigrationSignatures = CreateLegacyMigrationSignatures();
    }

    /// <summary>
    /// Performs prepare as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task PrepareAsync(CancellationToken cancellationToken = default) {
    try
    {
        return serviceActivity.RunAsync(
            nameof(DatabaseMigrationCompatibilityService),
            nameof(PrepareAsync),
            PrepareCoreAsync,
            cancellationToken,
            "Legacy migration compatibility inspection completed.");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(PrepareAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(PrepareAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates legacy migration signatures as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The database migration signature produced by the operation.</returns>
    private DatabaseMigrationSignature[] CreateLegacyMigrationSignatures() {
    try
    {
        return [
        new(
            "20260616222639_Initial",
            "10.0.9",
            [
                Table("ApplicationLogs"),
                Column("ApplicationLogs", "TimestampUtc"),
                Column("ApplicationLogs", "Level"),
                Column("ApplicationLogs", "LogLevelValue"),
                Column("ApplicationLogs", "Category"),
                Column("ApplicationLogs", "EventId"),
                Column("ApplicationLogs", "EventName"),
                Column("ApplicationLogs", "Message"),
                Column("ApplicationLogs", "Exception"),
                Column("ApplicationLogs", "MachineName"),
                Column("ApplicationLogs", "ProcessId"),
                Column("ApplicationLogs", "ThreadId"),
                Table("ChatMemoryConversations"),
                Column("ChatMemoryConversations", "ProviderName"),
                Table("CouncilKnowledgeEntries"),
                Column("CouncilKnowledgeEntries", "VerificationStatus"),
                Table("NativeCommandLogs"),
                Column("NativeCommandLogs", "PolicyDecision"),
                Table("ChatMemoryMessages"),
                Column("ChatMemoryMessages", "ConversationId")
            ]),
        new(
            "20260711220358_addingGeneralConfigTables",
            "10.0.9",
            [
                Table("RegexPatterns"),
                Column("RegexPatterns", "Pattern"),
                Table("Prompts"),
                Column("Prompts", "Language"),
                Table("SystemVariables"),
                Column("SystemVariables", "DataType")
            ]),
        new(
            "20260725000000_AddProjectCollaboration",
            "10.0.11",
            [
                Table("LocalGptProjects"),
                Column("LocalGptProjects", "CurrentVersion"),
                Table("LocalGptProjectTopics"),
                Column("LocalGptProjectTopics", "ProjectId"),
                Table("LocalGptProjectVersions"),
                Column("LocalGptProjectVersions", "PathSnapshot"),
                Table("LocalGptProjectTopicKnowledgeLinks"),
                Column("LocalGptProjectTopicKnowledgeLinks", "KnowledgeEntryId")
            ]),
        new(
            "20260725030000_AddCodeGenerationChangeReviews",
            "10.0.11",
            [
                Table("CodeGenerationChangeReviews"),
                Column("CodeGenerationChangeReviews", "ReviewHash"),
                Column("CodeGenerationChangeReviews", "Status")
            ]),
        new(
            "20260725150000_AddChatSessionControl",
            "10.0.11",
            [
                Column("ChatMemoryConversations", "ApplicationVersion"),
                Column("ChatMemoryConversations", "ProjectId"),
                Column("ChatMemoryConversations", "ProjectVersionId"),
                Column("ChatMemoryMessages", "FeedbackComment"),
                Column("ChatMemoryMessages", "FeedbackUpdatedAtUtc"),
                Column("ChatMemoryMessages", "IsPositiveFeedback")
            ]),
        new(
            "20260726000000_AddHumanCollaboration",
            "10.0.11",
            [
                Table("HumanCollaborationRequests"),
                Column("HumanCollaborationRequests", "DecisionReason"),
                Table("HumanCouncilParticipantProfiles"),
                Column("HumanCouncilParticipantProfiles", "Expertise"),
                Table("HumanCouncilContributions"),
                Column("HumanCouncilContributions", "EvaluationVerdict")
            ]),
        new(
            "20260726001000_AddDeferredDxAiInvocations",
            "10.0.11",
            [
                Table("DeferredDxAiInvocations"),
                Column("DeferredDxAiInvocations", "ParametersJson"),
                Column("DeferredDxAiInvocations", "ApprovalRequestId")
            ]),
        new(
            "20260726010000_AddDatabaseFirstProjectArchitecture",
            "10.0.11",
            [
                Table("LocalGptProjectRevisions"),
                Column("LocalGptProjectRevisions", "ProjectStructureJson"),
                Table("LocalGptProjectRequirements"),
                Column("LocalGptProjectRequirements", "RequiredCapability"),
                Table("LocalGptProjectRequirementLinks"),
                Column("LocalGptProjectRequirementLinks", "TargetKind"),
                Table("LocalGptProjectArtifacts"),
                Column("LocalGptProjectArtifacts", "ArtifactKind"),
                Table("ProjectDocumentImports"),
                Column("ProjectDocumentImports", "ContentHash"),
                Table("CouncilModelPresets"),
                Column("CouncilModelPresets", "ModelNamesJson"),
                Table("SqliteEditorFieldOverrides"),
                Column("SqliteEditorFieldOverrides", "EditorKind"),
                Table("CouncilKnowledgeUserRatings"),
                Column("CouncilKnowledgeUserRatings", "ApprovedForCouncilUse")
            ]),
        new(
            "20260726133000_AddOrganicSkillsAndHardwareRoutes",
            "10.0.11",
            [
                Column("CouncilModelPresets", "ModelRoutesJson"),
                Column("CouncilModelPresets", "AllowParallelHardwareRoads"),
                Table("OrganicSkills"),
                Column("OrganicSkills", "Id"),
                Column("OrganicSkills", "CapabilityKeysJson"),
                Table("ProjectOrganicSkillLinks"),
                Column("ProjectOrganicSkillLinks", "Id"),
                Column("ProjectOrganicSkillLinks", "SkillId"),
                Table("CouncilMemberOrganicSkillLinks"),
                Column("CouncilMemberOrganicSkillLinks", "Id"),
                Column("CouncilMemberOrganicSkillLinks", "OrganicCapabilitiesJson")
            ]),
        new(
            "20260726150000_AddCouncilTeamScripting",
            "10.0.11",
            [
                Table("CouncilTeamConfigurations"),
                Column("CouncilTeamConfigurations", "Id"),
                Column("CouncilTeamConfigurations", "WorkflowStepsJson"),
                Column("CouncilTeamConfigurations", "ExpertPreparationPromptTemplate"),
                Column("CouncilTeamConfigurations", "LeaderSynthesisPromptTemplate"),
                Column("CouncilTeamConfigurations", "MainRoundInstructionTemplate"),
                Column("CouncilTeamConfigurations", "IsUserModified")
            ]),
        new(
            "20260731152000_AddHumanQuestionFlow",
            "10.0.11",
            [
                Column("HumanCollaborationRequests", "QuestionScope"),
                Column("HumanCollaborationRequests", "GateMode"),
                Column("HumanCollaborationRequests", "TargetMembersText"),
                Column("HumanCollaborationRequests", "RequestedCouncilRound"),
                Column("HumanCollaborationRequests", "RequestedCouncilPhase")
            ]),
        new(
            "20260802010000_AddCouncilRuntimeClasses",
            "10.0.11",
            [
                Table("CouncilRuntimeClassConfigurations"),
                Column("CouncilRuntimeClassConfigurations", "Key"),
                Column("CouncilRuntimeClassConfigurations", "FieldsJson"),
                Column("CouncilRuntimeClassConfigurations", "InputBindingsJson"),
                Column("CouncilRuntimeClassConfigurations", "SourceReferencesJson")
            ]),
        new(
            "20260802020000_AddEmbeddedFirmwareAndWorkspaceEnvironments",
            "10.0.11",
            [
                Column("ProjectWorkspaceRoots", "AccessPolicyJson"),
                Column("ProjectWorkspaceRoots", "BuildArguments"),
                Column("ProjectWorkspaceRoots", "DefaultSubdirectoriesJson"),
                Column("ProjectWorkspaceRoots", "EnvironmentKind"),
                Column("ProjectWorkspaceRoots", "EnvironmentRootPath"),
                Column("ProjectWorkspaceRoots", "EnvironmentVariablesJson"),
                Column("ProjectWorkspaceRoots", "ExpectedStructureRegex"),
                Column("ProjectWorkspaceRoots", "LastPermissionCheckedAtUtc"),
                Column("ProjectWorkspaceRoots", "LastPermissionReadAccess"),
                Column("ProjectWorkspaceRoots", "LastPermissionStatus"),
                Column("ProjectWorkspaceRoots", "LastPermissionSummary"),
                Column("ProjectWorkspaceRoots", "LastPermissionWriteAccess"),
                Column("ProjectWorkspaceRoots", "PreferredCompilerInstallationId")
            ])
    ];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(CreateLegacyMigrationSignatures)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(CreateLegacyMigrationSignatures)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs prepare core as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PrepareCoreAsync(CancellationToken cancellationToken)
    {
    try
    {
            if (!File.Exists(databaseFileHealth.DatabasePath))
                return;

            var sourceConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databaseFileHealth.DatabasePath,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Private
            }.ToString();
            var connection = new SqliteConnection(sourceConnectionString);
            await using var configuredConnectionAsyncDisposal = connection.ConfigureAwait(false);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await ClearAbandonedMigrationLockAsync(connection, cancellationToken).ConfigureAwait(false);
            await EnsureMigrationHistoryTableAsync(connection, cancellationToken).ConfigureAwait(false);
            var appliedMigrations = await ReadAppliedMigrationsAsync(connection, cancellationToken).ConfigureAwait(false);
            var schema = await ReadSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
            if (!schema.Keys.Any(IsApplicationTable))
                return;

            string? backupPath = null;
            var adopted = new List<string>();
            var repaired = new List<string>();

            foreach (var signature in legacyMigrationSignatures)
            {
                var state = EvaluateSignature(signature, schema);
                var markedApplied = appliedMigrations.Contains(signature.Id);
                if (markedApplied && state == DatabaseMigrationSignatureState.Complete)
                    continue;

                if (state == DatabaseMigrationSignatureState.Missing && !markedApplied)
                    break; // EF will apply this and every later migration in normal order.

                if (state == DatabaseMigrationSignatureState.Complete && !markedApplied)
                {
                    backupPath ??= await CreateCompatibilityBackupAsync(connection, cancellationToken).ConfigureAwait(false);
                    await InsertMigrationHistoryAsync(connection, signature, cancellationToken).ConfigureAwait(false);
                    appliedMigrations.Add(signature.Id);
                    adopted.Add(signature.Id);
                    continue;
                }

                if (state == DatabaseMigrationSignatureState.Partial && IsSupportedApplicationLogsBootstrap(signature, schema) && !markedApplied)
                {
                    backupPath ??= await CreateCompatibilityBackupAsync(connection, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation(
                        "Detected a compatible pre-existing ApplicationLogs table without EF migration history. " +
                        "The idempotent initial migration will preserve it and create the remaining initial tables. " +
                        "Compatibility backup: {BackupPath}",
                        backupPath);
                    break;
                }

                // A history row with missing schema, or a partially applied recent migration, must not be ignored.
                // Repair only migrations with explicit lossless plans. Malformed newly introduced tables may be renamed to a compatibility archive; no row is deleted.
                backupPath ??= await CreateCompatibilityBackupAsync(connection, cancellationToken).ConfigureAwait(false);
                var didRepair = await TryRepairKnownMigrationAsync(connection, signature.Id, schema, cancellationToken).ConfigureAwait(false);
                if (didRepair)
                {
                    schema = await ReadSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
                    state = EvaluateSignature(signature, schema);
                    if (state == DatabaseMigrationSignatureState.Complete)
                    {
                        if (!markedApplied)
                        {
                            await InsertMigrationHistoryAsync(connection, signature, cancellationToken).ConfigureAwait(false);
                            appliedMigrations.Add(signature.Id);
                            adopted.Add(signature.Id);
                        }
                        repaired.Add(signature.Id);
                        continue;
                    }
                }

                var missing = signature.Requirements
                    .Where(requirement => !RequirementExists(requirement, schema))
                    .Select(requirement => requirement.ColumnName is null
                        ? requirement.TableName
                        : $"{requirement.TableName}.{requirement.ColumnName}")
                    .Take(20)
                    .ToArray();
                throw new InvalidOperationException(
                    $"The SQLite database contains an incomplete schema for migration '{signature.Id}'. " +
                    $"Missing markers: {string.Join(", ", missing)}. " +
                    $"LocalGPT created the lossless compatibility backup '{backupPath}'. No row was deleted; malformed new tables are retained as compatibility archives; " +
                    "this migration has no safe additive repair plan yet.");
            }

            if (adopted.Count > 0 || repaired.Count > 0)
            {
                logger.LogWarning(
                    "Migration compatibility completed. Adopted: {Adopted}; additively repaired: {Repaired}. " +
                    "No application row was deleted. Compatibility backup: {BackupPath}",
                    adopted.Count == 0 ? "none" : string.Join(", ", adopted),
                    repaired.Count == 0 ? "none" : string.Join(", ", repaired),
                    backupPath);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(PrepareCoreAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(PrepareCoreAsync)} failed.");
        throw;
    }
}
}
