using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Actions;

/// <summary>
/// Action <c>hitscan</c>: spends one <c>ammoStat</c>, flashes <c>flashVariable</c>, optionally aims at the requested cell and
/// damages the nearest living <c>targetRole</c> actor inside the crosshair cone with line of sight.
/// </summary>
/// <param name="parameters">Reads the action parameters.</param>
/// <param name="actors">Finds the player and reads stats.</param>
/// <param name="grid">Tests line of sight.</param>
/// <param name="world">Reads the wall glyph.</param>
/// <param name="angles">Computes cone deltas.</param>
/// <param name="messages">Writes shot messages.</param>
/// <param name="logger">Receives action diagnostics.</param>
public sealed class HitscanActionPrimitive(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    IGameGridService grid,
    IGameWorldService world,
    IGameAngleService angles,
    IGameMessageService messages,
    ILogger<HitscanActionPrimitive> logger) : IGameActionPrimitive
{
    /// <inheritdoc />
    public string Key => "hitscan";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectAction action, int? aimX, int? aimY)
    {
        try
        {
            var session = context.Session;
            var actionParameters = parameters.Parse(action.ParametersJson);
            var player = actors.Player(session.Actors, context.Ruleset);
            if (player is null)
                return;
            var ammoStat = parameters.ReadString(actionParameters, "ammoStat", "ammo");
            var ammo = actors.Stat(player, ammoStat, 0);
            if (ammo <= 0)
            {
                messages.Say(context, parameters.ReadString(actionParameters, "emptyKey", "shot.empty"));
                return;
            }

            player.Stats[ammoStat] = ammo - 1;
            var flashVariable = parameters.ReadString(actionParameters, "flashVariable", string.Empty);
            if (flashVariable.Length > 0)
                session.Variables[flashVariable] = parameters.ReadInt(actionParameters, "flashTurns", 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (aimX.HasValue && aimY.HasValue)
                player.FacingRadians = Math.Atan2(aimY.Value - player.Y, aimX.Value - player.X);

            var wall = world.Wall(context.Ruleset);
            var targetRole = parameters.ReadString(actionParameters, "targetRole", "enemy");
            var range = parameters.ReadDouble(actionParameters, "range", 14d);
            var minimumCone = parameters.ReadDouble(actionParameters, "minimumCone", .045d);
            var coneHalfWidth = parameters.ReadDouble(actionParameters, "coneHalfWidth", .55d);
            GameActorState? target = null;
            var targetDistance = double.MaxValue;
            foreach (var candidate in session.Actors.Where(actor => !actor.IsEliminated && string.Equals(actor.Role, targetRole, StringComparison.OrdinalIgnoreCase)))
            {
                var dx = candidate.X + .5d - (player.X + .5d);
                var dy = candidate.Y + .5d - (player.Y + .5d);
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance <= 0.01d || distance > range)
                    continue;
                var angle = Math.Atan2(dy, dx);
                var delta = Math.Abs(angles.SignedDelta(player.FacingRadians, angle));
                var hitCone = Math.Max(minimumCone, Math.Atan2(coneHalfWidth, distance));
                if (delta > hitCone || !grid.HasLineOfSight(session.WorldMap, player.X, player.Y, candidate.X, candidate.Y, wall))
                    continue;
                if (distance < targetDistance)
                {
                    target = candidate;
                    targetDistance = distance;
                }
            }

            if (target is null)
            {
                messages.Say(context, parameters.ReadString(actionParameters, "missKey", "shot.miss"));
                return;
            }

            var healthStat = parameters.ReadString(actionParameters, "healthStat", "health");
            var damage = parameters.ReadInt(actionParameters, "damage", 50);
            var health = Math.Max(0, actors.Stat(target, healthStat, 0) - damage);
            target.Stats[healthStat] = health;
            target.IsEliminated = health <= 0;
            var extra = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["target"] = target.Name,
                ["damage"] = damage,
                ["health"] = health,
                ["maximum"] = actors.Stat(target, $"{healthStat}:max", health)
            };
            messages.Say(context, target.IsEliminated
                ? parameters.ReadString(actionParameters, "downKey", "shot.down")
                : parameters.ReadString(actionParameters, "hitKey", "shot.hit"), extra);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying hitscan action {ActionKey} failed.", action.ActionKey);
            throw;
        }
    }

    /// <inheritdoc />
    public GameSceneControlStep? DescribeSceneControl(GameProjectAction action)
    {
        try
        {
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Describing hitscan action {ActionKey} for scenes failed.", action.ActionKey);
            throw;
        }
    }
}
