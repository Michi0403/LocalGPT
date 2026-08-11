using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the DevExpress ai function catalog service contract.
/// </summary>
public interface IDxAiFunctionCatalogService
{
    /// <summary>
    /// Runs the synchronize async operation.
    /// </summary>
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> SynchronizeAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets entries async.
    /// </summary>
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets entry async.
    /// </summary>
    Task<DxAiFunctionCatalogEntry?> GetEntryAsync(string catalogKey, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets by function name async.
    /// </summary>
    Task<DxAiFunctionCatalogEntry?> GetByFunctionNameAsync(string functionName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves policy async.
    /// </summary>
    Task<DxAiFunctionCatalogEntry> SavePolicyAsync(DxAiFunctionCatalogSaveRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets exposed to peer async.
    /// </summary>
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetExposedToPeerAsync(string peerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the public service method invoker contract.
/// </summary>
public interface IPublicServiceMethodInvoker
{
    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    Task<object?> InvokeAsync(PublicServiceMethodInvocationRequest request, CancellationToken cancellationToken = default);
}
