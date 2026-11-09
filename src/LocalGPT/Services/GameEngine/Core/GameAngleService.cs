using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Angle arithmetic of grid views and steering; compass labels come from the ruleset texts <c>compass.0</c> to <c>compass.7</c>.
/// </summary>
/// <param name="texts">Resolves the compass labels.</param>
/// <param name="logger">Receives angle diagnostics.</param>
public sealed class GameAngleService(IGameTextService texts, ILogger<GameAngleService> logger) : IGameAngleService
{
    /// <inheritdoc />
    public double Normalize(double radians)
    {
        try
        {
            while (radians < 0)
                radians += Math.PI * 2d;
            while (radians >= Math.PI * 2d)
                radians -= Math.PI * 2d;
            return radians;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing an angle failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public double SignedDelta(double from, double to)
    {
        try
        {
            var delta = Normalize(to) - Normalize(from);
            while (delta > Math.PI)
                delta -= Math.PI * 2d;
            while (delta < -Math.PI)
                delta += Math.PI * 2d;
            return delta;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Computing an angle delta failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string Compass(GameRuleset ruleset, double radians)
    {
        try
        {
            var degrees = Normalize(radians) * 180d / Math.PI;
            var sector = degrees switch
            {
                >= 337.5 or < 22.5 => 0,
                < 67.5 => 1,
                < 112.5 => 2,
                < 157.5 => 3,
                < 202.5 => 4,
                < 247.5 => 5,
                < 292.5 => 6,
                _ => 7
            };
            return texts.Get(ruleset, $"compass.{sector}");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the compass label failed.");
            throw;
        }
    }
}
