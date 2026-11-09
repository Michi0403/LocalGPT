using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Reads the grid world of a session and its ruleset-declared map glyphs.
/// </summary>
/// <param name="parameters">Reads the map parameters.</param>
/// <param name="logger">Receives world diagnostics.</param>
public sealed class GameWorldService(IGameParameterReader parameters, ILogger<GameWorldService> logger) : IGameWorldService
{
    /// <inheritdoc />
    public char Wall(GameRuleset ruleset)
    {
        try
        {
            var glyph = parameters.ReadString(parameters.Parse(ruleset.MapParametersJson), "wallGlyph", "#");
            return glyph.Length == 0 ? '#' : glyph[0];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the wall glyph failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public char Floor(GameRuleset ruleset)
    {
        try
        {
            var glyph = parameters.ReadString(parameters.Parse(ruleset.MapParametersJson), "floorGlyph", ".");
            return glyph.Length == 0 ? '.' : glyph[0];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the floor glyph failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public bool IsOccupied(CouncilGameSessionState session, string role, int x, int y, string? exceptKey)
    {
        try
        {
            return session.Actors.Any(actor =>
                !actor.IsEliminated &&
                actor.X == x && actor.Y == y &&
                string.Equals(actor.Role, role, StringComparison.OrdinalIgnoreCase) &&
                (exceptKey is null || !string.Equals(actor.Key, exceptKey, StringComparison.OrdinalIgnoreCase)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Testing occupancy of {X},{Y} failed.", x, y);
            throw;
        }
    }
}
