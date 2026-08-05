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
    private readonly IDatabaseFileHealthService databaseFileHealth;
    private readonly IServiceActivityService serviceActivity;
    private readonly ILogger<DatabaseMigrationCompatibilityService> logger;
    private readonly TimeSpan abandonedMigrationLockAge = TimeSpan.FromMinutes(10);
    private readonly DatabaseMigrationSignature[] legacyMigrationSignatures;

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

    public Task PrepareAsync(CancellationToken cancellationToken = default) =>
        serviceActivity.RunAsync(
            nameof(DatabaseMigrationCompatibilityService),
            nameof(PrepareAsync),
            PrepareCoreAsync,
            cancellationToken,
            "Legacy migration compatibility inspection completed.");

    private DatabaseMigrationSignature[] CreateLegacyMigrationSignatures() =>
    [
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

    private async Task PrepareCoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(databaseFileHealth.DatabasePath))
            return;

        var sourceConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFileHealth.DatabasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private
        }.ToString();
        await using var connection = new SqliteConnection(sourceConnectionString);
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

    private async Task<bool> TryRepairKnownMigrationAsync(
        SqliteConnection connection,
        string migrationId,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        CancellationToken cancellationToken)
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

    private async Task EnsureOrganicSkillColumnsAsync(
        SqliteConnection connection,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        CancellationToken cancellationToken)
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

    private async Task EnsureCouncilTeamColumnsAsync(
        SqliteConnection connection,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        CancellationToken cancellationToken)
    {
        var columns = new (string Name, string Definition)[]
        {
            ("Key", "TEXT NOT NULL DEFAULT ''"), ("DisplayName", "TEXT NOT NULL DEFAULT ''"),
            ("Purpose", "TEXT NOT NULL DEFAULT ''"), ("RolesJson", "TEXT NOT NULL DEFAULT '[]'"),
            ("PreferredCapabilitiesJson", "TEXT NOT NULL DEFAULT '[]'"), ("ArchitectureContractsJson", "TEXT NOT NULL DEFAULT '[]'"),
            ("WorkflowStepsJson", "TEXT NOT NULL DEFAULT '[]'"), ("ExpertPreparationPromptTemplate", "TEXT NOT NULL DEFAULT ''"),
            ("LeaderSynthesisPromptTemplate", "TEXT NOT NULL DEFAULT ''"), ("MainRoundInstructionTemplate", "TEXT NOT NULL DEFAULT ''"),
            ("SeedVersion", "INTEGER NOT NULL DEFAULT 1"), ("IsSystemSeed", "INTEGER NOT NULL DEFAULT 1"),
            ("IsUserModified", "INTEGER NOT NULL DEFAULT 0"), ("IsEnabled", "INTEGER NOT NULL DEFAULT 1"),
            ("CreatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'"), ("UpdatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'")
        };
        foreach (var column in columns)
            await AddColumnIfMissingAsync(connection, schema, "CouncilTeamConfigurations", column.Name, column.Definition, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> ArchiveMalformedIdentityTableAsync(
        SqliteConnection connection,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        string table,
        CancellationToken cancellationToken)
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

    private const string SqliteGuidExpression =
        "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1,1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6)))";

    private async Task AddColumnIfMissingAsync(
        SqliteConnection connection,
        IReadOnlyDictionary<string, HashSet<string>> schema,
        string table,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
        if (schema.TryGetValue(table, out var columns) && columns.Contains(column))
            return;
        await ExecuteSqlAsync(
            connection,
            $"ALTER TABLE {QuoteSqliteIdentifier(table)} ADD COLUMN {QuoteSqliteIdentifier(column)} {definition};",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteSqlAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

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

    private const string CouncilTeamTableRepairSql = """
    CREATE TABLE IF NOT EXISTS "CouncilTeamConfigurations" (
        "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilTeamConfigurations" PRIMARY KEY, "Key" TEXT NOT NULL DEFAULT '',
        "DisplayName" TEXT NOT NULL DEFAULT '', "Purpose" TEXT NOT NULL DEFAULT '', "RolesJson" TEXT NOT NULL DEFAULT '[]',
        "PreferredCapabilitiesJson" TEXT NOT NULL DEFAULT '[]', "ArchitectureContractsJson" TEXT NOT NULL DEFAULT '[]',
        "WorkflowStepsJson" TEXT NOT NULL DEFAULT '[]', "ExpertPreparationPromptTemplate" TEXT NOT NULL DEFAULT '',
        "LeaderSynthesisPromptTemplate" TEXT NOT NULL DEFAULT '', "MainRoundInstructionTemplate" TEXT NOT NULL DEFAULT '',
        "SeedVersion" INTEGER NOT NULL DEFAULT 1, "IsSystemSeed" INTEGER NOT NULL DEFAULT 1,
        "IsUserModified" INTEGER NOT NULL DEFAULT 0, "IsEnabled" INTEGER NOT NULL DEFAULT 1,
        "CreatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00', "UpdatedAtUtc" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00');
    """;

    private const string CouncilTeamIndexRepairSql = """
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilTeamConfigurations_Key" ON "CouncilTeamConfigurations" ("Key");
    CREATE INDEX IF NOT EXISTS "IX_CouncilTeamConfigurations_IsEnabled_UpdatedAtUtc" ON "CouncilTeamConfigurations" ("IsEnabled", "UpdatedAtUtc");
    """;

    private bool IsApplicationTable(string tableName) =>
        !tableName.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase) &&
        !tableName.StartsWith("__EF", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(tableName, "__LocalGptIntegrityProbe", StringComparison.OrdinalIgnoreCase);

    private DatabaseMigrationSignatureState EvaluateSignature(
        DatabaseMigrationSignature signature,
        IReadOnlyDictionary<string, HashSet<string>> schema)
    {
        var presentCount = signature.Requirements.Count(requirement => RequirementExists(requirement, schema));
        if (presentCount == 0)
            return DatabaseMigrationSignatureState.Missing;
        return presentCount == signature.Requirements.Length
            ? DatabaseMigrationSignatureState.Complete
            : DatabaseMigrationSignatureState.Partial;
    }

    private bool RequirementExists(
        DatabaseSchemaRequirement requirement,
        IReadOnlyDictionary<string, HashSet<string>> schema)
    {
        if (!schema.TryGetValue(requirement.TableName, out var columns))
            return false;
        return requirement.ColumnName is null || columns.Contains(requirement.ColumnName);
    }

    private bool IsSupportedApplicationLogsBootstrap(
        DatabaseMigrationSignature signature,
        IReadOnlyDictionary<string, HashSet<string>> schema)
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

    private async Task<string> CreateCompatibilityBackupAsync(
        SqliteConnection sourceConnection,
        CancellationToken cancellationToken)
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
        await using var destinationConnection = new SqliteConnection(destinationConnectionString);
        await destinationConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        sourceConnection.BackupDatabase(destinationConnection);
        return backupPath;
    }


    private async Task ClearAbandonedMigrationLockAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText =
            """
            SELECT COUNT(*) FROM "sqlite_master"
            WHERE "type" = 'table' AND "name" = '__EFMigrationsLock';
            """;
        var tableExists = Convert.ToInt32(
            await tableCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0;
        if (!tableExists)
            return;

        await using var readCommand = connection.CreateCommand();
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

        await using var clearCommand = connection.CreateCommand();
        clearCommand.CommandText = "DELETE FROM \"__EFMigrationsLock\";";
        await clearCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        logger.LogWarning(
            "Cleared an abandoned SQLite migration lock acquired at {AcquiredAtUtc}; lock age was {LockAge}.",
            acquiredAtUtc,
            age);
    }

    private async Task EnsureMigrationHistoryTableAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<HashSet<string>> ReadAppliedMigrationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            result.Add(reader.GetString(0));
        return result;
    }

    private async Task<Dictionary<string, HashSet<string>>> ReadSchemaAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var schema = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var tableNames = new List<string>();

        await using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.CommandText =
                "SELECT \"name\" FROM \"sqlite_master\" WHERE \"type\" = 'table' ORDER BY \"name\";";
            await using var reader = await tableCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                tableNames.Add(reader.GetString(0));
        }

        foreach (var tableName in tableNames)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var columnCommand = connection.CreateCommand();
            columnCommand.CommandText = $"PRAGMA table_info({QuoteSqliteIdentifier(tableName)});";
            await using var reader = await columnCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                columns.Add(reader.GetString(1));
            schema[tableName] = columns;
        }

        return schema;
    }

    private async Task InsertMigrationHistoryAsync(
        SqliteConnection connection,
        DatabaseMigrationSignature signature,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
            "VALUES ($migrationId, $productVersion);";
        command.Parameters.AddWithValue("$migrationId", signature.Id);
        command.Parameters.AddWithValue("$productVersion", signature.ProductVersion);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private string QuoteSqliteIdentifier(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private DatabaseSchemaRequirement Table(string tableName) => new(tableName, null);
    private DatabaseSchemaRequirement Column(string tableName, string columnName) => new(tableName, columnName);
}
