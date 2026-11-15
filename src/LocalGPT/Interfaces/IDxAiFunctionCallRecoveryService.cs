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
    /// <summary>
    /// Recovers only explicit <c>&lt;localgpt-dx-call&gt;</c> tagged function calls wherever the tag appears in completed assistant text, including after prose; bare JSON, fenced JSON and other shapes are never recovered by this operation.
    /// </summary>
    /// <param name="content">Completed assistant text that may contain an explicit tagged function call after other prose.</param>
    /// <param name="automaticInvocation">Value indicating whether the recovered calls are resolved for automatic invocation.</param>
    /// <returns>The recovery result; <see cref="DxAiFunctionTextRecoveryResult.Recognized"/> is false when no tag resolved to a registered function, and <see cref="DxAiFunctionTextRecoveryResult.VisibleContent"/> holds the text without the tag.</returns>
    DxAiFunctionTextRecoveryResult RecoverTagged(string content, bool automaticInvocation = true);
}
