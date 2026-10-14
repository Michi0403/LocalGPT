using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Routes semantic ASCII/game controls through the authoritative Council game-session service.</summary>
/// <param name="games">Council game session service dependency used by the ascii semantic action workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class AsciiSemanticActionService(
    ICouncilGameSessionService games,
    ILogger<AsciiSemanticActionService> logger) : IAsciiSemanticActionService
{
    /// <summary>
    /// Stores the in-memory actions collection maintained internally by <see cref="AsciiSemanticActionService"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<AsciiSemanticActionDescriptor> Actions =
    [
        new() { Key = "move-forward", DisplayName = "move forward", Description = "Move forward / choice up", Category = "Movement" },
        new() { Key = "move-backward", DisplayName = "move backward", Description = "Move backward / choice down", Category = "Movement" },
        new() { Key = "strafe-left", DisplayName = "strafe left", Description = "Strafe left", Category = "Movement" },
        new() { Key = "strafe-right", DisplayName = "strafe right", Description = "Strafe right", Category = "Movement" },
        new() { Key = "turn-left", DisplayName = "turn left", Description = "Turn / aim left", Category = "Movement" },
        new() { Key = "turn-right", DisplayName = "turn right", Description = "Turn / aim right", Category = "Movement" },
        new() { Key = "shoot", DisplayName = "shoot", Description = "Primary action / shoot", Category = "Action" },
        new() { Key = "duck", DisplayName = "duck", Description = "Duck / secondary action", Category = "Action" },
        new() { Key = "use", DisplayName = "use", Description = "Use / accept", Category = "Action" },
        new() { Key = "choice-1", DisplayName = "choice 1", Description = "Choose option 1", Category = "Choice" },
        new() { Key = "choice-2", DisplayName = "choice 2", Description = "Choose option 2", Category = "Choice" },
        new() { Key = "choice-3", DisplayName = "choice 3", Description = "Choose option 3", Category = "Choice" }
    ];

    /// <summary>
    /// Lists actions as part of the ascii semantic action service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<AsciiSemanticActionDescriptor> ListActions()
    {
        try
        {
            logger.LogDebug("Listed {ActionCount} semantic ASCII actions.", Actions.Count);
            return Actions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Listing semantic ASCII actions failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs invoke as part of the ascii semantic action service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    public async Task<CouncilGameSessionSnapshot> InvokeAsync(AsciiSemanticActionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var action = request.Action.Trim().ToLowerInvariant();
            if (!Actions.Any(item => string.Equals(item.Key, action, StringComparison.Ordinal)))
                throw new ArgumentException($"Unknown semantic ASCII action '{request.Action}'.", nameof(request));
            return await games.ApplyControlAsync(new CouncilGameControlRequest
            {
                SessionId = request.SessionId,
                Action = action,
                Source = "SemanticAction",
                ActorName = "AI Council semantic action",
                ActorKind = CouncilGameActorKind.Director,
                RuntimeClassKey = "games.ascii.semantic.ai",
                ExpectedTurn = request.ExpectedTurn
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invoking semantic ASCII action failed.");
            throw;
        }
    }
}
