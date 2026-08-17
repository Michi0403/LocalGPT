using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates database migration compatibility behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class DatabaseMigrationCompatibilityService
{
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

}
