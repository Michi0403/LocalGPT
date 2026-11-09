using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Actions;

/// <summary>
/// Action <c>toggle-flag</c>: flips a boolean session variable and says <c>onKey</c> or <c>offKey</c>.
/// </summary>
/// <param name="parameters">Reads the action parameters.</param>
/// <param name="messages">Writes the stance message.</param>
/// <param name="logger">Receives action diagnostics.</param>
public sealed class ToggleFlagActionPrimitive(
    IGameParameterReader parameters,
    IGameMessageService messages,
    ILogger<ToggleFlagActionPrimitive> logger) : IGameActionPrimitive
{
    /// <inheritdoc />
    public string Key => "toggle-flag";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectAction action, int? aimX, int? aimY)
    {
        try
        {
            var actionParameters = parameters.Parse(action.ParametersJson);
            var variable = parameters.ReadString(actionParameters, "variable", action.ActionKey);
            var set = context.Session.Variables.TryGetValue(variable, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            context.Session.Variables[variable] = set ? "false" : "true";
            messages.Say(context, parameters.ReadString(actionParameters, set ? "offKey" : "onKey", string.Empty));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying toggle action {ActionKey} failed.", action.ActionKey);
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
            logger.LogError(exception, "Describing toggle action {ActionKey} for scenes failed.", action.ActionKey);
            throw;
        }
    }
}
