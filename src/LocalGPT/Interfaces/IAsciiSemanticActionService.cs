using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Maps pointer, keyboard, gamepad and AI requests onto one authoritative semantic ASCII/game action contract.</summary>
public interface IAsciiSemanticActionService
{
    IReadOnlyList<AsciiSemanticActionDescriptor> ListActions();
    Task<CouncilGameSessionSnapshot> InvokeAsync(AsciiSemanticActionRequest request, CancellationToken cancellationToken = default);
}
