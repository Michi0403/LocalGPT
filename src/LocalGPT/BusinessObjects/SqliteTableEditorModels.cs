namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a sqlite table summary application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Name">Name value supplied to the sqlite table summary operation and used when producing its result.</param>
    /// <param name="ColumnCount">Column count value supplied to the sqlite table summary operation and used when producing its result.</param>
    /// <param name="RowCount">Row count value supplied to the sqlite table summary operation and used when producing its result.</param>
    /// <param name="HasPrimaryKey">Value indicating whether primary key should apply to this operation.</param>
    public sealed record SqliteTableSummary(
        string Name,
        int ColumnCount,
        long RowCount,
        bool HasPrimaryKey);

    /// <summary>
    /// Represents a sqlite column summary application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Name">Name value supplied to the sqlite column summary operation and used when producing its result.</param>
    /// <param name="Type">Type value supplied to the sqlite column summary operation and used when producing its result.</param>
    /// <param name="IsNotNull">Value indicating whether not null should apply to this operation.</param>
    /// <param name="IsPrimaryKey">Value indicating whether primary key should apply to this operation.</param>
    /// <param name="DefaultValue">Default value value supplied to the sqlite column summary operation and used when producing its result.</param>
    public sealed record SqliteColumnSummary(
        string Name,
        string Type,
        bool IsNotNull,
        bool IsPrimaryKey,
        string? DefaultValue);

    /// <summary>
    /// Represents a sqlite table snapshot application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class SqliteTableSnapshot
    {
        /// <summary>
        /// Gets or sets the database path used by this sqlite table snapshot instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The database path value exposed by <see cref="SqliteTableSnapshot"/>.</value>
        public string DatabasePath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the table name value that forms part of the sqlite table snapshot state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The table name value exposed by <see cref="SqliteTableSnapshot"/>.</value>
        public string TableName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the columns collection maintained or exposed by this sqlite table snapshot instance for downstream processing.
        /// </summary>
        /// <value>The columns value exposed by <see cref="SqliteTableSnapshot"/>.</value>
        public List<SqliteColumnSummary> Columns { get; set; } = [];
        /// <summary>
        /// Gets or sets the rows collection maintained or exposed by this sqlite table snapshot instance for downstream processing.
        /// </summary>
        /// <value>The rows value exposed by <see cref="SqliteTableSnapshot"/>.</value>
        public List<SqliteRowSnapshot> Rows { get; set; } = [];
    }

    /// <summary>
    /// Represents a sqlite row snapshot application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class SqliteRowSnapshot
    {
        /// <summary>
        /// Gets or sets the stable row identifier used to identify or correlate this sqlite row snapshot instance with related application state.
        /// </summary>
        /// <value>The row identifier value exposed by <see cref="SqliteRowSnapshot"/>.</value>
        public long RowId { get; set; }
        /// <summary>
        /// Gets or sets the values collection maintained or exposed by this sqlite row snapshot instance for downstream processing.
        /// </summary>
        /// <value>The values value exposed by <see cref="SqliteRowSnapshot"/>.</value>
        public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the display name value that forms part of the sqlite row snapshot state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The display name value exposed by <see cref="SqliteRowSnapshot"/>.</value>
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
    /// Represents a sqlite cell update application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class SqliteCellUpdate
    {
        /// <summary>
        /// Gets or sets the column name value that forms part of the sqlite cell update state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The column name value exposed by <see cref="SqliteCellUpdate"/>.</value>
        public string ColumnName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the value value that forms part of the sqlite cell update state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The value value exposed by <see cref="SqliteCellUpdate"/>.</value>
        public string? Value { get; set; }
    }
}
