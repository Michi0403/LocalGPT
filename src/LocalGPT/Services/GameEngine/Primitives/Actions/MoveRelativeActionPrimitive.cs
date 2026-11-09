using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Actions;

/// <summary>
/// Action <c>move-relative</c>: moves the player one grid step along its heading (<c>sign</c>, optionally rotated by
/// <c>offsetSign</c> × π / <c>offsetPiDivisor</c>); walls and living <c>blockingRole</c> actors block, and a successful
/// step runs the <c>goalTrigger</c> rules.
/// </summary>
/// <param name="parameters">Reads the action parameters.</param>
/// <param name="actors">Finds the player.</param>
/// <param name="grid">Tests walkability.</param>
/// <param name="world">Reads the wall glyph and occupancy.</param>
/// <param name="messages">Writes movement messages.</param>
/// <param name="logger">Receives action diagnostics.</param>
public sealed class MoveRelativeActionPrimitive(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    IGameGridService grid,
    IGameWorldService world,
    IGameMessageService messages,
    ILogger<MoveRelativeActionPrimitive> logger) : IGameActionPrimitive
{
    /// <inheritdoc />
    public string Key => "move-relative";

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
            var sign = parameters.ReadInt(actionParameters, "sign", 1);
            var offsetSign = parameters.ReadInt(actionParameters, "offsetSign", 0);
            var angle = offsetSign == 0
                ? player.FacingRadians
                : player.FacingRadians + offsetSign * (Math.PI / parameters.ReadDouble(actionParameters, "offsetPiDivisor", 2d));
            var dx = sign < 0 ? -Math.Cos(angle) : Math.Cos(angle);
            var dy = sign < 0 ? -Math.Sin(angle) : Math.Sin(angle);
            var nextX = player.X + (int)Math.Round(dx);
            var nextY = player.Y + (int)Math.Round(dy);
            var blockingRole = parameters.ReadString(actionParameters, "blockingRole", string.Empty);
            var extra = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["x"] = nextX, ["y"] = nextY };
            if (!grid.IsWalkable(session.WorldMap, nextX, nextY, world.Wall(context.Ruleset)) ||
                (blockingRole.Length > 0 && world.IsOccupied(session, blockingRole, nextX, nextY, null)))
            {
                session.LastMoveBlocked = true;
                session.BlockedMoveStreak++;
                messages.Say(context, parameters.ReadString(actionParameters, "blockedKey", "move.blocked"), extra);
                return;
            }

            player.X = nextX;
            player.Y = nextY;
            session.BlockedMoveStreak = 0;
            session.LastMoveBlocked = false;
            messages.Say(context, parameters.ReadString(actionParameters, "movedKey", "move.moved"), extra);
            var goalTrigger = parameters.ReadString(actionParameters, "goalTrigger", string.Empty);
            if (goalTrigger.Length > 0)
                context.Engine.RunRules(session, goalTrigger);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying movement action {ActionKey} failed.", action.ActionKey);
            throw;
        }
    }

    /// <inheritdoc />
    public GameSceneControlStep? DescribeSceneControl(GameProjectAction action)
    {
        try
        {
            var actionParameters = parameters.Parse(action.ParametersJson);
            var sign = parameters.ReadInt(actionParameters, "sign", 1);
            var offsetSign = parameters.ReadInt(actionParameters, "offsetSign", 0);
            var quarter = (sign < 0 ? 2 : 0) + (offsetSign < 0 ? 1 : offsetSign > 0 ? 3 : 0);
            return new GameSceneControlStep(0, quarter % 4);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Describing movement action {ActionKey} for scenes failed.", action.ActionKey);
            throw;
        }
    }
}
