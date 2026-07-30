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
                logger.LogError(ex, $"Error in IsLongTextColumn columnName: {columnName} value:{value}", columnName, value, logger);
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
                logger.LogError(ex, $"Error in BuildColumnTitle column: {column.ToString()}", column, logger);
                return string.Empty ;
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
                    expando[pair.Key] = pair.Value;
                return (ExpandoObject)expando;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToGridRow row: {row.ToString()}", row, logger);
                return null;
            }
         
        }
    }
}
