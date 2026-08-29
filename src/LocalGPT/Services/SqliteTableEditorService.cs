using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using System.Text;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates sqlite table editor behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="databaseInitializer">Database initialization service dependency used by the sqlite table editor workflow to provide the corresponding application capability.</param>
    /// <param name="databaseOptions">Database options value supplied to the sqlite table editor operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="sqliteUtility">Sqlite utility service dependency used by the sqlite table editor workflow to provide the corresponding application capability.</param>
    public sealed class SqliteTableEditorService(
        IDatabaseInitializationService databaseInitializer,
        LocalGptDatabaseOptions databaseOptions,
        ILogger<SqliteTableEditorService> logger,
        SqliteUtilityService sqliteUtility,
        ILocalGptRuntimePolicyDataService runtimePolicy) : ISqliteTableEditorService
    {

        /// <summary>
        /// Gets the database path used by this sqlite table editor instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The database path value exposed by <see cref="SqliteTableEditorService"/>.</value>
        public string DatabasePath => databaseOptions.DatabasePath;

        /// <summary>
        /// Retrieves tables as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        public async Task<IReadOnlyList<SqliteTableSummary>> GetTablesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredConnectionAsyncDisposal = connection.ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                var names = new List<string>();
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.CommandText = """
                    SELECT name
                    FROM sqlite_master
                    WHERE type = 'table'
                      AND name NOT LIKE 'sqlite_%'
                    ORDER BY name;
                    """;

                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    await using var configuredReaderAsyncDisposal = reader.ConfigureAwait(false);
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        names.Add(reader.GetString(0));
                    }
                }

                var tables = new List<SqliteTableSummary>();
                foreach (var name in names)
                {
                    var columns = await sqliteUtility.GetColumnsAsync(connection, name, cancellationToken).ConfigureAwait(false);
                    var rowCount = await sqliteUtility.GetRowCountAsync(connection, name, cancellationToken).ConfigureAwait(false);
                    tables.Add(new SqliteTableSummary(
                        name,
                        columns.Count,
                        rowCount,
                        columns.Any(column => column.IsPrimaryKey)));
                }

                return tables;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not read SQLite table summaries.");
                return new List<SqliteTableSummary>();
            }
        }

        /// <summary>
        /// Retrieves table as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The sqlite table snapshot produced by the operation.</returns>
        public async Task<SqliteTableSnapshot> GetTableAsync(string tableName, int take = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredConnectionAsyncDisposal = connection.ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                await sqliteUtility.EnsureValidTableAsync(connection, tableName, cancellationToken).ConfigureAwait(false);

                var safeTake = Math.Clamp(take, 1, Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.SqliteTableEditorMaximumRows)));
                var columns = await sqliteUtility.GetColumnsAsync(connection, tableName, cancellationToken).ConfigureAwait(false);
                var rows = new List<SqliteRowSnapshot>();

                var command = connection.CreateCommand();
                await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
                command.CommandText = $"""
                SELECT rowid AS "__rowid", *
                FROM {sqliteUtility.QuoteIdentifier(tableName)}
                ORDER BY rowid DESC
                LIMIT $take 
                """;
                command.Parameters.AddWithValue("$take", safeTake);

                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredReaderAsyncDisposal = reader.ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var row = new SqliteRowSnapshot
                    {
                        RowId = Convert.ToInt64(reader["__rowid"], CultureInfo.InvariantCulture)
                    };

                    foreach (var column in columns)
                    {
                        var value = reader[column.Name];
                        row.Values[column.Name] = value is null or DBNull
                            ? null
                            : Convert.ToString(value, CultureInfo.InvariantCulture);
                    }

                    rows.Add(row);
                }

                return new SqliteTableSnapshot
                {
                    DatabasePath = DatabasePath,
                    TableName = tableName,
                    Columns = columns,
                    Rows = rows
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not read SQLite table {TableName}.", tableName);
                throw;
            }
        }

        /// <summary>
        /// Updates row as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="rowId">Identifier of the row to use for this operation.</param>
        /// <param name="updates">Sqlite cell update dependency used by the sqlite table editor workflow to provide the corresponding application capability.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        public async Task UpdateRowAsync(string tableName, long rowId, IReadOnlyList<SqliteCellUpdate> updates, CancellationToken cancellationToken = default)
        {
            try
            {
                if (updates.Count == 0)
                    return;

                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredConnectionAsyncDisposal = connection.ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                await sqliteUtility.EnsureValidTableAsync(connection, tableName, cancellationToken).ConfigureAwait(false);

                var columns = await sqliteUtility.GetColumnsAsync(connection, tableName, cancellationToken).ConfigureAwait(false);
                var editableColumns = columns
                    .Where(column => !column.IsPrimaryKey)
                    .Select(column => column.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var sanitizedUpdates = updates
                    .Where(update => editableColumns.Contains(update.ColumnName))
                    .GroupBy(update => update.ColumnName, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Last())
                    .ToList();

                if (sanitizedUpdates.Count == 0)
                    return;

                var command = connection.CreateCommand();
                await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
                var assignments = new List<string>();
                for (var index = 0; index < sanitizedUpdates.Count; index++)
                {
                    var parameterName = $"$value{index}";
                    assignments.Add($"{sqliteUtility.QuoteIdentifier(sanitizedUpdates[index].ColumnName)} = {parameterName}");
                    var column = columns.Single(item => item.Name.Equals(sanitizedUpdates[index].ColumnName, StringComparison.OrdinalIgnoreCase));
                    command.Parameters.AddWithValue(parameterName, sqliteUtility.ToSqliteValue(sanitizedUpdates[index].Value, column));
                }

                command.CommandText = $"""
                UPDATE {sqliteUtility.QuoteIdentifier(tableName)}
                SET {string.Join(", ", assignments)}
                WHERE rowid = $rowid;
                """;
                command.Parameters.AddWithValue("$rowid", rowId);
                try
                {
                    var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    if (affected != 1)
                        throw new InvalidOperationException($"SQLite update affected {affected} rows instead of exactly one.");
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(sqliteUtility.CreateSqliteEditError("update", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not update row {RowId} in SQLite table {TableName}.", rowId, tableName);
                throw;
            }
        }

        /// <summary>
        /// Performs insert row as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="values">String dependency used by the sqlite table editor workflow to provide the corresponding application capability.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        public async Task InsertRowAsync(string tableName, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredConnectionAsyncDisposal = connection.ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                await sqliteUtility.EnsureValidTableAsync(connection, tableName, cancellationToken).ConfigureAwait(false);

                var columns = await sqliteUtility.GetColumnsAsync(connection, tableName, cancellationToken).ConfigureAwait(false);
                var allowedColumns = columns
                    .Where(column => !column.IsPrimaryKey || !column.Type.Contains("INT", StringComparison.OrdinalIgnoreCase))
                    .Select(column => column.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var sanitizedValues = values
                    .Where(pair => allowedColumns.Contains(pair.Key) && pair.Value is not null)
                    .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Last())
                    .ToList();

                if (sanitizedValues.Count == 0)
                    throw new InvalidOperationException("Enter at least one editable value before inserting a row.");

                var command = connection.CreateCommand();
                await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
                var columnNames = new List<string>();
                var parameterNames = new List<string>();
                for (var index = 0; index < sanitizedValues.Count; index++)
                {
                    var parameterName = $"$value{index}";
                    columnNames.Add(sqliteUtility.QuoteIdentifier(sanitizedValues[index].Key));
                    parameterNames.Add(parameterName);
                    var column = columns.Single(item => item.Name.Equals(sanitizedValues[index].Key, StringComparison.OrdinalIgnoreCase));
                    command.Parameters.AddWithValue(parameterName, sqliteUtility.ToSqliteValue(sanitizedValues[index].Value, column));
                }

                command.CommandText = $"""
                INSERT INTO {sqliteUtility.QuoteIdentifier(tableName)} ({string.Join(", ", columnNames)})
                VALUES ({string.Join(", ", parameterNames)});
                """;
                try
                {
                    var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    if (affected != 1)
                        throw new InvalidOperationException($"SQLite insert affected {affected} rows instead of exactly one.");
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(sqliteUtility.CreateSqliteEditError("insert", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not insert a row into SQLite table {TableName}.", tableName);
                throw;
            }
           
        }

        /// <summary>
        /// Deletes row as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="rowId">Identifier of the row to use for this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        public async Task DeleteRowAsync(string tableName, long rowId, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredConnectionAsyncDisposal = connection.ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                await sqliteUtility.EnsureValidTableAsync(connection, tableName, cancellationToken).ConfigureAwait(false);

                var command = connection.CreateCommand();
                await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
                command.CommandText = $"DELETE FROM {sqliteUtility.QuoteIdentifier(tableName)} WHERE rowid = $rowid;";
                command.Parameters.AddWithValue("$rowid", rowId);
                try
                {

                    logger.LogWarning("Deleting row {RowId} from SQLite table {TableName}; referential consistency is the caller's responsibility.", rowId, tableName);
                    var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    if (affected != 1)
                        throw new InvalidOperationException($"SQLite delete affected {affected} rows instead of exactly one.");
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(sqliteUtility.CreateSqliteEditError("delete", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not delete row {RowId} from SQLite table {TableName}.", rowId, tableName);
                throw;
            }
        }

        /// <summary>
        /// Ensures database file as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task EnsureDatabaseFileAsync(CancellationToken cancellationToken)
        {
    try
    {
                await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteTableEditorService)}.{nameof(EnsureDatabaseFileAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SqliteTableEditorService)}.{nameof(EnsureDatabaseFileAsync)} failed.");
        throw;
    }
}

        /// <summary>
        /// Opens connection as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The sqlite connection produced by the operation.</returns>
        private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var connection = new SqliteConnection($"Data Source={DatabasePath}");
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not open the LocalGPT SQLite database at {DatabasePath}.", DatabasePath);
                throw;
            }
        }
 
    }
}
