using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates council code generation plan behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilCodeGenerationPlanService(
    ILogger<CouncilCodeGenerationPlanService> logger) : ICouncilCodeGenerationPlanService
{
    /// <summary>
    /// Defines the max embedded plan characters constant used by <see cref="CouncilCodeGenerationPlanService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxEmbeddedPlanCharacters = 4_000_000;

    /// <summary>
    /// Stores the internal tagged plan pattern state used by <see cref="CouncilCodeGenerationPlanService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex taggedPlanPattern = new(
        @"<localgpt-change-review>\s*(?<json>.*?)\s*</localgpt-change-review>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    /// <summary>
    /// Stores the internal fenced plan pattern state used by <see cref="CouncilCodeGenerationPlanService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex fencedPlanPattern = new(
        @"```(?:localgpt-change-review|json\s+localgpt-change-review)\s*(?<json>.*?)\s*```",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Performs parse as part of the council code generation plan service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilAnswer">Council answer value supplied to the council code generation plan operation and used when producing its result.</param>
    /// <returns>The council code generation plan result produced by the operation.</returns>
    public CouncilCodeGenerationPlanResult Parse(string councilAnswer)
    {
        var operationId = Guid.NewGuid();
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId,
            ["Operation"] = "ParseCouncilCodeGenerationPlan"
        });

        if (string.IsNullOrWhiteSpace(councilAnswer))
        {
            logger.LogDebug("No council answer was supplied for code-generation plan parsing.");
            return new CouncilCodeGenerationPlanResult
            {
                Warning = "The council answer was empty, so LocalGPT used the bounded fallback generation plan."
            };
        }

        try
        {
            var candidates = taggedPlanPattern.Matches(councilAnswer)
                .Select(match => (Json: match.Groups["json"].Value, Format: "TaggedJson"))
                .Concat(fencedPlanPattern.Matches(councilAnswer)
                    .Select(match => (Json: match.Groups["json"].Value, Format: "FencedJson")))
                .ToList();

            foreach (var candidate in candidates.AsEnumerable().Reverse())
            {
                if (string.IsNullOrWhiteSpace(candidate.Json) || candidate.Json.Length > MaxEmbeddedPlanCharacters)
                    continue;

                try
                {
                    var payload = JsonSerializer.Deserialize<CodeGenerationReviewPayload>(candidate.Json, jsonOptions);
                    if (payload is null)
                        continue;

                    payload.Files ??= [];
                    payload.CodeDomTypes ??= [];
                    payload.Outputs ??= [];
                    if (payload.Files.Count == 0 && payload.CodeDomTypes.Count == 0 && payload.Outputs.Count == 0)
                    {
                        continue;
                    }

                    logger.LogInformation(
                        "Parsed council code-generation plan with {FileCount} file(s), {CodeDomTypeCount} CodeDOM type(s), and {OutputCount} output(s) from {SourceFormat}.",
                        payload.Files.Count,
                        payload.CodeDomTypes.Count,
                        payload.Outputs.Count,
                        candidate.Format);

                    return new CouncilCodeGenerationPlanResult
                    {
                        Found = true,
                        Payload = payload,
                        SourceFormat = candidate.Format
                    };
                }
                catch (JsonException ex)
                {
                    logger.LogDebug(ex, "Ignored one malformed embedded council change-review JSON block; block content was omitted from logs.");
                }
            }

            logger.LogInformation("No valid embedded council change-review JSON block was found; the bounded fallback plan will be used.");
            return new CouncilCodeGenerationPlanResult
            {
                Warning = "The council did not emit a valid structured change-review block, so LocalGPT used the bounded fallback generation plan."
            };
        }
        catch (RegexMatchTimeoutException ex)
        {
            logger.LogWarning(ex, "Council change-review plan parsing timed out; answer content was omitted from logs.");
            return new CouncilCodeGenerationPlanResult
            {
                Warning = "Structured change-review parsing timed out, so LocalGPT used the bounded fallback generation plan."
            };
        }
    }
}
