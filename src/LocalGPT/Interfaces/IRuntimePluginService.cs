using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Persists, materializes, loads, and invokes user-owned runtime extensions.</summary>
public interface IRuntimePluginService
{
    /// <summary>Refreshes the runtime descriptor cache from persistence.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);
    /// <summary>Lists persisted runtime extensions.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RuntimePluginDefinition>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs save as part of the runtime plugin service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The runtime plugin definition produced by the operation.</returns>
    Task<RuntimePluginDefinition> SaveAsync(SaveRuntimePluginRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs delete as part of the runtime plugin service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> DeleteAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Builds/materializes and activates an extension.</summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The runtime plugin build result produced by the operation.</returns>
    Task<RuntimePluginBuildResult> BuildAndLoadAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves descriptors as part of the runtime plugin service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<DxaichatFunctionInfo> GetDescriptors();
    /// <summary>Tries to resolve one enabled dynamic descriptor without database I/O.</summary>
    /// <param name="functionName">Function name value supplied to the runtime plugin operation and used when producing its result.</param>
    /// <param name="descriptor">Descriptor value supplied to the runtime plugin operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryGetDescriptor(string functionName, out DxaichatFunctionInfo descriptor);
    /// <summary>Invokes one runtime extension after the central registry security policy has run.</summary>
    /// <param name="functionName">Function name value supplied to the runtime plugin operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    Task<DxAiFunctionInvocationResult> InvokeAsync(string functionName, DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default);
    /// <summary>Returns a starter source template for the requested extension kind.</summary>
    /// <param name="kind">Kind value supplied to the runtime plugin operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetTemplate(RuntimePluginKind kind);
}
