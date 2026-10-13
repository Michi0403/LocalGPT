using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Persistent, payload-free operations history for the function workbench.</summary>
public interface IDxAiFunctionInvocationAuditService
{
    Task RecordAsync(DxAiFunctionInvocationResult result, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DxAiFunctionInvocationAuditEntry>> GetRecentAsync(int take = 30, CancellationToken cancellationToken = default);
}
