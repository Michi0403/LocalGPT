namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a sqlite table summary.
    /// </summary>
    public sealed record SqliteTableSummary(
        string Name,
        int ColumnCount,
        long RowCount,
        bool HasPrimaryKey);

    /// <summary>
    /// Represents a sqlite column summary.
    /// </summary>
    public sealed record SqliteColumnSummary(
        string Name,
        string Type,
        bool IsNotNull,
        bool IsPrimaryKey,
        string? DefaultValue);

    /// <summary>
    /// Represents a sqlite table snapshot.
    /// </summary>
    public sealed class SqliteTableSnapshot
    {
        /// <summary>
        /// Gets or sets database path.
        /// </summary>
        public string DatabasePath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets table name.
        /// </summary>
        public string TableName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets columns.
        /// </summary>
        public List<SqliteColumnSummary> Columns { get; set; } = [];
        /// <summary>
        /// Gets or sets rows.
        /// </summary>
        public List<SqliteRowSnapshot> Rows { get; set; } = [];
    }

    /// <summary>
    /// Represents a sqlite row snapshot.
    /// </summary>
    public sealed class SqliteRowSnapshot
    {
        /// <summary>
        /// Gets or sets row identifier.
        /// </summary>
        public long RowId { get; set; }
        /// <summary>
        /// Gets or sets values.
        /// </summary>
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

    /// <summary>
    /// Represents a sqlite cell update.
    /// </summary>
    public sealed class SqliteCellUpdate
    {
        /// <summary>
        /// Gets or sets column name.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets value.
        /// </summary>
        public string? Value { get; set; }
    }
}
