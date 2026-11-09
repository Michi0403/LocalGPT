using System.Text.Json;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Maps;

/// <summary>
/// Map generator <c>rooms-and-corridors</c>: one seeded map per level row, rooms joined by L-shaped corridors,
/// the player in the first room, a goal marker in the last room and actors spawned by a declarative schedule.
/// </summary>
/// <param name="parameters">Reads map, level and spawn parameters.</param>
/// <param name="grid">Carves rooms and corridors.</param>
/// <param name="world">Reads the map glyphs.</param>
/// <param name="hashes">Derives level seeds.</param>
/// <param name="support">Shares seeds, grids, player placement and spawning.</param>
/// <param name="actors">Finds the player.</param>
/// <param name="messages">Writes the level start message.</param>
/// <param name="formatter">Formats spawned actor keys.</param>
/// <param name="logger">Receives map diagnostics.</param>
public sealed class RoomsAndCorridorsMapGenerator(
    IGameParameterReader parameters,
    IGameGridService grid,
    IGameWorldService world,
    IGameStableHashService hashes,
    IGameMapSupportService support,
    IGameActorQueryService actors,
    IGameMessageService messages,
    IGameTemplateFormatter formatter,
    ILogger<RoomsAndCorridorsMapGenerator> logger) : IGameMapGeneratorPrimitive
{
    /// <inheritdoc />
    public string Key => "rooms-and-corridors";

    /// <inheritdoc />
    public void Generate(GameEngineContext context)
    {
        try
        {
            var session = context.Session;
            var ruleset = context.Ruleset;
            var levels = ruleset.Levels.OrderBy(item => item.LevelIndex).ToList();
            if (levels.Count == 0)
                throw new InvalidOperationException("The rooms-and-corridors map generator needs at least one level row.");
            session.Progression.LevelIndex = Math.Clamp(session.Progression.LevelIndex, 0, levels.Count - 1);
            var level = levels[session.Progression.LevelIndex];
            var levelParameters = parameters.Parse(level.ParametersJson);
            var mapParameters = parameters.Parse(ruleset.MapParametersJson);
            var wall = world.Wall(ruleset);
            var floor = world.Floor(ruleset);
            var width = level.MapWidth;
            var height = level.MapHeight;
            if (session.CampaignSeed <= 0)
                session.CampaignSeed = support.BaseSeed(session);
            var seed = Math.Max(1, hashes.SeedFromText($"{Math.Max(1, session.CampaignSeed)}|{level.LevelNumber}|{session.ScenarioPrompt}"));
            var random = new Random(seed);
            var cells = support.SolidCells(width, height, wall);
            var centers = new List<(int X, int Y)>();

            var first = parameters.ReadObject(mapParameters, "firstRoom");
            grid.CarveRoom(cells,
                parameters.ReadInt(first, "x", 1),
                parameters.ReadInt(first, "y", 1),
                Math.Min(parameters.ReadInt(first, "width", 8), width - 2),
                Math.Min(parameters.ReadInt(first, "height", 6), height - 2),
                floor);
            centers.Add((Math.Min(parameters.ReadInt(first, "centerX", 6), width - 3), Math.Min(parameters.ReadInt(first, "centerY", 3), height - 3)));

            var roomWidth = parameters.ReadObject(mapParameters, "roomWidth");
            var roomHeight = parameters.ReadObject(mapParameters, "roomHeight");
            var roomCount = parameters.ReadInt(levelParameters, "roomCount", 5);
            var previous = centers[0];
            for (var index = 1; index < roomCount; index++)
            {
                var nextWidth = random.Next(
                    parameters.ReadInt(roomWidth, "min", 5),
                    Math.Min(parameters.ReadInt(roomWidth, "cap", 10), Math.Max(parameters.ReadInt(roomWidth, "floor", 6), width / parameters.ReadInt(roomWidth, "divisor", 3))));
                var nextHeight = random.Next(
                    parameters.ReadInt(roomHeight, "min", 4),
                    Math.Min(parameters.ReadInt(roomHeight, "cap", 8), Math.Max(parameters.ReadInt(roomHeight, "floor", 5), height / parameters.ReadInt(roomHeight, "divisor", 3))));
                var maximumRoomX = Math.Max(parameters.ReadInt(mapParameters, "roomXMaxFloor", 3), width - nextWidth - 1);
                var maximumRoomY = Math.Max(parameters.ReadInt(mapParameters, "roomYMaxFloor", 2), height - nextHeight - 1);
                var roomX = random.Next(parameters.ReadInt(mapParameters, "roomXMin", 2), maximumRoomX + 1);
                var roomY = random.Next(parameters.ReadInt(mapParameters, "roomYMin", 1), maximumRoomY + 1);
                grid.CarveRoom(cells, roomX, roomY, nextWidth, nextHeight, floor);
                var center = (
                    X: Math.Clamp(roomX + nextWidth / 2, 1, width - 2),
                    Y: Math.Clamp(roomY + nextHeight / 2, 1, height - 2));
                grid.CarveCorridor(cells, previous.X, previous.Y, center.X, center.Y, random.Next(0, 2) == 0, floor);
                centers.Add(center);
                previous = center;
            }

            session.MapSeed = seed;
            session.WorldMap = cells.Select(row => new string(row)).ToList();
            var start = parameters.ReadObject(mapParameters, "player");
            support.PlacePlayer(context, parameters.ReadInt(start, "x", 3), parameters.ReadInt(start, "y", 3));
            ApplyLevelPlayerStats(context, mapParameters, levelParameters);
            var reset = parameters.ReadObject(mapParameters, "resetVariables");
            foreach (var variable in reset.EnumerateObject())
                session.Variables[variable.Name] = parameters.ReadString(reset, variable.Name, string.Empty);

            session.Markers =
            [
                new GameMarkerState
                {
                    Key = parameters.ReadString(mapParameters, "goalMarker", "goal"),
                    Glyph = parameters.ReadString(mapParameters, "goalGlyph", "X"),
                    X = centers[^1].X,
                    Y = centers[^1].Y
                }
            ];
            SpawnScheduledActors(context, mapParameters, levelParameters, level, centers, random, wall);
            messages.Say(context, string.IsNullOrWhiteSpace(session.ScenarioPrompt)
                ? parameters.ReadString(mapParameters, "startTextKey", "level.start")
                : parameters.ReadString(mapParameters, "startScenarioTextKey", "level.start.scenario"));
            logger.LogInformation("Game session {SessionId} generated level {Level} ({Width}x{Height}, {Rooms} rooms, seed {Seed}).",
                session.Id, level.LevelNumber, width, height, centers.Count, seed);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Generating the rooms-and-corridors level of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }

    /// <summary>
    /// Sets player stats from level parameters named by the map parameter <c>playerStatsFromLevel</c>.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="mapParameters">The map parameters.</param>
    /// <param name="levelParameters">The level parameters.</param>
    private void ApplyLevelPlayerStats(GameEngineContext context, JsonElement mapParameters, JsonElement levelParameters)
    {
        try
        {
            var player = actors.Player(context.Session.Actors, context.Ruleset);
            if (player is null)
                return;
            var mapping = parameters.ReadObject(mapParameters, "playerStatsFromLevel");
            foreach (var stat in mapping.EnumerateObject())
            {
                var source = parameters.ReadObject(mapping, stat.Name);
                var levelParameter = parameters.ReadString(source, "levelParameter", stat.Name);
                var fallback = parameters.ReadInt(source, "default", actors.Stat(player, stat.Name, 0));
                player.Stats[stat.Name] = parameters.ReadInt(levelParameters, levelParameter, fallback);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying level player stats failed.");
            throw;
        }
    }

    /// <summary>
    /// Spawns the level actors: room centres first, then shuffled floor cells away from the player.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="mapParameters">The map parameters with the <c>spawns</c> object.</param>
    /// <param name="levelParameters">The level parameters.</param>
    /// <param name="level">The level row.</param>
    /// <param name="centers">The room centres in carving order.</param>
    /// <param name="random">The level random sequence, continued after carving.</param>
    /// <param name="wall">The wall glyph.</param>
    private void SpawnScheduledActors(
        GameEngineContext context,
        JsonElement mapParameters,
        JsonElement levelParameters,
        GameProjectLevel level,
        IReadOnlyList<(int X, int Y)> centers,
        Random random,
        char wall)
    {
        try
        {
            var session = context.Session;
            var spawns = parameters.ReadObject(mapParameters, "spawns");
            var role = parameters.ReadString(spawns, "role", "enemy");
            var player = actors.Player(session.Actors, context.Ruleset);
            session.Actors.RemoveAll(actor => player is null || !ReferenceEquals(actor, player));
            var playerX = player?.X ?? 0;
            var playerY = player?.Y ?? 0;
            var requestedCount = Math.Clamp(
                (int)Math.Round(
                    parameters.ReadInt(levelParameters, "roomCount", 5) * parameters.ReadDouble(levelParameters, parameters.ReadString(spawns, "densityParameter", "enemyDensity"), 1d),
                    MidpointRounding.AwayFromZero),
                1,
                parameters.ReadInt(spawns, "maximum", 32));
            var goal = session.Markers.FirstOrDefault();
            var occupied = new HashSet<(int X, int Y)> { (playerX, playerY) };
            if (goal is not null)
                occupied.Add((goal.X, goal.Y));
            var candidates = new List<(int X, int Y)>();
            if (centers.Count > 0)
                candidates.Add(centers[0]);
            candidates.AddRange(centers.Skip(1));

            var minimumDistance = parameters.ReadInt(spawns, "minimumPlayerDistance", 3);
            var map = session.WorldMap;
            var floorCells = new List<(int X, int Y)>();
            for (var y = 1; y < map.Count - 1; y++)
                for (var x = 1; x < map[y].Length - 1; x++)
                    if (map[y][x] != wall && Math.Abs(x - playerX) + Math.Abs(y - playerY) >= minimumDistance)
                        floorCells.Add((x, y));
            foreach (var cell in floorCells.OrderBy(_ => random.Next()))
                candidates.Add(cell);

            var schedule = parameters.ReadObjects(spawns, "schedule");
            var scaling = parameters.ReadObjects(spawns, "scaling");
            var keyTemplate = parameters.ReadString(spawns, "keyTemplate", "actor-{number:00}");
            var spawned = 0;
            foreach (var candidate in candidates)
            {
                if (spawned >= requestedCount)
                    break;
                if (!occupied.Add(candidate))
                    continue;
                var index = spawned;
                var archetypeKey = ScheduledArchetype(schedule, index);
                var tokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["number"] = index + 1,
                    ["index"] = index,
                    ["level"] = level.LevelNumber
                };
                var actor = support.Spawn(context.Ruleset, archetypeKey, formatter.Format(keyTemplate, tokens), tokens, candidate.X, candidate.Y);
                foreach (var rule in scaling)
                {
                    var stat = parameters.ReadString(rule, "stat", string.Empty);
                    if (stat.Length == 0 || !actor.Stats.TryGetValue(stat, out var baseValue))
                        continue;
                    var multiplier = parameters.ReadDouble(levelParameters, parameters.ReadString(rule, "levelParameter", string.Empty), 1d);
                    var value = Math.Max(parameters.ReadInt(rule, "minimum", 0), (int)Math.Round(baseValue * multiplier, MidpointRounding.AwayFromZero));
                    actor.Stats[stat] = value;
                    actor.Stats[$"{stat}:max"] = value;
                }
                session.Actors.Add(actor);
                spawned++;
            }
            logger.LogDebug("Spawned {Count} {Role} actor(s) for level {Level}.", spawned, role, level.LevelNumber);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Spawning scheduled level actors failed.");
            throw;
        }
    }

    /// <summary>
    /// Picks the archetype of a spawn index: the first schedule entry whose <c>index</c> or <c>everyNth</c> condition matches,
    /// or the first unconditional entry.
    /// </summary>
    /// <param name="schedule">The schedule entries.</param>
    /// <param name="index">The zero-based spawn index.</param>
    /// <returns>The archetype key.</returns>
    private string ScheduledArchetype(IReadOnlyList<JsonElement> schedule, int index)
    {
        try
        {
            foreach (var entry in schedule)
            {
                var exact = parameters.ReadInt(entry, "index", -1);
                var every = parameters.ReadInt(entry, "everyNth", 0);
                var matches = exact >= 0
                    ? index == exact
                    : every > 0
                        ? index > 0 && (index + 1) % every == 0
                        : true;
                if (matches)
                    return parameters.ReadString(entry, "archetype", string.Empty);
            }
            throw new InvalidOperationException("The spawn schedule has no entry for this spawn.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Choosing the scheduled archetype for spawn {SpawnIndex} failed.", index);
            throw;
        }
    }
}
