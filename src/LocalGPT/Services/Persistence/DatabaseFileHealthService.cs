using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Performs bounded SQLite integrity checks and preserves confirmed malformed files before recreation.
/// It deliberately does not treat locks, permission errors, or migration differences as corruption.
/// </summary>
public sealed class DatabaseFileHealthService(
    LocalGptDatabaseOptions options,
    ILogger<DatabaseFileHealthService> logger) : IDatabaseFileHealthService
{
    private readonly string[] DatabaseSuffixes = [string.Empty, "-wal", "-shm"];

    public string DatabasePath => options.DatabasePath;

    public async Task EnsureHealthyOrRecoverAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(DatabasePath);
            var directory = Path.GetDirectoryName(DatabasePath)
                ?? throw new InvalidOperationException("The LocalGPT database path has no parent directory.");
            Directory.CreateDirectory(directory);

            if (File.Exists(GetRecoveryMarkerPath()))
            {
                logger.LogWarning(
                    "A pending SQLite recovery marker exists for {DatabasePath}; preserving the current files before database initialization.",
                    DatabasePath);
                await RecoverMalformedDatabaseAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!File.Exists(DatabasePath))
            {
                if (DatabaseSuffixes.Skip(1).Any(suffix => File.Exists(DatabasePath + suffix)))
                {
                    logger.LogWarning(
                        "SQLite sidecars exist without the base database at {DatabasePath}; preserving the orphan files.",
                        DatabasePath);
                    await RecoverMalformedDatabaseAsync(cancellationToken).ConfigureAwait(false);
                }
                return;
            }

            var quickCheck = await RunQuickCheckAsync(cancellationToken).ConfigureAwait(false);
            if (quickCheck == DatabaseProbeResult.Corrupt)
            {
                await RecoverMalformedDatabaseAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            if (quickCheck == DatabaseProbeResult.Inconclusive)
                return;

            var writeProbe = await RunWriteProbeAsync(cancellationToken).ConfigureAwait(false);
            if (writeProbe == DatabaseProbeResult.Corrupt)
                await RecoverMalformedDatabaseAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(EnsureHealthyOrRecoverAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(EnsureHealthyOrRecoverAsync)} failed.");
        throw;
    }
}

    public bool IsSqliteCorruption(Exception exception)
    {
    try
    {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqliteException sqlite &&
                    (sqlite.SqliteErrorCode is 11 or 26 ||
                     ContainsCorruptionText(sqlite.Message)))
                {
                    return true;
                }

                if (ContainsCorruptionText(current.Message))
                    return true;
            }

            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(IsSqliteCorruption)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(IsSqliteCorruption)} failed.");
        throw;
    }
}

    public async Task RecoverMalformedDatabaseAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            WriteRecoveryMarker();

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var parent = Path.GetDirectoryName(DatabasePath)
                ?? throw new InvalidOperationException("The LocalGPT database path has no parent directory.");
            var backupDirectory = Path.Combine(parent, "CorruptDatabaseBackups", timestamp);
            Directory.CreateDirectory(backupDirectory);

            foreach (var suffix in DatabaseSuffixes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = DatabasePath + suffix;
                if (!File.Exists(sourcePath))
                    continue;

                var backupPath = Path.Combine(backupDirectory, Path.GetFileName(sourcePath));
                if (TryMoveToBackup(sourcePath, backupPath))
                    continue;

                TryQuarantineOrDelete(sourcePath, $"{sourcePath}.malformed-{timestamp}");
            }

            logger.LogWarning(
                "Preserved confirmed malformed SQLite files in {BackupDirectory}. A clean database will be created during initialization.",
                backupDirectory);

            if (!DatabaseSuffixes.Any(suffix => File.Exists(DatabasePath + suffix)))
                TryDeleteRecoveryMarker();

            await Task.CompletedTask.ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(RecoverMalformedDatabaseAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(RecoverMalformedDatabaseAsync)} failed.");
        throw;
    }
}

    private async Task<DatabaseProbeResult> RunQuickCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={DatabasePath};Mode=ReadOnly;Cache=Private");
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = options.ProbeCommandTimeoutSeconds;
            command.CommandText = "PRAGMA quick_check;";
            var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) ?? string.Empty;
            if (string.Equals(result.Trim(), "ok", StringComparison.OrdinalIgnoreCase))
                return DatabaseProbeResult.Healthy;

            logger.LogWarning("SQLite quick_check reported '{Result}' for {DatabasePath}.", result, DatabasePath);
            return DatabaseProbeResult.Corrupt;
        }
        catch (Exception ex) when (IsSqliteCorruption(ex))
        {
            logger.LogWarning(ex, "SQLite quick_check confirmed corruption in {DatabasePath}.", DatabasePath);
            return DatabaseProbeResult.Corrupt;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "SQLite quick_check was inconclusive for {DatabasePath}; the file will not be replaced without confirmed corruption.",
                DatabasePath);
            return DatabaseProbeResult.Inconclusive;
        }
    }

    private async Task<DatabaseProbeResult> RunWriteProbeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={DatabasePath};Mode=ReadWrite;Cache=Private");
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = options.ProbeCommandTimeoutSeconds;
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS "__LocalGptIntegrityProbe" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK___LocalGptIntegrityProbe" PRIMARY KEY AUTOINCREMENT,
                    "CheckedAtUtc" TEXT NOT NULL
                );
                INSERT INTO "__LocalGptIntegrityProbe" ("CheckedAtUtc") VALUES (datetime('now'));
                DELETE FROM "__LocalGptIntegrityProbe";
                """;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return DatabaseProbeResult.Healthy;
        }
        catch (Exception ex) when (IsSqliteCorruption(ex))
        {
            logger.LogWarning(ex, "SQLite write probe confirmed corruption in {DatabasePath}.", DatabasePath);
            return DatabaseProbeResult.Corrupt;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "SQLite write probe was inconclusive for {DatabasePath}; the file will not be replaced without confirmed corruption.",
                DatabasePath);
            return DatabaseProbeResult.Inconclusive;
        }
    }

    private bool ContainsCorruptionText(string? message) {
    try
    {
        return !string.IsNullOrWhiteSpace(message) &&
        (message.Contains("database disk image is malformed", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("file is not a database", StringComparison.OrdinalIgnoreCase));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(ContainsCorruptionText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(ContainsCorruptionText)} failed.");
        throw;
    }
}

    private bool TryMoveToBackup(string sourcePath, string backupPath)
    {
        try
        {
            File.Move(sourcePath, backupPath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not move SQLite file {SourcePath} to {BackupPath}.", sourcePath, backupPath);
            return false;
        }
    }

    private void TryQuarantineOrDelete(string sourcePath, string fallbackPath)
    {
        try
        {
            File.Move(sourcePath, fallbackPath, overwrite: true);
        }
        catch (Exception moveException) when (moveException is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(moveException, "Could not quarantine SQLite file {SourcePath}; attempting deletion.", sourcePath);
            try
            {
                File.Delete(sourcePath);
            }
            catch (Exception deleteException) when (deleteException is IOException or UnauthorizedAccessException)
            {
                logger.LogError(deleteException, "Could not remove SQLite file {SourcePath}.", sourcePath);
            }
        }
    }

    private string GetRecoveryMarkerPath() {
    try
    {
        return $"{DatabasePath}.recover";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(GetRecoveryMarkerPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseFileHealthService)}.{nameof(GetRecoveryMarkerPath)} failed.");
        throw;
    }
}

    private void WriteRecoveryMarker()
    {
        try
        {
            File.WriteAllText(GetRecoveryMarkerPath(), $"SQLite recovery requested at {DateTimeOffset.UtcNow:O}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not write the SQLite recovery marker for {DatabasePath}.", DatabasePath);
        }
    }

    private void TryDeleteRecoveryMarker()
    {
        try
        {
            File.Delete(GetRecoveryMarkerPath());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not delete the SQLite recovery marker for {DatabasePath}.", DatabasePath);
        }
    }
}
