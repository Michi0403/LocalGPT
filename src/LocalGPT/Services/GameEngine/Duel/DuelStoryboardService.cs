using System.Globalization;
using System.Text.Json;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Renders storyboard frames from rule rows with the trigger <c>storyboard.{phase}</c>. Each row declares an optional
/// <c>when</c> condition and value specifications for <c>headline</c>, <c>body</c>, <c>footer</c>, <c>leftPose</c>,
/// <c>rightPose</c>, <c>leftHealth</c>, <c>rightHealth</c>, <c>center</c>, <c>hud</c>, <c>leftStatement</c> and <c>rightStatement</c>.
/// </summary>
/// <param name="parameters">Reads the frame rows.</param>
/// <param name="evaluator">Evaluates conditions and value specifications.</param>
/// <param name="frames">Renders the frames.</param>
/// <param name="logger">Receives storyboard diagnostics.</param>
public sealed class DuelStoryboardService(
    IGameParameterReader parameters,
    IGameValueSpecEvaluator evaluator,
    IDuelFrameFactory frames,
    ILogger<DuelStoryboardService> logger) : IDuelStoryboardService
{
    /// <inheritdoc />
    public List<GameViewFrame> Build(GameEngineContext context, DuelSettings settings, string phase, GameActorState? left, GameActorState? right, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            var result = new List<GameViewFrame>();
            var trigger = $"storyboard.{phase}";
            foreach (var row in context.Ruleset.Rules
                .Where(rule => rule.IsEnabled && string.Equals(rule.Trigger, trigger, StringComparison.OrdinalIgnoreCase))
                .OrderBy(rule => rule.SortOrder))
            {
                var frame = parameters.Parse(row.ParametersJson);
                if (!evaluator.Test(parameters.ReadString(frame, "when", string.Empty), tokens))
                    continue;
                var leftPose = Value(frame, "leftPose", tokens);
                var rightPose = Value(frame, "rightPose", tokens);
                result.Add(frames.Make(context, settings, new DuelFrameRequest
                {
                    Headline = Value(frame, "headline", tokens),
                    Left = left,
                    Right = right,
                    Body = Value(frame, "body", tokens),
                    Footer = Value(frame, "footer", tokens),
                    LeftPose = leftPose.Length == 0 ? "idle" : leftPose,
                    RightPose = rightPose.Length == 0 ? "idle" : rightPose,
                    LeftHealth = Number(Value(frame, "leftHealth", tokens)),
                    RightHealth = Number(Value(frame, "rightHealth", tokens)),
                    CenterEffect = parameters.TryRead(frame, "center", out _) ? Value(frame, "center", tokens) : "VS",
                    HudMotif = Value(frame, "hud", tokens),
                    LeftStatement = Value(frame, "leftStatement", tokens),
                    RightStatement = Value(frame, "rightStatement", tokens)
                }));
            }
            logger.LogDebug("Duel storyboard phase {Phase} rendered {FrameCount} frame(s).", phase, result.Count);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building duel storyboard phase {Phase} failed.", phase);
            throw;
        }
    }

    /// <summary>
    /// Evaluates one frame field.
    /// </summary>
    /// <param name="frame">The frame row parameters.</param>
    /// <param name="name">The field name.</param>
    /// <param name="tokens">The storyboard tokens.</param>
    /// <returns>The field text, or an empty string when the field is absent.</returns>
    private string Value(JsonElement frame, string name, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            return parameters.TryRead(frame, name, out var specification) ? evaluator.Evaluate(specification, tokens) : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluating duel frame field {FieldName} failed.", name);
            throw;
        }
    }

    /// <summary>
    /// Parses an optional health override.
    /// </summary>
    /// <param name="value">The evaluated text.</param>
    /// <returns>The number, or <c>null</c> for an empty or non-numeric value.</returns>
    private int? Number(string value)
    {
        try
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing a duel health override failed.");
            throw;
        }
    }
}
