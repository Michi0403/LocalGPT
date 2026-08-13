using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for DevExpress AI function catalog behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDxAiFunctionCatalogService
{
    /// <summary>
    /// Performs synchronize as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> SynchronizeAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves entries as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves entry as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="catalogKey">Catalog key value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
    Task<DxAiFunctionCatalogEntry?> GetEntryAsync(string catalogKey, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves by function name as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
    Task<DxAiFunctionCatalogEntry?> GetByFunctionNameAsync(string functionName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists policy as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
    Task<DxAiFunctionCatalogEntry> SavePolicyAsync(DxAiFunctionCatalogSaveRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves exposed to peer as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetExposedToPeerAsync(string peerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for public service method invoker behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPublicServiceMethodInvoker
{
    /// <summary>
    /// Performs invoke for <see cref="IPublicServiceMethodInvoker"/>, keeping the operation consistent with the state and invariants of the surrounding public service method invoker workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The object produced by the operation.</returns>
    Task<object?> InvokeAsync(PublicServiceMethodInvocationRequest request, CancellationToken cancellationToken = default);
}
