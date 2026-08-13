using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for DevExpress AI function call recovery behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDxAiFunctionCallRecoveryService
{
    /// <summary>
    /// Performs recover as part of the DevExpress AI function call recovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the DevExpress AI function call recovery operation and used when producing its result.</param>
    /// <param name="automaticInvocation">Value indicating whether automatic invocation should apply to this operation.</param>
    /// <returns>The DevExpress AI function text recovery result produced by the operation.</returns>
    DxAiFunctionTextRecoveryResult Recover(string content, bool automaticInvocation = true);
    /// <summary>
    /// Performs looks like structured function call as part of the DevExpress AI function call recovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the DevExpress AI function call recovery operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool LooksLikeStructuredFunctionCall(string content);
}
