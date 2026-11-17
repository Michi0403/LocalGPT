using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Normalizes architecture descriptions at the coordinated source-manifest boundary without interpreting source or executing model JSON.</summary>
public sealed partial class MultiModelCouncilService
{
    /// <summary>Resolves the run-local output budget for configured preparation steps.</summary>
    /// <param name="teamKey">Configured team identity.</param>
    /// <param name="stepKey">Configured workflow step.</param>
    /// <returns>Optional ceiling; other teams and steps keep their existing policy.</returns>
    private int? GetFileWorkPreparationOutputCeiling(string teamKey, string stepKey)
    {
        try
        {
            var policy = runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson");
            return policy.Enabled && string.Equals(teamKey, policy.TeamKey, StringComparison.OrdinalIgnoreCase) &&
                (policy.PreparationStepKeys ?? []).Contains(stepKey, StringComparer.OrdinalIgnoreCase)
                ? Math.Max(64, policy.MaximumPreparationOutputTokens) : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to resolve the configured source preparation budget.");
            throw;
        }
    }

    /// <summary>Accepts shared contracts expressed as text or a JSON object; the persisted draft still owns one textual representation.</summary>
    /// <param name="answer">Visible architect response, excluding model thinking.</param>
    /// <returns>Parsed manifest for subsequent domain validation by the existing draft gateway.</returns>
    private CreateCodeGenerationReviewRequest ReadSourceManifest(string answer)
    {
        try
        {
            var fields = ReadFileWorkJson<Dictionary<string, JsonElement>>(answer);
            var normalized = new Dictionary<string, JsonElement>(fields, StringComparer.OrdinalIgnoreCase);
            if (normalized.TryGetValue("draftSharedContract", out var contract) &&
                contract.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                normalized["draftSharedContract"] = JsonSerializer.SerializeToElement(contract.GetRawText());
            return JsonSerializer.Deserialize<CreateCodeGenerationReviewRequest>(JsonSerializer.Serialize(normalized),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new JsonException("Missing source manifest.");
        }
        catch (Exception exception)
        {
            logger.LogDebug("Source manifest normalization failed with {Category}.", exception.GetType().Name);
            throw;
        }
    }
}
