using LocalGPT.BusinessObjects;
using System.Dynamic;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class TableFunctions
    {
        public static bool IsLongTextColumn(string columnName, string value)
        {
            return value.Length > 120 ||
                columnName.Contains("Content", StringComparison.OrdinalIgnoreCase) ||
                columnName.Contains("Message", StringComparison.OrdinalIgnoreCase) ||
                columnName.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
                columnName.Contains("Sources", StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildColumnTitle(SqliteColumnSummary column)
        {
            var required = column.IsNotNull ? "required" : "nullable";
            var key = column.IsPrimaryKey ? " Primary keys are protected by the editor." : string.Empty;
            return $"{column.Name} ({column.Type}, {required}).{key}";
        }
        public static ExpandoObject ToGridRow(SqliteRowSnapshot row)
        {
            IDictionary<string, object?> expando = new ExpandoObject();
            expando["__rowid"] = row.RowId;
            foreach (var pair in row.Values)
                expando[pair.Key] = pair.Value;
            return (ExpandoObject)expando;
        }
    }
}
