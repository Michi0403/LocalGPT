using System.Globalization;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Rules;

/// <summary>
/// Rule <c>decay-variable</c>: counts a numeric session variable down towards zero, for example a muzzle flash.
/// </summary>
/// <param name="parameters">Reads the rule parameters.</param>
/// <param name="logger">Receives rule diagnostics.</param>
public sealed class DecayVariableRulePrimitive(
    IGameParameterReader parameters,
    ILogger<DecayVariableRulePrimitive> logger) : IGameRulePrimitive
{
    /// <inheritdoc />
    public string Key => "decay-variable";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectRule rule)
    {
        try
        {
            var variable = parameters.ReadString(parameters.Parse(rule.ParametersJson), "variable", string.Empty);
            if (variable.Length == 0)
                return;
            var current = context.Session.Variables.TryGetValue(variable, out var value) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : 0;
            context.Session.Variables[variable] = Math.Max(0, current - 1).ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }
}
