using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Shared building blocks of map generator primitives.
/// </summary>
/// <param name="hashes">Derives seeds.</param>
/// <param name="parameters">Reads archetype parameters.</param>
/// <param name="formatter">Formats archetype name templates.</param>
/// <param name="actors">Finds the player actor.</param>
/// <param name="logger">Receives map diagnostics.</param>
public sealed class GameMapSupportService(
    IGameStableHashService hashes,
    IGameParameterReader parameters,
    IGameTemplateFormatter formatter,
    IGameActorQueryService actors,
    ILogger<GameMapSupportService> logger) : IGameMapSupportService
{
    /// <inheritdoc />
    public int BaseSeed(CouncilGameSessionState session)
    {
        try
        {
            var seed = session.MapSeed > 0
                ? session.MapSeed
                : !string.IsNullOrWhiteSpace(session.ScenarioPrompt)
                    ? hashes.SeedFromText(session.ScenarioPrompt)
                    : BitConverter.ToInt32(session.Id.ToByteArray(), 0) & int.MaxValue;
            return Math.Max(1, seed);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deriving the map seed of session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public char[][] SolidCells(int width, int height, char wall)
    {
        try
        {
            return Enumerable.Range(0, height)
                .Select(_ => Enumerable.Repeat(wall, width).ToArray())
                .ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a {Width}x{Height} map grid failed.", width, height);
            throw;
        }
    }

    /// <inheritdoc />
    public void PlacePlayer(GameEngineContext context, int x, int y)
    {
        try
        {
            var player = actors.Player(context.Session.Actors, context.Ruleset);
            if (player is not null)
            {
                player.X = x;
                player.Y = y;
                player.FacingRadians = 0d;
            }
            context.Session.BlockedMoveStreak = 0;
            context.Session.LastMoveBlocked = false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Placing the player failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public GameActorState Spawn(GameRuleset ruleset, string archetypeKey, string key, IReadOnlyDictionary<string, object?> nameTokens, int x, int y)
    {
        try
        {
            var archetype = ruleset.Archetypes.FirstOrDefault(item => string.Equals(item.ArchetypeKey, archetypeKey, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"The game ruleset has no archetype '{archetypeKey}'.");
            var archetypeParameters = parameters.Parse(archetype.ParametersJson);
            var template = parameters.ReadString(archetypeParameters, "nameTemplate", archetype.DisplayName);
            var actor = new GameActorState
            {
                Key = key,
                ArchetypeKey = archetype.ArchetypeKey,
                Role = archetype.Role,
                Name = formatter.Format(template, nameTokens),
                Glyph = archetype.Glyph,
                X = x,
                Y = y
            };
            var stats = parameters.ReadObject(archetypeParameters, "stats");
            foreach (var stat in stats.EnumerateObject())
            {
                var value = parameters.ReadInt(stats, stat.Name, 0);
                actor.Stats[stat.Name] = value;
                actor.Stats[$"{stat.Name}:max"] = value;
            }
            return actor;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Spawning archetype {ArchetypeKey} as {ActorKey} failed.", archetypeKey, key);
            throw;
        }
    }

    /// <inheritdoc />
    public GameActorState? CreatePlayer(GameRuleset ruleset)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ruleset.PlayerArchetypeKey))
                return null;
            var archetype = ruleset.Archetypes.FirstOrDefault(item => string.Equals(item.ArchetypeKey, ruleset.PlayerArchetypeKey, StringComparison.OrdinalIgnoreCase));
            if (archetype is null)
            {
                logger.LogWarning("The game ruleset names player archetype {ArchetypeKey}, but has no such archetype row.", ruleset.PlayerArchetypeKey);
                return null;
            }
            var archetypeParameters = parameters.Parse(archetype.ParametersJson);
            var player = new GameActorState
            {
                Key = "player",
                ArchetypeKey = archetype.ArchetypeKey,
                Role = archetype.Role,
                Name = archetype.DisplayName,
                Glyph = archetype.Glyph,
                X = parameters.ReadInt(archetypeParameters, "startX", 0),
                Y = parameters.ReadInt(archetypeParameters, "startY", 0),
                IsActive = true
            };
            foreach (var stat in ruleset.Stats.OrderBy(item => item.SortOrder))
            {
                player.Stats[stat.StatKey] = stat.InitialValue;
                player.Stats[$"{stat.StatKey}:max"] = stat.MaximumValue;
            }
            return player;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the player actor failed.");
            throw;
        }
    }
}
