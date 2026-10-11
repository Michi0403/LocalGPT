using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for sqlite table editor behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface ISqliteTableEditorService
    {
        /// <summary>
        /// Gets the database path used by this sqlite table editor instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The database path value exposed by <see cref="ISqliteTableEditorService"/>.</value>
        string DatabasePath { get; }
        /// <summary>
        /// Retrieves tables as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<SqliteTableSummary>> GetTablesAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Retrieves table as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The sqlite table snapshot produced by the operation.</returns>
        Task<SqliteTableSnapshot> GetTableAsync(string tableName, int take = 100, CancellationToken cancellationToken = default);
        /// <summary>Reads a bounded page of table rows, retaining the existing service's table and column validation.</summary>
        /// <param name="tableName">Validated SQLite entity table.</param>
        /// <param name="take">Maximum page size.</param>
        /// <param name="skip">Nonnegative count of more recent rows to skip.</param>
        /// <param name="cancellationToken">Cancellation token for the database query.</param>
        /// <returns>The requested page and full table column metadata.</returns>
        Task<SqliteTableSnapshot> GetTablePageAsync(string tableName, int take, int skip, CancellationToken cancellationToken = default);
        /// <summary>Reads a bounded set of records matching one foreign-key/parent-key column.</summary>
        /// <param name="tableName">Name of a validated SQLite table.</param>
        /// <param name="columnName">Name of a validated column.</param>
        /// <param name="value">The relation value, or null for an SQL NULL match.</param>
        /// <param name="take">Maximum number of matching rows.</param>
        /// <param name="cancellationToken">Cancellation token for the query.</param>
        /// <returns>The matching records with their normal table metadata.</returns>
        Task<SqliteTableSnapshot> GetRelatedRowsAsync(string tableName, string columnName, string? value, int take = 100, CancellationToken cancellationToken = default);
        /// <summary>Reads a bounded offset page of rows matching a validated foreign-key relationship.</summary>
        /// <param name="tableName">Validated related table.</param>
        /// <param name="columnName">Validated relation column.</param>
        /// <param name="value">Exact relation value, or null for SQL NULL.</param>
        /// <param name="take">Maximum page size.</param>
        /// <param name="skip">Nonnegative offset within the related records.</param>
        /// <param name="cancellationToken">Cancellation token for the database query.</param>
        /// <returns>The selected relationship page.</returns>
        Task<SqliteTableSnapshot> GetRelatedRowsPageAsync(string tableName, string columnName, string? value, int take, int skip, CancellationToken cancellationToken = default);
        /// <summary>
        /// Updates row as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="rowId">Identifier of the row to use for this operation.</param>
        /// <param name="updates">Sqlite cell update dependency used by the sqlite table editor workflow to provide the corresponding application capability.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task UpdateRowAsync(string tableName, long rowId, IReadOnlyList<SqliteCellUpdate> updates, CancellationToken cancellationToken = default);
        /// <summary>
        /// Performs insert row as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="values">String dependency used by the sqlite table editor workflow to provide the corresponding application capability.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task InsertRowAsync(string tableName, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default);
        /// <summary>
        /// Deletes row as part of the sqlite table editor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the sqlite table editor operation and used when producing its result.</param>
        /// <param name="rowId">Identifier of the row to use for this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task DeleteRowAsync(string tableName, long rowId, CancellationToken cancellationToken = default);
    }
}
