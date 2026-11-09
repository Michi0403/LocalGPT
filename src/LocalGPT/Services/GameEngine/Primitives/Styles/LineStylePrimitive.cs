using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Styles;

/// <summary>
/// Style <c>line-style</c>: colours the non-blank span of the declared <c>rows</c> and of every row containing one of <c>containsAny</c>.
/// </summary>
/// <param name="parameters">Reads the rule parameters.</param>
/// <param name="styles">Reads the declared style.</param>
/// <param name="logger">Receives style diagnostics.</param>
public sealed class LineStylePrimitive(
    IGameParameterReader parameters,
    IGameStyleReader styles,
    ILogger<LineStylePrimitive> logger) : IGameStylePrimitive
{
    /// <inheritdoc />
    public string Key => "line-style";

    /// <inheritdoc />
    public void AddRuns(GameProjectRule rule, int row, string line, List<CouncilAsciiStyleRun> runs)
    {
        try
        {
            var ruleParameters = parameters.Parse(rule.ParametersJson);
            var selected = parameters.ReadInts(ruleParameters, "rows").Contains(row) ||
                parameters.ReadStrings(ruleParameters, "containsAny").Any(token => token.Length > 0 && line.Contains(token, StringComparison.OrdinalIgnoreCase));
            if (!selected)
                return;
            var first = 0;
            while (first < line.Length && char.IsWhiteSpace(line[first]))
                first++;
            var last = line.Length - 1;
            while (last >= first && char.IsWhiteSpace(line[last]))
                last--;
            if (last >= first)
                runs.Add(new CouncilAsciiStyleRun { Y = row, X = first, Length = last - first + 1, Style = styles.Read(parameters.ReadObject(ruleParameters, "style")) });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying style rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }
}
