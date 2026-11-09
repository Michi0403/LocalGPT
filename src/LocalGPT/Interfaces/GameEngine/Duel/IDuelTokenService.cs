using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Builds the storyboard tokens of a duel step.
/// </summary>
public interface IDuelTokenService
{
    /// <summary>
    /// Builds the session and settings tokens.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <returns>The base tokens.</returns>
    Dictionary<string, object?> Base(GameEngineContext context, DuelSettings settings);

    /// <summary>
    /// Adds the identity tokens of an owner under a prefix such as <c>left</c>, <c>right</c> or <c>champion</c>.
    /// </summary>
    /// <param name="tokens">The tokens to extend.</param>
    /// <param name="prefix">The token prefix.</param>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner, or <c>null</c>.</param>
    void AddOwner(Dictionary<string, object?> tokens, string prefix, GameEngineContext context, DuelSettings settings, GameActorState? owner);

    /// <summary>
    /// Adds the exchange tokens of both sides, the winner and the finisher.
    /// </summary>
    /// <param name="tokens">The tokens to extend.</param>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="outcome">The exchange outcome.</param>
    void AddExchange(Dictionary<string, object?> tokens, GameEngineContext context, DuelSettings settings, DuelExchangeOutcome outcome);
}
