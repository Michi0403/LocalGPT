using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// The bounded authoritative event timeline of a duel progression.
/// </summary>
public interface IDuelTimelineService
{
    /// <summary>
    /// Adds an event whose kind and detail come from the texts <c>timeline.{eventKey}.kind</c> and <c>timeline.{eventKey}</c>.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="eventKey">The event text key suffix.</param>
    /// <param name="left">The left owner, or <c>null</c>.</param>
    /// <param name="right">The right owner, or <c>null</c>.</param>
    /// <param name="tokens">The detail tokens.</param>
    void Add(GameEngineContext context, DuelSettings settings, string eventKey, GameActorState? left, GameActorState? right, IReadOnlyDictionary<string, object?> tokens);

    /// <summary>
    /// Formats a timeline entry counted from the newest one.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="offsetFromEnd">0 for the newest entry.</param>
    /// <returns>The timeline line, or an empty string.</returns>
    string Line(CouncilGameSessionState session, int offsetFromEnd);
}
