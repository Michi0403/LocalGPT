using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace LocalGPT.Services
{
    public sealed class SqliteTableEditorService : ISqliteTableEditorService
    {
        private const int MaxRows = 500;

        public string DatabasePath => CouncilChatStaticsGeneral.GetDefaultDatabasePath();

        public async Task<IReadOnlyList<SqliteTableSummary>> GetTablesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken);
                await using var connection = await OpenConnectionAsync(cancellationToken);
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

                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        names.Add(reader.GetString(0));
                    }
                }

                var tables = new List<SqliteTableSummary>();
                foreach (var name in names)
                {
                    var columns = await SQLLiteFunctions.GetColumnsAsync(connection, name, cancellationToken);
                    var rowCount = await SQLLiteFunctions.GetRowCountAsync(connection, name, cancellationToken);
                    tables.Add(new SqliteTableSummary(
                        name,
                        columns.Count,
                        rowCount,
                        columns.Any(column => column.IsPrimaryKey)));
                }

                return tables;
            }
            catch (Exception ex)
            {
                Console.WriteLine( $"Error in GetTablesAsync ex {ex.ToString()}");
                return new List<SqliteTableSummary>();
            }
        }

        public async Task<SqliteTableSnapshot?> GetTableAsync(string tableName, int take = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken);
                await using var connection = await OpenConnectionAsync(cancellationToken);
                ArgumentNullException.ThrowIfNull(connection);
                await SQLLiteFunctions.EnsureValidTableAsync(connection, tableName, cancellationToken);

                var safeTake = Math.Clamp(take, 1, MaxRows);
                var columns = await SQLLiteFunctions.GetColumnsAsync(connection, tableName, cancellationToken);
                var rows = new List<SqliteRowSnapshot>();

                await using var command = connection.CreateCommand();
                command.CommandText = $"""
                SELECT rowid AS "__rowid", *
                FROM {SQLLiteFunctions.QuoteIdentifier(tableName)}
                ORDER BY rowid DESC
                LIMIT $take;
                """;
                command.Parameters.AddWithValue("$take", safeTake);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTableAsync ex {ex.ToString()}");
                return null;
            }
            
        }

        public async Task UpdateRowAsync(string tableName, long rowId, IReadOnlyList<SqliteCellUpdate> updates, CancellationToken cancellationToken = default)
        {
            try
            {
                if (updates.Count == 0)
                    return;

                await EnsureDatabaseFileAsync(cancellationToken);
                await using var connection = await OpenConnectionAsync(cancellationToken);
                ArgumentNullException.ThrowIfNull(connection);
                await SQLLiteFunctions.EnsureValidTableAsync(connection, tableName, cancellationToken);

                var columns = await SQLLiteFunctions.GetColumnsAsync(connection, tableName, cancellationToken);
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

                SQLLiteFunctions.ValidateRequiredColumnUpdates(columns, sanitizedUpdates);

                await using var command = connection.CreateCommand();
                var assignments = new List<string>();
                for (var index = 0; index < sanitizedUpdates.Count; index++)
                {
                    var parameterName = $"$value{index}";
                    assignments.Add($"{SQLLiteFunctions.QuoteIdentifier(sanitizedUpdates[index].ColumnName)} = {parameterName}");
                    command.Parameters.AddWithValue(parameterName, SQLLiteFunctions.ToSqliteValue(sanitizedUpdates[index].Value));
                }

                command.CommandText = $"""
                UPDATE {SQLLiteFunctions.QuoteIdentifier(tableName)}
                SET {string.Join(", ", assignments)}
                WHERE rowid = $rowid;
                """;
                command.Parameters.AddWithValue("$rowid", rowId);
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(SQLLiteFunctions.CreateSqliteEditError("update", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateRowAsync ex tableName {tableName} rowId {rowId} updates {updates.ToString()} ex {ex.ToString()}");
            }
        }

        public async Task InsertRowAsync(string tableName, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken);
                await using var connection = await OpenConnectionAsync(cancellationToken);
                ArgumentNullException.ThrowIfNull(connection);
                await SQLLiteFunctions.EnsureValidTableAsync(connection, tableName, cancellationToken);

                var columns = await SQLLiteFunctions.GetColumnsAsync(connection, tableName, cancellationToken);
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

                SQLLiteFunctions.ValidateRequiredInsertColumns(columns, sanitizedValues);

                await using var command = connection.CreateCommand();
                var columnNames = new List<string>();
                var parameterNames = new List<string>();
                for (var index = 0; index < sanitizedValues.Count; index++)
                {
                    var parameterName = $"$value{index}";
                    columnNames.Add(SQLLiteFunctions.QuoteIdentifier(sanitizedValues[index].Key));
                    parameterNames.Add(parameterName);
                    command.Parameters.AddWithValue(parameterName, SQLLiteFunctions.ToSqliteValue(sanitizedValues[index].Value));
                }

                command.CommandText = $"""
                INSERT INTO {SQLLiteFunctions.QuoteIdentifier(tableName)} ({string.Join(", ", columnNames)})
                VALUES ({string.Join(", ", parameterNames)});
                """;
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(SQLLiteFunctions.CreateSqliteEditError("insert", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InsertRowAsync tableName {tableName} values {values.ToString()} ex {ex.ToString()}");
            }
           
        }

        public async Task DeleteRowAsync(string tableName, long rowId, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseFileAsync(cancellationToken);
                await using var connection = await OpenConnectionAsync(cancellationToken);
                ArgumentNullException.ThrowIfNull(connection);
                await SQLLiteFunctions.EnsureValidTableAsync(connection, tableName, cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM {SQLLiteFunctions.QuoteIdentifier(tableName)} WHERE rowid = $rowid;";
                command.Parameters.AddWithValue("$rowid", rowId);
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException(SQLLiteFunctions.CreateSqliteEditError("delete", tableName, ex), ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteRowAsync tableName {tableName} rowId {rowId.ToString()} ex {ex.ToString()}");
            }
        }

        private async Task EnsureDatabaseFileAsync(CancellationToken cancellationToken)
        {
            try
            {
                var directory = Path.GetDirectoryName(DatabasePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                await connection.OpenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EnsureDatabaseFileAsync {ex.ToString()}");
            }

        }

        private async Task<SqliteConnection?> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var connection = new SqliteConnection($"Data Source={DatabasePath}");
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OpenConnectionAsync {ex.ToString()}");
                return null;
            }
        }
 
    }
}
