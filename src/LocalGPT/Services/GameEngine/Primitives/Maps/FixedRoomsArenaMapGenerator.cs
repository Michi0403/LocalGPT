using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Maps;

/// <summary>
/// Map generator <c>fixed-rooms-arena</c>: one seeded arena of a fixed size with a first room, a fixed number of
/// additional rooms, a goal marker in the last room and an explicit spawn list placed at cells or room centres.
/// </summary>
/// <param name="parameters">Reads map and spawn parameters.</param>
/// <param name="grid">Carves rooms and corridors.</param>
/// <param name="world">Reads the map glyphs.</param>
/// <param name="support">Shares seeds, grids, player placement and spawning.</param>
/// <param name="actors">Finds the player.</param>
/// <param name="messages">Writes the arena start message.</param>
/// <param name="logger">Receives map diagnostics.</param>
public sealed class FixedRoomsArenaMapGenerator(
    IGameParameterReader parameters,
    IGameGridService grid,
    IGameWorldService world,
    IGameMapSupportService support,
    IGameActorQueryService actors,
    IGameMessageService messages,
    ILogger<FixedRoomsArenaMapGenerator> logger) : IGameMapGeneratorPrimitive
{
    /// <inheritdoc />
    public string Key => "fixed-rooms-arena";

    /// <inheritdoc />
    public void Generate(GameEngineContext context)
    {
        try
        {
            var session = context.Session;
            var ruleset = context.Ruleset;
            var mapParameters = parameters.Parse(ruleset.MapParametersJson);
            var wall = world.Wall(ruleset);
            var floor = world.Floor(ruleset);
            var width = parameters.ReadInt(mapParameters, "width", 32);
            var height = parameters.ReadInt(mapParameters, "height", 20);
            var seed = support.BaseSeed(session);
            var random = new Random(seed);
            var cells = support.SolidCells(width, height, wall);
            var centers = new List<(int X, int Y)>();

            var first = parameters.ReadObject(mapParameters, "firstRoom");
            grid.CarveRoom(cells,
                parameters.ReadInt(first, "x", 1),
                parameters.ReadInt(first, "y", 1),
                parameters.ReadInt(first, "width", 8),
                parameters.ReadInt(first, "height", 6),
                floor);
            centers.Add((parameters.ReadInt(first, "centerX", 6), parameters.ReadInt(first, "centerY", 3)));

            var roomWidth = parameters.ReadObject(mapParameters, "roomWidth");
            var roomHeight = parameters.ReadObject(mapParameters, "roomHeight");
            var previous = centers[0];
            var additionalRooms = parameters.ReadInt(mapParameters, "additionalRooms", 5);
            for (var index = 0; index < additionalRooms; index++)
            {
                var nextWidth = random.Next(parameters.ReadInt(roomWidth, "min", 5), parameters.ReadInt(roomWidth, "maxExclusive", 9));
                var nextHeight = random.Next(parameters.ReadInt(roomHeight, "min", 4), parameters.ReadInt(roomHeight, "maxExclusive", 7));
                var roomX = random.Next(parameters.ReadInt(mapParameters, "roomXMin", 10), Math.Max(parameters.ReadInt(mapParameters, "roomXMaxFloor", 11), width - nextWidth - 1));
                var roomY = random.Next(parameters.ReadInt(mapParameters, "roomYMin", 1), Math.Max(parameters.ReadInt(mapParameters, "roomYMaxFloor", 2), height - nextHeight - 1));
                grid.CarveRoom(cells, roomX, roomY, nextWidth, nextHeight, floor);
                var center = (X: roomX + nextWidth / 2, Y: roomY + nextHeight / 2);
                grid.CarveCorridor(cells, previous.X, previous.Y, center.X, center.Y, random.Next(0, 2) == 0, floor);
                centers.Add(center);
                previous = center;
            }

            session.MapSeed = seed;
            session.WorldMap = cells.Select(row => new string(row)).ToList();
            var start = parameters.ReadObject(mapParameters, "player");
            support.PlacePlayer(context, parameters.ReadInt(start, "x", 3), parameters.ReadInt(start, "y", 3));
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

            var player = actors.Player(session.Actors, ruleset);
            session.Actors.RemoveAll(actor => player is null || !ReferenceEquals(actor, player));
            var number = 0;
            foreach (var spawn in parameters.ReadObjects(mapParameters, "spawns"))
            {
                number++;
                var centerIndex = parameters.ReadInt(spawn, "center", -1);
                var x = centerIndex >= 0 && centerIndex < centers.Count ? centers[centerIndex].X : parameters.ReadInt(spawn, "x", 0);
                var y = centerIndex >= 0 && centerIndex < centers.Count ? centers[centerIndex].Y : parameters.ReadInt(spawn, "y", 0);
                var tokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["number"] = number, ["index"] = number - 1 };
                var actor = support.Spawn(ruleset, parameters.ReadString(spawn, "archetype", string.Empty), parameters.ReadString(spawn, "key", $"actor-{number}"), tokens, x, y);
                var name = parameters.ReadString(spawn, "name", string.Empty);
                if (name.Length > 0)
                    actor.Name = name;
                session.Actors.Add(actor);
            }

            messages.Say(context, string.IsNullOrWhiteSpace(session.ScenarioPrompt)
                ? parameters.ReadString(mapParameters, "startTextKey", "arena.start")
                : parameters.ReadString(mapParameters, "startScenarioTextKey", "arena.start.scenario"));
            logger.LogInformation("Game session {SessionId} generated a fixed arena ({Width}x{Height}, {Rooms} rooms, seed {Seed}, {Spawns} spawn(s)).",
                session.Id, width, height, centers.Count, seed, number);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Generating the fixed arena of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }
}
