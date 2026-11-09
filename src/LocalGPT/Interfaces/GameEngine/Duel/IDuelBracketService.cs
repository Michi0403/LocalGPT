using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Single-elimination bracket progression over the session's progression state.
/// </summary>
public interface IDuelBracketService
{
    /// <summary>
    /// Advances byes and rounds until a playable match or a champion exists.
    /// </summary>
    /// <param name="session">The session.</param>
    void Prepare(CouncilGameSessionState session);

    /// <summary>
    /// Gets the keys of the current match.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <returns>Two entrant keys, or an empty list when no match is playable.</returns>
    IReadOnlyList<string> CurrentPairKeys(CouncilGameSessionState session);

    /// <summary>
    /// Records a decided match and moves to the next pairing.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="winner">The winning owner.</param>
    void AdvanceWinner(CouncilGameSessionState session, GameActorState winner);
}
