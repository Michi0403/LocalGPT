using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Styles;

/// <summary>
/// Style <c>character-style</c>: colours every run of the declared <c>characters</c>.
/// </summary>
/// <param name="parameters">Reads the rule parameters.</param>
/// <param name="styles">Reads the declared style.</param>
/// <param name="logger">Receives style diagnostics.</param>
public sealed class CharacterStylePrimitive(
    IGameParameterReader parameters,
    IGameStyleReader styles,
    ILogger<CharacterStylePrimitive> logger) : IGameStylePrimitive
{
    /// <inheritdoc />
    public string Key => "character-style";

    /// <inheritdoc />
    public void AddRuns(GameProjectRule rule, int row, string line, List<CouncilAsciiStyleRun> runs)
    {
        try
        {
            var ruleParameters = parameters.Parse(rule.ParametersJson);
            var characters = parameters.ReadString(ruleParameters, "characters", string.Empty);
            if (characters.Length == 0)
                return;
            var style = styles.Read(parameters.ReadObject(ruleParameters, "style"));
            var start = -1;
            for (var index = 0; index <= line.Length; index++)
            {
                var matches = index < line.Length && characters.IndexOf(line[index]) >= 0;
                if (matches && start < 0)
                    start = index;
                if ((!matches || index == line.Length) && start >= 0)
                {
                    runs.Add(new CouncilAsciiStyleRun { Y = row, X = start, Length = index - start, Style = style });
                    start = -1;
                    if (runs.Count >= styles.MaximumRuns)
                        return;
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
