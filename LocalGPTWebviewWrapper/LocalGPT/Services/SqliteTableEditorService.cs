using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using System.Text;

namespace LocalGPT.Services
{
    public sealed class SqliteTableEditorService(
        IDatabaseInitializationService databaseInitializer,
        LocalGptDatabaseOptions databaseOptions,
        ILogger<SqliteTableEditorService> logger,
        SqliteUtilityService sqliteUtility) : ISqliteTableEditorService
    {
        private const int MaxRows = 500;

        public string DatabasePath => databaseOptions.DatabasePath;

        public async Task<IReadOnlyList<SqliteTableSummary>> GetTablesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                var names = new List<string>();
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = """
                    SELECT name
                    FROM sqlite_master
                    WHERE type = 'table'
                      AND name NOT LIKE 'sqlite_%'
                    ORDER BY name;
                    """;

                    await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
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

        public async Task<SqliteTableSnapshot> GetTableAsync(string tableName, int take = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                await sqliteUtility.EnsureValidTableAsync(connection, tableName, cancellationToken).ConfigureAwait(false);

                var safeTake = Math.Clamp(take, 1, MaxRows);
                var columns = await sqliteUtility.GetColumnsAsync(connection, tableName, cancellationToken).ConfigureAwait(false);
                var rows = new List<SqliteRowSnapshot>();

                await using var command = connection.CreateCommand();
                command.CommandText = $"""
                SELECT rowid AS "__rowid", *
                FROM {sqliteUtility.QuoteIdentifier(tableName)}
                ORDER BY rowid DESC
                LIMIT $take 
                """;
                command.Parameters.AddWithValue("$take", safeTake);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
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

        public async Task UpdateRowAsync(string tableName, long rowId, IReadOnlyList<SqliteCellUpdate> updates, CancellationToken cancellationToken = default)
        {
            try
            {
                if (updates.Count == 0)
                    return;

                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                await sqliteUtility.EnsureValidTableAsync(connection, tableName, cancellationToken);

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

                await using var command = connection.CreateCommand();
                var assignments = new List<string>();
                for (var index = 0; index < sanitizedUpdates.Count; index++)
                {
                    var parameterName = $"$value{index}";
                    assignments.Add($"{sqliteUtility.QuoteIdentifier(sanitizedUpdates[index].ColumnName)} = {parameterName}");
                    command.Parameters.AddWithValue(parameterName, sqliteUtility.ToSqliteValue(sanitizedUpdates[index].Value));
                }

                command.CommandText = $"""
                UPDATE {sqliteUtility.QuoteIdentifier(tableName)}
                SET {string.Join(", ", assignments)}
                WHERE rowid = $rowid;
                """;
                command.Parameters.AddWithValue("$rowid", rowId);
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(sqliteUtility.CreateSqliteEditError("update", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not update row {RowId} in SQLite table {TableName}.", rowId, tableName);
            }
        }

        public async Task InsertRowAsync(string tableName, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                await sqliteUtility.EnsureValidTableAsync(connection, tableName, cancellationToken).ConfigureAwait(false);

                var columns = await sqliteUtility.GetColumnsAsync(connection, tableName, cancellationToken).ConfigureAwait(false);
                var allowedColumns = columns
                    .Where(column => !column.IsPrimaryKey)
                    .Select(column => column.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var sanitizedValues = values
                    .Where(pair => allowedColumns.Contains(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Last())
                    .ToList();

                if (sanitizedValues.Count == 0)
                    throw new InvalidOperationException("Enter at least one editable value before inserting a row.");

                await using var command = connection.CreateCommand();
                var columnNames = new List<string>();
                var parameterNames = new List<string>();
                for (var index = 0; index < sanitizedValues.Count; index++)
                {
                    var parameterName = $"$value{index}";
                    columnNames.Add(sqliteUtility.QuoteIdentifier(sanitizedValues[index].Key));
                    parameterNames.Add(parameterName);
                    command.Parameters.AddWithValue(parameterName, sqliteUtility.ToSqliteValue(sanitizedValues[index].Value));
                }

                command.CommandText = $"""
                INSERT INTO {sqliteUtility.QuoteIdentifier(tableName)} ({string.Join(", ", columnNames)})
                VALUES ({string.Join(", ", parameterNames)});
                """;
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(sqliteUtility.CreateSqliteEditError("insert", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not insert a row into SQLite table {TableName}.", tableName);
            }
           
        }

        public async Task DeleteRowAsync(string tableName, long rowId, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken).ConfigureAwait(false);
                await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(connection);
                await sqliteUtility.EnsureValidTableAsync(connection, tableName, cancellationToken).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM {sqliteUtility.QuoteIdentifier(tableName)} WHERE rowid = $rowid;";
                command.Parameters.AddWithValue("$rowid", rowId);
                try
                {

                    logger.LogWarning("Deleting row {RowId} from SQLite table {TableName}; referential consistency is the caller's responsibility.", rowId, tableName);
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(sqliteUtility.CreateSqliteEditError("delete", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not delete row {RowId} from SQLite table {TableName}.", rowId, tableName);
            }
        }

        private async Task EnsureDatabaseFileAsync(CancellationToken cancellationToken)
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<SqliteConnection?> OpenConnectionAsync(CancellationToken cancellationToken)
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
                return null;
            }
        }
 
    }
}
