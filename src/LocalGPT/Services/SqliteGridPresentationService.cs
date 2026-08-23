using LocalGPT.BusinessObjects;
using System.Dynamic;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates sqlite grid presentation behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="serviceLogger">Sqlite grid presentation service dependency used by the sqlite grid presentation workflow to provide the corresponding application capability.</param>
    public sealed class SqliteGridPresentationService(ILogger<SqliteGridPresentationService> serviceLogger)
    {
        /// <summary>
        /// Determines whether long text column as part of the sqlite grid presentation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="columnName">Column name value supplied to the sqlite grid presentation operation and used when producing its result.</param>
        /// <param name="value">Value value supplied to the sqlite grid presentation operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsLongTextColumn(string columnName, string value, ILogger logger)
        {
            try
            {
                serviceLogger.LogTrace("SQLite grid presentation operation {Operation} started.", nameof(IsLongTextColumn));
                return value.Length > 120 ||
                columnName.Contains("Content", StringComparison.OrdinalIgnoreCase) ||
                columnName.Contains("Message", StringComparison.OrdinalIgnoreCase) ||
                columnName.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
                columnName.Contains("Sources", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify SQLite grid column {ColumnName}; cell content was omitted.", columnName);
                return false;
            }
        }

        /// <summary>
        /// Builds column title as part of the sqlite grid presentation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="column">Column value supplied to the sqlite grid presentation operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildColumnTitle(SqliteColumnSummary column, ILogger logger)
        {
            try
            {
                serviceLogger.LogTrace("SQLite grid presentation operation {Operation} started.", nameof(BuildColumnTitle));
                var required = column.IsNotNull ? "required" : "nullable";
                var key = column.IsPrimaryKey ? " Primary keys are protected by the editor." : string.Empty;
                return $"{column.Name} ({column.Type}, {required}).{key}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build a SQLite grid title for column {ColumnName}.", column.Name);
                return string.Empty ;
            }

        }
        /// <summary>
        /// Builds cell preview as part of the sqlite grid presentation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="columnName">Column name value supplied to the sqlite grid presentation operation and used when producing its result.</param>
        /// <param name="value">Value value supplied to the sqlite grid presentation operation and used when producing its result.</param>
        /// <returns>The object produced by the operation.</returns>
        public object? BuildCellPreview(string columnName, object? value)
        {
            try
            {
                if (value is null or DBNull) return null;
                if (value is not string text) return value;
                var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
                var lineCount = normalized.Count(character => character == '\n') + 1;
                var singleLine = string.Join(" ↵ ", normalized.Split('\n').Select(line => line.Trim()));
                if (!IsLongTextColumn(columnName, normalized, serviceLogger) && singleLine.Length <= 240)
                    return singleLine;
                const int previewLength = 320;
                var preview = singleLine.Length <= previewLength ? singleLine : singleLine[..previewLength] + "…";
                return $"{preview}  [compressed: {normalized.Length:N0} chars / {lineCount:N0} lines]";
            }
            catch (Exception ex)
            {
                serviceLogger.LogWarning(ex, "Could not create a compact SQLite grid preview for column {ColumnName}.", columnName);
                return "[preview unavailable; open the row editor]";
            }
        }

        /// <summary>
        /// Performs to grid row as part of the sqlite grid presentation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="row">Row value supplied to the sqlite grid presentation operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The expando object produced by the operation.</returns>
        public ExpandoObject? ToGridRow(SqliteRowSnapshot row, ILogger logger)
        {
            try
            {
                serviceLogger.LogTrace("SQLite grid presentation operation {Operation} started.", nameof(ToGridRow));
                IDictionary<string, object?> expando = new ExpandoObject();
                expando["__record"] = row.DisplayName;
                expando["__rowid"] = row.RowId;
                foreach (var pair in row.Values)
                    expando[pair.Key] = BuildCellPreview(pair.Key, pair.Value);
                return (ExpandoObject)expando;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build a compact SQLite grid row; row values were omitted.");
                return null;
            }
         
        }
    }
}
