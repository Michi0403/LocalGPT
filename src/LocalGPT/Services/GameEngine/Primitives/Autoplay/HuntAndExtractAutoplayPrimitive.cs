using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Autoplay;

/// <summary>
/// Autoplay <c>hunt-and-extract</c>: turns to and fires at visible living targets, paths to the nearest target, and walks
/// to the goal marker once no target remains. Action keys and the aiming tolerance come from the autoplay parameters.
/// </summary>
/// <param name="parameters">Reads the autoplay parameters.</param>
/// <param name="actors">Finds the player, targets and the goal marker.</param>
/// <param name="grid">Tests line of sight and finds paths.</param>
/// <param name="world">Reads the wall glyph.</param>
/// <param name="angles">Computes steering deltas.</param>
/// <param name="logger">Receives autoplay diagnostics.</param>
public sealed class HuntAndExtractAutoplayPrimitive(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    IGameGridService grid,
    IGameWorldService world,
    IGameAngleService angles,
    ILogger<HuntAndExtractAutoplayPrimitive> logger) : IGameAutoplayPrimitive
{
    /// <inheritdoc />
    public string Key => "hunt-and-extract";

    /// <inheritdoc />
    public string SelectAction(CouncilGameSessionSnapshot snapshot, JsonElement autoplayParameters)
    {
        try
        {
            var ruleset = snapshot.Ruleset;
            var player = actors.Player(snapshot.Actors, ruleset);
            if (player is null)
                return snapshot.LegalActions.FirstOrDefault() ?? string.Empty;
            var keys = parameters.ReadObject(autoplayParameters, "actions");
            var shoot = parameters.ReadString(keys, "shoot", "shoot");
            var turnLeft = parameters.ReadString(keys, "turnLeft", "turn-left");
            var turnRight = parameters.ReadString(keys, "turnRight", "turn-right");
            var use = parameters.ReadString(keys, "use", "use");
            var map = snapshot.WorldMap;
            var wall = world.Wall(ruleset);
            var tolerance = Math.PI / parameters.ReadDouble(autoplayParameters, "aimPiDivisor", 24d);
            var range = parameters.ReadDouble(autoplayParameters, "range", 14d);
            var ammo = actors.Stat(player, parameters.ReadString(autoplayParameters, "ammoStat", "ammo"), 0);
            var facing = Math.Round(angles.Normalize(player.FacingRadians) * 180d / Math.PI, 1) * Math.PI / 180d;
            var targets = actors.Living(snapshot.Actors, parameters.ReadString(autoplayParameters, "targetRole", "enemy"))
                .OrderBy(target => Math.Abs(target.X - player.X) + Math.Abs(target.Y - player.Y))
                .ThenBy(target => target.Key, StringComparer.Ordinal)
                .ToList();

            foreach (var target in targets)
            {
                if (!grid.HasLineOfSight(map, player.X, player.Y, target.X, target.Y, wall))
                    continue;
                var targetAngle = Math.Atan2(target.Y - player.Y, target.X - player.X);
                var delta = angles.SignedDelta(facing, targetAngle);
                var distance = Math.Sqrt(Math.Pow(target.X - player.X, 2) + Math.Pow(target.Y - player.Y, 2));
                if (Math.Abs(delta) <= tolerance && distance <= range && ammo > 0)
                    return shoot;
                if (Math.Abs(delta) > tolerance)
                    return delta > 0 ? turnRight : turnLeft;
            }

            if (targets.Count > 0)
            {
                var target = targets[0];
                var blocked = targets.Skip(1).Select(item => (item.X, item.Y)).ToHashSet();
                var path = grid.FindShortestPath(map, player.X, player.Y, target.X, target.Y, wall, blocked, true);
                var pathAction = SelectPathAction(snapshot, player, keys, path, facing, target.X, target.Y, true, ammo, tolerance, targets);
                if (!string.IsNullOrWhiteSpace(pathAction))
                    return pathAction;
                return snapshot.BlockedMoveStreak > 0 ? turnLeft : turnRight;
            }

            var goal = actors.Marker(snapshot.Markers, parameters.ReadString(autoplayParameters, "goalMarker", "goal"));
            if (goal is null)
                return turnRight;
            if (player.X == goal.X && player.Y == goal.Y)
                return use;
            var goalPath = grid.FindShortestPath(map, player.X, player.Y, goal.X, goal.Y, wall, null, false);
            var goalAction = SelectPathAction(snapshot, player, keys, goalPath, facing, goal.X, goal.Y, false, ammo, tolerance, targets);
            return string.IsNullOrWhiteSpace(goalAction) ? turnRight : goalAction;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting the hunt-and-extract autoplay action failed.");
            throw;
        }
    }

    /// <summary>
    /// Chooses the steering or movement action that follows the first step of a path.
    /// </summary>
    /// <param name="snapshot">The session snapshot.</param>
    /// <param name="player">The player actor.</param>
    /// <param name="keys">The action key parameters.</param>
    /// <param name="path">The path including the start cell.</param>
    /// <param name="facing">The rounded player heading.</param>
    /// <param name="targetX">The path target column.</param>
    /// <param name="targetY">The path target row.</param>
    /// <param name="targetIsActor">Whether the target is a hostile actor rather than a marker.</param>
    /// <param name="ammo">The player's ammunition.</param>
    /// <param name="tolerance">The aiming tolerance.</param>
    /// <param name="targets">The living targets, which block movement.</param>
    /// <returns>The action, or an empty string when the path has no next step.</returns>
    private string SelectPathAction(
        CouncilGameSessionSnapshot snapshot,
        GameActorState player,
        JsonElement keys,
        IReadOnlyList<(int X, int Y)> path,
        double facing,
        int targetX,
        int targetY,
        bool targetIsActor,
        int ammo,
        double tolerance,
        IReadOnlyList<GameActorState> targets)
    {
        try
        {
            if (path.Count < 2)
                return string.Empty;
            var turnLeft = parameters.ReadString(keys, "turnLeft", "turn-left");
            var turnRight = parameters.ReadString(keys, "turnRight", "turn-right");
            var next = path[1];
            if (targetIsActor && next.X == targetX && next.Y == targetY)
            {
                var targetDelta = angles.SignedDelta(facing, Math.Atan2(targetY - player.Y, targetX - player.X));
                if (Math.Abs(targetDelta) > tolerance)
                    return targetDelta > 0 ? turnRight : turnLeft;
                return ammo > 0 ? parameters.ReadString(keys, "shoot", "shoot") : parameters.ReadString(keys, "backward", "move-backward");
            }

            var delta = angles.SignedDelta(facing, Math.Atan2(next.Y - player.Y, next.X - player.X));
            if (Math.Abs(delta) > tolerance)
                return delta > 0 ? turnRight : turnLeft;
            if (!grid.IsWalkable(snapshot.WorldMap, next.X, next.Y, world.Wall(snapshot.Ruleset)) ||
                targets.Any(target => target.X == next.X && target.Y == next.Y))
                return delta >= 0 ? turnRight : turnLeft;
            return parameters.ReadString(keys, "forward", "move-forward");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting the autoplay path action failed.");
            throw;
        }
    }
}
