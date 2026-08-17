using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates sqlite utility behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="serviceLogger">Sqlite utility service dependency used by the sqlite utility workflow to provide the corresponding application capability.</param>
    public sealed class SqliteUtilityService(ILogger<SqliteUtilityService> serviceLogger)
    {

        /// <summary>
        /// Parses value as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <typeparam name="T">Type used for t values handled by <see cref="SqliteUtilityService"/>.</typeparam>
        /// <param name="valueString">Value string value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="dataType">Data type value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The t produced by the operation.</returns>
        public T ParseValue<T>(string valueString, string? dataType, ILogger logger)
        {
            try
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)valueString;
                else if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(valueString);
                else if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(valueString);
                else if (typeof(T) == typeof(double))
                    return (T)(object)double.Parse(valueString);
                else if (typeof(T) == typeof(float))
                    return (T)(object)float.Parse(valueString);
                else if (typeof(T) == typeof(DateTime))
                    return (T)(object)DateTime.Parse(valueString);
                else
                    return (T)(object)valueString;
                // Add more conversions as needed
                throw new NotSupportedException($"Parsing to {typeof(T)} is not supported");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ParseValue valueString {valueString} dataType {dataType} ex {ex.ToString()}");
                throw;
            }
        }
        /// <summary>
        /// Determines whether power shell as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="executable">Executable value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsPowerShell(string executable, ILogger logger)
        {
            try
            {
                return executable.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsPowerShell executable {executable}");
                return false;
            }
        }

        /// <summary>
        /// Determines whether gradle as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="executable">Executable value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsGradle(string executable, ILogger logger)
        {
            try
            {
                return executable.Equals("gradle", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("gradle.bat", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("gradlew", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("gradlew.bat", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsPowerShell executable {executable}");
                return false;
            }
        }

        /// <summary>
        /// Performs classify command profile as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="executable">Executable value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="arguments">Arguments value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ClassifyCommandProfile(string executable, string arguments, ILogger logger)
        {
            try
            {
                if (IsGradle(executable, logger))
                {
                    return arguments.Contains("runClient", StringComparison.OrdinalIgnoreCase)
                        ? "GradleRunClient"
                        : "GradleBuildOnly";
                }

                if (executable.Equals("java", StringComparison.OrdinalIgnoreCase) ||
                    executable.Equals("java.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var normalized = arguments.Trim();
                    return normalized.Equals("-version", StringComparison.OrdinalIgnoreCase) ||
                        normalized.Equals("--version", StringComparison.OrdinalIgnoreCase)
                        ? "JavaVersionOnly"
                        : "JavaAllowlistedCommand";
                }

                return "CustomAllowlistedCommand";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ClassifyCommandProfile executable {executable} arguments {arguments}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs contains path segment as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="fileName">File name value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool ContainsPathSegment(string fileName, ILogger logger)
        {
            try
            {
                return fileName.Contains(Path.DirectorySeparatorChar) ||
               fileName.Contains(Path.AltDirectorySeparatorChar);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ClassifyCommandProfile fileName {fileName}");
                return false;
            }

        }

        /// <summary>
        /// Performs sanitize file name as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string SanitizeFileName(string value, ILogger logger)
        {
            try
            {
                var invalid = Path.GetInvalidFileNameChars();
                var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
                return string.IsNullOrWhiteSpace(safe) ? "command" : safe;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SanitizeFileName value {value}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Ensures valid table as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="connection">Connection value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="tableName">Table name value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        public async Task EnsureValidTableAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken, ILogger? logger = null)
        {
    try
    {
                try
                {
                    if (string.IsNullOrWhiteSpace(tableName))
                        throw new InvalidOperationException("Select a table first.");

                    var command = connection.CreateCommand();
                    await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
                    command.CommandText = """
                    SELECT COUNT(*)
                    FROM sqlite_master
                    WHERE type = 'table'
                      AND name = $name
                      AND name NOT LIKE 'sqlite_%';
                    """;
                    command.Parameters.AddWithValue("$name", tableName);
                    var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture);
                    if (count == 0)
                        throw new InvalidOperationException($"SQLite table '{tableName}' was not found or is not editable.");
                }
                catch (Exception ex)
                {
                    (logger ?? serviceLogger).LogError(ex, "Could not validate SQLite table {TableName}.", tableName);
                    throw;
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(EnsureValidTableAsync)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(EnsureValidTableAsync)} failed.");
        throw;
    }
}

        /// <summary>
        /// Retrieves columns as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="connection">Connection value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="tableName">Table name value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public async Task<List<SqliteColumnSummary>> GetColumnsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken, ILogger? logger = null)
        {
    try
    {
                try
                {
                    await EnsureValidTableAsync(connection, tableName, cancellationToken, logger).ConfigureAwait(false);

                    var command = connection.CreateCommand();
                    await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
                    command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName, logger)});";

                    var columns = new List<SqliteColumnSummary>();
                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    await using var configuredReaderAsyncDisposal = reader.ConfigureAwait(false);
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
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
                catch (Exception ex)
                {
                    (logger ?? serviceLogger).LogError(ex, "Could not read SQLite schema for table {TableName}.", tableName);
                    throw;
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(GetColumnsAsync)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(GetColumnsAsync)} failed.");
        throw;
    }
}

        /// <summary>
        /// Retrieves row count as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="connection">Connection value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="tableName">Table name value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The long produced by the operation.</returns>
        public async Task<long> GetRowCountAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken, ILogger? logger = null)
        {
    try
    {
                try
                {
                    await EnsureValidTableAsync(connection, tableName, cancellationToken, logger).ConfigureAwait(false);

                    var command = connection.CreateCommand();
                    await using var configuredCommandAsyncDisposal = command.ConfigureAwait(false);
                    command.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(tableName, logger)};";
                    return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    (logger ?? serviceLogger).LogError(ex, "Could not count SQLite rows for table {TableName}.", tableName);
                    throw;
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(GetRowCountAsync)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(GetRowCountAsync)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs to sqlite value as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The object produced by the operation.</returns>
        public object? ToSqliteValue(string? value, ILogger? logger = null)
        {
            try
            {
                return value is null ? DBNull.Value : value;
            }
            catch (Exception ex)
            {
                if (logger is not null)
                {
                    logger.LogError(ex, $"Error in ToSqliteValue value {value?.ToString()}");
                }
                else
                {
                    serviceLogger.LogError(ex, "Could not convert a value to SQLite storage form; value content was omitted.");
                }
                return null;
            }

        }


        /// <summary>
        /// Performs to sqlite value as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="column">Column value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <returns>The object produced by the operation.</returns>
        public object ToSqliteValue(string? value, SqliteColumnSummary column)
        {
    try
    {
                if (value is null || value.Equals("[null]", StringComparison.OrdinalIgnoreCase))
                {
                    if (column.IsNotNull && string.IsNullOrWhiteSpace(column.DefaultValue))
                        throw new InvalidOperationException($"Column '{column.Name}' is required and cannot be NULL.");
                    return DBNull.Value;
                }

                var type = column.Type?.Trim() ?? string.Empty;
                if (type.Contains("INT", StringComparison.OrdinalIgnoreCase))
                {
                    if (bool.TryParse(value, out var boolean))
                        return boolean ? 1L : 0L;
                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                        return integer;
                    throw new FormatException($"Column '{column.Name}' requires an integer value.");
                }
                if (type.Contains("REAL", StringComparison.OrdinalIgnoreCase) ||
                    type.Contains("FLOA", StringComparison.OrdinalIgnoreCase) ||
                    type.Contains("DOUB", StringComparison.OrdinalIgnoreCase) ||
                    type.Contains("NUM", StringComparison.OrdinalIgnoreCase) ||
                    type.Contains("DEC", StringComparison.OrdinalIgnoreCase))
                {
                    if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                        return number;
                    throw new FormatException($"Column '{column.Name}' requires a numeric value using invariant decimal notation.");
                }
                if (column.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && value.Length == 36 && !Guid.TryParse(value, out _))
                    throw new FormatException($"Column '{column.Name}' requires a GUID value.");
                if ((column.Name.Contains("Date", StringComparison.OrdinalIgnoreCase) || column.Name.EndsWith("AtUtc", StringComparison.OrdinalIgnoreCase)) &&
                    !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                {
                    throw new FormatException($"Column '{column.Name}' requires an ISO-8601 date/time value.");
                }
                return value;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(ToSqliteValue)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(ToSqliteValue)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates sqlite edit error as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="operation">Operation value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="tableName">Table name value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="exception">Exception value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateSqliteEditError(string operation, string tableName, SqliteException exception, ILogger? logger = null)
        {
    try
    {
                try
                {
                    return $"SQLite {operation} failed for table '{tableName}'. Check required fields, foreign keys, and value types. SQLite said: {exception.SqliteErrorCode} {exception.Message}";
                }
                catch (Exception ex)
                {
                    (logger ?? serviceLogger).LogError(
                        ex,
                        "Could not create the SQLite edit error description for operation {Operation} and table {TableName}.",
                        operation,
                        tableName);
                    return string.Empty;
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(CreateSqliteEditError)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(CreateSqliteEditError)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs quote identifier as part of the sqlite utility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="identifier">Identifier value supplied to the sqlite utility operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string QuoteIdentifier(string identifier, ILogger? logger = null)
        {
    try
    {
                try
                {
                    if (string.IsNullOrWhiteSpace(identifier))
                        throw new InvalidOperationException("SQLite identifier cannot be empty.");

                    var builder = new StringBuilder(identifier.Length + 2);
                    builder.Append('"');
                    builder.Append(identifier.Replace("\"", "\"\"", StringComparison.Ordinal));
                    builder.Append('"');
                    return builder.ToString();
                }
                catch (Exception ex)
                {
                    (logger ?? serviceLogger).LogError(
                        ex,
                        "Could not quote a SQLite identifier; identifier content was omitted.");
                    return string.Empty;
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(QuoteIdentifier)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(SqliteUtilityService)}.{nameof(QuoteIdentifier)} failed.");
        throw;
    }
}
    }
}
