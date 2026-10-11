using System.Text;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Scene-data-driven two-dimensional world simulation and console projection, independent of FPS and game identity.</summary>
public sealed class GameSceneEngine(ILogger<GameSceneEngine> logger)
{
    /// <summary>Checks bounded world geometry, AI art occupancy, clocks, parallax and palette data before a scene is accepted.</summary>
    public IReadOnlyList<GameSceneFinding> Curate(GameSceneDocument scene)
    {
        try
        {
            var findings = new List<GameSceneFinding>();
            if (scene.WorldWidth is < 1 or > 1024 || scene.WorldHeight is < 1 or > 1024 || scene.ViewWidth is < 8 or > 240 || scene.ViewHeight is < 6 or > 100)
                findings.Add(new("WorldBounds", "The world or viewport dimensions exceed the supported bounds.", true));
            if (scene.ChunkSize is < 4 or > 128 || scene.FixedTicksPerSecond is < 10 or > 240)
                findings.Add(new("SimulationBounds", "Chunk size must be 4–128 and fixed tick rate must be 10–240 Hz.", true));
            if (!double.IsFinite(scene.WorldSecondsPerRealSecond) || scene.WorldSecondsPerRealSecond is <= 0 or > 100 || !double.IsFinite(scene.SecondsPerTurn) || scene.SecondsPerTurn is <= 0 or > 3600)
                findings.Add(new("WorldTime", "World-time scaling and seconds per turn must be finite positive values.", true));
            if (scene.Tiles.Count > 65536 || scene.Actors.Count > 1024 || scene.Layers.Count > 64 || scene.PaletteTimeline.Count > 256 || scene.Timeline.Count > 2048)
                findings.Add(new("SceneCapacity", "The scene exceeds the maintained world/actor/layer/timeline limits.", true));
            if (scene.Animations.Count > 2048 || scene.Hud.Count > 256)
                findings.Add(new("PresentationCapacity", "Animation and HUD data must remain within the scene's authored bounds.", true));
            if (scene.Sprites.Count > 1024)
                findings.Add(new("SpriteCapacity", "The scene may have no more than 1,024 independently curated pixel sprites.", true));
            var colors = scene.Tiles.Select(tile => tile.Color)
                .Concat(scene.Actors.Select(actor => actor.Color))
                .Concat(scene.Layers.Select(layer => layer.Color))
                .Concat(scene.Hud.Select(hud => hud.Color))
                .Concat(scene.PaletteTimeline.SelectMany(key => new[] { key.Foreground, key.Background }))
                .Concat(scene.Sprites.SelectMany(sprite => sprite.Palette.Values));
            if (colors.Any(color => string.IsNullOrEmpty(color) || color[0] != '#'
                || color.Length is not (4 or 7 or 9) || color.Skip(1).Any(character => !Uri.IsHexDigit(character))))
                findings.Add(new("PaletteColor", "Authored colors must be valid hexadecimal RGB/RGBA values to render safely in Blazor and Godot.", true));
            var sprites = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sprite in scene.Sprites)
            {
                if (string.IsNullOrWhiteSpace(sprite.Id) || !sprites.Add(sprite.Id))
                    findings.Add(new("SpriteIdentity", "Each pixel-art sprite requires a distinct nonempty ID.", true));
                if (sprite.PixelRows.Count is < 1 or > 64 || sprite.PixelRows.Any(row => row.Length > 64))
                    findings.Add(new("SpriteDimensions", $"Sprite {sprite.Id} must have 1–64 rows and at most 64 columns.", true));
                foreach (var row in sprite.PixelRows)
                    foreach (var token in row)
                        if (token != ' ' && !sprite.Palette.ContainsKey(token.ToString()))
                            findings.Add(new("PixelPalette", $"Sprite {sprite.Id} uses an undefined pixel palette token.", true));
            }

            var occupancy = new HashSet<(int X, int Y)>();
            var blockedDecoration = new HashSet<(int X, int Y)>();
            foreach (var tile in scene.Tiles)
            {
                if (tile.X < 0 || tile.Y < 0 || tile.X >= scene.WorldWidth || tile.Y >= scene.WorldHeight)
                    findings.Add(new("TileBounds", $"Tile ({tile.X}, {tile.Y}) is outside the world.", true));
                if (!occupancy.Add((tile.X, tile.Y)))
                    findings.Add(new("TileOverlap", $"Multiple authored tiles occupy ({tile.X}, {tile.Y}).", true));
                if (tile.Solid || tile.BlocksDecoration)
                    blockedDecoration.Add((tile.X, tile.Y));
                if (string.IsNullOrEmpty(tile.Glyph) || tile.Glyph.Length > 4)
                    findings.Add(new("TileArt", "Every tile requires a short glyph or sprite marker.", true));
                if (!string.IsNullOrEmpty(tile.SpriteId) && !sprites.Contains(tile.SpriteId))
                    findings.Add(new("TileSprite", "A tile references a missing authored sprite.", true));
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (scene.Actors.Count(actor => actor.PlayerControlled) > 1)
                findings.Add(new("PlayerControl", "A curated scene may designate only one directly controlled actor.", true));
            foreach (var actor in scene.Actors)
            {
                if (string.IsNullOrWhiteSpace(actor.Id) || !ids.Add(actor.Id))
                    findings.Add(new("ActorIdentity", "Each actor requires a unique persistent identity.", true));
                if (!double.IsFinite(actor.X) || !double.IsFinite(actor.Y) || !double.IsFinite(actor.VelocityX) || !double.IsFinite(actor.VelocityY)
                    || actor.X < 0 || actor.Y < 0 || actor.X >= scene.WorldWidth || actor.Y >= scene.WorldHeight)
                    findings.Add(new("ActorBounds", $"Actor {actor.Id} has invalid spatial or motion values.", true));
                if (blockedDecoration.Contains(((int)Math.Floor(actor.X), (int)Math.Floor(actor.Y))))
                    findings.Add(new("SemanticOcclusion", $"Actor/decorative content {actor.Id} occupies a solid or decoration-excluded cell.", true));
                if (!string.IsNullOrEmpty(actor.SpriteId) && !sprites.Contains(actor.SpriteId))
                    findings.Add(new("ActorSprite", "An actor references a missing authored sprite.", true));
            }
            foreach (var animation in scene.Animations)
            {
                if (animation.Frames.Count is < 1 or > 64 || !double.IsFinite(animation.StartWorldSecond) || animation.StartWorldSecond < 0)
                    findings.Add(new("AnimationTiming", "A scene animation requires 1–64 frames and a finite start time.", true));
                foreach (var frame in animation.Frames)
                    if (!sprites.Contains(frame.SpriteId) || !double.IsFinite(frame.DurationWorldSeconds) || frame.DurationWorldSeconds is <= 0 or > 3600)
                        findings.Add(new("AnimationFrame", "Animation frames must name an authored sprite and positive world-time duration.", true));
            }
            foreach (var hud in scene.Hud)
                if (hud.X < 0 || hud.Y < 0 || hud.X >= scene.ViewWidth || hud.Y >= scene.ViewHeight || hud.Format.Length > 240)
                    findings.Add(new("HudBounds", "HUD elements must fit the viewport and remain under 240 characters.", true));
            foreach (var layer in scene.Layers)
            {
                if (layer.FirstScreenRow < 0 || layer.LastScreenRow < layer.FirstScreenRow || layer.LastScreenRow >= scene.ViewHeight
                    || !double.IsFinite(layer.ScrollCellsPerWorldSecondX) || !double.IsFinite(layer.ScrollCellsPerWorldSecondY)
                    || !double.IsFinite(layer.CameraFactorX) || !double.IsFinite(layer.CameraFactorY))
                    findings.Add(new("Parallax", $"Layer {layer.Name} has an invalid scanline band or speed.", true));
                if (layer.GlyphPattern.Length > 1024)
                    findings.Add(new("LayerArt", "Parallax patterns must be no longer than 1,024 cells.", true));
            }
            if (scene.PaletteTimeline.Any(key => !double.IsFinite(key.AtWorldSecond) || key.AtWorldSecond < 0))
                findings.Add(new("PaletteTime", "Palette transitions require nonnegative finite world seconds.", true));
            if (scene.Timeline.Any(item => !double.IsFinite(item.AtWorldSecond) || item.AtWorldSecond < 0))
                findings.Add(new("TimelineTime", "Cinematic/timeline transitions require nonnegative finite world seconds.", true));
            return findings;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Scene curation failed; authored artwork was not accepted.");
            throw;
        }
    }

