using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Renders one duel frame with the battle view, or the classic arena when the terminal is too small.
/// </summary>
public interface IDuelFrameFactory
{
    /// <summary>
    /// Renders a frame.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="request">The frame request.</param>
    /// <returns>The frame with battle style runs, or without runs for classic frames.</returns>
    GameViewFrame Make(GameEngineContext context, DuelSettings settings, DuelFrameRequest request);
}
