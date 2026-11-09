using System.Text;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Text shaping of terminal views and council games.
/// </summary>
/// <param name="logger">Receives text shaping diagnostics.</param>
public sealed class GameTextUtilityService(ILogger<GameTextUtilityService> logger) : IGameTextUtilityService
{
    /// <inheritdoc />
    public string Compact(string? value, int maximumLength)
    {
        try
        {
            var compact = string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return compact.Length <= maximumLength ? compact : compact[..maximumLength].TrimEnd();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Compacting game text failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string CleanSpokenLine(string? value)
    {
        try
        {
            // Small models copy prompt placeholders such as "<one short line>" and repeat their own sentence.
            // Spoken lines keep only real text: entities decoded, placeholders removed, repeated sentences shown once.
            var text = System.Net.WebUtility.HtmlDecode(value ?? string.Empty);
            var withoutPlaceholders = new StringBuilder(text.Length);
            var index = 0;
            while (index < text.Length)
            {
                var open = text.IndexOf('<', index);
                var close = open < 0 ? -1 : text.IndexOf('>', open + 1);
                if (open < 0 || close < 0)
                {
                    withoutPlaceholders.Append(text, index, text.Length - index);
                    break;
                }
                withoutPlaceholders.Append(text, index, open - index);
                var inner = text.AsSpan(open + 1, close - open - 1);
                var isPlaceholder = inner.Length is >= 3 and <= 120
                    && char.IsLetter(inner[0])
                    && inner.Contains(' ')
                    && inner.IndexOfAny('<', '=', '"') < 0;
                if (!isPlaceholder)
                    withoutPlaceholders.Append(text, open, close - open + 1);
                index = close + 1;
            }

            var cleaned = withoutPlaceholders.ToString();
            var sentences = new List<string>();
            var sentenceStart = 0;
            for (var position = 0; position < cleaned.Length; position++)
            {
                if (cleaned[position] is '.' or '!' or '?' && (position + 1 == cleaned.Length || char.IsWhiteSpace(cleaned[position + 1])))
                {
                    sentences.Add(cleaned[sentenceStart..(position + 1)].Trim());
                    sentenceStart = position + 1;
                }
            }
            if (sentenceStart < cleaned.Length)
                sentences.Add(cleaned[sentenceStart..].Trim());
            var unique = new List<string>();
            foreach (var sentence in sentences)
            {
                if (sentence.Length > 0 && !unique.Contains(sentence, StringComparer.OrdinalIgnoreCase))
                    unique.Add(sentence);
            }
            return string.Join(' ', unique);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cleaning a spoken game line failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string JoinDistinct(string separator, IEnumerable<string?> values)
    {
        try
        {
            var unique = new List<string>();
            foreach (var value in values)
            {
                var trimmed = (value ?? string.Empty).Trim();
                if (trimmed.Length > 0 && !unique.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    unique.Add(trimmed);
            }
            return string.Join(separator, unique);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Joining distinct game values failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string ShortModel(string? value)
    {
        try
        {
            var model = Compact(value, 20);
            var separator = model.LastIndexOf(" — ", StringComparison.Ordinal);
            if (separator >= 0 && separator + 3 < model.Length)
                model = model[(separator + 3)..];
            var at = model.IndexOf(" @ ", StringComparison.Ordinal);
            if (at > 0)
                model = model[..at];
            return Compact(model, 16);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Shortening a model label failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string ShortSummaryModel(string? value)
    {
        try
        {
            var model = (value ?? string.Empty).Trim();
            var separator = model.LastIndexOf(" — ", StringComparison.Ordinal);
            if (separator >= 0 && separator + 3 < model.Length)
                model = model[(separator + 3)..];
            var at = model.IndexOf(" @ ", StringComparison.Ordinal);
            if (at > 0)
                model = model[..at];
            return model.Length <= 20 ? model : model[..20];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Shortening a summary model label failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string Fit(string? value, int width)
    {
        try
        {
            var normalized = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
            return normalized.Length > width ? normalized[..width] : normalized.PadRight(width);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Fitting game text failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public void Put(char[] row, int x, string text)
    {
        try
        {
            for (var index = 0; index < text.Length && x + index < row.Length; index++)
            {
                if (x + index >= 0)
                    row[x + index] = text[index];
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Writing game text into a row failed.");
            throw;
        }
    }
}
