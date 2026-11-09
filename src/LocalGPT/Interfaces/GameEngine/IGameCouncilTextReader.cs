namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Reads Council answers for council-driven games: plain text, tagged lines and prompt placeholders.
/// </summary>
public interface IGameCouncilTextReader
{
    /// <summary>
    /// Decodes markup entities and removes invisible and control characters.
    /// </summary>
    /// <param name="value">The answer content.</param>
    /// <returns>The plain text.</returns>
    string PlainModelText(string? value);

    /// <summary>
    /// Reads the value of the first line written as <c>TAG: value</c>, ignoring prompt placeholders.
    /// </summary>
    /// <param name="content">The answer content.</param>
    /// <param name="tag">The tag.</param>
    /// <returns>The value or an empty string.</returns>
    string ReadTag(string? content, string tag);

    /// <summary>
    /// Reads the first tagged value among several tags, as exchange commands are written.
    /// </summary>
    /// <param name="content">The answer content.</param>
    /// <param name="tags">The tags in priority order.</param>
    /// <returns>The value or an empty string.</returns>
    string ReadTaggedValue(string? content, IReadOnlyList<string> tags);

    /// <summary>
    /// Removes unresolved prompt placeholders such as <c>{{Name}}</c> or <c>&lt;value&gt;</c>.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns>The text without placeholders.</returns>
    string StripPromptPlaceholders(string? value);

    /// <summary>
    /// Tests whether a value is an echoed angle-bracket prompt placeholder such as <c>&lt;one short line&gt;</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> for placeholders.</returns>
    bool IsTemplatePlaceholder(string? value);
}
