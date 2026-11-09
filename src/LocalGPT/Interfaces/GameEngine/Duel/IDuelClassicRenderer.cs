using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Renders the classic ASCII arena of a duel frame with posed rigs.
/// </summary>
public interface IDuelClassicRenderer
{
    /// <summary>
    /// Renders the classic frame.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="request">The frame request.</param>
    /// <returns>The frame text.</returns>
    string Render(GameEngineContext context, DuelSettings settings, DuelFrameRequest request);
}
