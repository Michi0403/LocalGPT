using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// A view primitive: renders the session into a terminal frame.
/// </summary>
public interface IGameViewPrimitive
{
    /// <summary>
    /// Gets the primitive key referenced by <c>ViewKey</c>.
    /// </summary>
    /// <value>The stable primitive key.</value>
    string Key { get; }

    /// <summary>
    /// Renders the current frame.
    /// </summary>
    /// <param name="context">The engine context of the session.</param>
    /// <returns>The frame.</returns>
    GameViewFrame Render(GameEngineContext context);
}
