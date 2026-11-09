using LocalGPT.BusinessObjects;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// A per-frame character and colour canvas of the duel battle view. Styles are cached so equal colours share one
/// style instance, which lets <see cref="ToRuns"/> merge neighbouring cells into runs.
/// </summary>
public sealed class DuelCanvas
{
    /// <summary>The character cells by row.</summary>
    private readonly char[][] cells;

    /// <summary>The cell styles by row.</summary>
    private readonly CouncilAsciiTextStyle?[][] styles;

    /// <summary>The shared styles by colour pair and weight.</summary>
    private readonly Dictionary<(int Foreground, int Background, bool Bold), CouncilAsciiTextStyle> cache = new();

    /// <summary>
    /// Initializes a blank canvas.
    /// </summary>
    /// <param name="width">The width in cells.</param>
    /// <param name="height">The height in cells.</param>
    public DuelCanvas(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        cells = Enumerable.Range(0, Height).Select(_ => Enumerable.Repeat(' ', Width).ToArray()).ToArray();
        styles = Enumerable.Range(0, Height).Select(_ => new CouncilAsciiTextStyle?[Width]).ToArray();
    }

    /// <summary>Gets the width in cells.</summary>
    /// <value>The canvas width.</value>
    public int Width { get; }

    /// <summary>Gets the height in cells.</summary>
    /// <value>The canvas height.</value>
    public int Height { get; }

    /// <summary>
    /// Gets the shared 256-colour style of a colour pair.
    /// </summary>
    /// <param name="foreground">The foreground colour.</param>
    /// <param name="background">The background colour.</param>
    /// <param name="bold">Whether the text is bold.</param>
    /// <returns>The shared style.</returns>
    public CouncilAsciiTextStyle Style(int foreground, int background, bool bold = false)
    {
        var key = (Math.Clamp(foreground, 0, 255), Math.Clamp(background, 0, 255), bold);
        if (!cache.TryGetValue(key, out var style))
        {
            style = new CouncilAsciiTextStyle
            {
                ColorMode = CouncilAsciiColorMode.Indexed256,
                ForegroundColor = key.Item1,
                BackgroundColor = key.Item2,
                Bold = bold
            };
            cache[key] = style;
        }
        return style;
    }

    /// <summary>
    /// Gets the background colour already painted at a cell.
    /// </summary>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <param name="fallback">The colour outside the canvas or on unpainted cells.</param>
    /// <returns>The background colour.</returns>
    public int BackgroundAt(int x, int y, int fallback)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return fallback;
        return styles[y][x]?.BackgroundColor ?? fallback;
    }

    /// <summary>
    /// Paints one cell; surrogates and control characters become blanks.
    /// </summary>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <param name="glyph">The glyph.</param>
    /// <param name="style">The style.</param>
    public void Put(int x, int y, char glyph, CouncilAsciiTextStyle style)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return;
        cells[y][x] = char.IsSurrogate(glyph) || char.IsControl(glyph) ? ' ' : glyph;
        styles[y][x] = style;
    }

    /// <summary>
    /// Paints a rectangle.
    /// </summary>
    /// <param name="x">The left column.</param>
    /// <param name="y">The top row.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="glyph">The glyph.</param>
    /// <param name="style">The style.</param>
    public void Fill(int x, int y, int width, int height, char glyph, CouncilAsciiTextStyle style)
    {
        for (var row = y; row < y + height; row++)
            for (var column = x; column < x + width; column++)
                Put(column, row, glyph, style);
    }

    /// <summary>
    /// Writes text, keeping the painted background when none is given.
    /// </summary>
    /// <param name="x">The start column.</param>
    /// <param name="y">The row.</param>
    /// <param name="value">The text.</param>
    /// <param name="foreground">The foreground colour.</param>
    /// <param name="background">The background colour, or <c>null</c> to keep the painted one.</param>
    /// <param name="bold">Whether the text is bold.</param>
    /// <param name="maximum">The maximum number of characters.</param>
    /// <returns>The number of written characters.</returns>
    public int Text(int x, int y, string? value, int foreground, int? background = null, bool bold = false, int maximum = int.MaxValue)
    {
        var written = 0;
        foreach (var character in value ?? string.Empty)
        {
            if (written >= maximum)
                break;
            if (char.IsSurrogate(character))
                continue;
            var column = x + written;
            Put(column, y, character, Style(foreground, background ?? BackgroundAt(column, y, 0), bold));
            written++;
        }
        return written;
    }

    /// <summary>
    /// Gets the canvas text.
    /// </summary>
    /// <returns>The rows joined with the environment newline.</returns>
    public string ToText() => string.Join(Environment.NewLine, cells.Select(row => new string(row)));

    /// <summary>
    /// Gets the style runs of painted cells.
    /// </summary>
    /// <returns>The runs, merging neighbouring cells with the same style instance.</returns>
    public List<CouncilAsciiStyleRun> ToRuns()
    {
        var runs = new List<CouncilAsciiStyleRun>();
        for (var y = 0; y < Height; y++)
        {
            var x = 0;
            while (x < Width)
            {
                var style = styles[y][x];
                var start = x;
                while (x < Width && ReferenceEquals(styles[y][x], style))
                    x++;
                if (style is not null)
                    runs.Add(new CouncilAsciiStyleRun { Y = y, X = start, Length = x - start, Style = style });
            }
        }
        return runs;
    }
}
