using System.Globalization;
using System.Text.Json;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Council;

/// <summary>
/// Reads member-drawn sprites from Council answers. A tagged palette line (<c>colorsTag</c>) names legend colours by the
/// declared <c>paletteKeys</c>; the rows follow the sprite tag or the first fenced block. Rows made only of legend
/// characters (with the declared <c>legendAliases</c>) become a trimmed pixel sprite; other printable rows become ASCII art.
/// </summary>
/// <param name="reader">Decodes answers and detects placeholders.</param>
/// <param name="parameters">Reads the sprite parameters.</param>
/// <param name="logger">Receives sprite diagnostics.</param>
public sealed class GameSpriteReader(
    IGameCouncilTextReader reader,
    IGameParameterReader parameters,
    ILogger<GameSpriteReader> logger) : IGameSpriteReader
{
    /// <inheritdoc />
    public GameSpriteReading Read(string? content, JsonElement spriteParameters)
    {
        try
        {
            var lines = reader.PlainModelText(content).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
            var palette = parameters.ReadInts(spriteParameters, "defaultPalette").ToList();
            var paletteKeys = parameters.ReadStrings(spriteParameters, "paletteKeys");
            var colorsTag = parameters.ReadString(spriteParameters, "colorsTag", "COLORS") + ":";
            var spriteTag = parameters.ReadString(spriteParameters, "spriteTag", "SPRITE") + ":";
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(colorsTag, StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var token in trimmed[colorsTag.Length..].Split([' ', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries))
                {
                    var separator = token.IndexOfAny(['=', ':']);
                    if (separator <= 0)
                        continue;
                    var name = token[..separator].Trim();
                    var keyIndex = paletteKeys.ToList().FindIndex(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (keyIndex >= 0 && keyIndex < palette.Count &&
                        int.TryParse(token[(separator + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var color) && color is >= 0 and <= 255)
                        palette[keyIndex] = color;
                }
                break;
            }

            var legend = Legend(spriteParameters);
            var empty = parameters.ReadString(spriteParameters, "emptyPixel", ".");
            var emptyCell = empty.Length == 0 ? '.' : empty[0];
            var markerIndex = Array.FindIndex(lines, line => line.TrimStart().StartsWith(spriteTag, StringComparison.OrdinalIgnoreCase));
            var candidates = new List<string>();
            var start = markerIndex >= 0 ? markerIndex : Array.FindIndex(lines, line => line.TrimStart().StartsWith("```", StringComparison.Ordinal));
            if (start < 0)
                return new GameSpriteReading { Palette = palette };
            if (markerIndex >= 0)
            {
                var inline = lines[markerIndex].TrimStart()[spriteTag.Length..].TrimEnd();
                if (!string.IsNullOrWhiteSpace(inline))
                    candidates.Add(inline);
            }
            var maximumCandidates = parameters.ReadInt(spriteParameters, "maximumCandidates", 24);
            for (var index = start + 1; index < lines.Length && candidates.Count < maximumCandidates; index++)
            {
                var line = lines[index].TrimEnd();
                var trimmed = line.Trim();
                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    if (candidates.Count > 0)
                        break;
                    continue;
                }
                if (trimmed.Length == 0)
                {
                    if (candidates.Count > 0)
                        break;
                    continue;
                }
                var colon = trimmed.IndexOf(':');
                if (colon > 1 && colon <= 12 && trimmed[..colon].All(char.IsLetter))
                    break;
                candidates.Add(line);
            }

            if (candidates.Count >= parameters.ReadInt(spriteParameters, "minimumPixelRows", 4) &&
                candidates.All(row => row.Length <= parameters.ReadInt(spriteParameters, "maximumPixelWidth", 32) && row.All(cell => legend.ContainsKey(cell))))
            {
                var rows = candidates.Select(row => new string(row.Select(cell => legend[cell]).ToArray())).ToList();
                var width = rows.Max(row => row.Length);
                rows = rows.Select(row => row.PadRight(width, emptyCell)).ToList();
                while (rows.Count > 0 && rows[0].All(cell => cell == emptyCell)) rows.RemoveAt(0);
                while (rows.Count > 0 && rows[^1].All(cell => cell == emptyCell)) rows.RemoveAt(rows.Count - 1);
                if (rows.Count > 0)
                {
                    var firstColumn = Enumerable.Range(0, width).FirstOrDefault(column => rows.Any(row => row[column] != emptyCell));
                    var lastColumn = Enumerable.Range(0, width).Reverse().FirstOrDefault(column => rows.Any(row => row[column] != emptyCell));
                    rows = rows.Select(row => row[firstColumn..(lastColumn + 1)]).ToList();
                }
                var filled = rows.Sum(row => row.Count(cell => cell != emptyCell));
                // A drawing needs a shape: identical rows (bars, stripes) are not a figure.
                var distinctRows = rows.Distinct(StringComparer.Ordinal).Count();
                if (rows.Count >= parameters.ReadInt(spriteParameters, "minimumPixelRows", 4) &&
                    filled >= parameters.ReadInt(spriteParameters, "minimumFilledPixels", 8) &&
                    distinctRows >= parameters.ReadInt(spriteParameters, "minimumDistinctRows", 3))
                {
                    var maximumColumns = parameters.ReadInt(spriteParameters, "maximumPixelColumns", 20);
                    return new GameSpriteReading
                    {
                        Rows = rows.Take(parameters.ReadInt(spriteParameters, "maximumPixelRows", 16)).Select(row => row.Length > maximumColumns ? row[..maximumColumns] : row).ToList(),
                        Kind = "pixel",
                        Palette = palette
                    };
                }
            }

            var maximumAsciiRows = parameters.ReadInt(spriteParameters, "maximumAsciiRows", 14);
            var maximumAsciiColumns = parameters.ReadInt(spriteParameters, "maximumAsciiColumns", 36);
            var art = candidates
                .Where(row => !reader.IsTemplatePlaceholder(row) && row.All(cell => cell >= ' ' && cell <= '~'))
                .Take(maximumAsciiRows)
                .Select(row => row.Length > maximumAsciiColumns ? row[..maximumAsciiColumns] : row)
                .ToList();
            var minimumAsciiRows = parameters.ReadInt(spriteParameters, "minimumAsciiRows", 3);
            if (art.Count >= minimumAsciiRows && art.Count == Math.Min(candidates.Count, maximumAsciiRows) &&
                art.Select(row => row.Trim()).Distinct(StringComparer.Ordinal).Count() >= minimumAsciiRows)
            {
                var indent = art.Where(row => row.Trim().Length > 0).Select(row => row.Length - row.TrimStart().Length).DefaultIfEmpty(0).Min();
                return new GameSpriteReading
                {
                    Rows = art.Select(row => row.Length >= indent ? row[indent..] : row).ToList(),
                    Kind = "ascii",
                    Palette = palette
                };
            }
            return new GameSpriteReading { Palette = palette };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a Council sprite failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds the legend map from <c>legendAliases</c>: each legend character maps from itself and its alias characters.
    /// </summary>
    /// <param name="spriteParameters">The sprite parameters.</param>
    /// <returns>The source-to-legend map.</returns>
    private Dictionary<char, char> Legend(JsonElement spriteParameters)
    {
        try
        {
            var legend = new Dictionary<char, char>();
            var aliases = parameters.ReadObject(spriteParameters, "legendAliases");
            foreach (var entry in aliases.EnumerateObject())
            {
                if (entry.Name.Length == 0)
                    continue;
                var target = entry.Name[0];
                legend[target] = target;
                foreach (var alias in parameters.ReadString(aliases, entry.Name, string.Empty))
                    legend[alias] = target;
            }
            return legend;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the sprite legend failed.");
            throw;
        }
    }
}
