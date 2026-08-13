using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for variable store behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IVariableStoreService
{
    /// <summary>
    /// Performs get as part of the variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="IVariableStoreService"/>.</typeparam>
    /// <param name="name">Name value supplied to the variable store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The t produced by the operation.</returns>
    Task<T> GetAsync<T>(string name, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs set as part of the variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="IVariableStoreService"/>.</typeparam>
    /// <param name="name">Name value supplied to the variable store operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the variable store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task SetAsync<T>(string name, T value, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lists all as part of the variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IEnumerable<SystemVariable>> ListAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Lists all as part of the variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="filter">Filter value supplied to the variable store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IEnumerable<SystemVariable>> ListAllAsync(string filter, CancellationToken cancellationToken = default);
}
