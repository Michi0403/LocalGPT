using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Resolves user-visible game texts: the game's own text rows first, then the engine's maintained default texts.
/// </summary>
public interface IGameTextService
{
    /// <summary>
    /// Gets whether a text exists for the ruleset or the engine defaults.
    /// </summary>
    /// <param name="ruleset">The running ruleset.</param>
    /// <param name="key">The text key.</param>
    /// <returns><c>true</c> when a text exists.</returns>
    bool Has(GameRuleset ruleset, string key);

    /// <summary>
    /// Gets a text without formatting.
    /// </summary>
    /// <param name="ruleset">The running ruleset.</param>
    /// <param name="key">The text key.</param>
    /// <returns>The text, or an empty string when neither the game nor the engine defines it.</returns>
    string Get(GameRuleset ruleset, string key);

    /// <summary>
    /// Gets and formats a text.
    /// </summary>
    /// <param name="ruleset">The running ruleset.</param>
    /// <param name="key">The text key.</param>
    /// <param name="tokens">The token values.</param>
    /// <returns>The formatted text.</returns>
    string Format(GameRuleset ruleset, string key, IReadOnlyDictionary<string, object?> tokens);
}
