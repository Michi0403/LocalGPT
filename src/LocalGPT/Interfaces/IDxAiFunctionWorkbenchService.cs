using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>One application-facing contract for parameter preview, approved execution and audit readback.</summary>
public interface IDxAiFunctionWorkbenchService
{
    Task<DxAiFunctionInvocationResult> PreviewAsync(string functionName, string parametersJson, CancellationToken cancellationToken = default);
    Task<DxAiFunctionInvocationResult> ExecuteAsync(string functionName, string parametersJson, string previewFingerprint, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DxAiFunctionInvocationAuditEntry>> GetRecentAsync(int take = 30, CancellationToken cancellationToken = default);
}
