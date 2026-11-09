using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Renders the handheld-style ANSI battle screen: sky and grass field, member sprites on platforms, info boxes with
/// health bars and rosters, a message box and an owner command panel. Colours come from the view parameter
/// <c>palette</c>, labels from the <c>view.*</c> texts.
/// </summary>
/// <param name="parameters">Reads the view palette and sprite legend.</param>
/// <param name="texts">Formats view labels.</param>
/// <param name="logger">Receives renderer diagnostics.</param>
public sealed class DuelBattleRenderer(
    IGameParameterReader parameters,
    IGameTextService texts,
    ILogger<DuelBattleRenderer> logger) : IDuelBattleRenderer
{
    /// <inheritdoc />
    public GameViewFrame Render(GameEngineContext context, DuelScene scene)
    {
        try
        {
            var view = parameters.Parse(context.Ruleset.ViewParametersJson);
            var palette = parameters.ReadObject(view, "palette");
            var width = context.Session.FrameWidth;
            var height = context.Session.FrameHeight;
            var canvas = new DuelCanvas(width, height);
            var messageHeight = Math.Clamp(height / 5, 7, 9);
            var fieldTop = 1;
            var fieldBottom = height - messageHeight - 1;
            var horizon = fieldTop + (fieldBottom - fieldTop) * 55 / 100;
            var grassSplit = horizon + (fieldBottom - horizon) / 2;
            var sky = parameters.ReadInts(palette, "sky");
            for (var y = fieldTop; y <= fieldBottom; y++)
            {
                var band = (y - fieldTop) * 3 / Math.Max(1, horizon - fieldTop);
                var background = y < horizon
                    ? (sky.Count == 0 ? 117 : sky[Math.Clamp(band, 0, sky.Count - 1)])
                    : y < grassSplit ? Color(palette, "grassNear", 150) : Color(palette, "grassFar", 114);
                canvas.Fill(0, y, width, 1, ' ', canvas.Style(Color(palette, "fieldInk", 16), background));
            }

            canvas.Fill(0, 0, width, 1, ' ', canvas.Style(Color(palette, "titleForeground", 226), Color(palette, "titleBackground", 17), true));
            var title = texts.Format(context.Ruleset, "view.title", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["round"] = scene.Round,
                ["exchange"] = scene.Exchange,
                ["headline"] = scene.Headline
            });
            var titleLength = canvas.Text(1, 0, title, Color(palette, "titleForeground", 226), Color(palette, "titleBackground", 17), true, width - 2);
            if (!string.IsNullOrWhiteSpace(scene.HudMotif) && titleLength + scene.HudMotif.Length + 6 < width)
                canvas.Text(width - scene.HudMotif.Length - 2, 0, scene.HudMotif, Color(palette, "hud", 159), Color(palette, "titleBackground", 17));

            var scale = width >= 110 ? 2 : 1;
            var boxWidth = Math.Clamp(width * 38 / 100, 30, 48);
            var fieldMiddle = fieldTop + (fieldBottom - fieldTop) / 2;
            if (scene.SameEntrant && scene.Left is { } champion)
            {
                DrawSprite(context, view, palette, canvas, champion, width / 2, fieldBottom - 2, fieldBottom, scale, mirror: false, facing: 1);
                DrawInfoBox(context, palette, canvas, champion, 2, fieldTop + 1, boxWidth, scene);
                var crown = texts.Get(context.Ruleset, "view.champion");
                canvas.Text(Math.Max(boxWidth + 6, (width - crown.Length) / 2), fieldTop + 2, crown, Color(palette, "crown", 220), null, bold: true);
            }
            else
            {
                if (scene.Right is { } opponent)
                {
                    var opponentBottom = Math.Min(fieldMiddle + 2, fieldBottom - 4);
                    DrawSprite(context, view, palette, canvas, opponent, width * 72 / 100, opponentBottom, fieldBottom, scale, mirror: true, facing: -1);
                    DrawInfoBox(context, palette, canvas, opponent, 2, fieldTop + 1, boxWidth, scene);
                }
                if (scene.Left is { } player)
                {
                    DrawSprite(context, view, palette, canvas, player, width * 26 / 100, fieldBottom - 2, fieldBottom, scale, mirror: false, facing: 1);
                    DrawInfoBox(context, palette, canvas, player, width - boxWidth - 2, Math.Max(fieldTop + 7, fieldBottom - 6), boxWidth, scene);
                }
                if (scene.Left is not null && scene.Right is not null && !string.IsNullOrWhiteSpace(scene.CenterEffect))
                {
                    var effect = scene.CenterEffect.Trim();
                    canvas.Text(Math.Max(0, (width - effect.Length) / 2), fieldMiddle, effect, Color(palette, "centerEffect", 196), null, bold: true);
                }
            }

            DrawMessageBox(context, palette, canvas, scene, fieldBottom + 1, messageHeight);
            return new GameViewFrame { Text = canvas.ToText(), StyleRuns = canvas.ToRuns() };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the duel battle view failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads a palette colour.
    /// </summary>
    /// <param name="palette">The view palette.</param>
    /// <param name="name">The colour name.</param>
    /// <param name="fallback">The engine default colour.</param>
    /// <returns>The 256-colour index.</returns>
    private int Color(JsonElement palette, string name, int fallback)
    {
        try
        {
            return parameters.ReadInt(palette, name, fallback);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading duel palette colour {ColorName} failed.", name);
            throw;
        }
    }

    /// <summary>
    /// Draws a member sprite on its platform: pixel legend, ASCII art, or a neutral silhouette with an attribution note.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="view">The view parameters.</param>
    /// <param name="palette">The view palette.</param>
    /// <param name="canvas">The canvas.</param>
    /// <param name="side">The scene side.</param>
    /// <param name="centerX">The sprite centre column.</param>
    /// <param name="bottomY">The sprite bottom row.</param>
    /// <param name="fieldBottom">The last field row.</param>
    /// <param name="scale">The horizontal pixel scale.</param>
    /// <param name="mirror">Whether the sprite is mirrored.</param>
    /// <param name="facing">1 when facing right, -1 when facing left.</param>
    private void DrawSprite(GameEngineContext context, JsonElement view, JsonElement palette, DuelCanvas canvas, DuelSceneSide side, int centerX, int bottomY, int fieldBottom, int scale, bool mirror, int facing)
    {
        try
        {
            var sprite = parameters.ReadObject(view, "sprite");
            var fainted = side.Health <= 0;
            var offsets = parameters.ReadObject(parameters.ReadObject(sprite, "poseOffsets"), side.Pose);
            var dx = parameters.ReadInt(offsets, "dx", 0) * facing;
            var dy = fainted ? parameters.ReadInt(sprite, "faintedDy", 1) : parameters.ReadInt(offsets, "dy", 0);
            var flash = string.Equals(side.Pose, parameters.ReadString(sprite, "flashPose", "hit"), StringComparison.Ordinal) && !fainted;
            var rows = side.SpriteRows;
            var isPixel = string.Equals(side.SpriteKind, "pixel", StringComparison.Ordinal) && rows.Count > 0;
            var isAscii = string.Equals(side.SpriteKind, "ascii", StringComparison.Ordinal) && rows.Count > 0;
            var pixelWidth = rows.Count == 0 ? 10 : rows.Max(row => row.Length);
            var spriteColumns = isAscii ? pixelWidth : pixelWidth * scale;

            var platformWidth = spriteColumns + 8;
            var platformLeft = centerX - platformWidth / 2;
            var ink = Color(palette, "fieldInk", 16);
            if (bottomY + 1 <= fieldBottom)
                canvas.Fill(platformLeft + 2, bottomY + 1, platformWidth - 4, 1, ' ', canvas.Style(ink, Color(palette, "platformTop", 108)));
            if (bottomY + 2 <= fieldBottom)
                canvas.Fill(platformLeft + 5, bottomY + 2, platformWidth - 10, 1, ' ', canvas.Style(ink, Color(palette, "platformBottom", 65)));

            var left = centerX - spriteColumns / 2 + dx;
            if (isPixel)
            {
                var legend = parameters.ReadString(sprite, "pixelLegend", "#+-*o");
                var defaults = parameters.ReadInts(sprite, "defaultPalette");
                var flashLegend = parameters.ReadString(sprite, "flashLegend", string.Empty);
                var faint = parameters.ReadObject(sprite, "faintLegend");
                var empty = parameters.ReadString(sprite, "emptyPixel", ".");
                var emptyCell = empty.Length == 0 ? '.' : empty[0];
                var top = bottomY - rows.Count + 1 + dy;
                for (var py = 0; py < rows.Count; py++)
                {
                    var row = rows[py].PadRight(pixelWidth, emptyCell);
                    for (var px = 0; px < pixelWidth; px++)
                    {
                        var cell = mirror ? row[pixelWidth - 1 - px] : row[px];
                        if (cell == emptyCell)
                            continue;
                        var index = legend.IndexOf(cell);
                        if (index < 0)
                            index = 1;
                        var color = index < side.SpritePalette.Count
                            ? Math.Clamp(side.SpritePalette[index], 0, 255)
                            : index < defaults.Count ? defaults[index] : 75;
                        if (fainted)
                            color = parameters.ReadInt(faint, cell.ToString(), Color(palette, "faintBody", 245));
                        else if (flash && flashLegend.Contains(cell))
                            color = Color(palette, "flash", 231);
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
                var defaults = parameters.ReadInts(sprite, "defaultPalette");
                var outline = fainted ? Color(palette, "faintOutline", 238) : side.SpritePalette.Count > 0 ? Math.Clamp(side.SpritePalette[0], 0, 255) : defaults.ElementAtOrDefault(0);
                var body = fainted ? Color(palette, "faintBody", 245) : flash ? Color(palette, "flash", 231) : side.SpritePalette.Count > 1 ? Math.Clamp(side.SpritePalette[1], 0, 255) : defaults.ElementAtOrDefault(1);
                var outlineGlyphs = parameters.ReadString(sprite, "asciiOutlineGlyphs", "/\\|_()[]{}<>");
                var mirrorPairs = parameters.ReadString(sprite, "mirrorPairs", "/\\()<>[]{}");
                for (var py = 0; py < rows.Count; py++)
                {
                    var source = rows[py].PadRight(pixelWidth);
                    for (var px = 0; px < pixelWidth; px++)
                    {
                        var glyph = mirror ? Mirror(source[pixelWidth - 1 - px], mirrorPairs) : source[px];
                        if (glyph == ' ')
                            continue;
                        canvas.Text(left + px, top + py, glyph.ToString(), outlineGlyphs.Contains(glyph) ? outline : body, null, bold: true);
                    }
                }
                return;
            }

            // No usable member sprite: draw a neutral silhouette and say so instead of inventing art.
            const int silhouetteWidth = 10;
            const int silhouetteHeight = 8;
            var silhouetteLeft = centerX - silhouetteWidth * scale / 2;
            var silhouetteTop = bottomY - silhouetteHeight + 1 + dy;
            for (var py = 0; py < silhouetteHeight; py++)
            {
                for (var px = 0; px < silhouetteWidth; px++)
                {
                    var nx = (px + 0.5 - silhouetteWidth / 2.0) / (silhouetteWidth / 2.0);
                    var ny = (py + 0.5 - silhouetteHeight / 2.0) / (silhouetteHeight / 2.0);
                    var distance = nx * nx + ny * ny;
                    if (distance > 1.0)
                        continue;
                    var color = distance > 0.62 ? Color(palette, "silhouetteRim", 240) : Color(palette, "silhouetteCore", 244);
                    for (var repeat = 0; repeat < scale; repeat++)
                        canvas.Put(silhouetteLeft + px * scale + repeat, silhouetteTop + py, '█', canvas.Style(color, color));
                }
            }
            canvas.Put(centerX, silhouetteTop + silhouetteHeight / 2, '?', canvas.Style(Color(palette, "silhouetteMark", 231), Color(palette, "silhouetteCore", 244), true));
            var label = texts.Format(context.Ruleset, "view.no-sprite", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["author"] = side.SpriteAuthor });
            canvas.Text(Math.Max(0, centerX - label.Length / 2), Math.Min(fieldBottom, bottomY + 2), label, Color(palette, "noSpriteLabel", 88), null, maximum: Math.Max(0, canvas.Width - 1));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Drawing a duel sprite failed.");
            throw;
        }
    }

    /// <summary>
    /// Mirrors a directional ASCII glyph through the declared mirror pairs.
    /// </summary>
    /// <param name="glyph">The glyph.</param>
    /// <param name="pairs">Consecutive glyph pairs that mirror each other.</param>
    /// <returns>The mirrored glyph.</returns>
    private char Mirror(char glyph, string pairs)
    {
        try
        {
            for (var index = 0; index + 1 < pairs.Length; index += 2)
            {
                if (pairs[index] == glyph)
                    return pairs[index + 1];
                if (pairs[index + 1] == glyph)
                    return pairs[index];
            }
            return glyph;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Mirroring a duel sprite glyph failed.");
            throw;
        }
    }

    /// <summary>
    /// Draws a side's info box: member name and species, health bar, roster dots, switch budget and owner action.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="palette">The view palette.</param>
    /// <param name="canvas">The canvas.</param>
    /// <param name="side">The scene side.</param>
    /// <param name="x">The left column.</param>
    /// <param name="y">The top row.</param>
    /// <param name="boxWidth">The box width.</param>
    /// <param name="scene">The scene.</param>
    private void DrawInfoBox(GameEngineContext context, JsonElement palette, DuelCanvas canvas, DuelSceneSide side, int x, int y, int boxWidth, DuelScene scene)
    {
        try
        {
            var paper = Color(palette, "infoPaper", 230);
            var ink = Color(palette, "infoInk", 16);
            var frame = Color(palette, "infoFrame", 238);
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

            var nameLength = canvas.Text(x + 2, y + 1, side.MemberName, ink, paper, bold: true, maximum: inner - 2);
            var species = side.Species;
            if (!string.IsNullOrWhiteSpace(species) && nameLength + species.Length + 5 < inner)
                canvas.Text(x + boxWidth - 2 - species.Length, y + 1, species, Color(palette, "species", 241), paper);

            var startingHealth = Math.Max(1, scene.StartingHealth);
            var health = Math.Max(0, side.Health);
            var numbers = $" {health,3}/{startingHealth}";
            canvas.Text(x + 2, y + 2, texts.Get(context.Ruleset, "view.health"), Color(palette, "healthLabel", 208), paper, bold: true);
            var barWidth = Math.Max(4, inner - 4 - numbers.Length - 1);
            var ratio = Math.Clamp(health / (double)startingHealth, 0d, 1d);
            var filled = (int)Math.Round(barWidth * ratio);
            var barColor = ratio > 0.5 ? Color(palette, "barHigh", 40) : ratio > 0.2 ? Color(palette, "barMiddle", 220) : Color(palette, "barLow", 196);
            for (var cell = 0; cell < barWidth; cell++)
                canvas.Put(x + 5 + cell, y + 2, cell < filled ? '█' : ' ', canvas.Style(cell < filled ? barColor : Color(palette, "barEmpty", 240), Color(palette, "barTrack", 236)));
            canvas.Text(x + 5 + barWidth, y + 2, numbers, ink, paper, maximum: Math.Max(0, boxWidth - 6 - barWidth));

            var rosterX = x + 2;
            foreach (var (memberHealth, active) in side.Roster)
            {
                var glyph = memberHealth > 0 ? '●' : '○';
                var color = memberHealth <= 0 ? Color(palette, "rosterFainted", 245) : active ? Color(palette, "rosterActive", 160) : Color(palette, "rosterBench", 203);
                canvas.Put(rosterX, y + 3, glyph, canvas.Style(color, paper, bold: active));
                rosterX += 2;
            }
            var switches = texts.Format(context.Ruleset, "view.switches", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["used"] = side.SwitchesUsed,
                ["max"] = scene.MaximumSwitches
            });
            if (rosterX + switches.Length + 2 < x + boxWidth - 1)
                canvas.Text(x + boxWidth - 2 - switches.Length, y + 3, switches, Color(palette, "switches", 241), paper);

            var owner = texts.Format(context.Ruleset, "view.owner", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["owner"] = side.OwnerName,
                ["action"] = side.OwnerAction
            });
            canvas.Text(x + 2, y + 4, owner, Color(palette, "ownerLabel", 24), paper, bold: true, maximum: inner - 2);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Drawing a duel info box failed.");
            throw;
        }
    }

    /// <summary>
    /// Draws the message box and, on wide terminals, the owner command panel.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="palette">The view palette.</param>
    /// <param name="canvas">The canvas.</param>
    /// <param name="scene">The scene.</param>
    /// <param name="top">The top row.</param>
    /// <param name="messageHeight">The box height.</param>
    private void DrawMessageBox(GameEngineContext context, JsonElement palette, DuelCanvas canvas, DuelScene scene, int top, int messageHeight)
    {
        try
        {
            var width = canvas.Width;
            var commandWidth = width >= 90 ? Math.Clamp(width / 3, 26, 44) : 0;
            var messageWidth = width - commandWidth;
            var messageBackground = Color(palette, "messageBackground", 17);
            Frame(canvas, palette, 0, top, messageWidth, messageHeight, Color(palette, "messageBorder", 75), messageBackground, doubleLine: true);
            var innerWidth = Math.Max(1, messageWidth - 4);
            var lines = new List<(string Text, int Color, bool Bold)>();
            lines.AddRange(Wrap(scene.Body, innerWidth, 2).Select(line => (line, Color(palette, "messageBody", 231), true)));
            lines.AddRange(Wrap(scene.Footer, innerWidth, 1).Select(line => (line, Color(palette, "messageFooter", 189), false)));
            if (scene.Left is { } left && !string.IsNullOrWhiteSpace(left.Statement))
                lines.AddRange(Wrap($"{left.OwnerName}: {left.Statement}", innerWidth, 1).Select(line => (line, Color(palette, "leftStatement", 159), false)));
            if (scene.Right is { } right && !scene.SameEntrant && !string.IsNullOrWhiteSpace(right.Statement))
                lines.AddRange(Wrap($"{right.OwnerName}: {right.Statement}", innerWidth, 1).Select(line => (line, Color(palette, "rightStatement", 223), false)));
            if (!string.IsNullOrWhiteSpace(scene.TimelineRecent))
                lines.Add((scene.TimelineRecent, Color(palette, "timelineRecent", 103), false));
            if (!string.IsNullOrWhiteSpace(scene.TimelinePrevious))
                lines.Add((scene.TimelinePrevious, Color(palette, "timelinePrevious", 60), false));
            for (var index = 0; index < Math.Min(lines.Count, messageHeight - 2); index++)
                canvas.Text(2, top + 1 + index, lines[index].Text, lines[index].Color, messageBackground, lines[index].Bold, innerWidth);

            if (commandWidth == 0)
                return;
            var panelX = messageWidth;
            var panelBackground = Color(palette, "panelBackground", 18);
            Frame(canvas, palette, panelX, top, commandWidth, messageHeight, Color(palette, "panelBorder", 214), panelBackground, doubleLine: false);
            var panelInner = commandWidth - 4;
            canvas.Text(panelX + 2, top, texts.Get(context.Ruleset, "view.commands.title"), Color(palette, "panelTitle", 214), panelBackground, bold: true, maximum: panelInner);
            var row = top + 1;
            foreach (var side in new[] { scene.Left, scene.SameEntrant ? null : scene.Right })
            {
                if (side is null || row >= top + messageHeight - 1)
                    continue;
                canvas.Text(panelX + 2, row++, texts.Format(context.Ruleset, "view.commands.owner", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["owner"] = side.OwnerName }),
                    Color(palette, "panelOwner", 230), panelBackground, bold: true, maximum: panelInner);
                if (row < top + messageHeight - 1)
                {
                    canvas.Text(panelX + 4, row++, texts.Format(context.Ruleset, "view.commands.action", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["action"] = side.OwnerAction,
                        ["used"] = side.SwitchesUsed,
                        ["max"] = scene.MaximumSwitches
                    }), Color(palette, "panelAction", 189), panelBackground, maximum: panelInner - 2);
                }
            }
            if (row < top + messageHeight - 1)
                canvas.Text(panelX + 2, top + messageHeight - 2, texts.Get(context.Ruleset, "view.commands.footer"), Color(palette, "panelFooter", 103), panelBackground, maximum: panelInner);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Drawing the duel message box failed.");
            throw;
        }
    }

    /// <summary>
    /// Wraps text into at most a number of lines, cutting single words longer than the width.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="width">The line width.</param>
    /// <param name="maximumLines">The maximum number of lines.</param>
    /// <returns>The wrapped lines.</returns>
    private List<string> Wrap(string? text, int width, int maximumLines)
    {
        try
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
        catch (Exception exception)
        {
            logger.LogError(exception, "Wrapping duel message text failed.");
            throw;
        }
    }

    /// <summary>
    /// Draws a filled box frame with single or double lines.
    /// </summary>
    /// <param name="canvas">The canvas.</param>
    /// <param name="palette">The view palette.</param>
    /// <param name="x">The left column.</param>
    /// <param name="y">The top row.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="borderColor">The border colour.</param>
    /// <param name="background">The background colour.</param>
    /// <param name="doubleLine">Whether double lines are drawn.</param>
    private void Frame(DuelCanvas canvas, JsonElement palette, int x, int y, int width, int height, int borderColor, int background, bool doubleLine)
    {
        try
        {
            var border = canvas.Style(borderColor, background);
            canvas.Fill(x, y, width, height, ' ', canvas.Style(Color(palette, "messageFill", 231), background));
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
        catch (Exception exception)
        {
            logger.LogError(exception, "Drawing a duel box frame failed.");
            throw;
        }
    }
}
