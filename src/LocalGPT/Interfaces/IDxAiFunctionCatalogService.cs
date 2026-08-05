using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IDxAiFunctionCatalogService
{
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> SynchronizeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);
    Task<DxAiFunctionCatalogEntry?> GetEntryAsync(string catalogKey, CancellationToken cancellationToken = default);
    Task<DxAiFunctionCatalogEntry?> GetByFunctionNameAsync(string functionName, CancellationToken cancellationToken = default);
    Task<DxAiFunctionCatalogEntry> SavePolicyAsync(DxAiFunctionCatalogSaveRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetExposedToPeerAsync(string peerId, CancellationToken cancellationToken = default);
}

public interface IPublicServiceMethodInvoker
{
    Task<object?> InvokeAsync(PublicServiceMethodInvocationRequest request, CancellationToken cancellationToken = default);
}
