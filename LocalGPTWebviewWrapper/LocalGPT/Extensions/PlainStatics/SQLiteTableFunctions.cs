using LocalGPT.BusinessObjects.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class SQLiteTableFunctions
    {
        public static async Task EnsureCreatedAsync(LocalGptMemoryDbContext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                await db.Database.ExecuteSqlRawAsync(
                    """
                CREATE TABLE IF NOT EXISTS "NativeCommandLogs" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_NativeCommandLogs" PRIMARY KEY AUTOINCREMENT,
                    "StartedAtUtc" TEXT NOT NULL,
                    "CompletedAtUtc" TEXT NOT NULL,
                    "FeatureName" TEXT NOT NULL,
                    "RequestedBy" TEXT NOT NULL,
                    "CommandProfile" TEXT NOT NULL DEFAULT 'CustomAllowlistedCommand',
                    "Executable" TEXT NOT NULL,
                    "Arguments" TEXT NOT NULL,
                    "WorkingDirectory" TEXT NOT NULL,
                    "ExitCode" INTEGER NOT NULL,
                    "DurationMilliseconds" REAL NOT NULL,
                    "StdoutPath" TEXT NOT NULL,
                    "StderrPath" TEXT NOT NULL,
                    "PolicyDecision" TEXT NOT NULL,
                    "PolicyReason" TEXT NOT NULL
                );
                """,
                    cancellationToken).ConfigureAwait(false);

                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "NativeCommandLogs" ADD COLUMN "CommandProfile" TEXT NOT NULL DEFAULT 'CustomAllowlistedCommand';""",
                    cancellationToken).ConfigureAwait(false);

                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_NativeCommandLogs_StartedAtUtc" ON "NativeCommandLogs" ("StartedAtUtc");""",
                    cancellationToken).ConfigureAwait(false);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_NativeCommandLogs_Executable" ON "NativeCommandLogs" ("Executable");""",
                    cancellationToken).ConfigureAwait(false);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_NativeCommandLogs_PolicyDecision" ON "NativeCommandLogs" ("PolicyDecision");""",
                    cancellationToken).ConfigureAwait(false);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_NativeCommandLogs_CommandProfile" ON "NativeCommandLogs" ("CommandProfile");""",
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EnsureCreatedAsync db: {db.ToString()}");
            }
        }

        public static async Task EnsureHealthyOrRecoverAsync(
            string databasePath,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
                Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

                if (File.Exists(GetRecoveryMarkerPath(databasePath)))
                {
                    logger.LogWarning(
                        "LocalGPT found a pending SQLite recovery marker for {DatabasePath}. Backing up and recreating the local store before opening SQLite.",
                        databasePath);

                    await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (!File.Exists(databasePath))
                {
                    if (HasSidecars(databasePath))
                    {
                        logger.LogWarning(
                            "LocalGPT found SQLite WAL/SHM sidecar files without the base database at {DatabasePath}. Backing up and removing orphan sidecars.",
                            databasePath);

                        await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                var quickCheck = await TryRunQuickCheckAsync(databasePath, cancellationToken, logger).ConfigureAwait(false);
                if (!quickCheck.IsHealthy)
                {
                    logger.LogWarning(
                        "LocalGPT SQLite database quick_check failed with '{QuickCheckResult}'. Backing up and recreating {DatabasePath}.",
                        quickCheck.Result,
                        databasePath);

                    await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken).ConfigureAwait(false);
                    return;
                }

                var writeProbe = await TryRunWriteProbeAsync(databasePath, cancellationToken, logger).ConfigureAwait(false);
                if (writeProbe.IsHealthy)
                    return;

                logger.LogWarning(
                    "LocalGPT SQLite database write probe failed with '{WriteProbeResult}'. Backing up and recreating {DatabasePath}.",
                    writeProbe.Result,
                    databasePath);

                await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EnsureHealthyOrRecoverAsync databasePath: {databasePath.ToString()}");
            }
            
        }

        public static Task RecoverMalformedDatabaseAsync(
            string databasePath,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            try
            {
                WriteRecoveryMarker(databasePath, logger);
                return BackupAndRemoveDatabaseAsync(databasePath, logger, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RecoverMalformedDatabaseAsync databasePath: {databasePath.ToString()}");
                return Task.CompletedTask;
            }
        }

        public static bool IsSqliteCorruption(Exception exception, ILogger logger)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsSqliteCorruption exception: {exception.ToString()}");

                return false;
            }
        }

        public static async Task<(bool IsHealthy, string Result)> TryRunQuickCheckAsync(
            string databasePath,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                try
                {
                    await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Cache=Private");
                    await connection.OpenAsync(cancellationToken);
                    await using var command = connection.CreateCommand();
                    command.CommandTimeout = GlobalVariableSlopCollectionToRemove.ProbeCommandTimeoutSeconds;
                    command.CommandText = "PRAGMA quick_check;";
                    var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
                    return (string.Equals(result.Trim(), "ok", StringComparison.OrdinalIgnoreCase), result);
                }
                catch (Exception ex) when (SQLiteTableFunctions.IsSqliteCorruption(ex,logger))
                {
                    return (false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryRunQuickCheckAsync databasePath: {databasePath.ToString()}");
                return (false, $"Error in TryRunQuickCheckAsync databasePath: {databasePath.ToString()} ex {ex.ToString()}");
            }
           
        }

        public static async Task<(bool IsHealthy, string Result)> TryFindCorruptionLogEvidenceAsync(
            string databasePath,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Cache=Private");
                await connection.OpenAsync(cancellationToken);

                if (!await HasTableAsync(connection, "ApplicationLogs", cancellationToken))
                    return (true, "ApplicationLogs table not present");

                await using var command = connection.CreateCommand();
                command.CommandTimeout = GlobalVariableSlopCollectionToRemove.ProbeCommandTimeoutSeconds;
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
            catch (Exception ex) when (SQLiteTableFunctions.IsSqliteCorruption(ex,logger))
            {
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryFindCorruptionLogEvidenceAsync databasePath: {databasePath.ToString()}");
                return (false, $"Error in TryFindCorruptionLogEvidenceAsync databasePath: {databasePath.ToString()} ex {ex.ToString()}");
            }
        }

        public static async Task<(bool IsHealthy, string Result)> TryRunWriteProbeAsync(
            string databasePath,
            CancellationToken cancellationToken, ILogger logger)
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

                if (await HasColumnAsync(connection, transaction, "CouncilKnowledgeEntries", "LastUsedAtUtc", cancellationToken,logger))
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
            catch (Exception ex) when (SQLiteTableFunctions.IsSqliteCorruption(ex,logger))
            {
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryFindCorruptionLogEvidenceAsync databasePath: {databasePath.ToString()}");
                return (false, $"Error in TryFindCorruptionLogEvidenceAsync databasePath: {databasePath.ToString()} ex {ex.ToString()}");
            }
        }

        public static async Task<bool> HasColumnAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            string columnName,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandTimeout = GlobalVariableSlopCollectionToRemove.ProbeCommandTimeoutSeconds;
                command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in HasColumnAsync connection: {connection.ToString()} transaction {transaction.ToString()} tableName {tableName.ToString()} columnName {columnName.ToString()}");
                return false;
            }
          
        }

        public static async Task<bool> HasTableAsync(
            SqliteConnection connection,
            string tableName,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandTimeout = GlobalVariableSlopCollectionToRemove.ProbeCommandTimeoutSeconds;
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in HasTableAsync connection: {connection.ToString()} tableName {tableName.ToString()}");
                return false;
            }
          
        }

        public static async Task ExecuteNonQueryAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string sql,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandTimeout = GlobalVariableSlopCollectionToRemove.ProbeCommandTimeoutSeconds;
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in HasTableAsync connection: {connection.ToString()} transaction {transaction.ToString()} sql {sql.ToString()}");
            }
        }

        public static Task BackupAndRemoveDatabaseAsync(
            string databasePath,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "CorruptDatabaseBackups", timestamp);
                Directory.CreateDirectory(backupDirectory);

                foreach (var suffix in GlobalVariableSlopCollectionToRemove.SidecarSuffixes)
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BackupAndRemoveDatabaseAsync databasePath: {databasePath.ToString()}");
                return Task.CompletedTask;
            }
           
        }

        public static bool TryMoveToBackup(string sourcePath, string backupPath, ILogger logger)
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryMoveToBackup sourcePath: {sourcePath.ToString()} backupPath: {backupPath.ToString()}");
                return false;
            }
        }

        public static bool HasSidecars(string databasePath, ILogger logger)
        {
            try
            {
                return GlobalVariableSlopCollectionToRemove.SidecarSuffixes
    .Where(suffix => suffix.Length > 0)
    .Any(suffix => File.Exists(databasePath + suffix));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in HasSidecars databasePath: {databasePath.ToString()}");
                return false;
            }
        }

        public static bool AnyDatabaseFiles(string databasePath, ILogger logger)
        {
            try
            {
                return GlobalVariableSlopCollectionToRemove.SidecarSuffixes.Any(suffix => File.Exists(databasePath + suffix));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AnyDatabaseFiles databasePath: {databasePath.ToString()}");
                return false;
            }
        }

        public static string GetRecoveryMarkerPath(string databasePath, ILogger logger)
        {
            try
            {
                return $"{databasePath}.recover";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AnyDatabaseFiles databasePath: {databasePath.ToString()}");
                return string.Empty;
            }
        }

        public static void WriteRecoveryMarker(string databasePath, ILogger logger)
        {
            try
            {
                File.WriteAllText(
                    GetRecoveryMarkerPath(databasePath, logger),
                    $"SQLite recovery requested at {DateTimeOffset.UtcNow:O}{Environment.NewLine}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not write SQLite recovery marker for {DatabasePath}.", databasePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteRecoveryMarker databasePath: {databasePath.ToString()}");
            }
        }

        public static void TryDeleteRecoveryMarker(string databasePath, ILogger logger)
        {
            try
            {
                File.Delete(GetRecoveryMarkerPath(databasePath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not delete SQLite recovery marker for {DatabasePath}.", databasePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryDeleteRecoveryMarker databasePath: {databasePath.ToString()}");
            }
        }

        public static void TryDeleteOrRename(string sourcePath, string fallbackPath, ILogger logger)
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryDeleteOrRename sourcePath: {sourcePath.ToString()} fallbackPath: {fallbackPath.ToString()}");
            }
        }
        public static async Task EnsureCreatedCouncilKnowledgeTableAsync(LocalGptMemoryDbContext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """
                CREATE TABLE IF NOT EXISTS "CouncilKnowledgeEntries" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilKnowledgeEntries" PRIMARY KEY,
                    "CreatedAtUtc" TEXT NOT NULL,
                    "UpdatedAtUtc" TEXT NOT NULL,
                    "Topic" TEXT NOT NULL,
                    "Scope" TEXT NOT NULL,
                    "Content" TEXT NOT NULL,
                    "Source" TEXT NOT NULL,
                    "HelpfulSources" TEXT NOT NULL,
                    "Tags" TEXT NOT NULL,
                    "Confidence" INTEGER NOT NULL,
                    "VerificationStatus" TEXT NOT NULL DEFAULT 'NeedsVerification',
                    "ReviewStatus" TEXT NOT NULL DEFAULT 'NeedsUserReview',
                    "ExpiresAtUtc" TEXT NULL,
                    "LastVerifiedAtUtc" TEXT NULL,
                    "LastUsedAtUtc" TEXT NULL,
                    "SupersededByKnowledgeId" TEXT NULL,
                    "StalenessReason" TEXT NOT NULL DEFAULT '',
                    "StalenessDetectedAtUtc" TEXT NULL,
                    "StalenessDetectedBy" TEXT NOT NULL DEFAULT '',
                    "SourceHash" TEXT NOT NULL DEFAULT '',
                    "SourceDateUtc" TEXT NULL,
                    "IsUserApproved" INTEGER NOT NULL DEFAULT 0,
                    "IsPinned" INTEGER NOT NULL,
                    "IsArchived" INTEGER NOT NULL
                );
                """,
                    cancellationToken);

                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "IsUserApproved" INTEGER NOT NULL DEFAULT 0;""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "VerificationStatus" TEXT NOT NULL DEFAULT 'NeedsVerification';""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "ReviewStatus" TEXT NOT NULL DEFAULT 'NeedsUserReview';""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "ExpiresAtUtc" TEXT NULL;""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "LastVerifiedAtUtc" TEXT NULL;""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "LastUsedAtUtc" TEXT NULL;""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "SupersededByKnowledgeId" TEXT NULL;""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "StalenessReason" TEXT NOT NULL DEFAULT '';""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "StalenessDetectedAtUtc" TEXT NULL;""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "StalenessDetectedBy" TEXT NOT NULL DEFAULT '';""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "SourceHash" TEXT NOT NULL DEFAULT '';""",
                    cancellationToken);
                await TryAddColumnAsync(
                    db,
                    """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "SourceDateUtc" TEXT NULL;""",
                    cancellationToken);

                await db.Database.ExecuteSqlRawAsync(
                    """
                UPDATE "CouncilKnowledgeEntries"
                SET "VerificationStatus" =
                    CASE
                        WHEN "IsArchived" = 1 THEN 'Archived'
                        WHEN "Source" LIKE 'LocalGPT % seed' THEN 'SourceBacked'
                        WHEN "Source" = 'LocalGPT SQL seed' THEN 'SourceBacked'
                        WHEN "IsUserApproved" = 1 THEN 'UserVerified'
                        WHEN "Source" LIKE 'AI Council %' THEN 'ModelSuggested'
                        ELSE 'NeedsVerification'
                    END
                WHERE "VerificationStatus" IS NULL OR trim("VerificationStatus") = '';
                """,
                    cancellationToken);

                await db.Database.ExecuteSqlRawAsync(
                    """
                UPDATE "CouncilKnowledgeEntries"
                SET "ReviewStatus" =
                    CASE
                        WHEN "IsArchived" = 1 THEN 'Archived'
                        WHEN "SupersededByKnowledgeId" IS NOT NULL AND trim("SupersededByKnowledgeId") <> '' THEN 'Superseded'
                        WHEN "ExpiresAtUtc" IS NOT NULL AND trim("ExpiresAtUtc") <> '' AND "ExpiresAtUtc" <= datetime('now') THEN 'Expired'
                        WHEN "VerificationStatus" IN ('SourceBacked', 'UserVerified') THEN 'Current'
                        WHEN "VerificationStatus" IN ('ModelSuggested', 'NeedsVerification') THEN 'NeedsUserReview'
                        ELSE 'NeedsUserReview'
                    END
                WHERE "ReviewStatus" IS NULL OR trim("ReviewStatus") = '' OR "ReviewStatus" IN ('NeedsVerification', 'NeedsUserReview');
                """,
                    cancellationToken);

                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_UpdatedAtUtc" ON "CouncilKnowledgeEntries" ("UpdatedAtUtc");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_IsUserApproved_UpdatedAtUtc" ON "CouncilKnowledgeEntries" ("IsUserApproved", "UpdatedAtUtc");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_IsPinned_UpdatedAtUtc" ON "CouncilKnowledgeEntries" ("IsPinned", "UpdatedAtUtc");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_VerificationStatus" ON "CouncilKnowledgeEntries" ("VerificationStatus");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_ReviewStatus" ON "CouncilKnowledgeEntries" ("ReviewStatus");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_ExpiresAtUtc" ON "CouncilKnowledgeEntries" ("ExpiresAtUtc");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_LastVerifiedAtUtc" ON "CouncilKnowledgeEntries" ("LastVerifiedAtUtc");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_LastUsedAtUtc" ON "CouncilKnowledgeEntries" ("LastUsedAtUtc");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_SupersededByKnowledgeId" ON "CouncilKnowledgeEntries" ("SupersededByKnowledgeId");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_Scope" ON "CouncilKnowledgeEntries" ("Scope");""",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EnsureCreatedCouncilKnowledgeTableAsync db: {db.ToString()}");
            }
        }

        public static async Task TryAddColumnAsync(LocalGptMemoryDbContext db, string sql, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                      ex,
                      $"Duplicate Exception db {db.ToString()} sql {sql.ToString()}", db, sql);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EnsureCreatedCouncilKnowledgeTableAsync db: {db.ToString()}");
            }
        }
        public static async Task EnsureCreatedApplicationLogSchemaAsync(LocalGptMemoryDbContext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """
                CREATE TABLE IF NOT EXISTS "ApplicationLogs" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ApplicationLogs" PRIMARY KEY AUTOINCREMENT,
                    "TimestampUtc" TEXT NOT NULL,
                    "Level" TEXT NOT NULL,
                    "LogLevelValue" INTEGER NOT NULL,
                    "Category" TEXT NOT NULL,
                    "EventId" INTEGER NOT NULL,
                    "EventName" TEXT NULL,
                    "Message" TEXT NOT NULL,
                    "Exception" TEXT NULL,
                    "MachineName" TEXT NOT NULL,
                    "ProcessId" INTEGER NOT NULL,
                    "ThreadId" INTEGER NOT NULL
                );
                """,
                    cancellationToken);

                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_TimestampUtc" ON "ApplicationLogs" ("TimestampUtc");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_LogLevelValue" ON "ApplicationLogs" ("LogLevelValue");""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_LogLevelValue_TimestampUtc" ON "ApplicationLogs" ("LogLevelValue", "TimestampUtc");""",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EnsureCreatedApplicationLogSchemaAsync db: {db.ToString()}");
            }
        }
    }
}
