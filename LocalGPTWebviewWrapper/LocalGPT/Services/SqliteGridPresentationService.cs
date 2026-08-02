using LocalGPT.BusinessObjects;
using System.Dynamic;

namespace LocalGPT.Services
{
    public sealed class SqliteGridPresentationService(ILogger<SqliteGridPresentationService> serviceLogger)
    {
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

        public ExpandoObject? ToGridRow(SqliteRowSnapshot row, ILogger logger)
        {
            try
            {
                serviceLogger.LogTrace("SQLite grid presentation operation {Operation} started.", nameof(ToGridRow));
                IDictionary<string, object?> expando = new ExpandoObject();
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
