using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Reads the grid world of a session and the map glyphs declared by the ruleset's map parameters.
/// </summary>
public interface IGameWorldService
{
    /// <summary>
    /// Gets the wall glyph (map parameter <c>wallGlyph</c>).
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <returns>The wall glyph.</returns>
    char Wall(GameRuleset ruleset);

    /// <summary>
    /// Gets the floor glyph (map parameter <c>floorGlyph</c>).
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <returns>The floor glyph.</returns>
    char Floor(GameRuleset ruleset);

    /// <summary>
    /// Tests whether a living actor of a role stands on a cell.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="role">The blocking role.</param>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <param name="exceptKey">An actor key to ignore.</param>
    /// <returns><c>true</c> when the cell is occupied.</returns>
    bool IsOccupied(CouncilGameSessionState session, string role, int x, int y, string? exceptKey);
}
