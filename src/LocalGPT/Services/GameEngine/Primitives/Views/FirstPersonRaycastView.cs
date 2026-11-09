using System.Globalization;
using System.Text.Json;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Views;

/// <summary>
/// View <c>first-person-raycast</c>: a ray-marched first-person grid view with distance-shaded walls, billboard actor
/// sprites, a radar overlay, a crosshair and heads-up rows rendered from ruleset texts.
/// </summary>
/// <param name="parameters">Reads the view parameters.</param>
/// <param name="actors">Finds the player, visible actors and markers.</param>
/// <param name="grid">Casts rays and tests line of sight.</param>
/// <param name="world">Reads the wall glyph.</param>
/// <param name="angles">Computes sprite angles.</param>
/// <param name="tokens">Builds heads-up tokens.</param>
/// <param name="texts">Formats heads-up rows.</param>
/// <param name="textUtility">Fits and writes rows.</param>
/// <param name="logger">Receives view diagnostics.</param>
public sealed class FirstPersonRaycastView(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    IGameGridService grid,
    IGameWorldService world,
    IGameAngleService angles,
    IGameTokenService tokens,
    IGameTextService texts,
    IGameTextUtilityService textUtility,
    ILogger<FirstPersonRaycastView> logger) : IGameViewPrimitive
{
    /// <inheritdoc />
    public string Key => "first-person-raycast";

    /// <inheritdoc />
    public GameViewFrame Render(GameEngineContext context)
    {
        try
        {
            var session = context.Session;
            var ruleset = context.Ruleset;
            var viewParameters = parameters.Parse(ruleset.ViewParametersJson);
            var player = actors.Player(session.Actors, ruleset);
            var width = session.FrameWidth;
            var height = session.FrameHeight;
            var hudRows = parameters.ReadObjects(viewParameters, "hudRows");
            var viewHeight = Math.Max(1, height - hudRows.Count - 2);
            var lines = Enumerable.Range(0, height).Select(_ => new string(' ', width).ToCharArray()).ToArray();
            if (player is null)
                return new GameViewFrame { Text = string.Join(Environment.NewLine, lines.Select(line => new string(line))) };

            var fieldOfView = Math.PI / parameters.ReadDouble(viewParameters, "fieldOfViewPiDivisor", 3d);
            var wall = world.Wall(ruleset);
            var rayStep = parameters.ReadDouble(viewParameters, "rayStep", .05d);
            var maximumDistance = parameters.ReadDouble(viewParameters, "maximumDistance", 20d);
            var wallScale = parameters.ReadDouble(viewParameters, "wallScale", .82d);
            var duckFlag = parameters.ReadString(viewParameters, "duckVariable", string.Empty);
            var ducking = duckFlag.Length > 0 && session.Variables.TryGetValue(duckFlag, out var duckValue) && string.Equals(duckValue, "true", StringComparison.OrdinalIgnoreCase);
            var duckOffset = ducking ? parameters.ReadInt(viewParameters, "duckOffset", 2) : 0;
            var ceiling = parameters.ReadString(viewParameters, "ceilingGlyph", ".");
            var wallBands = parameters.ReadObjects(viewParameters, "wallBands");
            var floorBands = parameters.ReadObjects(viewParameters, "floorBands");
            for (var x = 0; x < width; x++)
            {
                var rayAngle = player.FacingRadians - fieldOfView / 2d + fieldOfView * x / Math.Max(1, width - 1);
                var distance = grid.CastRay(session.WorldMap, player.X + .5d, player.Y + .5d, rayAngle, wall, rayStep, maximumDistance);
                var correctedDistance = Math.Max(.15d, distance * Math.Cos(rayAngle - player.FacingRadians));
                var wallHeight = Math.Clamp((int)Math.Round(viewHeight / correctedDistance * wallScale), 1, viewHeight);
                var top = Math.Max(0, (viewHeight - wallHeight) / 2 + duckOffset);
                var bottom = Math.Min(viewHeight - 1, top + wallHeight);
                for (var y = 0; y < viewHeight; y++)
                {
                    lines[y][x] = y < top
                        ? (ceiling.Length == 0 ? ' ' : ceiling[0])
                        : y <= bottom ? WallGlyph(wallBands, correctedDistance, x, y) : FloorGlyph(floorBands, x, y);
                }
            }

            RenderSprites(context, viewParameters, lines, player, viewHeight, width, fieldOfView, wall);
            RenderRadar(context, viewParameters, lines, player, viewHeight, width, wall);
            RenderCrosshair(context, viewParameters, lines, viewHeight, width);

            var separator = parameters.ReadString(viewParameters, "separator", "═");
            var separatorRow = new string(separator.Length == 0 ? ' ' : separator[0], width);
            var values = tokens.Build(session);
            textUtility.Put(lines[viewHeight], 0, separatorRow);
            var hasLevels = ruleset.Levels.Count > 0;
            for (var index = 0; index < hudRows.Count; index++)
            {
                var row = hudRows[index];
                var key = !hasLevels && parameters.TryRead(row, "noLevelsText", out _)
                    ? parameters.ReadString(row, "noLevelsText", string.Empty)
                    : parameters.ReadString(row, "text", string.Empty);
                if (viewHeight + 1 + index < height)
                    textUtility.Put(lines[viewHeight + 1 + index], 0, textUtility.Fit(texts.Format(ruleset, key, values), width));
            }
            if (viewHeight + hudRows.Count + 1 < height)
                textUtility.Put(lines[viewHeight + hudRows.Count + 1], 0, separatorRow);
            return new GameViewFrame { Text = string.Join(Environment.NewLine, lines.Select(line => new string(line))) };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the first-person view of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }

    /// <summary>
    /// Picks a wall glyph from distance bands: the first band whose <c>below</c> exceeds the distance (or that has no
    /// <c>below</c>) wins; with <c>modulo</c>, cells where (x + y) % modulo is zero use the first glyph, others the second.
    /// </summary>
    /// <param name="bands">The wall bands.</param>
    /// <param name="distance">The corrected wall distance.</param>
    /// <param name="x">The screen column.</param>
    /// <param name="y">The screen row.</param>
    /// <returns>The wall glyph.</returns>
    private char WallGlyph(IReadOnlyList<JsonElement> bands, double distance, int x, int y)
    {
        try
        {
            foreach (var band in bands)
            {
                if (parameters.TryRead(band, "below", out _) && distance >= parameters.ReadDouble(band, "below", 0d))
                    continue;
                var glyphs = parameters.ReadString(band, "glyphs", "#");
                var modulo = parameters.ReadInt(band, "modulo", 0);
                if (glyphs.Length == 0)
                    return ' ';
                return modulo > 0 && glyphs.Length > 1 ? ((x + y) % modulo == 0 ? glyphs[0] : glyphs[1]) : glyphs[0];
            }
            return '#';
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Choosing a wall glyph failed.");
            throw;
        }
    }

    /// <summary>
    /// Picks a floor glyph: the first band whose <c>modulo</c> divides x + y (or that has no modulo) wins.
    /// </summary>
    /// <param name="bands">The floor bands.</param>
    /// <param name="x">The screen column.</param>
    /// <param name="y">The screen row.</param>
    /// <returns>The floor glyph.</returns>
    private char FloorGlyph(IReadOnlyList<JsonElement> bands, int x, int y)
    {
        try
        {
            foreach (var band in bands)
            {
                var modulo = parameters.ReadInt(band, "modulo", 0);
                if (modulo > 0 && (x + y) % modulo != 0)
                    continue;
                var glyph = parameters.ReadString(band, "glyph", " ");
                return glyph.Length == 0 ? ' ' : glyph[0];
            }
            return ' ';
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Choosing a floor glyph failed.");
            throw;
        }
    }

    /// <summary>
    /// Draws billboard sprites of visible living actors of the sprite roles, farthest first.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="viewParameters">The view parameters.</param>
    /// <param name="lines">The frame cells.</param>
    /// <param name="player">The player.</param>
    /// <param name="viewHeight">The 3D view height.</param>
    /// <param name="width">The frame width.</param>
    /// <param name="fieldOfView">The field of view.</param>
    /// <param name="wall">The wall glyph.</param>
    private void RenderSprites(GameEngineContext context, JsonElement viewParameters, char[][] lines, GameActorState player, int viewHeight, int width, double fieldOfView, char wall)
    {
        try
        {
            var session = context.Session;
            var roles = parameters.ReadStrings(viewParameters, "spriteRoles");
            var sprite = parameters.ReadObject(viewParameters, "sprite");
            var scale = parameters.ReadDouble(sprite, "scale", .8d);
            var minimumWidth = parameters.ReadInt(sprite, "minimumWidth", 3);
            var maximumWidth = parameters.ReadInt(sprite, "maximumWidth", 9);
            var topEdge = parameters.ReadString(sprite, "topEdge", "^");
            var edge = parameters.ReadString(sprite, "edge", "#");
            var fallbackGlyph = parameters.ReadString(sprite, "fallbackGlyph", "M");
            var visible = session.Actors
                .Where(actor => !actor.IsEliminated && roles.Contains(actor.Role, StringComparer.OrdinalIgnoreCase) &&
                    grid.HasLineOfSight(session.WorldMap, player.X, player.Y, actor.X, actor.Y, wall))
                .Select(actor => new
                {
                    Actor = actor,
                    Dx = actor.X + .5d - (player.X + .5d),
                    Dy = actor.Y + .5d - (player.Y + .5d)
                })
                .Select(item => new
                {
                    item.Actor,
                    Distance = Math.Sqrt(item.Dx * item.Dx + item.Dy * item.Dy),
                    Delta = angles.SignedDelta(player.FacingRadians, Math.Atan2(item.Dy, item.Dx))
                })
                .Where(item => item.Distance > .1d && Math.Abs(item.Delta) <= fieldOfView / 2d)
                .OrderByDescending(item => item.Distance)
                .ToList();

            foreach (var item in visible)
            {
                var normalized = item.Delta / (fieldOfView / 2d);
                var centerX = (int)Math.Round((width - 1) * (.5d + normalized * .5d));
                var spriteHeight = Math.Clamp((int)Math.Round(viewHeight / item.Distance * scale), 3, Math.Max(3, viewHeight - 2));
                var spriteWidth = Math.Clamp(spriteHeight, minimumWidth, maximumWidth);
                var top = Math.Clamp(viewHeight / 2 - spriteHeight / 2, 0, Math.Max(0, viewHeight - spriteHeight));
                var left = Math.Clamp(centerX - spriteWidth / 2, 0, Math.Max(0, width - spriteWidth));
                var glyph = string.IsNullOrWhiteSpace(item.Actor.Glyph) ? fallbackGlyph[0] : item.Actor.Glyph[0];
                for (var row = 0; row < spriteHeight; row++)
                {
                    for (var column = 0; column < spriteWidth; column++)
                    {
                        var isEdge = row == 0 || row == spriteHeight - 1 || column == 0 || column == spriteWidth - 1;
                        lines[top + row][left + column] = isEdge ? (row == 0 ? topEdge[0] : edge[0]) : glyph;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering actor sprites failed.");
            throw;
        }
    }

    /// <summary>
    /// Draws the radar overlay centred on the player.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="viewParameters">The view parameters.</param>
    /// <param name="lines">The frame cells.</param>
    /// <param name="player">The player.</param>
    /// <param name="viewHeight">The 3D view height.</param>
    /// <param name="width">The frame width.</param>
    /// <param name="wall">The wall glyph.</param>
    private void RenderRadar(GameEngineContext context, JsonElement viewParameters, char[][] lines, GameActorState player, int viewHeight, int width, char wall)
    {
        try
        {
            var radar = parameters.ReadObject(viewParameters, "radar");
            var radarWidth = parameters.ReadInt(radar, "width", 15);
            var radarHeight = parameters.ReadInt(radar, "height", 9);
            if (radarWidth < 3 || radarHeight < 3 || width < radarWidth + 2 || viewHeight < radarHeight + 1)
                return;
            var session = context.Session;
            var roles = parameters.ReadStrings(viewParameters, "spriteRoles");
            var corner = parameters.ReadString(radar, "corner", "+");
            var horizontal = parameters.ReadString(radar, "horizontal", "-");
            var vertical = parameters.ReadString(radar, "vertical", "|");
            var wallGlyph = parameters.ReadString(radar, "wallGlyph", "#");
            var floorGlyph = parameters.ReadString(radar, "floorGlyph", ".");
            var playerGlyph = parameters.ReadString(radar, "playerGlyph", "@");
            var fallbackGlyph = parameters.ReadString(radar, "fallbackGlyph", "M");
            var left = width - radarWidth - 1;
            var top = parameters.ReadInt(radar, "top", 1);
            var border = corner[0] + new string(horizontal[0], radarWidth - 2) + corner[0];
            textUtility.Put(lines[top], left, border);
            for (var row = 0; row < radarHeight - 2; row++)
            {
                var mapY = player.Y + row - (radarHeight - 3) / 2;
                var buffer = new char[radarWidth];
                Array.Fill(buffer, ' ');
                buffer[0] = vertical[0];
                buffer[^1] = vertical[0];
                for (var column = 1; column < radarWidth - 1; column++)
                {
                    var mapX = player.X + column - (radarWidth - 2) / 2;
                    var glyph = !grid.IsWalkable(session.WorldMap, mapX, mapY, wall) ? wallGlyph[0] : floorGlyph[0];
                    var marker = session.Markers.FirstOrDefault(item => item.X == mapX && item.Y == mapY);
                    if (marker is not null && !string.IsNullOrEmpty(marker.Glyph))
                        glyph = marker.Glyph[0];
                    var actor = session.Actors.FirstOrDefault(candidate => !candidate.IsEliminated && candidate.X == mapX && candidate.Y == mapY &&
                        roles.Contains(candidate.Role, StringComparer.OrdinalIgnoreCase));
                    if (actor is not null)
                        glyph = string.IsNullOrWhiteSpace(actor.Glyph) ? fallbackGlyph[0] : actor.Glyph[0];
                    if (mapX == player.X && mapY == player.Y)
                        glyph = playerGlyph[0];
                    buffer[column] = glyph;
                }
                textUtility.Put(lines[top + row + 1], left, new string(buffer));
            }
            textUtility.Put(lines[top + radarHeight - 1], left, border);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the radar overlay failed.");
            throw;
        }
    }

    /// <summary>
    /// Draws the crosshair; the centre glyph flashes while the declared flash variable is positive.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="viewParameters">The view parameters.</param>
    /// <param name="lines">The frame cells.</param>
    /// <param name="viewHeight">The 3D view height.</param>
    /// <param name="width">The frame width.</param>
    private void RenderCrosshair(GameEngineContext context, JsonElement viewParameters, char[][] lines, int viewHeight, int width)
    {
        try
        {
            var crosshair = parameters.ReadObject(viewParameters, "crosshair");
            var flashVariable = parameters.ReadString(crosshair, "flashVariable", string.Empty);
            var flashing = flashVariable.Length > 0 && context.Session.Variables.TryGetValue(flashVariable, out var flash) &&
                int.TryParse(flash, NumberStyles.Integer, CultureInfo.InvariantCulture, out var turns) && turns > 0;
            var centerY = viewHeight / 2;
            var centerX = width / 2;
            lines[centerY][centerX] = parameters.ReadString(crosshair, flashing ? "flashGlyph" : "glyph", flashing ? "*" : "+")[0];
            var horizontal = parameters.ReadString(crosshair, "horizontal", "-")[0];
            var vertical = parameters.ReadString(crosshair, "vertical", "|")[0];
            if (centerX > 0)
                lines[centerY][centerX - 1] = horizontal;
            if (centerX + 1 < width)
                lines[centerY][centerX + 1] = horizontal;
            if (centerY > 0)
                lines[centerY - 1][centerX] = vertical;
            if (centerY + 1 < viewHeight)
                lines[centerY + 1][centerX] = vertical;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the crosshair failed.");
            throw;
        }
    }
}
