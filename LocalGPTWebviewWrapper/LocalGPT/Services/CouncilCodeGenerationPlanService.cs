using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

public sealed class CouncilCodeGenerationPlanService(
    ILogger<CouncilCodeGenerationPlanService> logger) : ICouncilCodeGenerationPlanService
{
    private const int MaxEmbeddedPlanCharacters = 4_000_000;

    private readonly Regex taggedPlanPattern = new(
        @"<localgpt-change-review>\s*(?<json>.*?)\s*</localgpt-change-review>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    private readonly Regex fencedPlanPattern = new(
        @"```(?:localgpt-change-review|json\s+localgpt-change-review)\s*(?<json>.*?)\s*```",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

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
