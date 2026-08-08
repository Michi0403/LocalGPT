using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IDxAiFunctionCallRecoveryService
{
    DxAiFunctionTextRecoveryResult Recover(string content, bool automaticInvocation = true);
    bool LooksLikeStructuredFunctionCall(string content);
}
