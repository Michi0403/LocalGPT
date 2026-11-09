using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Rules;

/// <summary>
/// Rule <c>set-variable</c>: writes a literal <c>value</c> or the ruleset text <c>textKey</c> into a session variable.
/// </summary>
/// <param name="parameters">Reads the rule parameters.</param>
/// <param name="messages">Formats texts.</param>
/// <param name="logger">Receives rule diagnostics.</param>
public sealed class SetVariableRulePrimitive(
    IGameParameterReader parameters,
    IGameMessageService messages,
    ILogger<SetVariableRulePrimitive> logger) : IGameRulePrimitive
{
    /// <inheritdoc />
    public string Key => "set-variable";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectRule rule)
    {
        try
        {
            var ruleParameters = parameters.Parse(rule.ParametersJson);
            var variable = parameters.ReadString(ruleParameters, "variable", string.Empty);
            if (variable.Length == 0)
                return;
            var textKey = parameters.ReadString(ruleParameters, "textKey", string.Empty);
            context.Session.Variables[variable] = textKey.Length > 0
                ? messages.Format(context, textKey)
                : parameters.ReadString(ruleParameters, "value", string.Empty);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }
}
