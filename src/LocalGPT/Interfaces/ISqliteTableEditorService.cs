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
