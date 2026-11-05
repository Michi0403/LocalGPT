using System.Text;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>
    /// Renders the Kernel Creature Tournament as an ANSI-colored, handheld-battle style screen. Every creature image comes
    /// from the sprite its Kernel Creature Council member drew; the engine only places, colors and animates those pixels.
    /// </summary>
    public sealed partial class CouncilGameSessionService
    {
        /// <summary>Presentation data for one side of a Kernel Creature Tournament battle frame.</summary>
        private sealed class KernelBattleSide
        {
            /// <summary>Gets the short trainer model label.</summary>
            public string TrainerName { get; init; } = string.Empty;
            /// <summary>Gets the trainer's last resolved command.</summary>
            public string TrainerAction { get; init; } = string.Empty;
            /// <summary>Gets the active creature's display name.</summary>
            public string CreatureName { get; init; } = string.Empty;
            /// <summary>Gets the active creature's short species label.</summary>
            public string Species { get; init; } = string.Empty;
            /// <summary>Gets the health value to display for this frame.</summary>
            public int Health { get; init; }
            /// <summary>Gets the trainer or creature line spoken in this frame.</summary>
            public string Statement { get; init; } = string.Empty;
            /// <summary>Gets the trainer switches used in the current fight.</summary>
            public int SwitchesUsed { get; init; }
            /// <summary>Gets the roster health and active markers in display order.</summary>
            public IReadOnlyList<(int Health, bool Active)> Roster { get; init; } = [];
            /// <summary>Gets the member-authored sprite rows of the active creature.</summary>
            public IReadOnlyList<string> SpriteRows { get; init; } = [];
            /// <summary>Gets the sprite interpretation (<c>pixel</c>, <c>ascii</c> or empty).</summary>
            public string SpriteKind { get; init; } = string.Empty;
            /// <summary>Gets the member-chosen palette (outline, body, shade, accent, eye).</summary>
            public IReadOnlyList<int> SpritePalette { get; init; } = [];
            /// <summary>Gets the Council member that authored the sprite.</summary>
            public string SpriteAuthor { get; init; } = string.Empty;
            /// <summary>Gets the animation pose for this frame.</summary>
            public string Pose { get; init; } = "idle";
        }

        /// <summary>Complete presentation input for one Kernel Creature Tournament battle frame.</summary>
        private sealed class KernelBattleScene
        {
            /// <summary>Gets the frame headline.</summary>
            public string Headline { get; init; } = string.Empty;
            /// <summary>Gets the main message-box text.</summary>
            public string Body { get; init; } = string.Empty;
            /// <summary>Gets the secondary message-box text.</summary>
            public string Footer { get; init; } = string.Empty;
            /// <summary>Gets the effect text drawn between the creatures.</summary>
            public string CenterEffect { get; init; } = string.Empty;
            /// <summary>Gets the compact HUD motif shown in the title bar.</summary>
            public string HudMotif { get; init; } = string.Empty;
            /// <summary>Gets the bracket round.</summary>
            public int Round { get; init; }
            /// <summary>Gets the exchange inside the current fight.</summary>
            public int Exchange { get; init; }
            /// <summary>Gets the tournament starting health used for bar scaling.</summary>
            public int StartingHealth { get; init; } = 100;
            /// <summary>Gets the maximum trainer switches per fight.</summary>
            public int MaximumSwitches { get; init; }
            /// <summary>Gets the newest timeline line.</summary>
            public string TimelineRecent { get; init; } = string.Empty;
            /// <summary>Gets the previous timeline line.</summary>
            public string TimelinePrevious { get; init; } = string.Empty;
            /// <summary>Gets the player-side (bottom-left) fighter, if any.</summary>
            public KernelBattleSide? Left { get; init; }
            /// <summary>Gets the opponent-side (top-right) fighter, if any.</summary>
            public KernelBattleSide? Right { get; init; }
            /// <summary>Gets a value indicating whether both sides are the same champion.</summary>
            public bool SameFighter { get; init; }
        }

        /// <summary>Fixed-size character grid with one indexed-color style per cell.</summary>
        private sealed class KernelBattleCanvas
        {
            /// <summary>Stores the glyph rows.</summary>
            private readonly char[][] cells;
            /// <summary>Stores the per-cell styles; null keeps the terminal default.</summary>
            private readonly CouncilAsciiTextStyle?[][] styles;
            /// <summary>Shares one style instance per color combination so runs merge cleanly.</summary>
            private readonly Dictionary<(int Foreground, int Background, bool Bold), CouncilAsciiTextStyle> cache = new();

            /// <summary>Initializes a blank canvas.</summary>
            /// <param name="width">Frame width in cells.</param>
            /// <param name="height">Frame height in cells.</param>
            public KernelBattleCanvas(int width, int height)
            {
                Width = Math.Max(1, width);
                Height = Math.Max(1, height);
                cells = Enumerable.Range(0, Height).Select(_ => Enumerable.Repeat(' ', Width).ToArray()).ToArray();
                styles = Enumerable.Range(0, Height).Select(_ => new CouncilAsciiTextStyle?[Width]).ToArray();
            }

            /// <summary>Gets the frame width in cells.</summary>
            public int Width { get; }
            /// <summary>Gets the frame height in cells.</summary>
            public int Height { get; }

            /// <summary>Returns the shared style for a color combination.</summary>
            /// <param name="foreground">xterm-256 foreground color.</param>
            /// <param name="background">xterm-256 background color.</param>
            /// <param name="bold">Whether the glyph is bold.</param>
            /// <returns>The cached style instance.</returns>
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

            /// <summary>Returns the background color already painted at a cell.</summary>
            /// <param name="x">Column.</param>
            /// <param name="y">Row.</param>
            /// <param name="fallback">Color used outside the canvas or for unstyled cells.</param>
            /// <returns>The background color.</returns>
            public int BackgroundAt(int x, int y, int fallback)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height)
                    return fallback;
                return styles[y][x]?.BackgroundColor ?? fallback;
            }

            /// <summary>Paints one cell when it is inside the canvas.</summary>
            /// <param name="x">Column.</param>
            /// <param name="y">Row.</param>
            /// <param name="glyph">Single-cell glyph.</param>
            /// <param name="style">Cell style.</param>
            public void Put(int x, int y, char glyph, CouncilAsciiTextStyle style)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height)
                    return;
                cells[y][x] = char.IsSurrogate(glyph) || char.IsControl(glyph) ? ' ' : glyph;
                styles[y][x] = style;
            }

            /// <summary>Paints a rectangle with one glyph and style.</summary>
            /// <param name="x">Left column.</param>
            /// <param name="y">Top row.</param>
            /// <param name="width">Rectangle width.</param>
            /// <param name="height">Rectangle height.</param>
            /// <param name="glyph">Fill glyph.</param>
            /// <param name="style">Fill style.</param>
            public void Fill(int x, int y, int width, int height, char glyph, CouncilAsciiTextStyle style)
            {
                for (var row = y; row < y + height; row++)
                    for (var column = x; column < x + width; column++)
                        Put(column, row, glyph, style);
            }

            /// <summary>Writes text; a null background keeps whatever scene color is already painted per cell.</summary>
            /// <param name="x">Left column.</param>
            /// <param name="y">Row.</param>
            /// <param name="value">Text to write.</param>
            /// <param name="foreground">xterm-256 foreground color.</param>
            /// <param name="background">xterm-256 background color, or null to keep the scene.</param>
            /// <param name="bold">Whether the text is bold.</param>
            /// <param name="maximum">Maximum cells to write.</param>
            /// <returns>The number of cells written.</returns>
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

            /// <summary>Returns the frame text with one line per row.</summary>
            /// <returns>The frame text.</returns>
            public string ToText() => string.Join(Environment.NewLine, cells.Select(row => new string(row)));

            /// <summary>Converts the per-cell styles into merged horizontal style runs.</summary>
            /// <returns>The style runs.</returns>
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

        /// <summary>
        /// Renders one Kernel Creature Tournament frame as an ANSI battle screen: opponent and info box at the top,
        /// player and info box at the bottom, message and command boxes below the field.
        /// </summary>
        /// <param name="width">Session frame width.</param>
        /// <param name="height">Session frame height.</param>
        /// <param name="scene">Engine-owned frame content.</param>
        /// <returns>The frame text and its per-cell style runs.</returns>
        private (string Text, List<CouncilAsciiStyleRun> Runs) RenderKernelBattleFrame(int width, int height, KernelBattleScene scene)
        {
            try
            {
                var canvas = new KernelBattleCanvas(width, height);
                var messageHeight = Math.Clamp(height / 5, 7, 9);
                var fieldTop = 1;
                var fieldBottom = height - messageHeight - 1;
                var horizon = fieldTop + (fieldBottom - fieldTop) * 55 / 100;
                var grassSplit = horizon + (fieldBottom - horizon) / 2;
                for (var y = fieldTop; y <= fieldBottom; y++)
                {
                    var band = (y - fieldTop) * 3 / Math.Max(1, horizon - fieldTop);
                    var background = y < horizon
                        ? band switch { 0 => 111, 1 => 117, _ => 153 }
                        : y < grassSplit ? 150 : 114;
                    canvas.Fill(0, y, width, 1, ' ', canvas.Style(16, background));
                }

                canvas.Fill(0, 0, width, 1, ' ', canvas.Style(226, 17, true));
                var title = $" ◆ KERNEL CREATURE TOURNAMENT ◆  ROUND {scene.Round} · E{scene.Exchange} · {scene.Headline}";
                var titleLength = canvas.Text(1, 0, title, 226, 17, true, width - 2);
                if (!string.IsNullOrWhiteSpace(scene.HudMotif) && titleLength + scene.HudMotif.Length + 6 < width)
                    canvas.Text(width - scene.HudMotif.Length - 2, 0, scene.HudMotif, 159, 17);

                var scale = width >= 110 ? 2 : 1;
                var boxWidth = Math.Clamp(width * 38 / 100, 30, 48);
                var fieldMiddle = fieldTop + (fieldBottom - fieldTop) / 2;
                if (scene.SameFighter && scene.Left is { } champion)
                {
                    DrawKernelSprite(canvas, champion, width / 2, fieldBottom - 2, fieldBottom, scale, mirror: false, facing: 1);
                    DrawKernelInfoBox(canvas, champion, 2, fieldTop + 1, boxWidth, scene);
                    var crown = "♛ CHAMPION ♛";
                    var crownX = Math.Max(boxWidth + 6, (width - crown.Length) / 2);
                    canvas.Text(crownX, fieldTop + 2, crown, 220, null, bold: true);
                }
                else
                {
                    if (scene.Right is { } opponent)
                    {
                        var opponentBottom = Math.Min(fieldMiddle + 2, fieldBottom - 4);
                        DrawKernelSprite(canvas, opponent, width * 72 / 100, opponentBottom, fieldBottom, scale, mirror: true, facing: -1);
                        DrawKernelInfoBox(canvas, opponent, 2, fieldTop + 1, boxWidth, scene);
                    }
                    if (scene.Left is { } player)
                    {
                        DrawKernelSprite(canvas, player, width * 26 / 100, fieldBottom - 2, fieldBottom, scale, mirror: false, facing: 1);
                        DrawKernelInfoBox(canvas, player, width - boxWidth - 2, Math.Max(fieldTop + 7, fieldBottom - 6), boxWidth, scene);
                    }
                    if (scene.Left is not null && scene.Right is not null && !string.IsNullOrWhiteSpace(scene.CenterEffect))
                    {
                        var effect = scene.CenterEffect.Trim();
                        canvas.Text(Math.Max(0, (width - effect.Length) / 2), fieldMiddle, effect, 196, null, bold: true);
                    }
                }

                DrawKernelMessageBox(canvas, scene, fieldBottom + 1, messageHeight);
                return (canvas.ToText(), canvas.ToRuns());
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Rendering the Kernel Creature Tournament battle frame failed.");
                throw;
            }
        }

        /// <summary>Draws a member-authored sprite on its arena platform, or an honest placeholder when none was supplied.</summary>
        /// <param name="canvas">Target canvas.</param>
        /// <param name="side">Fighter presentation data.</param>
        /// <param name="centerX">Horizontal sprite center.</param>
        /// <param name="bottomY">Row of the sprite's lowest pixel row.</param>
        /// <param name="fieldBottom">Last row of the battle field.</param>
        /// <param name="scale">Columns per sprite pixel.</param>
        /// <param name="mirror">Whether to mirror the sprite horizontally.</param>
        /// <param name="facing">+1 when facing right, -1 when facing left.</param>
        private void DrawKernelSprite(KernelBattleCanvas canvas, KernelBattleSide side, int centerX, int bottomY, int fieldBottom, int scale, bool mirror, int facing)
        {
            try
            {
                static int PaletteColor(IReadOnlyList<int> palette, int index, int fallback) =>
                    index < palette.Count ? Math.Clamp(palette[index], 0, 255) : fallback;

                var palette = side.SpritePalette;
                var fainted = side.Health <= 0;
                var dx = side.Pose switch { "attack" => 3 * facing, "hit" => -2 * facing, _ => 0 };
                var dy = fainted ? 1 : side.Pose == "victory" ? -1 : 0;
                var flash = side.Pose == "hit" && !fainted;
                var rows = side.SpriteRows;
                var isPixel = string.Equals(side.SpriteKind, "pixel", StringComparison.Ordinal) && rows.Count > 0;
                var isAscii = string.Equals(side.SpriteKind, "ascii", StringComparison.Ordinal) && rows.Count > 0;
                var pixelWidth = rows.Count == 0 ? 10 : rows.Max(row => row.Length);
                var spriteColumns = isAscii ? pixelWidth : pixelWidth * scale;

                var platformWidth = spriteColumns + 8;
                var platformLeft = centerX - platformWidth / 2;
                if (bottomY + 1 <= fieldBottom)
                    canvas.Fill(platformLeft + 2, bottomY + 1, platformWidth - 4, 1, ' ', canvas.Style(16, 108));
                if (bottomY + 2 <= fieldBottom)
                    canvas.Fill(platformLeft + 5, bottomY + 2, platformWidth - 10, 1, ' ', canvas.Style(16, 65));

                var left = centerX - spriteColumns / 2 + dx;
                if (isPixel)
                {
                    var top = bottomY - rows.Count + 1 + dy;
                    for (var py = 0; py < rows.Count; py++)
                    {
                        var row = rows[py].PadRight(pixelWidth, '.');
                        for (var px = 0; px < pixelWidth; px++)
                        {
                            var legend = mirror ? row[pixelWidth - 1 - px] : row[px];
                            if (legend == '.')
                                continue;
                            var color = legend switch
                            {
                                '#' => PaletteColor(palette, 0, 16),
                                '+' => PaletteColor(palette, 1, 75),
                                '-' => PaletteColor(palette, 2, 25),
                                '*' => PaletteColor(palette, 3, 220),
                                'o' => PaletteColor(palette, 4, 231),
                                _ => PaletteColor(palette, 1, 75)
                            };
                            if (fainted)
                                color = legend == '#' ? 238 : legend == 'o' ? 250 : 245;
                            else if (flash && legend is '+' or '-' or '*')
                                color = 231;
                            var style = canvas.Style(color, color);
                            for (var repeat = 0; repeat < scale; repeat++)
                                canvas.Put(left + px * scale + repeat, top + py, '█', style);
                        }
                    }
                    return;
                }

                if (isAscii)
                {
                    var top = bottomY - rows.Count + 1 + dy;
                    var outline = fainted ? 238 : PaletteColor(palette, 0, 16);
                    var body = fainted ? 245 : flash ? 231 : PaletteColor(palette, 1, 75);
                    for (var py = 0; py < rows.Count; py++)
                    {
                        var source = rows[py].PadRight(pixelWidth);
                        for (var px = 0; px < pixelWidth; px++)
                        {
                            var glyph = mirror ? source[pixelWidth - 1 - px] : source[px];
                            if (mirror)
                                glyph = glyph switch { '/' => '\\', '\\' => '/', '(' => ')', ')' => '(', '<' => '>', '>' => '<', '[' => ']', ']' => '[', '{' => '}', '}' => '{', _ => glyph };
                            if (glyph == ' ')
                                continue;
                            var color = "/\\|_()[]{}<>".Contains(glyph, StringComparison.Ordinal) ? outline : body;
                            canvas.Text(left + px, top + py, glyph.ToString(), color, null, bold: true);
                        }
                    }
                    return;
                }

                // No usable member sprite: draw a neutral silhouette and say so instead of inventing creature art.
                const int eggWidth = 10;
                const int eggHeight = 8;
                var eggLeft = centerX - eggWidth * scale / 2;
                var eggTop = bottomY - eggHeight + 1 + dy;
                for (var py = 0; py < eggHeight; py++)
                {
                    for (var px = 0; px < eggWidth; px++)
                    {
                        var nx = (px + 0.5 - eggWidth / 2.0) / (eggWidth / 2.0);
                        var ny = (py + 0.5 - eggHeight / 2.0) / (eggHeight / 2.0);
                        var distance = nx * nx + ny * ny;
                        if (distance > 1.0)
                            continue;
                        var color = distance > 0.62 ? 240 : 244;
                        for (var repeat = 0; repeat < scale; repeat++)
                            canvas.Put(eggLeft + px * scale + repeat, eggTop + py, '█', canvas.Style(color, color));
                    }
                }
                canvas.Put(centerX, eggTop + eggHeight / 2, '?', canvas.Style(231, 244, true));
                var label = $"no sprite drawn by {side.SpriteAuthor}";
                var labelRow = Math.Min(fieldBottom, bottomY + 2);
                canvas.Text(Math.Max(0, centerX - label.Length / 2), labelRow, label, 88, null, maximum: Math.Max(0, canvas.Width - 1));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Drawing a Kernel Creature Tournament sprite failed.");
                throw;
            }
        }

        /// <summary>Draws the name, health bar, roster and trainer box for one fighter.</summary>
        /// <param name="canvas">Target canvas.</param>
        /// <param name="side">Fighter presentation data.</param>
        /// <param name="x">Left column.</param>
        /// <param name="y">Top row.</param>
        /// <param name="boxWidth">Box width.</param>
        /// <param name="scene">Scene supplying health and switch limits.</param>
        private void DrawKernelInfoBox(KernelBattleCanvas canvas, KernelBattleSide side, int x, int y, int boxWidth, KernelBattleScene scene)
        {
            try
            {
                const int paper = 230;
                const int ink = 16;
                const int frame = 238;
                var inner = boxWidth - 2;
                canvas.Fill(x, y, boxWidth, 5, ' ', canvas.Style(ink, paper));
                canvas.Put(x, y, '╭', canvas.Style(frame, paper));
                canvas.Put(x + boxWidth - 1, y, '╮', canvas.Style(frame, paper));
                canvas.Put(x, y + 4, '╰', canvas.Style(frame, paper));
                canvas.Put(x + boxWidth - 1, y + 4, '╯', canvas.Style(frame, paper));
                for (var column = 1; column < boxWidth - 1; column++)
                {
                    canvas.Put(x + column, y, '─', canvas.Style(frame, paper));
                    canvas.Put(x + column, y + 4, '─', canvas.Style(frame, paper));
                }
                for (var row = 1; row < 4; row++)
                {
                    canvas.Put(x, y + row, '│', canvas.Style(frame, paper));
                    canvas.Put(x + boxWidth - 1, y + row, '│', canvas.Style(frame, paper));
                }

                var nameLength = canvas.Text(x + 2, y + 1, side.CreatureName, ink, paper, bold: true, maximum: inner - 2);
                var species = side.Species;
                if (!string.IsNullOrWhiteSpace(species) && nameLength + species.Length + 5 < inner)
                    canvas.Text(x + boxWidth - 2 - species.Length, y + 1, species, 241, paper);

                var startingHealth = Math.Max(1, scene.StartingHealth);
                var health = Math.Max(0, side.Health);
                var numbers = $" {health,3}/{startingHealth}";
                canvas.Text(x + 2, y + 2, "HP", 208, paper, bold: true);
                var barWidth = Math.Max(4, inner - 4 - numbers.Length - 1);
                var ratio = Math.Clamp(health / (double)startingHealth, 0d, 1d);
                var filled = (int)Math.Round(barWidth * ratio);
                var barColor = ratio > 0.5 ? 40 : ratio > 0.2 ? 220 : 196;
                for (var cell = 0; cell < barWidth; cell++)
                    canvas.Put(x + 5 + cell, y + 2, cell < filled ? '█' : ' ', canvas.Style(cell < filled ? barColor : 240, 236));
                canvas.Text(x + 5 + barWidth, y + 2, numbers, ink, paper, maximum: Math.Max(0, boxWidth - 6 - barWidth));

                var rosterX = x + 2;
                foreach (var (memberHealth, active) in side.Roster)
                {
                    var glyph = memberHealth > 0 ? '●' : '○';
                    var color = memberHealth <= 0 ? 245 : active ? 160 : 203;
                    canvas.Put(rosterX, y + 3, glyph, canvas.Style(color, paper, bold: active));
                    rosterX += 2;
                }
                var switches = $"SWITCH {side.SwitchesUsed}/{scene.MaximumSwitches}";
                if (rosterX + switches.Length + 2 < x + boxWidth - 1)
                    canvas.Text(x + boxWidth - 2 - switches.Length, y + 3, switches, 241, paper);

                var trainer = $" TR {side.TrainerName} · {side.TrainerAction} ";
                canvas.Text(x + 2, y + 4, trainer, 24, paper, bold: true, maximum: inner - 2);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Drawing a Kernel Creature Tournament info box failed.");
                throw;
            }
        }

        /// <summary>Draws the bottom message box and the trainer command panel.</summary>
        /// <param name="canvas">Target canvas.</param>
        /// <param name="scene">Engine-owned frame content.</param>
        /// <param name="top">First message-box row.</param>
        /// <param name="messageHeight">Message-box height.</param>
        private void DrawKernelMessageBox(KernelBattleCanvas canvas, KernelBattleScene scene, int top, int messageHeight)
        {
            try
            {
                static List<string> Wrap(string? text, int width, int maximumLines)
                {
                    var lines = new List<string>();
                    var current = new StringBuilder();
                    foreach (var word in (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var piece = word.Length > width ? word[..width] : word;
                        if (current.Length > 0 && current.Length + 1 + piece.Length > width)
                        {
                            lines.Add(current.ToString());
                            current.Clear();
                            if (lines.Count >= maximumLines)
                                return lines;
                        }
                        if (current.Length > 0)
                            current.Append(' ');
                        current.Append(piece);
                    }
                    if (current.Length > 0 && lines.Count < maximumLines)
                        lines.Add(current.ToString());
                    return lines;
                }

                void Frame(int x, int y, int width, int height, int borderColor, int background, bool doubleLine)
                {
                    var border = canvas.Style(borderColor, background);
                    canvas.Fill(x, y, width, height, ' ', canvas.Style(231, background));
                    var horizontal = doubleLine ? '═' : '─';
                    var vertical = doubleLine ? '║' : '│';
                    for (var column = x + 1; column < x + width - 1; column++)
                    {
                        canvas.Put(column, y, horizontal, border);
                        canvas.Put(column, y + height - 1, horizontal, border);
                    }
                    for (var row = y + 1; row < y + height - 1; row++)
                    {
                        canvas.Put(x, row, vertical, border);
                        canvas.Put(x + width - 1, row, vertical, border);
                    }
                    canvas.Put(x, y, doubleLine ? '╔' : '┌', border);
                    canvas.Put(x + width - 1, y, doubleLine ? '╗' : '┐', border);
                    canvas.Put(x, y + height - 1, doubleLine ? '╚' : '└', border);
                    canvas.Put(x + width - 1, y + height - 1, doubleLine ? '╝' : '┘', border);
                }

                var width = canvas.Width;
                var commandWidth = width >= 90 ? Math.Clamp(width / 3, 26, 44) : 0;
                var messageWidth = width - commandWidth;
                Frame(0, top, messageWidth, messageHeight, 75, 17, doubleLine: true);
                var innerWidth = Math.Max(1, messageWidth - 4);
                var lines = new List<(string Text, int Color, bool Bold)>();
                lines.AddRange(Wrap(scene.Body, innerWidth, 2).Select(line => (line, 231, true)));
                lines.AddRange(Wrap(scene.Footer, innerWidth, 1).Select(line => (line, 189, false)));
                if (scene.Left is { } left && !string.IsNullOrWhiteSpace(left.Statement))
                    lines.AddRange(Wrap($"{left.TrainerName}: {left.Statement}", innerWidth, 1).Select(line => (line, 159, false)));
                if (scene.Right is { } right && !scene.SameFighter && !string.IsNullOrWhiteSpace(right.Statement))
                    lines.AddRange(Wrap($"{right.TrainerName}: {right.Statement}", innerWidth, 1).Select(line => (line, 223, false)));
                if (!string.IsNullOrWhiteSpace(scene.TimelineRecent))
                    lines.Add((scene.TimelineRecent, 103, false));
                if (!string.IsNullOrWhiteSpace(scene.TimelinePrevious))
                    lines.Add((scene.TimelinePrevious, 60, false));
                for (var index = 0; index < Math.Min(lines.Count, messageHeight - 2); index++)
                    canvas.Text(2, top + 1 + index, lines[index].Text, lines[index].Color, 17, lines[index].Bold, innerWidth);

                if (commandWidth == 0)
                    return;
                var panelX = messageWidth;
                Frame(panelX, top, commandWidth, messageHeight, 214, 18, doubleLine: false);
                var panelInner = commandWidth - 4;
                canvas.Text(panelX + 2, top, " TRAINER COMMANDS ", 214, 18, bold: true, maximum: panelInner);
                var row = top + 1;
                foreach (var side in new[] { scene.Left, scene.SameFighter ? null : scene.Right })
                {
                    if (side is null || row >= top + messageHeight - 1)
                        continue;
                    canvas.Text(panelX + 2, row++, $"▶ {side.TrainerName}", 230, 18, bold: true, maximum: panelInner);
                    if (row < top + messageHeight - 1)
                        canvas.Text(panelX + 4, row++, $"{side.TrainerAction} · SWITCH {side.SwitchesUsed}/{scene.MaximumSwitches}", 189, 18, maximum: panelInner - 2);
                }
                if (row < top + messageHeight - 1)
                    canvas.Text(panelX + 2, top + messageHeight - 2, "HP & bracket: LocalGPT engine", 103, 18, maximum: panelInner);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Drawing the Kernel Creature Tournament message box failed.");
                throw;
            }
        }
    }
}
