using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Actions;

/// <summary>
/// Action <c>move-grid</c>: moves the player by a fixed <c>dx</c>/<c>dy</c> clamped to the declared bounds, without map tests.
/// </summary>
/// <param name="parameters">Reads the action parameters.</param>
/// <param name="actors">Finds the player.</param>
/// <param name="logger">Receives action diagnostics.</param>
public sealed class MoveGridActionPrimitive(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    ILogger<MoveGridActionPrimitive> logger) : IGameActionPrimitive
{
    /// <inheritdoc />
    public string Key => "move-grid";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectAction action, int? aimX, int? aimY)
    {
        try
        {
            var actionParameters = parameters.Parse(action.ParametersJson);
            var player = actors.Player(context.Session.Actors, context.Ruleset);
            if (player is null)
                return;
            player.X = Math.Clamp(player.X + parameters.ReadInt(actionParameters, "dx", 0),
                parameters.ReadInt(actionParameters, "minX", int.MinValue), parameters.ReadInt(actionParameters, "maxX", int.MaxValue));
            player.Y = Math.Clamp(player.Y + parameters.ReadInt(actionParameters, "dy", 0),
                parameters.ReadInt(actionParameters, "minY", int.MinValue), parameters.ReadInt(actionParameters, "maxY", int.MaxValue));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying grid movement action {ActionKey} failed.", action.ActionKey);
            throw;
        }
    }

    /// <inheritdoc />
    public GameSceneControlStep? DescribeSceneControl(GameProjectAction action)
    {
        try
        {
            var actionParameters = parameters.Parse(action.ParametersJson);
            var dx = parameters.ReadInt(actionParameters, "dx", 0);
            var dy = parameters.ReadInt(actionParameters, "dy", 0);
            int? quarter = dy < 0 ? 0 : dy > 0 ? 2 : dx < 0 ? 1 : dx > 0 ? 3 : null;
            return new GameSceneControlStep(0, quarter);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Describing grid movement action {ActionKey} for scenes failed.", action.ActionKey);
            throw;
        }
    }
}
