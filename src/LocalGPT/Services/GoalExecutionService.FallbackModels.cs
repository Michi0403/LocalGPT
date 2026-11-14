using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// Installed-model fallback ordering for goals that have neither explicit model selections nor a preferred model.
/// Larger models are tried first; models below a configurable parameter count are only used when too few larger ones exist.
/// The size comes from data the inventory already holds (the Ollama parameter size in the candidate details, otherwise
/// the model tag) and is parsed with the maintained <c>runtime.model.size-token</c> pattern; no network call is made.
/// </summary>
public sealed partial class GoalExecutionService
{
    /// <summary>Runtime value: minimum billions of parameters a fallback model should have.</summary>
    private const string FallbackMinimumModelBillionParametersKey = "GoalFallbackMinimumModelBillionParameters";

    /// <summary>Runtime pattern that extracts a parameter-count token such as 7b or 20.9B from model text.</summary>
    private const string ModelSizeTokenPatternKey = "runtime.model.size-token";

    /// <summary>Orders installed model candidates for the goal fallback: preferred key index, adequate size, loaded state, then size descending.</summary>
    /// <param name="candidates">Installed or configured model candidates.</param>
    /// <param name="preferredKeys">User-preferred Council model selection keys in priority order.</param>
    /// <param name="minimumBillions">Models below this many billion parameters (and not preferred) are placed last.</param>
    /// <param name="smallKeys">Receives the selection keys of the non-preferred models below the minimum.</param>
    /// <returns>The candidates in fallback order.</returns>
    private List<MultiModelCouncilModelCandidate> OrderFallbackCandidates(
        IEnumerable<MultiModelCouncilModelCandidate> candidates,
        List<string> preferredKeys,
        int minimumBillions,
        out HashSet<string> smallKeys)
    {
        var small = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var scored = candidates.Select(candidate =>
            {
                var key = candidate.SelectionKey;
                var index = preferredKeys.FindIndex(value => string.Equals(value, key, StringComparison.OrdinalIgnoreCase));
                var billions = EstimateModelBillionParameters(candidate);
                var isSmall = index < 0 && billions is not null && billions.Value < minimumBillions;
                if (isSmall)
                    small.Add(key);
                return (Candidate: candidate, Key: key, Index: index >= 0 ? index : int.MaxValue, IsSmall: isSmall, Billions: billions ?? minimumBillions);
            }).ToList();
            smallKeys = small;
            return scored
                .OrderBy(entry => entry.Index)
                .ThenBy(entry => entry.IsSmall)
                .ThenByDescending(entry => entry.Candidate.IsLoaded)
                .ThenByDescending(entry => entry.Billions)
                .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(entry => entry.Candidate)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ordering the goal fallback model candidates failed.");
            throw;
        }
    }

    /// <summary>Estimates the parameter count of a model from the candidate details, then from the model tag.</summary>
    /// <param name="candidate">The model candidate.</param>
    /// <returns>Approximate billions of parameters, or <see langword="null"/> when no size is known.</returns>
    private double? EstimateModelBillionParameters(MultiModelCouncilModelCandidate candidate)
    {
        try
        {
            var pattern = runtimePolicy.GetPattern(ModelSizeTokenPatternKey);
            foreach (var text in new[] { candidate.Details ?? string.Empty, candidate.ModelName ?? string.Empty })
            {
                var match = pattern.Match(text);
                if (match.Success
                    && double.TryParse(match.Groups["size"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var size))
                    return match.Groups["unit"].Value.Equals("m", StringComparison.OrdinalIgnoreCase) ? size / 1000d : size;
            }
            return null;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Estimating a fallback model parameter count failed; the model is treated as size-unknown.");
            return null;
        }
    }

    /// <summary>Reads the configured minimum model size in billions of parameters for the goal fallback.</summary>
    /// <returns>The configured value, or 7 when the runtime value is unavailable.</returns>
    private int ReadFallbackMinimumModelBillionParameters()
    {
        try
        {
            return Math.Max(0, runtimePolicy.GetInt(FallbackMinimumModelBillionParametersKey));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Runtime value {Key} is unavailable; the goal model fallback uses a minimum of 7 billion parameters.", FallbackMinimumModelBillionParametersKey);
            return 7;
        }
    }
}