    /// <summary>Advances exact fixed ticks, retaining any unprocessed backlog; neither rendering FPS nor UI latency controls world speed.</summary>
    public GameSceneClock Advance(GameSceneDocument scene, GameSceneClock state, double elapsedRealSeconds, bool takeTurn = false)
    {
        try
        {
            if (Curate(scene).Any(finding => finding.BlocksAcceptance))
                throw new InvalidOperationException("The scene must pass semantic curation before simulation.");
            if (!double.IsFinite(elapsedRealSeconds) || elapsedRealSeconds < 0 || elapsedRealSeconds > 3600)
                throw new ArgumentOutOfRangeException(nameof(elapsedRealSeconds));
            var deltaReal = 1d / scene.FixedTicksPerSecond;
            var pending = state.FractionalRealSeconds + elapsedRealSeconds + (takeTurn ? scene.SecondsPerTurn / scene.WorldSecondsPerRealSecond : 0);
            var ticks = Math.Min(10000, (long)Math.Floor((pending + 1e-10) / deltaReal));
            var blocked = scene.Tiles.Where(tile => tile.Solid).Select(tile => (tile.X, tile.Y)).ToHashSet();
            for (var i = 0L; i < ticks; i++)
            {
                foreach (var actor in scene.Actors)
                {
                    var current = state.ActorPositions.TryGetValue(actor.Id, out var point) ? point : new GameScenePosition(actor.X, actor.Y);
                    var seconds = deltaReal * scene.WorldSecondsPerRealSecond;
                    var nextX = current.X + actor.VelocityX * seconds;
                    var nextY = current.Y + actor.VelocityY * seconds;
                    if (actor.Collides && (nextX < 0 || nextX >= scene.WorldWidth || blocked.Contains(((int)Math.Floor(nextX), (int)Math.Floor(current.Y)))))
                        nextX = current.X;
                    if (actor.Collides && (nextY < 0 || nextY >= scene.WorldHeight || blocked.Contains(((int)Math.Floor(nextX), (int)Math.Floor(nextY)))))
                        nextY = current.Y;
                    state.ActorPositions[actor.Id] = new GameScenePosition(nextX, nextY);
                }
                state.Tick++;
            }
            state.FractionalRealSeconds = Math.Max(0, pending - ticks * deltaReal);
            state.WorldSeconds = state.Tick * deltaReal * scene.WorldSecondsPerRealSecond;
            state.CurrentTurn = (int)Math.Min(int.MaxValue, Math.Floor(state.WorldSeconds / scene.SecondsPerTurn));
            return state;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Fixed-step world simulation failed at tick {Tick}.", state.Tick);
            throw;
        }
    }

