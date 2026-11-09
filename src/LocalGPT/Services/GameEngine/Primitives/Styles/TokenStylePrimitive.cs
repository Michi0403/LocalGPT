using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Styles;

/// <summary>
/// Style <c>token-style</c>: colours every case-insensitive occurrence of the declared <c>tokens</c>, in token order.
/// </summary>
/// <param name="parameters">Reads the rule parameters.</param>
/// <param name="styles">Reads the declared style.</param>
/// <param name="logger">Receives style diagnostics.</param>
public sealed class TokenStylePrimitive(
    IGameParameterReader parameters,
    IGameStyleReader styles,
    ILogger<TokenStylePrimitive> logger) : IGameStylePrimitive
{
    /// <inheritdoc />
    public string Key => "token-style";

    /// <inheritdoc />
    public void AddRuns(GameProjectRule rule, int row, string line, List<CouncilAsciiStyleRun> runs)
    {
        try
        {
            var ruleParameters = parameters.Parse(rule.ParametersJson);
            var style = styles.Read(parameters.ReadObject(ruleParameters, "style"));
            foreach (var token in parameters.ReadStrings(ruleParameters, "tokens"))
            {
                if (token.Length == 0)
                    continue;
                var search = 0;
                while (search < line.Length && runs.Count < styles.MaximumRuns)
                {
                    var index = line.IndexOf(token, search, StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                        break;
                    runs.Add(new CouncilAsciiStyleRun { Y = row, X = index, Length = token.Length, Style = style });
                    search = index + Math.Max(1, token.Length);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying style rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }
}
