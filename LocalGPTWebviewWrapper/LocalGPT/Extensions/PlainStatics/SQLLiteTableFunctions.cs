using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class SQLLiteTableFunctions
    {
        public static void Normalize(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                entry.Topic = SQLLiteFunctions.TrimOrFallback(entry.Topic, 240, "Untitled knowledge entry", logger);
                entry.Scope = SQLLiteFunctions.TrimOrFallback(entry.Scope, 120, "AI Council", logger);
                entry.Source = SQLLiteFunctions.TrimOrFallback(entry.Source, 240, "Manual", logger);
                entry.Tags = SQLLiteFunctions.Trim(entry.Tags, 400, logger);
                entry.Confidence = Math.Clamp(entry.Confidence, 0, 100);
                entry.VerificationStatus = NormalizeVerificationStatus(entry, logger);
                entry.ReviewStatus = NormalizeReviewStatus(entry, logger);
                entry.StalenessReason = SQLLiteFunctions.Trim(entry.StalenessReason, 500, logger);
                entry.StalenessDetectedBy = SQLLiteFunctions.Trim(entry.StalenessDetectedBy, 160, logger);
                entry.SourceHash = SQLLiteFunctions.Trim(entry.SourceHash, 128, logger);
                if (string.IsNullOrWhiteSpace(entry.SourceHash))
                    entry.SourceHash = SQLLiteFunctions.ComputeSourceHash(entry, logger);

                if (entry.VerificationStatus is "SourceBacked" or "UserVerified" && entry.LastVerifiedAtUtc is null)
                    entry.LastVerifiedAtUtc = DateTime.UtcNow;

                if (entry.ReviewStatus == "Archived")
                    entry.IsArchived = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Normalize entry {entry.ToString()}");
            }
        }
        public static string BuildTrustLabel(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                var trust = entry.VerificationStatus switch
                {
                    "SourceBacked" => "source-backed seed",
                    "UserVerified" => "verified by user",
                    "ModelSuggested" => "model-suggested; treat as hypothesis until user approves",
                    "Archived" => "archived; do not use as active evidence",
                    _ => entry.IsUserApproved
                        ? "verified by user"
                        : "needs verification"
                };

                var review = entry.ReviewStatus switch
                {
                    "Current" => "current",
                    "NeedsUserReview" => "needs user review",
                    "NeedsSourceRefresh" => "needs source refresh",
                    "NeedsDiagnosticVerification" => "needs diagnostic verification",
                    "Expired" => "expired",
                    "Deprecated" => "deprecated",
                    "Superseded" => "superseded",
                    "Archived" => "archived",
                    _ => "needs review"
                };

                return $"{trust}; review: {review}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildTrustLabel entry {entry.ToString()}");
                return string.Empty;
            }
        }
        public static string NormalizeVerificationStatus(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                if (entry.IsArchived)
                    return "Archived";

                var requested = SQLLiteFunctions.Trim(entry.VerificationStatus, 80, logger).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
                if (IsKnownVerificationStatus(requested, logger))
                    return requested;

                if (entry.Source.Contains("seed", StringComparison.OrdinalIgnoreCase))
                    return "SourceBacked";

                if (entry.IsUserApproved)
                    return "UserVerified";

                if (entry.Source.StartsWith("AI Council ", StringComparison.OrdinalIgnoreCase))
                    return "ModelSuggested";

                return "NeedsVerification";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeVerificationStatus entry {entry.ToString()}");
                return string.Empty;
            }
        }
        public static bool IsKnownVerificationStatus(string value, ILogger logger)
        {
            try
            {
                return value is "SourceBacked" or "UserVerified" or "ModelSuggested" or "NeedsVerification" or "Archived";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsKnownVerificationStatus value {value.ToString()}");
                return false;
            }
        }
        public static string NormalizeReviewStatus(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                if (entry.IsArchived)
                    return "Archived";

                if (entry.SupersededByKnowledgeId is not null)
                    return "Superseded";

                var now = DateTime.UtcNow;
                if (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc.Value <= now)
                {
                    if (string.IsNullOrWhiteSpace(entry.StalenessReason))
                        entry.StalenessReason = "Knowledge expiry date passed.";
                    entry.StalenessDetectedAtUtc ??= now;
                    entry.StalenessDetectedBy = SQLLiteFunctions.TrimOrFallback(entry.StalenessDetectedBy, 160, "LocalGPT knowledge lifecycle", logger);
                    return "Expired";
                }

                var requested = SQLLiteFunctions.Trim(entry.ReviewStatus, 80, logger).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
                if (requested == "NeedsUserReview" &&
                    entry.IsUserApproved &&
                    entry.VerificationStatus is "SourceBacked" or "UserVerified")
                    return "Current";

                if (IsKnownReviewStatus(requested, logger))
                    return requested;

                return entry.VerificationStatus switch
                {
                    "SourceBacked" or "UserVerified" => "Current",
                    "Archived" => "Archived",
                    _ => "NeedsUserReview"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeReviewStatus entry {entry.ToString()}");
                return string.Empty;
            }
        }
        public static bool IsKnownReviewStatus(string value, ILogger logger)
        {
            try
            {
                return value is "Current" or "NeedsUserReview" or "NeedsSourceRefresh" or "NeedsDiagnosticVerification" or "Expired" or "Deprecated" or "Superseded" or "Archived";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsKnownReviewStatus value {value.ToString()}");
                return false;
            }
        }
        public static bool IsUsableForBriefing(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                if (entry.IsArchived)
                    return false;

                if (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc.Value <= DateTime.UtcNow)
                    return false;

                return entry.ReviewStatus is not "Archived" and not "Deprecated" and not "Superseded" and not "Expired";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsUsableForBriefing entry {entry.ToString()}");
                return false;
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

                if (File.Exists(GetRecoveryMarkerPath(databasePath, logger)))
                {
                    logger.LogWarning(
                        "LocalGPT found a pending SQLite recovery marker for {DatabasePath}. Backing up and recreating the local store before opening SQLite.",
                        databasePath);

                    await RecoverMalformedDatabaseAsync(databasePath, logger, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (!File.Exists(databasePath))
                {
                    if (HasSidecars(databasePath, logger))
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
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    await using var command = connection.CreateCommand();
                    command.CommandTimeout = GlobalVariableSlopCollectionToRemove.ProbeCommandTimeoutSeconds;
                    command.CommandText = "PRAGMA quick_check;";
                    var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) ?? string.Empty;
                    return (string.Equals(result.Trim(), "ok", StringComparison.OrdinalIgnoreCase), result);
                }
                catch (Exception ex) when (SQLLiteTableFunctions.IsSqliteCorruption(ex,logger))
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
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                if (!await HasTableAsync(connection, "ApplicationLogs", cancellationToken, logger).ConfigureAwait(false))
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
                var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
                return count > 0
                    ? (false, $"{count} recent SQLite corruption log entr{(count == 1 ? "y" : "ies")}")
                    : (true, "ok");
            }
            catch (Exception ex) when (SQLLiteTableFunctions.IsSqliteCorruption(ex,logger))
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
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

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
                    cancellationToken, logger).ConfigureAwait(false);

                await ExecuteNonQueryAsync(
                    connection,
                    transaction,
                    """
                    UPDATE "CouncilKnowledgeEntries"
                    SET "LastUsedAtUtc" = datetime('now')
                    WHERE "Id" IN (SELECT "Id" FROM "CouncilKnowledgeEntries" LIMIT 1);
                    """,
                    cancellationToken, logger).ConfigureAwait(false);
                

                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return (true, "ok");
            }
            catch (Exception ex) when (SQLLiteTableFunctions.IsSqliteCorruption(ex,logger))
            {
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryFindCorruptionLogEvidenceAsync databasePath: {databasePath.ToString()}");
                return (false, $"Error in TryFindCorruptionLogEvidenceAsync databasePath: {databasePath.ToString()} ex {ex.ToString()}");
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
                var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
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
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

                if (!AnyDatabaseFiles(databasePath, logger))
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
                File.Delete(GetRecoveryMarkerPath(databasePath, logger));
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
                try
                {
                    File.Move(sourcePath, fallbackPath, overwrite: true);
                }
                catch (Exception moveException)
                {
                    logger.LogWarning(moveException,$"Could not move malformed SQLite file {sourcePath}. Move failed to {fallbackPath} {moveException.ToString()}");
                    try
                    {
                        File.Delete(sourcePath);
                    }
                    catch (Exception deleteException) when (deleteException is IOException or UnauthorizedAccessException)
                    {
                        logger.LogError(deleteException, $"Error in TryDeleteOrRename delete failed sourcePath: {sourcePath.ToString()} fallbackPath: {fallbackPath.ToString()} {deleteException.ToString()}");
                    }
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
                    cancellationToken).ConfigureAwait(false);

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
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EnsureCreatedCouncilKnowledgeTableAsync db: {db.ToString()}");
            }
        }
    }
}