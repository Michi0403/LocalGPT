using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Persistent, payload-free operations history for the function workbench.</summary>
public interface IDxAiFunctionInvocationAuditService
{
    /// <summary>
    /// Performs record as part of the DevExpress AI function invocation audit service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the DevExpress AI function invocation audit operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RecordAsync(DxAiFunctionInvocationResult result, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves recent as part of the DevExpress AI function invocation audit service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the DevExpress AI function invocation audit operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DxAiFunctionInvocationAuditEntry>> GetRecentAsync(int take = 30, CancellationToken cancellationToken = default);
}
