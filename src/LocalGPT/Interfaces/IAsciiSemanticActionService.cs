using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Maps pointer, keyboard, gamepad and AI requests onto one authoritative semantic ASCII/game action contract.</summary>
public interface IAsciiSemanticActionService
{
    /// <summary>
    /// Lists actions as part of the ascii semantic action service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<AsciiSemanticActionDescriptor> ListActions();
    /// <summary>
    /// Performs invoke as part of the ascii semantic action service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    Task<CouncilGameSessionSnapshot> InvokeAsync(AsciiSemanticActionRequest request, CancellationToken cancellationToken = default);
}
