using System.Data;
using System.Globalization;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;

namespace LocalGPT.Services
{
    public sealed class SqliteTableEditorService : ISqliteTableEditorService
    {
        private const int MaxRows = 500;

        public string DatabasePath => EfChatMemoryService.GetDefaultDatabasePath();

        public async Task<IReadOnlyList<SqliteTableSummary>> GetTablesAsync(CancellationToken cancellationToken = default)
        {
            await EnsureDatabaseFileAsync(cancellationToken);
            await using var connection = await OpenConnectionAsync(cancellationToken);

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
                var columns = await GetColumnsAsync(connection, name, cancellationToken);
                var rowCount = await GetRowCountAsync(connection, name, cancellationToken);
                tables.Add(new SqliteTableSummary(
                    name,
                    columns.Count,
                    rowCount,
                    columns.Any(column => column.IsPrimaryKey)));
            }

            return tables;
        }

        public async Task<SqliteTableSnapshot> GetTableAsync(string tableName, int take = 100, CancellationToken cancellationToken = default)
        {
            await EnsureDatabaseFileAsync(cancellationToken);
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await EnsureValidTableAsync(connection, tableName, cancellationToken);

            var safeTake = Math.Clamp(take, 1, MaxRows);
            var columns = await GetColumnsAsync(connection, tableName, cancellationToken);
            var rows = new List<SqliteRowSnapshot>();

            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT rowid AS "__rowid", *
                FROM {QuoteIdentifier(tableName)}
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

        public async Task UpdateRowAsync(string tableName, long rowId, IReadOnlyList<SqliteCellUpdate> updates, CancellationToken cancellationToken = default)
        {
            if (updates.Count == 0)
                return;

            await EnsureDatabaseFileAsync(cancellationToken);
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await EnsureValidTableAsync(connection, tableName, cancellationToken);

            var columns = await GetColumnsAsync(connection, tableName, cancellationToken);
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

            ValidateRequiredColumnUpdates(columns, sanitizedUpdates);

            await using var command = connection.CreateCommand();
            var assignments = new List<string>();
            for (var index = 0; index < sanitizedUpdates.Count; index++)
            {
                var parameterName = $"$value{index}";
                assignments.Add($"{QuoteIdentifier(sanitizedUpdates[index].ColumnName)} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, ToSqliteValue(sanitizedUpdates[index].Value));
            }

            command.CommandText = $"""
                UPDATE {QuoteIdentifier(tableName)}
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
                throw new InvalidOperationException(CreateSqliteEditError("update", tableName, ex), ex);
            }
        }

        public async Task InsertRowAsync(string tableName, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default)
        {
            await EnsureDatabaseFileAsync(cancellationToken);
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await EnsureValidTableAsync(connection, tableName, cancellationToken);

            var columns = await GetColumnsAsync(connection, tableName, cancellationToken);
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

            ValidateRequiredInsertColumns(columns, sanitizedValues);

            await using var command = connection.CreateCommand();
            var columnNames = new List<string>();
            var parameterNames = new List<string>();
            for (var index = 0; index < sanitizedValues.Count; index++)
            {
                var parameterName = $"$value{index}";
                columnNames.Add(QuoteIdentifier(sanitizedValues[index].Key));
                parameterNames.Add(parameterName);
                command.Parameters.AddWithValue(parameterName, ToSqliteValue(sanitizedValues[index].Value));
            }

            command.CommandText = $"""
                INSERT INTO {QuoteIdentifier(tableName)} ({string.Join(", ", columnNames)})
                VALUES ({string.Join(", ", parameterNames)});
                """;
            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqliteException ex)
            {
                throw new InvalidOperationException(CreateSqliteEditError("insert", tableName, ex), ex);
            }
        }

        public async Task DeleteRowAsync(string tableName, long rowId, CancellationToken cancellationToken = default)
        {
            await EnsureDatabaseFileAsync(cancellationToken);
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await EnsureValidTableAsync(connection, tableName, cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {QuoteIdentifier(tableName)} WHERE rowid = $rowid;";
            command.Parameters.AddWithValue("$rowid", rowId);
            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqliteException ex)
            {
                throw new InvalidOperationException(CreateSqliteEditError("delete", tableName, ex), ex);
            }
        }

        private async Task EnsureDatabaseFileAsync(CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            await connection.OpenAsync(cancellationToken);
        }

        private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection($"Data Source={DatabasePath}");
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static async Task EnsureValidTableAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException("Select a table first.");

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = $name
                  AND name NOT LIKE 'sqlite_%';
                """;
            command.Parameters.AddWithValue("$name", tableName);
            var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            if (count == 0)
                throw new InvalidOperationException($"SQLite table '{tableName}' was not found or is not editable.");
        }

        private static async Task<List<SqliteColumnSummary>> GetColumnsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
        {
            await EnsureValidTableAsync(connection, tableName, cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";

            var columns = new List<SqliteColumnSummary>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var nameOrdinal = reader.GetOrdinal("name");
                var typeOrdinal = reader.GetOrdinal("type");
                var notNullOrdinal = reader.GetOrdinal("notnull");
                var primaryKeyOrdinal = reader.GetOrdinal("pk");
                var defaultValueOrdinal = reader.GetOrdinal("dflt_value");

                columns.Add(new SqliteColumnSummary(
                    reader.GetString(nameOrdinal),
                    reader.IsDBNull(typeOrdinal) ? string.Empty : reader.GetString(typeOrdinal),
                    reader.GetInt64(notNullOrdinal) != 0,
                    reader.GetInt64(primaryKeyOrdinal) != 0,
                    reader.IsDBNull(defaultValueOrdinal) ? null : reader.GetString(defaultValueOrdinal)));
            }

            return columns;
        }

        private static async Task<long> GetRowCountAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
        {
            await EnsureValidTableAsync(connection, tableName, cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(tableName)};";
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        }

        private static object ToSqliteValue(string? value)
        {
            return value is null ? DBNull.Value : value;
        }

        private static void ValidateRequiredColumnUpdates(
            IReadOnlyList<SqliteColumnSummary> columns,
            IReadOnlyList<SqliteCellUpdate> updates)
        {
            var requiredColumns = columns
                .Where(IsRequiredEditableColumn)
                .Select(column => column.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var nullAssignments = updates
                .Where(update => requiredColumns.Contains(update.ColumnName) && update.Value is null)
                .Select(update => update.ColumnName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (nullAssignments.Count > 0)
                throw new InvalidOperationException($"SQLite update blocked: required column(s) cannot be set to NULL: {string.Join(", ", nullAssignments)}.");
        }

        private static void ValidateRequiredInsertColumns(
            IReadOnlyList<SqliteColumnSummary> columns,
            IReadOnlyList<KeyValuePair<string, string?>> values)
        {
            var providedColumns = values
                .Where(pair => pair.Value is not null)
                .Select(pair => pair.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingColumns = columns
                .Where(IsRequiredEditableColumn)
                .Where(column => !providedColumns.Contains(column.Name))
                .Select(column => column.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingColumns.Count > 0)
                throw new InvalidOperationException($"SQLite insert blocked: required column(s) need a value first: {string.Join(", ", missingColumns)}.");
        }

        private static bool IsRequiredEditableColumn(SqliteColumnSummary column) =>
            column.IsNotNull &&
            !column.IsPrimaryKey &&
            string.IsNullOrWhiteSpace(column.DefaultValue);

        private static string CreateSqliteEditError(string operation, string tableName, SqliteException exception)
        {
            return $"SQLite {operation} failed for table '{tableName}'. Check required fields, foreign keys, and value types. SQLite said: {exception.SqliteErrorCode} {exception.Message}";
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new InvalidOperationException("SQLite identifier cannot be empty.");

            var builder = new StringBuilder(identifier.Length + 2);
            builder.Append('"');
            builder.Append(identifier.Replace("\"", "\"\"", StringComparison.Ordinal));
            builder.Append('"');
            return builder.ToString();
        }
    }
}
