using LocalGPT.BusinessObjects;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Drops obsolete columns that an EF migration replaced but could not drop on SQLite. Every drop is preceded by checks
/// against the real database and runs in one transaction with its backfill and its verification.
/// </summary>
public sealed partial class DatabaseMigrationCompatibilityService
{
    /// <summary>Oldest SQLite version that supports <c>ALTER TABLE ... DROP COLUMN</c>.</summary>
    private const string MinimumDropColumnSqliteVersion = "3.35.0";

    /// <summary>
    /// Lists every obsolete column that waits for a post-migration drop. Add an entry here whenever a migration replaces a
    /// column on SQLite; never put <c>DropColumn</c> into a migration's <c>Up</c>.
    /// </summary>
    /// <returns>The column retirements, in the order they are applied.</returns>
    private SqliteColumnRetirement[] CreateColumnRetirements()
    {
        try
        {
            return
            [
                // 7.0.0: the hard-coded game runtime profile became the declarative view and ruleset keys.
                new(
                    "LocalGptGameProjectProfiles",
                    "RuntimeProfile",
                    "20261108200000_AddGameEngineRules",
                    ["ViewKey", "RulesetTemplateKey"],
                    "UPDATE \"LocalGptGameProjectProfiles\" SET " +
                    "\"ViewKey\" = CASE \"RuntimeProfile\" WHEN 1 THEN 'text-template' ELSE 'first-person-raycast' END, " +
                    "\"RulesetTemplateKey\" = CASE \"RuntimeProfile\" WHEN 1 THEN 'green-dragon' ELSE 'ascii-arena' END " +
                    "WHERE \"RulesetTemplateKey\" = '';")
            ];
        }
        catch (Exception __serviceMethodException)
        {
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(CreateColumnRetirements)} failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ReconcileAfterMigrationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(databaseFileHealth.DatabasePath))
                return;

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databaseFileHealth.DatabasePath,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Private
            }.ToString();
            var connection = new SqliteConnection(connectionString);
            await using var configuredConnectionAsyncDisposal = connection.ConfigureAwait(false);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            string? backupPath = null;
            var retired = new List<string>();
            foreach (var retirement in CreateColumnRetirements())
            {
                if (!await IsColumnRetirementReadyAsync(connection, retirement, cancellationToken).ConfigureAwait(false))
                    continue;

                await EnsureDropColumnSupportedAsync(connection, cancellationToken).ConfigureAwait(false);
                backupPath ??= await CreateCompatibilityBackupAsync(connection, cancellationToken).ConfigureAwait(false);
                await RetireColumnAsync(connection, retirement, cancellationToken).ConfigureAwait(false);
                retired.Add($"{retirement.TableName}.{retirement.ColumnName}");
            }

            if (retired.Count > 0)
            {
                logger.LogWarning(
                    "Post-migration reconciliation dropped the obsolete SQLite column(s) {Columns} after verifying the schema. " +
                    "No row was deleted. Compatibility backup: {BackupPath}",
                    string.Join(", ", retired),
                    backupPath);
            }
            else
            {
                logger.LogDebug("Post-migration reconciliation found no obsolete SQLite column to drop.");
            }
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ReconcileAfterMigrationAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ReconcileAfterMigrationAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Checks the real database before a column is dropped: the column still exists, the replacing migration is recorded,
    /// the replacement columns exist, and no index, key, trigger or view still uses the column. A column that cannot be
    /// dropped but would block inserts (NOT NULL without a default) stops startup with an explanation instead of letting
    /// the seed stages fail later with a bare constraint error.
    /// </summary>
    /// <param name="connection">Open connection to the LocalGPT database.</param>
    /// <param name="retirement">The column to check.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the column exists and may be dropped now.</returns>
    private async Task<bool> IsColumnRetirementReadyAsync(
        SqliteConnection connection,
        SqliteColumnRetirement retirement,
        CancellationToken cancellationToken)
    {
        try
        {
            var schema = await ReadSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
            if (!schema.TryGetValue(retirement.TableName, out var columns) || !columns.Contains(retirement.ColumnName))
            {
                logger.LogDebug(
                    "Obsolete column {Table}.{Column} is already absent; nothing to drop.",
                    retirement.TableName,
                    retirement.ColumnName);
                return false;
            }

            var appliedMigrations = await ReadAppliedMigrationsAsync(connection, cancellationToken).ConfigureAwait(false);
            var missingReplacements = retirement.ReplacementColumns.Where(column => !columns.Contains(column)).ToArray();
            var dependents = await ReadColumnDependentsAsync(connection, retirement, cancellationToken).ConfigureAwait(false);
            var reasons = new List<string>();
            if (!appliedMigrations.Contains(retirement.RequiredMigrationId))
                reasons.Add($"migration {retirement.RequiredMigrationId} is not recorded as applied");
            if (missingReplacements.Length > 0)
                reasons.Add($"replacement column(s) {string.Join(", ", missingReplacements)} are missing");
            if (dependents.Count > 0)
                reasons.Add($"the schema still uses it ({string.Join("; ", dependents)})");
            if (reasons.Count == 0)
                return true;

            var reasonText = string.Join("; ", reasons);
            if (await BlocksInsertsAsync(connection, retirement, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    $"The obsolete SQLite column {retirement.TableName}.{retirement.ColumnName} must be dropped before LocalGPT can " +
                    $"insert into {retirement.TableName} (it is NOT NULL without a default), but it was kept because {reasonText}. " +
                    "No data was changed. Resolve the listed state, or restore the database backup, and start LocalGPT again.");
            }

            logger.LogWarning(
                "Obsolete column {Table}.{Column} was kept because {Reasons}. It accepts inserts without a value, so LocalGPT continues.",
                retirement.TableName,
                retirement.ColumnName,
                reasonText);
            return false;
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(IsColumnRetirementReadyAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(IsColumnRetirementReadyAsync)} failed.");
            throw;
        }
    }

    /// <summary>Lists every index, primary key, foreign key, trigger and view that still uses the column.</summary>
    /// <param name="connection">Open connection to the LocalGPT database.</param>
    /// <param name="retirement">The column to check.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>One description per dependent schema object; empty when nothing uses the column.</returns>
    private async Task<List<string>> ReadColumnDependentsAsync(
        SqliteConnection connection,
        SqliteColumnRetirement retirement,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = connection.CreateCommand();
            await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
            command.CommandText =
                """
                SELECT 'index ' || il."name" FROM pragma_index_list($table) AS il
                WHERE EXISTS (SELECT 1 FROM pragma_index_info(il."name") AS ii WHERE ii."name" = $column COLLATE NOCASE)
                UNION ALL
                SELECT 'primary key' FROM pragma_table_info($table) AS ti
                WHERE ti."name" = $column COLLATE NOCASE AND ti."pk" > 0
                UNION ALL
                SELECT 'foreign key to ' || fk."table" FROM pragma_foreign_key_list($table) AS fk
                WHERE fk."from" = $column COLLATE NOCASE
                UNION ALL
                SELECT "type" || ' ' || "name" FROM "sqlite_master"
                WHERE "type" IN ('trigger', 'view') AND "sql" LIKE '%' || $column || '%';
                """;
            command.Parameters.AddWithValue("$table", retirement.TableName);
            command.Parameters.AddWithValue("$column", retirement.ColumnName);
            var dependents = new List<string>();
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredReaderAsyncDisposal = reader.ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                dependents.Add(reader.GetString(0));
            return dependents;
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ReadColumnDependentsAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ReadColumnDependentsAsync)} failed.");
            throw;
        }
    }

    /// <summary>Reports whether the column is NOT NULL without a default, so that inserts which no longer know it fail.</summary>
    /// <param name="connection">Open connection to the LocalGPT database.</param>
    /// <param name="retirement">The column to check.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the column blocks inserts that omit it.</returns>
    private async Task<bool> BlocksInsertsAsync(
        SqliteConnection connection,
        SqliteColumnRetirement retirement,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = connection.CreateCommand();
            await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
            command.CommandText =
                """
                SELECT COUNT(*) FROM pragma_table_info($table)
                WHERE "name" = $column COLLATE NOCASE AND "notnull" = 1 AND "dflt_value" IS NULL;
                """;
            command.Parameters.AddWithValue("$table", retirement.TableName);
            command.Parameters.AddWithValue("$column", retirement.ColumnName);
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0;
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(BlocksInsertsAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(BlocksInsertsAsync)} failed.");
            throw;
        }
    }

    /// <summary>Stops with an explanation when the linked SQLite library is too old for <c>ALTER TABLE ... DROP COLUMN</c>.</summary>
    /// <param name="connection">Open connection to the LocalGPT database.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the version was verified.</returns>
    private async Task EnsureDropColumnSupportedAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var command = connection.CreateCommand();
            await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
            command.CommandText = "SELECT sqlite_version();";
            var versionText = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) ?? string.Empty;
            if (!Version.TryParse(versionText, out var version) || version < Version.Parse(MinimumDropColumnSqliteVersion))
            {
                throw new InvalidOperationException(
                    $"The linked SQLite library reports version '{versionText}', but dropping an obsolete column needs " +
                    $"{MinimumDropColumnSqliteVersion} or newer. No data was changed. Make sure the bundled e_sqlite3 library is deployed.");
            }

            logger.LogDebug("SQLite {SqliteVersion} supports ALTER TABLE ... DROP COLUMN.", versionText);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EnsureDropColumnSupportedAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(EnsureDropColumnSupportedAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Runs the backfill, drops the column and verifies the result in one transaction: the column is gone and the table
    /// has no foreign-key violation. Any failure rolls the whole step back.
    /// </summary>
    /// <param name="connection">Open connection to the LocalGPT database.</param>
    /// <param name="retirement">The verified column to drop.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the drop is committed.</returns>
    private async Task RetireColumnAsync(
        SqliteConnection connection,
        SqliteColumnRetirement retirement,
        CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredTransactionAsyncDisposal = transaction.ConfigureAwait(false);
            var backfilled = await ExecuteInTransactionAsync(connection, transaction, retirement.BackfillSql, cancellationToken).ConfigureAwait(false);
            await ExecuteInTransactionAsync(
                connection,
                transaction,
                $"ALTER TABLE {QuoteSqliteIdentifier(retirement.TableName)} DROP COLUMN {QuoteSqliteIdentifier(retirement.ColumnName)};",
                cancellationToken).ConfigureAwait(false);

            var verify = connection.CreateCommand();
            await using var configuredVerifyAsyncDisposal = verify.ConfigureAwait(false);
            verify.Transaction = (SqliteTransaction)transaction;
            verify.CommandText =
                """
                SELECT (SELECT COUNT(*) FROM pragma_table_info($table) WHERE "name" = $column COLLATE NOCASE),
                       (SELECT COUNT(*) FROM pragma_foreign_key_check($table));
                """;
            verify.Parameters.AddWithValue("$table", retirement.TableName);
            verify.Parameters.AddWithValue("$column", retirement.ColumnName);
            var reader = await verify.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) || reader.GetInt64(0) != 0 || reader.GetInt64(1) != 0)
                {
                    throw new InvalidOperationException(
                        $"Dropping {retirement.TableName}.{retirement.ColumnName} did not verify (column still present or foreign-key " +
                        "violations found); the change was rolled back and no data was changed.");
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Dropped obsolete column {Table}.{Column} after mapping {BackfilledRows} still-unmapped row(s); migration {Migration} owns its replacement.",
                retirement.TableName,
                retirement.ColumnName,
                backfilled,
                retirement.RequiredMigrationId);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(RetireColumnAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(RetireColumnAsync)} failed.");
            throw;
        }
    }

    /// <summary>Executes one statement inside the given transaction.</summary>
    /// <param name="connection">Open connection that owns the transaction.</param>
    /// <param name="transaction">The active transaction.</param>
    /// <param name="sql">The statement.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The number of rows the statement changed.</returns>
    private async Task<int> ExecuteInTransactionAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = connection.CreateCommand();
            await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = sql;
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ExecuteInTransactionAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseMigrationCompatibilityService)}.{nameof(ExecuteInTransactionAsync)} failed.");
            throw;
        }
    }
}
