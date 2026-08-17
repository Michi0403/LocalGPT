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

}
