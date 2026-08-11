using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the DevExpress ai function call recovery service contract.
/// </summary>
public interface IDxAiFunctionCallRecoveryService
{
    /// <summary>
    /// Runs the recover operation.
    /// </summary>
    DxAiFunctionTextRecoveryResult Recover(string content, bool automaticInvocation = true);
    /// <summary>
    /// Runs the looks like structured function call operation.
    /// </summary>
    bool LooksLikeStructuredFunctionCall(string content);
}
