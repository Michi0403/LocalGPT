using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Renders the handheld-style ANSI battle screen of a duel scene.
/// </summary>
public interface IDuelBattleRenderer
{
    /// <summary>
    /// Renders the scene.
    /// </summary>
    /// <param name="context">The engine context, for view texts and palette.</param>
    /// <param name="scene">The scene.</param>
    /// <returns>The frame text and its cell style runs.</returns>
    GameViewFrame Render(GameEngineContext context, DuelScene scene);
}
