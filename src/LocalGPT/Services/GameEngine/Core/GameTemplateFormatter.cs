using System.Globalization;
using System.Text;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Formats ruleset templates with composite-format-like tokens and invariant culture.
/// </summary>
/// <param name="logger">Receives formatting diagnostics.</param>
public sealed class GameTemplateFormatter(ILogger<GameTemplateFormatter> logger) : IGameTemplateFormatter
{
    /// <inheritdoc />
    public string Format(string? template, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;
            if (template.IndexOf('{') < 0 && template.IndexOf('}') < 0)
                return template;
            var builder = new StringBuilder(template.Length + 32);
            var index = 0;
            while (index < template.Length)
            {
                var character = template[index];
                if (character == '{')
                {
                    if (index + 1 < template.Length && template[index + 1] == '{')
                    {
                        builder.Append('{');
                        index += 2;
                        continue;
                    }
                    var close = template.IndexOf('}', index + 1);
                    if (close < 0)
                    {
                        builder.Append(template, index, template.Length - index);
                        break;
                    }
                    var specification = template.Substring(index + 1, close - index - 1);
                    if (TryFormatToken(specification, tokens, out var formatted))
                        builder.Append(formatted);
                    else
                        builder.Append(template, index, close - index + 1);
                    index = close + 1;
                    continue;
                }
                if (character == '}' && index + 1 < template.Length && template[index + 1] == '}')
                {
                    builder.Append('}');
                    index += 2;
                    continue;
                }
                builder.Append(character);
                index++;
            }
            return builder.ToString();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting game template failed.");
            throw;
        }
    }

    /// <summary>
    /// Formats one token specification of the form <c>name[,alignment][:format]</c>.
    /// </summary>
    /// <param name="specification">The text between the braces.</param>
    /// <param name="tokens">The token values.</param>
    /// <param name="formatted">The formatted token.</param>
    /// <returns><c>true</c> when the token is known.</returns>
    private bool TryFormatToken(string specification, IReadOnlyDictionary<string, object?> tokens, out string formatted)
    {
        try
        {
            formatted = string.Empty;
            var formatSeparator = specification.IndexOf(':');
            var head = formatSeparator < 0 ? specification : specification[..formatSeparator];
            var format = formatSeparator < 0 ? string.Empty : specification[(formatSeparator + 1)..];
            var alignmentSeparator = head.IndexOf(',');
            var name = (alignmentSeparator < 0 ? head : head[..alignmentSeparator]).Trim();
            if (name.Length == 0 || name.Contains(' ', StringComparison.Ordinal) || !tokens.TryGetValue(name, out var value))
                return false;
            var text = value switch
            {
                null => string.Empty,
                IFormattable formattable => formattable.ToString(format.Length == 0 ? null : format, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
            if (alignmentSeparator >= 0 &&
                int.TryParse(head[(alignmentSeparator + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var alignment))
            {
                text = alignment >= 0 ? text.PadLeft(alignment) : text.PadRight(-alignment);
            }
            formatted = text;
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting game template token {TokenSpecification} failed.", specification);
            throw;
        }
    }
}
