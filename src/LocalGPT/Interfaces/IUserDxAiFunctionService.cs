using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Manages user-owned DXFunctions whose implementation is a persisted Remote Control action pipeline.</summary>
public interface IUserDxAiFunctionService
{
    /// <summary>Returns whether a Remote Control key belongs to the generated JSON/OData source-adapter namespace.</summary>
    /// <param name="key">Remote Control connector or pipeline key to classify.</param>
    /// <returns><see langword="true"/> when the key is owned by the generated user-source adapter workflow.</returns>
    bool IsGeneratedSourceKey(string? key);
    /// <summary>Creates the deterministic generated Remote Control key for a user-owned JSON/OData source function.</summary>
    /// <param name="functionName">User-owned runtime function name in the <c>user.*</c> namespace.</param>
    /// <returns>The bounded deterministic generated source-adapter key.</returns>
    string CreateGeneratedSourceKey(string functionName);
    /// <summary>Reloads enabled and disabled definitions into the synchronized runtime descriptor cache.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);
    /// <summary>Lists persisted user DXFunction definitions.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<UserDxAiFunctionDefinition>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs get as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The user DevExpress AI function definition produced by the operation.</returns>
    Task<UserDxAiFunctionDefinition?> GetAsync(string functionName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs save as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The user DevExpress AI function definition produced by the operation.</returns>
    Task<UserDxAiFunctionDefinition> SaveAsync(SaveUserDxAiFunctionRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs delete as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> DeleteAsync(string functionName, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Returns the current enabled runtime descriptors without database I/O.</summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<DxaichatFunctionInfo> GetDescriptors();
    /// <summary>Tries to resolve one enabled runtime descriptor without database I/O.</summary>
    /// <param name="functionName">Function name value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <param name="descriptor">Descriptor value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryGetDescriptor(string functionName, out DxaichatFunctionInfo descriptor);
    /// <summary>Invokes the definition's Remote Control pipeline. Registry security policy is expected to have run before this call.</summary>
    /// <param name="functionName">Function name value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    Task<DxAiFunctionInvocationResult> InvokeAsync(string functionName, DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default);
}
