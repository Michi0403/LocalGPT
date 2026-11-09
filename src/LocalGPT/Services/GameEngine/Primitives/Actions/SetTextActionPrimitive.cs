using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Actions;

/// <summary>
/// Action <c>set-text</c>: writes a ruleset text into a session variable, for example a story choice outcome.
/// </summary>
/// <param name="parameters">Reads the action parameters.</param>
/// <param name="messages">Formats the text.</param>
/// <param name="logger">Receives action diagnostics.</param>
public sealed class SetTextActionPrimitive(
    IGameParameterReader parameters,
    IGameMessageService messages,
    ILogger<SetTextActionPrimitive> logger) : IGameActionPrimitive
{
    /// <inheritdoc />
    public string Key => "set-text";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectAction action, int? aimX, int? aimY)
    {
        try
        {
            var actionParameters = parameters.Parse(action.ParametersJson);
            var variable = parameters.ReadString(actionParameters, "variable", "text");
            context.Session.Variables[variable] = messages.Format(context, parameters.ReadString(actionParameters, "textKey", string.Empty));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying text action {ActionKey} failed.", action.ActionKey);
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
            logger.LogError(exception, "Describing text action {ActionKey} for scenes failed.", action.ActionKey);
            throw;
        }
    }
}
