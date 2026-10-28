using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Scene-data-driven two-dimensional world simulation and console projection, independent of FPS and game identity.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GameSceneEngine(ILogger<GameSceneEngine> logger)
{
    /// <summary>Checks bounded world geometry, AI art occupancy, clocks, parallax and palette data before a scene is accepted.</summary>
    /// <param name="scene">Scene value supplied to the game scene engine operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
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
            if (!Enum.IsDefined(typeof(GameSceneArtworkMode), scene.ArtworkMode))
                findings.Add(new("ArtworkMode", "Select an existing presentation mode.", true));
            if (scene.TextCanvases.Count > 64 || scene.RegexTransitions.Count > 64)
                findings.Add(new("SceneDeclarations", "A scene supports up to 64 text canvases and 64 regex transitions.", true));
            var authoredCharacterCells = scene.TextCanvases.Sum(canvas => canvas.Rows.Sum(row => row?.Length ?? 0))
                + scene.Sprites.Sum(sprite => sprite.AsciiRows.Sum(row => row?.Length ?? 0));
            if (authoredCharacterCells > 96000)
                findings.Add(new("CharacterArtBudget", "A scene supports up to 96,000 authored character-art cells total.", true));
            var canvasIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var canvas in scene.TextCanvases)
            {
                if (string.IsNullOrWhiteSpace(canvas.Id) || canvas.Id.Length > 128 || !canvasIds.Add(canvas.Id))
                    findings.Add(new("CanvasIdentity", "Each canvas requires a unique ID of at most 128 characters.", true));
                if (canvas.X < 0 || canvas.Y < 0 || canvas.X >= scene.ViewWidth || canvas.Y >= scene.ViewHeight
                    || canvas.Rows.Count is < 1 or > 100 || canvas.Y + canvas.Rows.Count > scene.ViewHeight
                    || canvas.Rows.Any(row => row is null || row.Length > 240 || canvas.X + row.Length > scene.ViewWidth || row.Any(char.IsControl)))
                    findings.Add(new("CanvasBounds", $"Canvas {canvas.Id} exceeds the viewport or contains control characters.", true));
                if (!double.IsFinite(canvas.FromWorldSecond) || !double.IsFinite(canvas.UntilWorldSecond)
                    || canvas.FromWorldSecond < 0 || (canvas.UntilWorldSecond != 0 && canvas.UntilWorldSecond <= canvas.FromWorldSecond))
                    findings.Add(new("CanvasTiming", $"Canvas {canvas.Id} has invalid presentation timing.", true));
                if (canvas.VisibleWhenVariable.Length > 80 || canvas.VisibleWhenValue.Length > 160)
                    findings.Add(new("CanvasPredicate", "Canvas predicates must be bounded presentation values.", true));
            }
            foreach (var binding in scene.RegexTransitions)
            {
                if (string.IsNullOrWhiteSpace(binding.RegexPatternName) || binding.RegexPatternName.Length > 128
                    || string.IsNullOrWhiteSpace(binding.Variable) || binding.Variable.Length > 80
                    || binding.CaptureGroup.Length > 80 || binding.ValueOnMatch.Length > 160)
                    findings.Add(new("RegexTransition", "A regex transition requires a bounded existing pattern name and scene-state variable.", true));
            }
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
                if ((sprite.PixelRows.Count == 0 && sprite.AsciiRows.Count == 0)
                    || sprite.PixelRows.Count > 64 || sprite.PixelRows.Any(row => row is null || row.Length > 64)
                    || sprite.AsciiRows.Count > 100 || sprite.AsciiRows.Any(row => row is null || row.Length > 240 || row.Any(char.IsControl)))
                    findings.Add(new("SpriteDimensions", $"Sprite {sprite.Id} requires bounded pixel or authored character-art rows without control characters.", true));
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

    /// <summary>Matches a reviewed input against catalog-compiled regexes and changes only declarative scene presentation variables.</summary>
    /// <param name="scene">Approved project-owned scene declarations.</param>
    /// <param name="state">Per-session state that owns mutable presentation values.</param>
    /// <param name="input">Approved input or text event; never interpreted as arbitrary executable code.</param>
    /// <param name="patterns">Existing catalog-compiled regular expressions keyed by their persisted names.</param>
    /// <returns>Number of matched scene-state assignments.</returns>
    public int ApplyRegexTransitions(GameSceneDocument scene, GameSceneClock state, string input, IReadOnlyDictionary<string, Regex> patterns)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(patterns);
        if (Curate(scene).Any(finding => finding.BlocksAcceptance))
            throw new InvalidOperationException("The scene must pass curation before accepting regex transitions.");
        if (input is null || input.Length > 4096)
            throw new ArgumentException("Scene input must be 4,096 characters or fewer.", nameof(input));
        var assignments = 0;
        var pending = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var binding in scene.RegexTransitions)
        {
            if (!patterns.TryGetValue(binding.RegexPatternName, out var regex))
                throw new InvalidOperationException($"The scene references an unavailable catalog regex '{binding.RegexPatternName}'.");
            var match = regex.Match(input);
            if (!match.Success)
                continue;
            var value = binding.ValueOnMatch;
            if (!string.IsNullOrWhiteSpace(binding.CaptureGroup))
            {
                var capture = match.Groups[binding.CaptureGroup];
                if (!capture.Success)
                    continue;
                value = capture.Value;
            }
            pending[binding.Variable] = value.Length > 160 ? value[..160] : value;
            assignments++;
        }
        foreach (var update in pending)
            state.SceneVariables[update.Key] = update.Value;
        return assignments;
    }

    /// <summary>Advances exact fixed ticks, retaining any unprocessed backlog; neither rendering FPS nor UI latency controls world speed.</summary>
    /// <param name="scene">Scene value supplied to the game scene engine operation and used when producing its result.</param>
    /// <param name="state">State value supplied to the game scene engine operation and used when producing its result.</param>
    /// <param name="elapsedRealSeconds">Elapsed real seconds value supplied to the game scene engine operation and used when producing its result.</param>
    /// <param name="takeTurn">Value indicating whether take turn should apply to this operation.</param>
    /// <returns>The game scene clock produced by the operation.</returns>
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
    /// <param name="scene">Scene value supplied to the game scene engine operation and used when producing its result.</param>
    /// <param name="state">State value supplied to the game scene engine operation and used when producing its result.</param>
    /// <returns>The game scene preview produced by the operation.</returns>
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
            string Expand(string template)
            {
                var result = (template ?? string.Empty)
                    .Replace("{tick}", state.Tick.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{worldSeconds}", state.WorldSeconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{turn}", state.CurrentTurn.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
                foreach (var entry in state.SceneVariables.OrderBy(item => item.Key, StringComparer.Ordinal))
                {
                    var safe = string.Join(' ', entry.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                    result = result.Replace("{" + entry.Key + "}", safe, StringComparison.Ordinal);
                }
                return result.Length > 4096 ? result[..4096] : result;
            }
            void PaintRows(IReadOnlyList<string> art, int startX, int startY)
            {
                for (var row = 0; row < art.Count; row++)
                {
                    var y = startY + row;
                    if (y < 0 || y >= height) continue;
                    var expanded = Expand(art[row]);
                    for (var column = 0; column < expanded.Length; column++)
                    {
                        var x = startX + column;
                        if (x >= 0 && x < width && expanded[column] != ' ')
                            cells[y, x] = expanded[column];
                    }
                }
            }
            if (scene.ArtworkMode != GameSceneArtworkMode.ClassicGlyph)
                foreach (var canvas in scene.TextCanvases)
                    if (state.WorldSeconds >= canvas.FromWorldSecond
                        && (canvas.UntilWorldSecond == 0 || state.WorldSeconds < canvas.UntilWorldSecond)
                        && (string.IsNullOrEmpty(canvas.VisibleWhenVariable)
                            || state.SceneVariables.TryGetValue(canvas.VisibleWhenVariable, out var visibleValue)
                                && string.Equals(visibleValue, canvas.VisibleWhenValue, StringComparison.Ordinal)))
                        PaintRows(canvas.Rows, canvas.X, canvas.Y);
            var spriteLookup = scene.Sprites.ToDictionary(item => item.Id, StringComparer.Ordinal);
            string AnimatedSpriteId(string targetId, string initial)
            {
                var animation = scene.Animations.FirstOrDefault(item => item.TargetId == targetId);
                if (animation is null || state.WorldSeconds < animation.StartWorldSecond || animation.Frames.Count == 0)
                    return initial;
                var duration = animation.Frames.Sum(frame => frame.DurationWorldSeconds);
                if (duration <= 0) return initial;
                var phase = state.WorldSeconds - animation.StartWorldSecond;
                phase = animation.Loop ? phase % duration : Math.Min(phase, duration - 0.000001);
                foreach (var frame in animation.Frames)
                {
                    phase -= frame.DurationWorldSeconds;
                    if (phase < 0) return frame.SpriteId;
                }
                return initial;
            }
            void PaintSprite(string id, int x, int y, string fallback)
            {
                if (scene.ArtworkMode == GameSceneArtworkMode.ClassicGlyph)
                {
                    if (x >= 0 && y >= 0 && x < width && y < height && fallback.Length > 0)
                        cells[y, x] = fallback[0];
                    return;
                }
                if (!spriteLookup.TryGetValue(id, out var sprite))
                {
                    if (x >= 0 && y >= 0 && x < width && y < height && fallback.Length > 0)
                        cells[y, x] = fallback[0];
                    return;
                }
                if (scene.ArtworkMode == GameSceneArtworkMode.AuthoredPixel && sprite.PixelRows.Count > 0)
                    PaintRows(sprite.PixelRows, x, y);
                else if (sprite.AsciiRows.Count > 0)
                    PaintRows(sprite.AsciiRows, x, y);
                else if (x >= 0 && y >= 0 && x < width && y < height && fallback.Length > 0)
                    cells[y, x] = fallback[0];
            }
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
                PaintSprite(AnimatedSpriteId(tile.Id, tile.SpriteId), x, y, tile.Glyph);
            }
            foreach (var actor in scene.Actors.OrderBy(item => item.Layer))
            {
                var current = state.ActorPositions.TryGetValue(actor.Id, out var position) ? position : new GameScenePosition(actor.X, actor.Y);
                var x = (int)Math.Floor(current.X - state.CameraX);
                var y = (int)Math.Floor(current.Y - state.CameraY);
                PaintSprite(AnimatedSpriteId(actor.Id, actor.SpriteId), x, y, actor.Glyph);
            }
            foreach (var hud in scene.Hud)
            {
                var formatted = Expand(hud.Format);
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
