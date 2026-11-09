using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Resolves one authoritative duel exchange from Council evidence.
/// </summary>
public interface IDuelExchangeResolver
{
    /// <summary>
    /// Resolves actions, switches, damage, recovery, knock-out switches, bench rest and the match decision.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="left">The left owner.</param>
    /// <param name="right">The right owner.</param>
    /// <param name="evidence">The Council evidence.</param>
    /// <returns>The exchange outcome.</returns>
    DuelExchangeOutcome Resolve(GameEngineContext context, DuelSettings settings, GameActorState left, GameActorState right, IReadOnlyList<GameCouncilEvidence> evidence);
}
