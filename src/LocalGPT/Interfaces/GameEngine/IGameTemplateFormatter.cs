namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Formats ruleset text templates. A token is written <c>{name}</c>, <c>{name,alignment}</c>, <c>{name:format}</c>
/// or <c>{name,alignment:format}</c>; unknown tokens stay literal so authored art may contain braces.
/// </summary>
public interface IGameTemplateFormatter
{
    /// <summary>
    /// Replaces the known tokens of a template.
    /// </summary>
    /// <param name="template">The template text.</param>
    /// <param name="tokens">The token values; numbers honour invariant format strings.</param>
    /// <returns>The formatted text.</returns>
    string Format(string? template, IReadOnlyDictionary<string, object?> tokens);
}
