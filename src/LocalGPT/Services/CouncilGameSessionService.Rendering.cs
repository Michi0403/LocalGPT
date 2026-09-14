using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council game session behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilGameSessionService
    {
    /// <summary>
    /// Performs render as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Render(CouncilGameSessionState session) {
    try
    {
        return session.GameKey == "green-dragon"
        ? RenderGreenDragon(session)
        : RenderDoomLike(session);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Render)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Render)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs render doom like as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderDoomLike(CouncilGameSessionState session)
    {
    try
    {
            var width = session.FrameWidth;
            var height = session.FrameHeight;
            var viewHeight = height - 6;
            var lines = Enumerable.Range(0, height).Select(_ => new string(' ', width).ToCharArray()).ToArray();
            for (var x = 0; x < width; x++)
            {
                var rayAngle = session.FacingRadians - FieldOfView / 2d + FieldOfView * x / Math.Max(1, width - 1);
                var distance = CastRay(session, session.PlayerX + .5d, session.PlayerY + .5d, rayAngle);
                var correctedDistance = Math.Max(.15d, distance * Math.Cos(rayAngle - session.FacingRadians));
                var wallHeight = Math.Clamp((int)Math.Round(viewHeight / correctedDistance * .82d), 1, viewHeight);
                var top = Math.Max(0, (viewHeight - wallHeight) / 2 - (session.IsDucking ? -2 : 0));
                var bottom = Math.Min(viewHeight - 1, top + wallHeight);
                for (var y = 0; y < viewHeight; y++)
                    lines[y][x] = y < top ? '.' : y <= bottom ? WallGlyph(correctedDistance, x, y) : FloorGlyph(x, y);
            }

            RenderEnemySprites(lines, session, viewHeight, width);
            RenderRadarOverlay(lines, session, viewHeight, width);

            var centerY = viewHeight / 2;
            var centerX = width / 2;
            lines[centerY][centerX] = session.MuzzleFlash > 0 ? '*' : '+';
            if (centerX > 0) lines[centerY][centerX - 1] = '-';
            if (centerX + 1 < width) lines[centerY][centerX + 1] = '-';
            if (centerY > 0) lines[centerY - 1][centerX] = '|';
            if (centerY + 1 < viewHeight) lines[centerY + 1][centerX] = '|';

            var enemyCount = session.Enemies.Count(enemy => enemy.IsAlive);
            Put(lines, viewHeight, 0, new string('═', width));
            Put(lines, viewHeight + 1, 0, Fit($" ASCII CORRIDOR // TURN {session.Turn:000} // {Compass(session.FacingRadians),3} // {(session.IsDucking ? "DUCK" : "STAND")}", width));
            Put(lines, viewHeight + 2, 0, Fit($" HP {session.Health:000}  AMMO {session.Ammo:000}  ENEMIES {enemyCount:00}  POS {session.PlayerX:00},{session.PlayerY:00}  X {session.ExtractionX:00},{session.ExtractionY:00}", width));
            Put(lines, viewHeight + 3, 0, Fit($" {session.CombatMessage}", width));
            Put(lines, viewHeight + 4, 0, Fit(" W/S move  A/D strafe  Q/R turn  SPACE shoot  CTRL duck  E use  F fullscreen", width));
            Put(lines, viewHeight + 5, 0, new string('═', width));
            return string.Join(Environment.NewLine, lines.Select(line => new string(line)));

    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderDoomLike)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderDoomLike)} failed.");
        throw;
    }
}


    /// <summary>
    /// Performs render green dragon as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderGreenDragon(CouncilGameSessionState session)
    {
    try
    {
            var width = session.FrameWidth;
            var lines = new List<string>
            {
                "┌" + new string('─', width - 2) + "┐",
                Fit("│                         THE LANTERN VILLAGE                                 │", width),
                Fit("│                                                                            │", width),
                Fit("│        /\\                  [OLD GATE]                    /\\              │", width),
                Fit("│       /  \\        * lanterns *                         /  \\             │", width),
                Fit("│      / INN\\        .-.-.-.-.-.                       /SHOP\\            │", width),
                Fit("│      |____|---------'         '-----------------------|____|             │", width),
                Fit("│           \\                    @ player                 /                 │", width),
                Fit("│            \\____________________.______________________/                  │", width),
                Fit("│                                 |                                          │", width),
                Fit("│                              FOREST PATH                                    │", width),
                Fit("│                                 |                                          │", width),
                Fit("│                            {green woods}                                    │", width),
                Fit("│                                                                            │", width),
                Fit($"│  {session.StoryLine}", width),
                Fit("│                                                                            │", width),
                Fit("│  1 Accept the lantern   2 Ask the villagers   3 Rest by the hearth       │", width),
                Fit("│  W/S move  A/D choose  E/Enter confirm  1/2/3 direct choice              │", width),
                Fit("│                                                                            │", width),
                Fit($"│  TURN {session.Turn:000}  PLAYER {session.PlayerX:00},{session.PlayerY:00}  LAST {session.LastAction}", width),
                Fit("│                                                                            │", width),
                Fit("│  Runtime story example inspired by open-source text-RPG architecture;      │", width),
                Fit("│  original scene and content, not affiliated with LOTGD.                    │", width),
                "└" + new string('─', width - 2) + "┘"
            };
            while (lines.Count < session.FrameHeight) lines.Insert(lines.Count - 1, Fit("│", width));
            return string.Join(Environment.NewLine, lines.Take(session.FrameHeight));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderGreenDragon)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderGreenDragon)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs cast ray as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="x">X value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="angle">Angle value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double CastRay(CouncilGameSessionState session, double x, double y, double angle)
    {
    try
    {
            const double maxDistance = 20d;
            var map = GetWorldMap(session);
            for (var distance = .05d; distance < maxDistance; distance += .05d)
            {
                var sampleX = (int)(x + Math.Cos(angle) * distance);
                var sampleY = (int)(y + Math.Sin(angle) * distance);
                if (sampleY < 0 || sampleY >= map.Count || sampleX < 0 || sampleX >= map[sampleY].Length) return distance;
                if (map[sampleY][sampleX] == '#') return distance;
            }
            return maxDistance;

    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(CastRay)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(CastRay)} failed.");
        throw;
    }
}

    /// <summary>Projects visible hostile actors into the deterministic first-person frame.</summary>
    private void RenderEnemySprites(char[][] lines, CouncilGameSessionState session, int viewHeight, int width)
    {
    try
    {
            var map = GetWorldMap(session);
            var visible = session.Enemies
                .Where(enemy => enemy.IsAlive && HasLineOfSight(map, session.PlayerX, session.PlayerY, enemy.X, enemy.Y))
                .Select(enemy => new
                {
                    Enemy = enemy,
                    Dx = enemy.X + .5d - (session.PlayerX + .5d),
                    Dy = enemy.Y + .5d - (session.PlayerY + .5d)
                })
                .Select(item => new
                {
                    item.Enemy,
                    Distance = Math.Sqrt(item.Dx * item.Dx + item.Dy * item.Dy),
                    Delta = SignedAngleDelta(session.FacingRadians, Math.Atan2(item.Dy, item.Dx))
                })
                .Where(item => item.Distance > .1d && Math.Abs(item.Delta) <= FieldOfView / 2d)
                .OrderByDescending(item => item.Distance)
                .ToList();

            foreach (var item in visible)
            {
                var normalized = item.Delta / (FieldOfView / 2d);
                var centerX = (int)Math.Round((width - 1) * (.5d + normalized * .5d));
                var spriteHeight = Math.Clamp((int)Math.Round(viewHeight / item.Distance * .8d), 3, Math.Max(3, viewHeight - 2));
                var spriteWidth = Math.Clamp(spriteHeight, 3, 9);
                var top = Math.Clamp(viewHeight / 2 - spriteHeight / 2, 0, Math.Max(0, viewHeight - spriteHeight));
                var left = Math.Clamp(centerX - spriteWidth / 2, 0, Math.Max(0, width - spriteWidth));
                var glyph = string.IsNullOrWhiteSpace(item.Enemy.Glyph) ? 'M' : item.Enemy.Glyph[0];
                for (var row = 0; row < spriteHeight; row++)
                {
                    for (var column = 0; column < spriteWidth; column++)
                    {
                        var edge = row == 0 || row == spriteHeight - 1 || column == 0 || column == spriteWidth - 1;
                        var fill = edge ? (row == 0 ? '^' : '#') : glyph;
                        lines[top + row][left + column] = fill;
                    }
                }
            }
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderEnemySprites)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderEnemySprites)} failed.");
        throw;
    }
}

    /// <summary>Draws a compact authoritative tactical radar over the first-person scene.</summary>
    private void RenderRadarOverlay(char[][] lines, CouncilGameSessionState session, int viewHeight, int width)
    {
    try
    {
            const int radarWidth = 15;
            const int radarHeight = 9;
            if (width < radarWidth + 2 || viewHeight < radarHeight + 1) return;
            var left = width - radarWidth - 1;
            var top = 1;
            var map = GetWorldMap(session);
            Put(lines, top, left, "+-------------+");
            for (var row = 0; row < radarHeight - 2; row++)
            {
                var mapY = session.PlayerY + row - (radarHeight - 3) / 2;
                var buffer = new char[radarWidth];
                Array.Fill(buffer, ' ');
                buffer[0] = '|';
                buffer[^1] = '|';
                for (var column = 1; column < radarWidth - 1; column++)
                {
                    var mapX = session.PlayerX + column - (radarWidth - 2) / 2;
                    var glyph = !IsWalkable(map, mapX, mapY) ? '#' : '.';
                    if (mapX == session.ExtractionX && mapY == session.ExtractionY) glyph = 'X';
                    var enemy = session.Enemies.FirstOrDefault(candidate => candidate.IsAlive && candidate.X == mapX && candidate.Y == mapY);
                    if (enemy is not null) glyph = string.IsNullOrWhiteSpace(enemy.Glyph) ? 'M' : enemy.Glyph[0];
                    if (mapX == session.PlayerX && mapY == session.PlayerY) glyph = '@';
                    buffer[column] = glyph;
                }
                Put(lines, top + row + 1, left, new string(buffer));
            }
            Put(lines, top + radarHeight - 1, left, "+-------------+");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderRadarOverlay)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderRadarOverlay)} failed.");
        throw;
    }
}


    /// <summary>
    /// Performs wall glyph as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="distance">Distance value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="x">X value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The char produced by the operation.</returns>
    private char WallGlyph(double distance, int x, int y) {
    try
    {
        return distance switch
    {
        < 2.2d => (x + y) % 2 == 0 ? '█' : '▓',
        < 4.2d => (x + y) % 3 == 0 ? '▓' : '▒',
        < 7.5d => '▒',
        _ => '░'
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(WallGlyph)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(WallGlyph)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs floor glyph as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="x">X value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The char produced by the operation.</returns>
    private char FloorGlyph(int x, int y) {
    try
    {
        return (x + y) % 5 == 0 ? ':' : (x + y) % 2 == 0 ? '.' : ' ';
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(FloorGlyph)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(FloorGlyph)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs put as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="lines">Lines value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="row">Row value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="column">Column value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the council game session operation and used when producing its result.</param>
    private void Put(char[][] lines, int row, int column, string text)
    {
    try
    {
            if (row < 0 || row >= lines.Length) return;
            for (var index = 0; index < text.Length && column + index < lines[row].Length; index++)
                lines[row][column + index] = text[index];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Put)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Put)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs fit as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Fit(string text, int width)
    {
    try
    {
            var normalized = (text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
            if (normalized.Length > width) return normalized[..width];
            return normalized.PadRight(width);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Fit)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Fit)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes frame as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="frame">Frame value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeFrame(string frame, int width, int height)
    {
    try
    {
            var sourceLines = (frame ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n');
            var result = new List<string>(height);
            for (var row = 0; row < height; row++)
            {
                var line = row < sourceLines.Length ? sourceLines[row] : string.Empty;
                result.Add(Fit(line, width));
            }
            return string.Join(Environment.NewLine, result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeFrame)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeFrame)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds caption as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildCaption(CouncilGameSessionState session) {
    try
    {
        return $"{session.DisplayName} · turn {session.Turn} · {session.CurrentTurnOwner} · renderer {session.FrameRenderer}";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildCaption)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildCaption)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs compass as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="radians">Radians value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Compass(double radians)
    {
    try
    {
            var degrees = NormalizeRadians(radians) * 180d / Math.PI;
            return degrees switch
            {
                >= 337.5 or < 22.5 => "E",
                < 67.5 => "SE",
                < 112.5 => "S",
                < 157.5 => "SW",
                < 202.5 => "W",
                < 247.5 => "NW",
                < 292.5 => "N",
                _ => "NE"
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Compass)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Compass)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes radians as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double NormalizeRadians(double value)
    {
    try
    {
            while (value < 0) value += Math.PI * 2d;
            while (value >= Math.PI * 2d) value -= Math.PI * 2d;
            return value;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeRadians)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeRadians)} failed.");
        throw;
    }
}

    }
}
