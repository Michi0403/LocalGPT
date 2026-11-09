using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Actions;

/// <summary>
/// Action <c>turn</c>: rotates the player by <c>direction</c> × π / <c>piDivisor</c> and optionally says <c>messageKey</c>.
/// </summary>
/// <param name="parameters">Reads the action parameters.</param>
/// <param name="actors">Finds the player.</param>
/// <param name="messages">Writes the optional message.</param>
/// <param name="logger">Receives action diagnostics.</param>
public sealed class TurnActionPrimitive(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    IGameMessageService messages,
    ILogger<TurnActionPrimitive> logger) : IGameActionPrimitive
{
    /// <inheritdoc />
    public string Key => "turn";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectAction action, int? aimX, int? aimY)
    {
        try
        {
            var actionParameters = parameters.Parse(action.ParametersJson);
            var player = actors.Player(context.Session.Actors, context.Ruleset);
            if (player is null)
                return;
            var direction = parameters.ReadInt(actionParameters, "direction", 1);
            player.FacingRadians += direction * (Math.PI / parameters.ReadDouble(actionParameters, "piDivisor", 12d));
            messages.Say(context, parameters.ReadString(actionParameters, "messageKey", string.Empty));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying turn action {ActionKey} failed.", action.ActionKey);
            throw;
        }
    }

    /// <inheritdoc />
    public GameSceneControlStep? DescribeSceneControl(GameProjectAction action)
    {
        try
        {
            return new GameSceneControlStep(parameters.ReadInt(parameters.Parse(action.ParametersJson), "direction", 1) < 0 ? 1 : 3, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Describing turn action {ActionKey} for scenes failed.", action.ActionKey);
            throw;
        }
    }
}
