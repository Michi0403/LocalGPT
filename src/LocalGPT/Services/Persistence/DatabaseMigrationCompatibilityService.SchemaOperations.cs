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

}
