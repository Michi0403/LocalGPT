using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Rules;

/// <summary>
/// Rule <c>chase-and-contact</c>: living <c>role</c> actors next to the player deal their <c>damageStat</c>; the others take
/// one breadth-first step towards the player on alternating turns. When none remain, the <c>goalTrigger</c> rules run and
/// the clear text is shown; when the player's <c>healthStat</c> reaches zero the game is lost.
/// </summary>
/// <param name="parameters">Reads the rule parameters.</param>
/// <param name="actors">Finds the player and living actors.</param>
/// <param name="grid">Finds paths.</param>
/// <param name="world">Reads the wall glyph and occupancy.</param>
/// <param name="messages">Writes contact, clear and defeat texts.</param>
/// <param name="logger">Receives rule diagnostics.</param>
public sealed class ChaseAndContactRulePrimitive(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    IGameGridService grid,
    IGameWorldService world,
    IGameMessageService messages,
    ILogger<ChaseAndContactRulePrimitive> logger) : IGameRulePrimitive
{
    /// <inheritdoc />
    public string Key => "chase-and-contact";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectRule rule)
    {
        try
        {
            var session = context.Session;
            if (session.Status != "Running")
                return;
            var ruleParameters = parameters.Parse(rule.ParametersJson);
            var player = actors.Player(session.Actors, context.Ruleset);
            if (player is null)
                return;
            var role = parameters.ReadString(ruleParameters, "role", "enemy");
            var alive = actors.Living(session.Actors, role);
            if (alive.Count == 0)
            {
                var levelBefore = session.Progression.LevelIndex;
                var goalTrigger = parameters.ReadString(ruleParameters, "goalTrigger", string.Empty);
                if (goalTrigger.Length > 0)
                    context.Engine.RunRules(session, goalTrigger);
                if (session.Progression.LevelIndex != levelBefore)
                    return;
                var keep = parameters.ReadString(ruleParameters, "keepMessageContaining", string.Empty);
                if (session.Status == "Running" && (keep.Length == 0 || !session.Message.Contains(keep, StringComparison.OrdinalIgnoreCase)))
                    messages.Say(context, parameters.ReadString(ruleParameters, "clearKey", "world.clear"));
                return;
            }

            var healthStat = parameters.ReadString(ruleParameters, "healthStat", "health");
            var damageStat = parameters.ReadString(ruleParameters, "damageStat", "contact-damage");
            var hitKey = parameters.ReadString(ruleParameters, "hitKey", "world.contact-hit");
            var wall = world.Wall(context.Ruleset);
            var hits = new List<string>();
            for (var index = 0; index < alive.Count; index++)
            {
                var actor = alive[index];
                var distance = Math.Abs(actor.X - player.X) + Math.Abs(actor.Y - player.Y);
                if (distance == 1)
                {
                    hits.Add(Hit(context, player, actor, healthStat, damageStat, hitKey));
                    continue;
                }

                if ((session.Turn + index) % 2 != 0)
                    continue;
                var blocked = session.Actors
                    .Where(other => !other.IsEliminated &&
                        string.Equals(other.Role, role, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(other.Key, actor.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(other => (other.X, other.Y))
                    .ToHashSet();
                var path = grid.FindShortestPath(session.WorldMap, actor.X, actor.Y, player.X, player.Y, wall, blocked, true);
                if (path.Count < 2)
                    continue;
                var next = path[1];
                if (next.X == player.X && next.Y == player.Y)
                {
                    hits.Add(Hit(context, player, actor, healthStat, damageStat, hitKey));
                }
                else if (!world.IsOccupied(session, role, next.X, next.Y, actor.Key))
                {
                    actor.X = next.X;
                    actor.Y = next.Y;
                }
            }

            if (hits.Count > 0)
            {
                session.Message = messages.Format(context, parameters.ReadString(ruleParameters, "incomingKey", "world.incoming"),
                    new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["hits"] = string.Join("; ", hits) }).Trim();
            }
            if (actors.Stat(player, healthStat, 0) <= 0)
            {
                messages.Complete(context,
                    parameters.ReadString(ruleParameters, "defeatOwnerKey", "outcome.defeat.owner"),
                    parameters.ReadString(ruleParameters, "defeatReasonKey", "outcome.defeat.reason"),
                    parameters.ReadString(ruleParameters, "defeatMessageKey", "outcome.defeat.message"));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }

    /// <summary>
    /// Applies contact damage to the player and formats the hit text.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="player">The player.</param>
    /// <param name="actor">The attacking actor.</param>
    /// <param name="healthStat">The player health stat.</param>
    /// <param name="damageStat">The attacker damage stat.</param>
    /// <param name="hitKey">The hit text key.</param>
    /// <returns>The hit text.</returns>
    private string Hit(GameEngineContext context, GameActorState player, GameActorState actor, string healthStat, string damageStat, string hitKey)
    {
        try
        {
            var damage = actors.Stat(actor, damageStat, 0);
            player.Stats[healthStat] = Math.Max(0, actors.Stat(player, healthStat, 0) - damage);
            return messages.Format(context, hitKey, new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["attacker"] = actor.Name,
                ["damage"] = damage
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying contact damage from {ActorKey} failed.", actor.Key);
            throw;
        }
    }
}
