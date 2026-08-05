using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

public sealed partial class CouncilTextService
{
    public string ProviderModelBenchmarkCouncilSignature(
        IEnumerable<ProviderModelReference> models,
        ILogger logger)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(models);
            return string.Join("\n", models
                .Select(model => model.SelectionKey)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the provider-model benchmark selection signature failed.");
            return string.Empty;
        }
    }

    public string ProviderModelReviewerSummary(
        ProviderModelReference model,
        IEnumerable<ProviderModelReference> councilModels,
        ILogger logger)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(councilModels);
            var reviewers = councilModels
                .Where(item => !item.SelectionKey.Equals(model.SelectionKey, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .Select(item => item.DisplayName)
                .ToList();
            return reviewers.Count == 0
                ? "deterministic checks plus bounded self-review"
                : string.Join(" + ", reviewers);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the provider-model benchmark reviewer summary failed for {ModelIdentity}.", model?.StableId);
            return "deterministic checks plus bounded self-review";
        }
    }
}
