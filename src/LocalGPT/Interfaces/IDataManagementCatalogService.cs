using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Reads database-backed table groups and their actual SQLite foreign-key hierarchy.</summary>
public interface IDataManagementCatalogService
{
    /// <summary>Retrieves all user-visible SQLite tables and parent/child relationships.</summary>
    /// <param name="cancellationToken">Cancellation token for the database read.</param>
    /// <returns>Live table summaries with foreign-key relationships.</returns>
    Task<IReadOnlyList<DataManagementTableInfo>> GetTablesAsync(CancellationToken cancellationToken = default);
}
