using System.Text;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Council;

/// <summary>
/// Reads Council answers for council-driven games. Provider streams are HTML-encoded for the chat transcript, while game
/// frames render plain text: entities are decoded at most twice and characters that do not occupy one terminal cell are removed.
/// </summary>
/// <param name="textUtility">Cleans and compacts spoken values.</param>
/// <param name="logger">Receives reader diagnostics.</param>
public sealed class GameCouncilTextReader(IGameTextUtilityService textUtility, ILogger<GameCouncilTextReader> logger) : IGameCouncilTextReader
{
    /// <inheritdoc />
    public string PlainModelText(string? value)
    {
        try
        {
            var text = value ?? string.Empty;
            for (var pass = 0; pass < 2 && text.Contains('&', StringComparison.Ordinal); pass++)
            {
                var decoded = System.Net.WebUtility.HtmlDecode(text);
                if (string.Equals(decoded, text, StringComparison.Ordinal))
                    break;
                text = decoded;
            }
            var plain = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                if (char.IsSurrogate(character) || character is '️' or '‍' or '​' or '﻿')
                    continue;
                if (char.IsControl(character) && character is not '\n' and not '\r' and not '\t')
                    continue;
                plain.Append(character);
            }
            return plain.ToString();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Decoding a Council answer for a game failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string ReadTag(string? content, string tag)
    {
        try
        {
            foreach (var line in PlainModelText(content).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(tag + ":", StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = string.Join(' ', trimmed[(tag.Length + 1)..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                return IsTemplatePlaceholder(value) ? string.Empty : StripPromptPlaceholders(value);
            }
            return string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council answer tag {Tag} failed.", tag);
            throw;
        }
    }

    /// <inheritdoc />
    public string ReadTaggedValue(string? content, IReadOnlyList<string> tags)
    {
        try
        {
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;
                foreach (var line in (content ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith(tag + ":", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var value = textUtility.Compact(textUtility.CleanSpokenLine(trimmed[(tag.Length + 1)..]), 96);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                    break;
                }
            }
            return string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a tagged Council value failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string StripPromptPlaceholders(string? value)
    {
        try
        {
            var text = value ?? string.Empty;
            var result = new StringBuilder(text.Length);
            var index = 0;
            while (index < text.Length)
            {
                var open = text.IndexOf('<', index);
                var close = open < 0 ? -1 : text.IndexOf('>', open + 1);
                if (open < 0 || close < 0)
                {
                    result.Append(text, index, text.Length - index);
                    break;
                }
                result.Append(text, index, open - index);
                var inner = text.AsSpan(open + 1, close - open - 1);
                var isPlaceholder = inner.Length is >= 3 and <= 120
                    && char.IsLetter(inner[0])
                    && inner.Contains(' ')
                    && inner.IndexOfAny('<', '=', '"') < 0;
                if (!isPlaceholder)
                    result.Append(text, open, close - open + 1);
                index = close + 1;
            }
            return string.Join(' ', result.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Removing prompt placeholders failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public bool IsTemplatePlaceholder(string? value)
    {
        try
        {
            var trimmed = (value ?? string.Empty).Trim();
            return trimmed.Length >= 3 && trimmed.Length <= 120
                && trimmed[0] == '<' && trimmed[^1] == '>'
                && trimmed.IndexOfAny(['<', '>'], 1, trimmed.Length - 2) < 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Testing a prompt placeholder failed.");
            throw;
        }
    }
}
