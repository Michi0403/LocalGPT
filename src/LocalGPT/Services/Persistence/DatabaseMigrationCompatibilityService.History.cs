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
