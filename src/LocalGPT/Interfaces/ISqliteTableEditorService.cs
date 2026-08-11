using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the sqlite table editor service contract.
    /// </summary>
    public interface ISqliteTableEditorService
    {
        string DatabasePath { get; }
        /// <summary>
        /// Gets tables async.
        /// </summary>
        Task<IReadOnlyList<SqliteTableSummary>> GetTablesAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets table async.
        /// </summary>
        Task<SqliteTableSnapshot> GetTableAsync(string tableName, int take = 100, CancellationToken cancellationToken = default);
        /// <summary>
        /// Updates row async.
        /// </summary>
        Task UpdateRowAsync(string tableName, long rowId, IReadOnlyList<SqliteCellUpdate> updates, CancellationToken cancellationToken = default);
        /// <summary>
        /// Runs the insert row async operation.
        /// </summary>
        Task InsertRowAsync(string tableName, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default);
        /// <summary>
        /// Deletes row async.
        /// </summary>
        Task DeleteRowAsync(string tableName, long rowId, CancellationToken cancellationToken = default);
    }
}
