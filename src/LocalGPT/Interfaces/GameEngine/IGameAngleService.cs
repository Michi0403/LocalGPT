using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Angle arithmetic of grid views and steering.
/// </summary>
public interface IGameAngleService
{
    /// <summary>
    /// Normalizes an angle into [0, 2π).
    /// </summary>
    /// <param name="radians">The angle.</param>
    /// <returns>The normalized angle.</returns>
    double Normalize(double radians);

    /// <summary>
    /// Gets the signed shortest rotation from one angle to another, in (-π, π].
    /// </summary>
    /// <param name="from">The current angle.</param>
    /// <param name="to">The desired angle.</param>
    /// <returns>The signed delta.</returns>
    double SignedDelta(double from, double to);

    /// <summary>
    /// Gets the compass label of a heading from the ruleset's compass texts.
    /// </summary>
    /// <param name="ruleset">The running ruleset.</param>
    /// <param name="radians">The heading.</param>
    /// <returns>The compass label.</returns>
    string Compass(GameRuleset ruleset, double radians);
}
