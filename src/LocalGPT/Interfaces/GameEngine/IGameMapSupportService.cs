using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Shared building blocks of map generator primitives: seeds, cell grids, player placement and actor spawning.
/// </summary>
public interface IGameMapSupportService
{
    /// <summary>
    /// Derives the session base seed: explicit map seed, else the scenario prompt hash, else the session identifier.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <returns>A positive seed.</returns>
    int BaseSeed(CouncilGameSessionState session);

    /// <summary>
    /// Creates a solid grid.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="wall">The wall glyph.</param>
    /// <returns>The mutable cells.</returns>
    char[][] SolidCells(int width, int height, char wall);

    /// <summary>
    /// Places the player actor at a cell facing east and resets movement blocking.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    void PlacePlayer(GameEngineContext context, int x, int y);

    /// <summary>
    /// Creates an actor from an archetype row: role, glyph, display name or name template, stats with maxima.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="archetypeKey">The archetype key.</param>
    /// <param name="key">The actor key.</param>
    /// <param name="nameTokens">Tokens of the archetype's name template.</param>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <returns>The actor.</returns>
    GameActorState Spawn(GameRuleset ruleset, string archetypeKey, string key, IReadOnlyDictionary<string, object?> nameTokens, int x, int y);

    /// <summary>
    /// Creates the player actor from the ruleset's player archetype and stat rows.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <returns>The player actor, or <c>null</c> for games without one.</returns>
    GameActorState? CreatePlayer(GameRuleset ruleset);
}
