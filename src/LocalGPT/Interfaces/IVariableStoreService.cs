using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the variable store service contract.
/// </summary>
public interface IVariableStoreService
{
    /// <summary>
    /// Gets async.
    /// </summary>
    Task<T> GetAsync<T>(string name, CancellationToken cancellationToken = default);
    /// <summary>
    /// Sets async.
    /// </summary>
    Task SetAsync<T>(string name, T value, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the list all async operation.
    /// </summary>
    Task<IEnumerable<SystemVariable>> ListAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the list all async operation.
    /// </summary>
    Task<IEnumerable<SystemVariable>> ListAllAsync(string filter, CancellationToken cancellationToken = default);
}
