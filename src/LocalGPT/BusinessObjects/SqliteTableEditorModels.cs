namespace LocalGPT.BusinessObjects
{
    public sealed record SqliteTableSummary(
        string Name,
        int ColumnCount,
        long RowCount,
        bool HasPrimaryKey);

    public sealed record SqliteColumnSummary(
        string Name,
        string Type,
        bool IsNotNull,
        bool IsPrimaryKey,
        string? DefaultValue);

    public sealed class SqliteTableSnapshot
    {
        public string DatabasePath { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public List<SqliteColumnSummary> Columns { get; set; } = [];
        public List<SqliteRowSnapshot> Rows { get; set; } = [];
    }

    public sealed class SqliteRowSnapshot
    {
        public long RowId { get; set; }
        public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string DisplayName
        {
            get
            {
                var firstValue = Values.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                return string.IsNullOrWhiteSpace(firstValue)
                    ? $"rowid {RowId}"
                    : $"rowid {RowId} - {firstValue}";
            }
        }
    }

    public sealed class SqliteCellUpdate
    {
        public string ColumnName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
