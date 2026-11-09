using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Reads and writes actor, stat and marker state of a session without knowing what a game calls them.
/// </summary>
public interface IGameActorQueryService
{
    /// <summary>
    /// Finds the controlled player actor.
    /// </summary>
    /// <param name="actors">The session actors.</param>
    /// <param name="ruleset">The ruleset naming the player archetype.</param>
    /// <returns>The player actor, or <c>null</c> for games without one.</returns>
    GameActorState? Player(IReadOnlyList<GameActorState> actors, GameRuleset ruleset);

    /// <summary>
    /// Lists the living actors of a role ordered by key.
    /// </summary>
    /// <param name="actors">The session actors.</param>
    /// <param name="role">The archetype role.</param>
    /// <returns>The living actors.</returns>
    List<GameActorState> Living(IReadOnlyList<GameActorState> actors, string role);

    /// <summary>
    /// Reads a stat.
    /// </summary>
    /// <param name="actor">The actor.</param>
    /// <param name="key">The stat key.</param>
    /// <param name="fallback">The value when the stat is absent.</param>
    /// <returns>The stat value.</returns>
    int Stat(GameActorState? actor, string key, int fallback);

    /// <summary>
    /// Writes a stat.
    /// </summary>
    /// <param name="actor">The actor.</param>
    /// <param name="key">The stat key.</param>
    /// <param name="value">The new value.</param>
    void SetStat(GameActorState actor, string key, int value);

    /// <summary>
    /// Reads an actor text.
    /// </summary>
    /// <param name="actor">The actor.</param>
    /// <param name="key">The text key.</param>
    /// <returns>The text or an empty string.</returns>
    string Text(GameActorState? actor, string key);

    /// <summary>
    /// Finds a marker.
    /// </summary>
    /// <param name="markers">The session markers.</param>
    /// <param name="key">The marker key.</param>
    /// <returns>The marker, or <c>null</c>.</returns>
    GameMarkerState? Marker(IReadOnlyList<GameMarkerState> markers, string key);

    /// <summary>
    /// Deep-copies actors for a snapshot.
    /// </summary>
    /// <param name="actors">The actors.</param>
    /// <returns>Independent copies.</returns>
    List<GameActorState> Clone(IEnumerable<GameActorState> actors);
}
