using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// A map generator primitive: builds the map, markers and spawned actors of the current level from ruleset parameters.
/// </summary>
public interface IGameMapGeneratorPrimitive
{
    /// <summary>
    /// Gets the primitive key referenced by <c>MapGeneratorKey</c>.
    /// </summary>
    /// <value>The stable primitive key.</value>
    string Key { get; }

    /// <summary>
    /// Generates the current level.
    /// </summary>
    /// <param name="context">The engine context of the session.</param>
    void Generate(GameEngineContext context);
}