    /// <summary>Projects open-world chunks, row parallax and actors into a bounded ASCII preview without selecting a specific game's renderer.</summary>
    public GameScenePreview Preview(GameSceneDocument scene, GameSceneClock state)
    {
        try
        {
            if (Curate(scene).Any(finding => finding.BlocksAcceptance))
                throw new InvalidOperationException("The scene must pass curation before preview.");
            var width = scene.ViewWidth;
            var height = scene.ViewHeight;
            var cells = new char[height, width];
            for (var row = 0; row < height; row++)
                for (var column = 0; column < width; column++)
                    cells[row, column] = ' ';
            foreach (var layer in scene.Layers.OrderBy(item => item.Depth))
            {
                if (layer.GlyphPattern.Length == 0)
                    continue;
                var scrollX = (int)Math.Floor(state.WorldSeconds * layer.ScrollCellsPerWorldSecondX + state.CameraX * layer.CameraFactorX);
                var scrollY = (int)Math.Floor(state.WorldSeconds * layer.ScrollCellsPerWorldSecondY + state.CameraY * layer.CameraFactorY);
                for (var row = layer.FirstScreenRow; row <= layer.LastScreenRow; row++)
                    for (var column = 0; column < width; column++)
                    {
                        var location = column + scrollX + (row + scrollY) * width;
                        if (!layer.Wrap && (location < 0 || location >= layer.GlyphPattern.Length))
                            continue;
                        var index = ((location % layer.GlyphPattern.Length) + layer.GlyphPattern.Length) % layer.GlyphPattern.Length;
                        if (layer.GlyphPattern[index] != ' ')
                            cells[row, column] = layer.GlyphPattern[index];
                    }
            }
            foreach (var tile in scene.Tiles)
            {
                var x = tile.X - (int)Math.Floor(state.CameraX);
                var y = tile.Y - (int)Math.Floor(state.CameraY);
                if (x >= 0 && y >= 0 && x < width && y < height && tile.Glyph.Length > 0)
                    cells[y, x] = tile.Glyph[0];
            }
            foreach (var actor in scene.Actors.OrderBy(item => item.Layer))
            {
                var current = state.ActorPositions.TryGetValue(actor.Id, out var position) ? position : new GameScenePosition(actor.X, actor.Y);
                var x = (int)Math.Floor(current.X - state.CameraX);
                var y = (int)Math.Floor(current.Y - state.CameraY);
                if (x >= 0 && y >= 0 && x < width && y < height && actor.Glyph.Length > 0)
                    cells[y, x] = actor.Glyph[0];
            }
            foreach (var hud in scene.Hud)
            {
                var formatted = hud.Format.Replace("{tick}", state.Tick.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{worldSeconds}", state.WorldSeconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{turn}", state.CurrentTurn.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
                for (var index = 0; index < formatted.Length && hud.X + index < width; index++)
                    cells[hud.Y, hud.X + index] = formatted[index];
            }
            var output = new StringBuilder((width + 1) * height);
            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                    output.Append(cells[row, column]);
                if (row + 1 < height)
                    output.AppendLine();
            }
            var palette = scene.PaletteTimeline.Where(key => key.AtWorldSecond <= state.WorldSeconds)
                .OrderByDescending(key => key.AtWorldSecond).FirstOrDefault();
            return new GameScenePreview(output.ToString(), palette?.Foreground ?? "#eeeeee", palette?.Background ?? "#101820", state.WorldSeconds, state.Tick);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Game scene preview failed at tick {Tick}.", state.Tick);
            throw;
        }
    }
}
