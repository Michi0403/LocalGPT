using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>One application-facing contract for parameter preview, approved execution and audit readback.</summary>
public interface IDxAiFunctionWorkbenchService
{
    /// <summary>
    /// Performs preview as part of the DevExpress AI function workbench service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="parametersJson">Parameters json value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    Task<DxAiFunctionInvocationResult> PreviewAsync(string functionName, string parametersJson, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs execute as part of the DevExpress AI function workbench service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="parametersJson">Parameters json value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="previewFingerprint">Preview fingerprint value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    Task<DxAiFunctionInvocationResult> ExecuteAsync(string functionName, string parametersJson, string previewFingerprint, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves recent as part of the DevExpress AI function workbench service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DxAiFunctionInvocationAuditEntry>> GetRecentAsync(int take = 30, CancellationToken cancellationToken = default);
}
