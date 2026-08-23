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
                string? preferredColumn = null;
                string? preferredValue = null;
                foreach (var columnName in new[]
                {
                    "DisplayName", "Name", "Title", "Topic", "Key", "FunctionName", "ModelName",
                    "ProviderName", "Label", "Version", "ProjectRelativePath", "Scope", "Status", "Description"
                })
                {
                    if (!Values.TryGetValue(columnName, out var candidate) || string.IsNullOrWhiteSpace(candidate))
                        continue;

                    preferredColumn = columnName;
                    preferredValue = candidate.Trim();
                    break;
                }

                if (string.IsNullOrWhiteSpace(preferredValue))
                {
                    var fallback = Values
                        .Where(pair => !string.IsNullOrWhiteSpace(pair.Value)
                            && !pair.Key.Equals("Id", StringComparison.OrdinalIgnoreCase)
                            && !pair.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
                    preferredColumn = fallback.Key;
                    preferredValue = fallback.Value?.Trim();
                }

                var stableIdentity = Values
                    .Where(pair => pair.Key.Equals("Id", StringComparison.OrdinalIgnoreCase)
                        || pair.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Value)
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

                if (string.IsNullOrWhiteSpace(preferredValue))
                    return !string.IsNullOrWhiteSpace(stableIdentity)
                        ? $"Record {stableIdentity}"
                        : $"Record #{RowId}";

                var compactValue = preferredValue.Length <= 96 ? preferredValue : $"{preferredValue[..93]}...";
                var prefix = string.IsNullOrWhiteSpace(preferredColumn) ? "Record" : preferredColumn;
                var compactIdentity = string.IsNullOrWhiteSpace(stableIdentity)
                    ? string.Empty
                    : stableIdentity.Length <= 16 ? stableIdentity : stableIdentity[..12] + "...";
                var identitySuffix = string.IsNullOrWhiteSpace(compactIdentity)
                    ? $" · row {RowId}"
                    : $" · id {compactIdentity}";
                return $"{prefix}: {compactValue}{identitySuffix}";
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
