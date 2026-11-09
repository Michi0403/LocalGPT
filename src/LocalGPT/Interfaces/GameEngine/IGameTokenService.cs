using LocalGPT.BusinessObjects;
namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Builds the generic template tokens of a running session (turn, owner, level, player, stats, variables, flags, markers, alive counts).
/// </summary>
public interface IGameTokenService
{
    /// <summary>
    /// Builds the session tokens.
    /// </summary>
    /// <param name="session">The running session.</param>
    /// <returns>A case-insensitive token dictionary that callers may extend.</returns>
    Dictionary<string, object?> Build(CouncilGameSessionState session);
}
