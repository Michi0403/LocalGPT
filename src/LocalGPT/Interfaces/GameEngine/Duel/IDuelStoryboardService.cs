using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Renders the storyboard frames of a phase from the ruleset's <c>storyboard.{phase}</c> rule rows.
/// </summary>
public interface IDuelStoryboardService
{
    /// <summary>
    /// Builds the frames of a phase whose conditions hold.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="phase">The storyboard phase.</param>
    /// <param name="left">The left owner, or <c>null</c>.</param>
    /// <param name="right">The right owner, or <c>null</c>.</param>
    /// <param name="tokens">The storyboard tokens.</param>
    /// <returns>The frames.</returns>
    List<GameViewFrame> Build(GameEngineContext context, DuelSettings settings, string phase, GameActorState? left, GameActorState? right, IReadOnlyDictionary<string, object?> tokens);
}
