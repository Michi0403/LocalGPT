using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Data
{
    public static class LocalGptDatabaseRecovery
    {
        private const int ProbeCommandTimeoutSeconds = 5;
        private static readonly string[] SidecarSuffixes = ["", "-wal", "-shm"];

        public static async Task EnsureHealthyOrRecoverAsync(
            string databasePath,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            if (File.Exists(GetRecoveryMarkerPath(databasePath)))
            {
                logger.LogWarning(
                    "LocalGPT found a pending SQLite recovery marker for {DatabasePath}. Backing up and recreating the local store before opening SQLite.",
                    databasePath);

                await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken);
                return;
            }

            if (!File.Exists(databasePath))
            {
                if (HasSidecars(databasePath))
                {
                    logger.LogWarning(
                        "LocalGPT found SQLite WAL/SHM sidecar files without the base database at {DatabasePath}. Backing up and removing orphan sidecars.",
                        databasePath);

                    await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken);
                }

                return;
            }

            var quickCheck = await TryRunQuickCheckAsync(databasePath, cancellationToken);
            if (!quickCheck.IsHealthy)
            {
                logger.LogWarning(
                    "LocalGPT SQLite database quick_check failed with '{QuickCheckResult}'. Backing up and recreating {DatabasePath}.",
                    quickCheck.Result,
                    databasePath);

                await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken);
                return;
            }

            var writeProbe = await TryRunWriteProbeAsync(databasePath, cancellationToken);
            if (writeProbe.IsHealthy)
                return;

            logger.LogWarning(
                "LocalGPT SQLite database write probe failed with '{WriteProbeResult}'. Backing up and recreating {DatabasePath}.",
                writeProbe.Result,
                databasePath);

            await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken);
        }

        public static Task RecoverMalformedDatabaseAsync(
            string databasePath,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            WriteRecoveryMarker(databasePath, logger);
            return BackupAndRemoveDatabaseAsync(databasePath, logger, cancellationToken);
        }

        public static bool IsSqliteCorruption(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqliteException sqlite &&
                    (sqlite.SqliteErrorCode is 11 or 26 ||
                     sqlite.Message.Contains("malformed", StringComparison.OrdinalIgnoreCase) ||
                     sqlite.Message.Contains("file is not a database", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                if (current.Message.Contains("database disk image is malformed", StringComparison.OrdinalIgnoreCase) ||
                    current.Message.Contains("file is not a database", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<(bool IsHealthy, string Result)> TryRunQuickCheckAsync(
            string databasePath,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Cache=Private");
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandTimeout = ProbeCommandTimeoutSeconds;
                command.CommandText = "PRAGMA quick_check;";
                var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
                return (string.Equals(result.Trim(), "ok", StringComparison.OrdinalIgnoreCase), result);
            }
            catch (Exception ex) when (IsSqliteCorruption(ex))
            {
                return (false, ex.Message);
            }
        }

        private static async Task<(bool IsHealthy, string Result)> TryFindCorruptionLogEvidenceAsync(
            string databasePath,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Cache=Private");
                await connection.OpenAsync(cancellationToken);

                if (!await HasTableAsync(connection, "ApplicationLogs", cancellationToken))
                    return (true, "ApplicationLogs table not present");

                await using var command = connection.CreateCommand();
                command.CommandTimeout = ProbeCommandTimeoutSeconds;
                command.CommandText =
                    """
                    SELECT COUNT(*)
                    FROM "ApplicationLogs"
                    WHERE "TimestampUtc" >= datetime('now', '-7 days')
                      AND (
                        "Message" LIKE '%database disk image is malformed%' OR
                        "Exception" LIKE '%database disk image is malformed%' OR
                        "Exception" LIKE '%SQLite Error 11%'
                      );
                    """;
                var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
                return count > 0
                    ? (false, $"{count} recent SQLite corruption log entr{(count == 1 ? "y" : "ies")}")
                    : (true, "ok");
            }
            catch (Exception ex) when (IsSqliteCorruption(ex))
            {
                return (false, ex.Message);
            }
        }

        private static async Task<(bool IsHealthy, string Result)> TryRunWriteProbeAsync(
            string databasePath,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadWrite;Cache=Private");
                await connection.OpenAsync(cancellationToken);
                await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

                await ExecuteNonQueryAsync(
                    connection,
                    transaction,
                    """
                    CREATE TABLE IF NOT EXISTS "__LocalGptIntegrityProbe" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK___LocalGptIntegrityProbe" PRIMARY KEY AUTOINCREMENT,
                        "CheckedAtUtc" TEXT NOT NULL
                    );
                    INSERT INTO "__LocalGptIntegrityProbe" ("CheckedAtUtc") VALUES (datetime('now'));
                    DELETE FROM "__LocalGptIntegrityProbe";
                    """,
                    cancellationToken);

                if (await HasColumnAsync(connection, transaction, "CouncilKnowledgeEntries", "LastUsedAtUtc", cancellationToken))
                {
                    await ExecuteNonQueryAsync(
                        connection,
                        transaction,
                        """
                        UPDATE "CouncilKnowledgeEntries"
                        SET "LastUsedAtUtc" = datetime('now')
                        WHERE "Id" IN (SELECT "Id" FROM "CouncilKnowledgeEntries" LIMIT 1);
                        """,
                        cancellationToken);
                }

                await transaction.RollbackAsync(cancellationToken);
                return (true, "ok");
            }
            catch (Exception ex) when (IsSqliteCorruption(ex))
            {
                return (false, ex.Message);
            }
        }

        private static async Task<bool> HasColumnAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            string columnName,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = ProbeCommandTimeoutSeconds;
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static async Task<bool> HasTableAsync(
            SqliteConnection connection,
            string tableName,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = ProbeCommandTimeoutSeconds;
            command.CommandText =
                """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = $tableName;
                """;
            command.Parameters.AddWithValue("$tableName", tableName);
            var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }

        private static async Task ExecuteNonQueryAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string sql,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = ProbeCommandTimeoutSeconds;
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static Task BackupAndRemoveDatabaseAsync(
            string databasePath,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "CorruptDatabaseBackups", timestamp);
            Directory.CreateDirectory(backupDirectory);

            foreach (var suffix in SidecarSuffixes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = databasePath + suffix;
                if (!File.Exists(sourcePath))
                    continue;

                var fileName = Path.GetFileName(sourcePath);
                var backupPath = Path.Combine(backupDirectory, fileName);
                if (TryMoveToBackup(sourcePath, backupPath, logger))
                    continue;

                TryDeleteOrRename(sourcePath, $"{sourcePath}.malformed-{timestamp}", logger);
            }

            logger.LogWarning(
                "LocalGPT preserved malformed SQLite files in {BackupDirectory}. A clean database will be created on next access.",
                backupDirectory);

            if (!AnyDatabaseFiles(databasePath))
                TryDeleteRecoveryMarker(databasePath, logger);

            return Task.CompletedTask;
        }

        private static bool TryMoveToBackup(string sourcePath, string backupPath, ILogger logger)
        {
            try
            {
                File.Move(sourcePath, backupPath, overwrite: true);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(
                    ex,
                    "Could not move malformed SQLite file {SourcePath} to {BackupPath}. LocalGPT will try a safe remove or retry on the next start.",
                    sourcePath,
                    backupPath);
                return false;
            }
        }

        private static bool HasSidecars(string databasePath)
        {
            return SidecarSuffixes
                .Where(suffix => suffix.Length > 0)
                .Any(suffix => File.Exists(databasePath + suffix));
        }

        private static bool AnyDatabaseFiles(string databasePath)
        {
            return SidecarSuffixes.Any(suffix => File.Exists(databasePath + suffix));
        }

        private static string GetRecoveryMarkerPath(string databasePath)
        {
            return $"{databasePath}.recover";
        }

        private static void WriteRecoveryMarker(string databasePath, ILogger logger)
        {
            try
            {
                File.WriteAllText(
                    GetRecoveryMarkerPath(databasePath),
                    $"SQLite recovery requested at {DateTimeOffset.UtcNow:O}{Environment.NewLine}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not write SQLite recovery marker for {DatabasePath}.", databasePath);
            }
        }

        private static void TryDeleteRecoveryMarker(string databasePath, ILogger logger)
        {
            try
            {
                File.Delete(GetRecoveryMarkerPath(databasePath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not delete SQLite recovery marker for {DatabasePath}.", databasePath);
            }
        }

        private static void TryDeleteOrRename(string sourcePath, string fallbackPath, ILogger logger)
        {
            try
            {
                File.Delete(sourcePath);
            }
            catch (Exception deleteException) when (deleteException is IOException or UnauthorizedAccessException)
            {
                try
                {
                    File.Move(sourcePath, fallbackPath, overwrite: true);
                }
                catch (Exception moveException)
                {
                    logger.LogWarning(
                        moveException,
                        "Could not remove malformed SQLite file {SourcePath}. Delete failed with: {DeleteMessage}",
                        sourcePath,
                        deleteException.Message);
                }
            }
        }
    }
}
