using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Owns legacy SQLite schema adoption and compatibility backup logic.
/// This responsibility is isolated from database seeding so migration reconciliation is testable,
/// logged, and represented in bounded LocalGPT short-term operational memory.
/// </summary>
public sealed class DatabaseMigrationCompatibilityService : IDatabaseMigrationCompatibilityService
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
            "10.0.10",
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
            "10.0.10",
            [
                Table("CodeGenerationChangeReviews"),
                Column("CodeGenerationChangeReviews", "ReviewHash"),
                Column("CodeGenerationChangeReviews", "Status")
            ]),
        new(
            "20260725150000_AddChatSessionControl",
            "10.0.10",
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
            "10.0.10",
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
            "10.0.10",
            [
                Table("DeferredDxAiInvocations"),
                Column("DeferredDxAiInvocations", "ParametersJson"),
                Column("DeferredDxAiInvocations", "ApprovalRequestId")
            ]),
        new(
            "20260726010000_AddDatabaseFirstProjectArchitecture",
            "10.0.10",
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
            "10.0.10",
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
            "10.0.10",
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
            "10.0.10",
            [
                Column("HumanCollaborationRequests", "QuestionScope"),
                Column("HumanCollaborationRequests", "GateMode"),
                Column("HumanCollaborationRequests", "TargetMembersText"),
                Column("HumanCollaborationRequests", "RequestedCouncilRound"),
                Column("HumanCollaborationRequests", "RequestedCouncilPhase")
            ]),
        new(
            "20260802010000_AddCouncilRuntimeClasses",
            "10.0.10",
            [
                Table("CouncilRuntimeClassConfigurations"),
                Column("CouncilRuntimeClassConfigurations", "Key"),
                Column("CouncilRuntimeClassConfigurations", "FieldsJson"),
                Column("CouncilRuntimeClassConfigurations", "InputBindingsJson"),
                Column("CouncilRuntimeClassConfigurations", "SourceReferencesJson")
            ]),
        new(
            "20260802020000_AddEmbeddedFirmwareAndWorkspaceEnvironments",
            "10.0.10",
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

    /// <summary>
    /// Attempts to repair known migration as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="migrationId">Identifier of the migration to use for this operation.</param>
    /// <param name="schema">String dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> TryRepairKnownMigrationAsync(
        SqliteConnection connection,
        string migrationId,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        CancellationToken cancellationToken)
    {
    try
    {
            if (string.Equals(migrationId, "20260726133000_AddOrganicSkillsAndHardwareRoutes", StringComparison.Ordinal))
            {
                if (!schema.ContainsKey("CouncilModelPresets"))
                    return false;
                await AddColumnIfMissingAsync(connection, schema, "CouncilModelPresets", "ModelRoutesJson", "TEXT NOT NULL DEFAULT '[]'", cancellationToken).ConfigureAwait(false);
                await AddColumnIfMissingAsync(connection, schema, "CouncilModelPresets", "AllowParallelHardwareRoads", "INTEGER NOT NULL DEFAULT 1", cancellationToken).ConfigureAwait(false);

                var archives = new List<(string Archive, string Target)>();
                foreach (var table in new[] { "OrganicSkills", "ProjectOrganicSkillLinks", "CouncilMemberOrganicSkillLinks" })
                {
                    var archive = await ArchiveMalformedIdentityTableAsync(connection, schema, table, cancellationToken).ConfigureAwait(false);
                    if (archive is not null) archives.Add((archive, table));
                }

                await ExecuteSqlAsync(connection, OrganicSkillTableRepairSql, cancellationToken).ConfigureAwait(false);
                var repairedSchema = await ReadSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
                await EnsureOrganicSkillColumnsAsync(connection, repairedSchema, cancellationToken).ConfigureAwait(false);
                foreach (var archive in archives)
                    await TryCopyCompatibilityRowsAsync(connection, archive.Archive, archive.Target, cancellationToken).ConfigureAwait(false);
                await ExecuteSqlAsync(connection, OrganicSkillIndexRepairSql, cancellationToken).ConfigureAwait(false);
                return true;
            }

            if (string.Equals(migrationId, "20260726150000_AddCouncilTeamScripting", StringComparison.Ordinal))
            {
                var archive = await ArchiveMalformedIdentityTableAsync(connection, schema, "CouncilTeamConfigurations", cancellationToken).ConfigureAwait(false);
                await ExecuteSqlAsync(connection, CouncilTeamTableRepairSql, cancellationToken).ConfigureAwait(false);
                var repairedSchema = await ReadSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
                await EnsureCouncilTeamColumnsAsync(connection, repairedSchema, cancellationToken).ConfigureAwait(false);
                if (archive is not null)
                    await TryCopyCompatibilityRowsAsync(connection, archive, "CouncilTeamConfigurations", cancellationToken).ConfigureAwait(false);
                await ExecuteSqlAsync(connection, CouncilTeamIndexRepairSql, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(TryRepairKnownMigrationAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(TryRepairKnownMigrationAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures organic skill columns as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="schema">String dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnsureOrganicSkillColumnsAsync(
        SqliteConnection connection,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        CancellationToken cancellationToken)
    {
    try
    {
            var organic = new (string Name, string Definition)[]
            {
                ("Key", "TEXT NOT NULL DEFAULT ''"), ("DisplayName", "TEXT NOT NULL DEFAULT ''"),
                ("Description", "TEXT NOT NULL DEFAULT ''"), ("SourcePeerId", "TEXT NOT NULL DEFAULT 'localgpt'"),
                ("OrgansJson", "TEXT NOT NULL DEFAULT '[]'"), ("CapabilityKeysJson", "TEXT NOT NULL DEFAULT '[]'"),
                ("UiActivationKeysJson", "TEXT NOT NULL DEFAULT '[]'"), ("IsOnline", "INTEGER NOT NULL DEFAULT 1"),
                ("IsEnabled", "INTEGER NOT NULL DEFAULT 1"), ("IsUserApproved", "INTEGER NOT NULL DEFAULT 0"),
                ("CreatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'"), ("UpdatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'")
            };
            foreach (var column in organic)
                await AddColumnIfMissingAsync(connection, schema, "OrganicSkills", column.Name, column.Definition, cancellationToken).ConfigureAwait(false);

            var projectLinks = new (string Name, string Definition)[]
            {
                ("ProjectId", "TEXT NOT NULL DEFAULT ''"), ("SkillId", "TEXT NOT NULL DEFAULT ''"),
                ("IsRequired", "INTEGER NOT NULL DEFAULT 1"), ("IsEnabled", "INTEGER NOT NULL DEFAULT 1"),
                ("Notes", "TEXT NOT NULL DEFAULT ''"), ("UpdatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'")
            };
            foreach (var column in projectLinks)
                await AddColumnIfMissingAsync(connection, schema, "ProjectOrganicSkillLinks", column.Name, column.Definition, cancellationToken).ConfigureAwait(false);

            var memberLinks = new (string Name, string Definition)[]
            {
                ("MemberKey", "TEXT NOT NULL DEFAULT ''"), ("SkillId", "TEXT NOT NULL DEFAULT ''"),
                ("Proficiency", "INTEGER NOT NULL DEFAULT 50"), ("IsSelfRevealed", "INTEGER NOT NULL DEFAULT 0"),
                ("IsEnabled", "INTEGER NOT NULL DEFAULT 0"), ("Evidence", "TEXT NOT NULL DEFAULT ''"),
                ("DxFunctionsJson", "TEXT NOT NULL DEFAULT '[]'"), ("ControllerMethodsJson", "TEXT NOT NULL DEFAULT '[]'"),
                ("OrganicCapabilitiesJson", "TEXT NOT NULL DEFAULT '[]'"), ("UpdatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'")
            };
            foreach (var column in memberLinks)
                await AddColumnIfMissingAsync(connection, schema, "CouncilMemberOrganicSkillLinks", column.Name, column.Definition, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EnsureOrganicSkillColumnsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EnsureOrganicSkillColumnsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures council team columns as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="schema">String dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnsureCouncilTeamColumnsAsync(
        SqliteConnection connection,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        CancellationToken cancellationToken)
    {
    try
    {
            var columns = new (string Name, string Definition)[]
            {
                ("Key", "TEXT NOT NULL DEFAULT ''"), ("DisplayName", "TEXT NOT NULL DEFAULT ''"),
                ("Purpose", "TEXT NOT NULL DEFAULT ''"), ("RolesJson", "TEXT NOT NULL DEFAULT '[]'"),
                ("PreferredCapabilitiesJson", "TEXT NOT NULL DEFAULT '[]'"), ("AllowedAutomaticFunctionsJson", "TEXT NOT NULL DEFAULT '[]'"),
                ("ArchitectureContractsJson", "TEXT NOT NULL DEFAULT '[]'"), ("WorkflowStepsJson", "TEXT NOT NULL DEFAULT '[]'"),
                ("ExpertPreparationPromptTemplate", "TEXT NOT NULL DEFAULT ''"), ("LeaderSynthesisPromptTemplate", "TEXT NOT NULL DEFAULT ''"),
                ("MainRoundInstructionTemplate", "TEXT NOT NULL DEFAULT ''"), ("SeedVersion", "INTEGER NOT NULL DEFAULT 1"),
                ("IsSystemSeed", "INTEGER NOT NULL DEFAULT 1"), ("IsUserModified", "INTEGER NOT NULL DEFAULT 0"),
                ("IsEnabled", "INTEGER NOT NULL DEFAULT 1"), ("IsDeleted", "INTEGER NOT NULL DEFAULT 0"),
                ("AllMembersReadinessPreflightMode", "INTEGER NOT NULL DEFAULT 0"),
                ("IncludeAllMembersReadinessPreflightInWorkflowContext", "INTEGER NOT NULL DEFAULT 0"),
                ("AllMembersReadinessPreflightMaxOutputTokens", "INTEGER NOT NULL DEFAULT 192"),
                ("AllMembersReadinessPreflightPromptTemplate", "TEXT NOT NULL DEFAULT ''"),
                ("CreatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'"), ("UpdatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'")
            };
            foreach (var column in columns)
                await AddColumnIfMissingAsync(connection, schema, "CouncilTeamConfigurations", column.Name, column.Definition, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EnsureCouncilTeamColumnsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EnsureCouncilTeamColumnsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs archive malformed identity table as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="schema">String dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <param name="table">Table value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string?> ArchiveMalformedIdentityTableAsync(
        SqliteConnection connection,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        string table,
        CancellationToken cancellationToken)
    {
    try
    {
            if (!schema.TryGetValue(table, out var columns) || columns.Contains("Id"))
                return null;

            var archive = $"{table}__compat_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}";
            await ExecuteSqlAsync(
                connection,
                $"ALTER TABLE {QuoteSqliteIdentifier(table)} RENAME TO {QuoteSqliteIdentifier(archive)};",
                cancellationToken).ConfigureAwait(false);
            logger.LogWarning(
                "Archived malformed partially-created table {Table} as {Archive}. Its rows remain untouched and a canonical table will be recreated.",
                table, archive);
            return archive;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ArchiveMalformedIdentityTableAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ArchiveMalformedIdentityTableAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to copy compatibility rows as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="archive">Archive value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task TryCopyCompatibilityRowsAsync(
        SqliteConnection connection,
        string archive,
        string target,
        CancellationToken cancellationToken)
    {
        var schema = await ReadSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
        if (!schema.TryGetValue(archive, out var sourceColumns) || !schema.TryGetValue(target, out var targetColumns))
            return;

        var common = sourceColumns
            .Where(column => !string.Equals(column, "Id", StringComparison.OrdinalIgnoreCase) && targetColumns.Contains(column))
            .OrderBy(column => column, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (common.Count == 0)
            return;

        var targetList = string.Join(", ", new[] { QuoteSqliteIdentifier("Id") }.Concat(common.Select(QuoteSqliteIdentifier)));
        var sourceList = string.Join(", ", new[] { SqliteGuidExpression }.Concat(common.Select(QuoteSqliteIdentifier)));
        try
        {
            await ExecuteSqlAsync(
                connection,
                $"INSERT OR IGNORE INTO {QuoteSqliteIdentifier(target)} ({targetList}) SELECT {sourceList} FROM {QuoteSqliteIdentifier(archive)};",
                cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Copied compatible columns from {Archive} into canonical table {Target}; the archive remains available for audit/recovery.", archive, target);
        }
        catch (SqliteException ex)
        {
            logger.LogWarning(ex, "Could not automatically copy every archived row from {Archive} into {Target}. The lossless archive remains in the database.", archive, target);
        }
    }

    /// <summary>
    /// Defines the sqlite GUID expression constant used by <see cref="DatabaseMigrationCompatibilityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string SqliteGuidExpression =
        "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1,1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6)))";

    /// <summary>
    /// Adds column if missing as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="schema">String dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <param name="table">Table value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="column">Column value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="definition">Definition value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task AddColumnIfMissingAsync(
        SqliteConnection connection,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        string table,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
    try
    {
            if (schema.TryGetValue(table, out var columns) && columns.Contains(column))
                return;
            await ExecuteSqlAsync(
                connection,
                $"ALTER TABLE {QuoteSqliteIdentifier(table)} ADD COLUMN {QuoteSqliteIdentifier(column)} {definition};",
                cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(AddColumnIfMissingAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(AddColumnIfMissingAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Executes SQL as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="sql">Sql value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ExecuteSqlAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
    try
    {
            var command = connection.CreateCommand();
            await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ExecuteSqlAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ExecuteSqlAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Defines the organic skill table repair SQL constant used by <see cref="DatabaseMigrationCompatibilityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string OrganicSkillTableRepairSql = """
    CREATE TABLE IF NOT EXISTS "OrganicSkills" (
        "Id" TEXT NOT NULL CONSTRAINT "PK_OrganicSkills" PRIMARY KEY, "Key" TEXT NOT NULL DEFAULT '',
        "DisplayName" TEXT NOT NULL DEFAULT '', "Description" TEXT NOT NULL DEFAULT '', "SourcePeerId" TEXT NOT NULL DEFAULT 'localgpt',
        "OrgansJson" TEXT NOT NULL DEFAULT '[]', "CapabilityKeysJson" TEXT NOT NULL DEFAULT '[]', "UiActivationKeysJson" TEXT NOT NULL DEFAULT '[]',
        "IsOnline" INTEGER NOT NULL DEFAULT 1, "IsEnabled" INTEGER NOT NULL DEFAULT 1, "IsUserApproved" INTEGER NOT NULL DEFAULT 0,
        "CreatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00', "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00');
    CREATE TABLE IF NOT EXISTS "ProjectOrganicSkillLinks" (
        "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectOrganicSkillLinks" PRIMARY KEY, "ProjectId" TEXT NOT NULL DEFAULT '',
        "SkillId" TEXT NOT NULL DEFAULT '', "IsRequired" INTEGER NOT NULL DEFAULT 1, "IsEnabled" INTEGER NOT NULL DEFAULT 1,
        "Notes" TEXT NOT NULL DEFAULT '', "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00',
        CONSTRAINT "FK_ProjectOrganicSkillLinks_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ProjectOrganicSkillLinks_OrganicSkills_SkillId" FOREIGN KEY ("SkillId") REFERENCES "OrganicSkills" ("Id") ON DELETE CASCADE);
    CREATE TABLE IF NOT EXISTS "CouncilMemberOrganicSkillLinks" (
        "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilMemberOrganicSkillLinks" PRIMARY KEY, "MemberKey" TEXT NOT NULL DEFAULT '',
        "SkillId" TEXT NOT NULL DEFAULT '', "Proficiency" INTEGER NOT NULL DEFAULT 50, "IsSelfRevealed" INTEGER NOT NULL DEFAULT 0,
        "IsEnabled" INTEGER NOT NULL DEFAULT 0, "Evidence" TEXT NOT NULL DEFAULT '', "DxFunctionsJson" TEXT NOT NULL DEFAULT '[]',
        "ControllerMethodsJson" TEXT NOT NULL DEFAULT '[]', "OrganicCapabilitiesJson" TEXT NOT NULL DEFAULT '[]',
        "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00',
        CONSTRAINT "FK_CouncilMemberOrganicSkillLinks_OrganicSkills_SkillId" FOREIGN KEY ("SkillId") REFERENCES "OrganicSkills" ("Id") ON DELETE CASCADE);
    """;

    /// <summary>
    /// Defines the organic skill index repair SQL constant used by <see cref="DatabaseMigrationCompatibilityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string OrganicSkillIndexRepairSql = """
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_OrganicSkills_Key" ON "OrganicSkills" ("Key");
    CREATE INDEX IF NOT EXISTS "IX_OrganicSkills_IsEnabled_IsOnline_UpdatedAtUtc" ON "OrganicSkills" ("IsEnabled", "IsOnline", "UpdatedAtUtc");
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_ProjectId_SkillId" ON "ProjectOrganicSkillLinks" ("ProjectId", "SkillId");
    CREATE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_ProjectId_IsEnabled_IsRequired" ON "ProjectOrganicSkillLinks" ("ProjectId", "IsEnabled", "IsRequired");
    CREATE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_SkillId" ON "ProjectOrganicSkillLinks" ("SkillId");
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_MemberKey_SkillId" ON "CouncilMemberOrganicSkillLinks" ("MemberKey", "SkillId");
    CREATE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_MemberKey_IsEnabled_Proficiency" ON "CouncilMemberOrganicSkillLinks" ("MemberKey", "IsEnabled", "Proficiency");
    CREATE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_SkillId" ON "CouncilMemberOrganicSkillLinks" ("SkillId");
    """;

    /// <summary>
    /// Defines the council team table repair SQL constant used by <see cref="DatabaseMigrationCompatibilityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CouncilTeamTableRepairSql = """
    CREATE TABLE IF NOT EXISTS "CouncilTeamConfigurations" (
        "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilTeamConfigurations" PRIMARY KEY, "Key" TEXT NOT NULL DEFAULT '',
        "DisplayName" TEXT NOT NULL DEFAULT '', "Purpose" TEXT NOT NULL DEFAULT '', "RolesJson" TEXT NOT NULL DEFAULT '[]',
        "PreferredCapabilitiesJson" TEXT NOT NULL DEFAULT '[]', "AllowedAutomaticFunctionsJson" TEXT NOT NULL DEFAULT '[]',
        "ArchitectureContractsJson" TEXT NOT NULL DEFAULT '[]', "WorkflowStepsJson" TEXT NOT NULL DEFAULT '[]',
        "ExpertPreparationPromptTemplate" TEXT NOT NULL DEFAULT '', "LeaderSynthesisPromptTemplate" TEXT NOT NULL DEFAULT '',
        "MainRoundInstructionTemplate" TEXT NOT NULL DEFAULT '', "SeedVersion" INTEGER NOT NULL DEFAULT 1,
        "IsSystemSeed" INTEGER NOT NULL DEFAULT 1, "IsUserModified" INTEGER NOT NULL DEFAULT 0,
        "IsEnabled" INTEGER NOT NULL DEFAULT 1, "IsDeleted" INTEGER NOT NULL DEFAULT 0,
        "AllMembersReadinessPreflightMode" INTEGER NOT NULL DEFAULT 0,
        "IncludeAllMembersReadinessPreflightInWorkflowContext" INTEGER NOT NULL DEFAULT 0,
        "AllMembersReadinessPreflightMaxOutputTokens" INTEGER NOT NULL DEFAULT 192,
        "AllMembersReadinessPreflightPromptTemplate" TEXT NOT NULL DEFAULT '',
        "CreatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00', "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00');
    """;

    /// <summary>
    /// Defines the council team index repair SQL constant used by <see cref="DatabaseMigrationCompatibilityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CouncilTeamIndexRepairSql = """
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilTeamConfigurations_Key" ON "CouncilTeamConfigurations" ("Key");
    CREATE INDEX IF NOT EXISTS "IX_CouncilTeamConfigurations_IsEnabled_UpdatedAtUtc" ON "CouncilTeamConfigurations" ("IsEnabled", "UpdatedAtUtc");
    """;

    /// <summary>
    /// Determines whether application table as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="tableName">Table name value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsApplicationTable(string tableName) {
    try
    {
        return !tableName.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase) &&
        !tableName.StartsWith("__EF", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(tableName, "__LocalGptIntegrityProbe", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(IsApplicationTable)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(IsApplicationTable)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs evaluate signature as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="signature">Signature value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="schema">String dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <returns>The database migration signature state produced by the operation.</returns>
    private DatabaseMigrationSignatureState EvaluateSignature(
        DatabaseMigrationSignature signature,
        IReadOnlyDictionary<string, HashSet<string>> schema)
    {
    try
    {
            var presentCount = signature.Requirements.Count(requirement => RequirementExists(requirement, schema));
            if (presentCount == 0)
                return DatabaseMigrationSignatureState.Missing;
            return presentCount == signature.Requirements.Length
                ? DatabaseMigrationSignatureState.Complete
                : DatabaseMigrationSignatureState.Partial;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EvaluateSignature)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EvaluateSignature)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs requirement exists as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requirement">Requirement value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="schema">String dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool RequirementExists(
        DatabaseSchemaRequirement requirement,
        IReadOnlyDictionary<string, HashSet<string>> schema)
    {
    try
    {
            if (!schema.TryGetValue(requirement.TableName, out var columns))
                return false;
            return requirement.ColumnName is null || columns.Contains(requirement.ColumnName);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(RequirementExists)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(RequirementExists)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether supported application logs bootstrap as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="signature">Signature value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="schema">String dependency used by the database migration compatibility workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsSupportedApplicationLogsBootstrap(
        DatabaseMigrationSignature signature,
        IReadOnlyDictionary<string, HashSet<string>> schema)
    {
    try
    {
            if (!string.Equals(signature.Id, "20260616222639_Initial", StringComparison.Ordinal))
                return false;

            var applicationLogRequirements = signature.Requirements
                .Where(requirement => string.Equals(requirement.TableName, "ApplicationLogs", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (!applicationLogRequirements.All(requirement => RequirementExists(requirement, schema)))
                return false;

            return signature.Requirements
                .Where(requirement => !string.Equals(requirement.TableName, "ApplicationLogs", StringComparison.OrdinalIgnoreCase))
                .All(requirement => !RequirementExists(requirement, schema));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(IsSupportedApplicationLogsBootstrap)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(IsSupportedApplicationLogsBootstrap)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates compatibility backup as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourceConnection">Source connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> CreateCompatibilityBackupAsync(
        SqliteConnection sourceConnection,
        CancellationToken cancellationToken)
    {
    try
    {
            var databasePath = databaseFileHealth.DatabasePath;
            var parent = Path.GetDirectoryName(databasePath)
                ?? throw new InvalidOperationException("The LocalGPT database path has no parent directory.");
            var backupDirectory = Path.Combine(
                parent,
                "CompatibilityBackups",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(backupDirectory);
            var backupPath = Path.Combine(backupDirectory, Path.GetFileName(databasePath));

            var destinationConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = backupPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Private
            }.ToString();
            var destinationConnection = new SqliteConnection(destinationConnectionString);
            await using var configuredDestinationConnectionAsyncDisposal = destinationConnection.ConfigureAwait(false);
            await destinationConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            sourceConnection.BackupDatabase(destinationConnection);
            return backupPath;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(CreateCompatibilityBackupAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(CreateCompatibilityBackupAsync)} failed.");
        throw;
    }
}


    /// <summary>
    /// Performs clear abandoned migration lock as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ClearAbandonedMigrationLockAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
    try
    {
            var tableCommand = connection.CreateCommand();
            await using var configuredTableCommandAsyncDisposal = tableCommand.ConfigureAwait(false);
            tableCommand.CommandText =
                """
                SELECT COUNT(*) FROM "sqlite_master"
                WHERE "type" = 'table' AND "name" = '__EFMigrationsLock';
                """;
            var tableExists = Convert.ToInt32(
                await tableCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0;
            if (!tableExists)
                return;

            var readCommand = connection.CreateCommand();
            await using var configuredReadCommandAsyncDisposal = readCommand.ConfigureAwait(false);
            readCommand.CommandText =
                """
                SELECT "Timestamp" FROM "__EFMigrationsLock" WHERE "Id" = 1 LIMIT 1;
                """;
            var timestampText = Convert.ToString(
                await readCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            if (string.IsNullOrWhiteSpace(timestampText))
                return;

            if (!DateTimeOffset.TryParse(timestampText, out var acquiredAtUtc))
            {
                throw new InvalidOperationException(
                    "The SQLite migration lock contains an unreadable timestamp. Close every LocalGPT instance and " +
                    "remove the __EFMigrationsLock row manually before retrying.");
            }

            var age = DateTimeOffset.UtcNow - acquiredAtUtc.ToUniversalTime();
            if (age < abandonedMigrationLockAge)
            {
                throw new InvalidOperationException(
                    $"A SQLite migration lock acquired at {acquiredAtUtc:O} is still present. " +
                    "Another LocalGPT instance may be migrating this database. Close other instances or retry after " +
                    $"the lock is older than {abandonedMigrationLockAge.TotalMinutes:0} minutes.");
            }

            var clearCommand = connection.CreateCommand();
            await using var configuredClearCommandAsyncDisposal = clearCommand.ConfigureAwait(false);
            clearCommand.CommandText = "DELETE FROM \"__EFMigrationsLock\";";
            await clearCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning(
                "Cleared an abandoned SQLite migration lock acquired at {AcquiredAtUtc}; lock age was {LockAge}.",
                acquiredAtUtc,
                age);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ClearAbandonedMigrationLockAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ClearAbandonedMigrationLockAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures migration history table as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnsureMigrationHistoryTableAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
    try
    {
            var command = connection.CreateCommand();
            await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EnsureMigrationHistoryTableAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EnsureMigrationHistoryTableAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads applied migrations as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The hash set string produced by the operation.</returns>
    private async Task<HashSet<string>> ReadAppliedMigrationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
    try
    {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var command = connection.CreateCommand();
            await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
            command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";";
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredReaderAsyncDisposal = reader.ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                result.Add(reader.GetString(0));
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ReadAppliedMigrationsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ReadAppliedMigrationsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads schema as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The dictionary string hash set string produced by the operation.</returns>
    private async Task<Dictionary<string, HashSet<string>>> ReadSchemaAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
    try
    {
            var schema = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var tableNames = new List<string>();

            var tableCommand = connection.CreateCommand();
            await using (tableCommand.ConfigureAwait(false))
            {
                tableCommand.CommandText =
                    "SELECT \"name\" FROM \"sqlite_master\" WHERE \"type\" = 'table' ORDER BY \"name\";";
                var reader = await tableCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredReaderAsyncDisposal = reader.ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    tableNames.Add(reader.GetString(0));
            }

            foreach (var tableName in tableNames)
            {
                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var columnCommand = connection.CreateCommand();
                await using var configuredColumnCommandAsyncDisposal = columnCommand.ConfigureAwait(false);
                columnCommand.CommandText = $"PRAGMA table_info({QuoteSqliteIdentifier(tableName)});";
                var reader = await columnCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredReaderAsyncDisposal = reader.ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    columns.Add(reader.GetString(1));
                schema[tableName] = columns;
            }

            return schema;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ReadSchemaAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ReadSchemaAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs insert migration history as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="signature">Signature value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task InsertMigrationHistoryAsync(
        SqliteConnection connection,
        DatabaseMigrationSignature signature,
        CancellationToken cancellationToken)
    {
    try
    {
            var command = connection.CreateCommand();
            await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
            command.CommandText =
                "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                "VALUES ($migrationId, $productVersion);";
            command.Parameters.AddWithValue("$migrationId", signature.Id);
            command.Parameters.AddWithValue("$productVersion", signature.ProductVersion);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(InsertMigrationHistoryAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(InsertMigrationHistoryAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs quote sqlite identifier as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="identifier">Identifier value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string QuoteSqliteIdentifier(string identifier) {
    try
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(QuoteSqliteIdentifier)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(QuoteSqliteIdentifier)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs table as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="tableName">Table name value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <returns>The database schema requirement produced by the operation.</returns>
    private DatabaseSchemaRequirement Table(string tableName) {
    try
    {
        return new(tableName, null);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(Table)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(Table)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs column as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="tableName">Table name value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <param name="columnName">Column name value supplied to the database migration compatibility operation and used when producing its result.</param>
    /// <returns>The database schema requirement produced by the operation.</returns>
    private DatabaseSchemaRequirement Column(string tableName, string columnName) {
    try
    {
        return new(tableName, columnName);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(Column)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(Column)} failed.");
        throw;
    }
}
}
