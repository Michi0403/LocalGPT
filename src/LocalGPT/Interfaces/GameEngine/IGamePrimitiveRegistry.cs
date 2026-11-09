namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Looks up the registered engine primitives by the keys a ruleset references.
/// </summary>
public interface IGamePrimitiveRegistry
{
    /// <summary>
    /// Finds a map generator.
    /// </summary>
    /// <param name="key">The primitive key.</param>
    /// <returns>The primitive, or <c>null</c> when the key is empty or unknown.</returns>
    IGameMapGeneratorPrimitive? MapGenerator(string? key);

    /// <summary>
    /// Finds an action primitive.
    /// </summary>
    /// <param name="key">The primitive key.</param>
    /// <returns>The primitive, or <c>null</c> when the key is empty or unknown.</returns>
    IGameActionPrimitive? Action(string? key);

    /// <summary>
    /// Finds a rule primitive.
    /// </summary>
    /// <param name="key">The primitive key.</param>
    /// <returns>The primitive, or <c>null</c> when the key is empty or unknown.</returns>
    IGameRulePrimitive? Rule(string? key);

    /// <summary>
    /// Finds a view primitive.
    /// </summary>
    /// <param name="key">The primitive key.</param>
    /// <returns>The primitive, or <c>null</c> when the key is empty or unknown.</returns>
    IGameViewPrimitive? View(string? key);

    /// <summary>
    /// Finds an autoplay primitive.
    /// </summary>
    /// <param name="key">The primitive key.</param>
    /// <returns>The primitive, or <c>null</c> when the key is empty or unknown.</returns>
    IGameAutoplayPrimitive? Autoplay(string? key);

    /// <summary>
    /// Finds a style primitive.
    /// </summary>
    /// <param name="key">The primitive key.</param>
    /// <returns>The primitive, or <c>null</c> when the key is empty or unknown.</returns>
    IGameStylePrimitive? Style(string? key);

    /// <summary>
    /// Finds a council progression primitive.
    /// </summary>
    /// <param name="key">The primitive key.</param>
    /// <returns>The primitive, or <c>null</c> when the key is empty or unknown.</returns>
    IGameCouncilProgressionPrimitive? CouncilProgression(string? key);

    /// <summary>
    /// Lists the registered keys of every primitive kind for authoring surfaces.
    /// </summary>
    /// <returns>The keys grouped by primitive kind.</returns>
    IReadOnlyDictionary<string, IReadOnlyList<string>> ListKeys();
}
