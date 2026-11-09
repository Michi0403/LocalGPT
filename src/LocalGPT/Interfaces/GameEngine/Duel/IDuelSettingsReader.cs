using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Parses the parameters of a team duel progression rule row.
/// </summary>
public interface IDuelSettingsReader
{
    /// <summary>
    /// Parses the duel settings.
    /// </summary>
    /// <param name="rule">The progression rule row.</param>
    /// <returns>The settings.</returns>
    DuelSettings Read(GameProjectRule rule);
}
