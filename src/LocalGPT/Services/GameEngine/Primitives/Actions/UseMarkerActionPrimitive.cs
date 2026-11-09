using System.Globalization;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Actions;

/// <summary>
/// Action <c>use-marker</c>: pulses <c>pulseVariable</c>, runs the <c>goalTrigger</c> rules and, while the game runs, says
/// <c>lockedKey</c> when living <c>lockedRole</c> actors remain or <c>openKey</c> otherwise.
/// </summary>
/// <param name="parameters">Reads the action parameters.</param>
/// <param name="actors">Counts living actors.</param>
/// <param name="messages">Writes the use message.</param>
/// <param name="logger">Receives action diagnostics.</param>
public sealed class UseMarkerActionPrimitive(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    IGameMessageService messages,
    ILogger<UseMarkerActionPrimitive> logger) : IGameActionPrimitive
{
    /// <inheritdoc />
    public string Key => "use-marker";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectAction action, int? aimX, int? aimY)
    {
        try
        {
            var session = context.Session;
            var actionParameters = parameters.Parse(action.ParametersJson);
            var pulseVariable = parameters.ReadString(actionParameters, "pulseVariable", string.Empty);
            if (pulseVariable.Length > 0)
                session.Variables[pulseVariable] = parameters.ReadInt(actionParameters, "pulseTurns", 2).ToString(CultureInfo.InvariantCulture);
            var goalTrigger = parameters.ReadString(actionParameters, "goalTrigger", string.Empty);
            if (goalTrigger.Length > 0)
                context.Engine.RunRules(session, goalTrigger);
            if (session.Status != "Running")
                return;
            var lockedRole = parameters.ReadString(actionParameters, "lockedRole", string.Empty);
            var locked = lockedRole.Length > 0 && actors.Living(session.Actors, lockedRole).Count > 0;
            messages.Say(context, parameters.ReadString(actionParameters, locked ? "lockedKey" : "openKey", string.Empty));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying use action {ActionKey} failed.", action.ActionKey);
            throw;
        }
    }

    /// <inheritdoc />
    public GameSceneControlStep? DescribeSceneControl(GameProjectAction action)
    {
        try
        {
            return new GameSceneControlStep(0, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Describing use action {ActionKey} for scenes failed.", action.ActionKey);
            throw;
        }
    }
}
