using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Reads and writes generic actor, stat and marker state.
/// </summary>
/// <param name="logger">Receives actor diagnostics.</param>
public sealed class GameActorQueryService(ILogger<GameActorQueryService> logger) : IGameActorQueryService
{
    /// <inheritdoc />
    public GameActorState? Player(IReadOnlyList<GameActorState> actors, GameRuleset ruleset)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ruleset.PlayerArchetypeKey))
                return null;
            return actors.FirstOrDefault(actor => string.Equals(actor.ArchetypeKey, ruleset.PlayerArchetypeKey, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding the player actor failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public List<GameActorState> Living(IReadOnlyList<GameActorState> actors, string role)
    {
        try
        {
            return actors
                .Where(actor => !actor.IsEliminated && string.Equals(actor.Role, role, StringComparison.OrdinalIgnoreCase))
                .OrderBy(actor => actor.Key, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing living actors of role {Role} failed.", role);
            throw;
        }
    }

    /// <inheritdoc />
    public int Stat(GameActorState? actor, string key, int fallback)
    {
        try
        {
            return actor is not null && actor.Stats.TryGetValue(key, out var value) ? value : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading actor stat {StatKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public void SetStat(GameActorState actor, string key, int value)
    {
        try
        {
            actor.Stats[key] = value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Writing actor stat {StatKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public string Text(GameActorState? actor, string key)
    {
        try
        {
            return actor is not null && actor.Texts.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading actor text {TextKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public GameMarkerState? Marker(IReadOnlyList<GameMarkerState> markers, string key)
    {
        try
        {
            return markers.FirstOrDefault(marker => string.Equals(marker.Key, key, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding marker {MarkerKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public List<GameActorState> Clone(IEnumerable<GameActorState> actors)
    {
        try
        {
            return actors.Select(actor => new GameActorState
            {
                Key = actor.Key,
                ArchetypeKey = actor.ArchetypeKey,
                Role = actor.Role,
                Name = actor.Name,
                Glyph = actor.Glyph,
                OwnerKey = actor.OwnerKey,
                X = actor.X,
                Y = actor.Y,
                FacingRadians = actor.FacingRadians,
                IsActive = actor.IsActive,
                IsEliminated = actor.IsEliminated,
                Stats = new Dictionary<string, int>(actor.Stats, StringComparer.OrdinalIgnoreCase),
                Texts = new Dictionary<string, string>(actor.Texts, StringComparer.OrdinalIgnoreCase),
                Rig = new CouncilAsciiActorRig
                {
                    RigKey = actor.Rig.RigKey,
                    Width = actor.Rig.Width,
                    Height = actor.Rig.Height,
                    HeadGlyph = actor.Rig.HeadGlyph,
                    TorsoGlyph = actor.Rig.TorsoGlyph,
                    Joints = actor.Rig.Joints.Select(joint => new CouncilAsciiActorJoint { Name = joint.Name, X = joint.X, Y = joint.Y }).ToList()
                },
                SpriteRows = [.. actor.SpriteRows],
                SpriteKind = actor.SpriteKind,
                SpritePalette = [.. actor.SpritePalette]
            }).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cloning game actors failed.");
            throw;
        }
    }
}
