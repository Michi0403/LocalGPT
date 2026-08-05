using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface ISqliteTableEditorService
    {
        string DatabasePath { get; }
        Task<IReadOnlyList<SqliteTableSummary>> GetTablesAsync(CancellationToken cancellationToken = default);
        Task<SqliteTableSnapshot> GetTableAsync(string tableName, int take = 100, CancellationToken cancellationToken = default);
        Task UpdateRowAsync(string tableName, long rowId, IReadOnlyList<SqliteCellUpdate> updates, CancellationToken cancellationToken = default);
        Task InsertRowAsync(string tableName, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default);
        Task DeleteRowAsync(string tableName, long rowId, CancellationToken cancellationToken = default);
    }
}
