using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates council text behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class CouncilTextService
{
    /// <summary>
    /// Performs provider model benchmark council signature as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="models">Provider model reference dependency used by the council text workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs provider model reviewer summary as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Model value supplied to the council text operation and used when producing its result.</param>
    /// <param name="councilModels">Provider model reference dependency used by the council text workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs provider unavailable selection notice as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="unavailable">String dependency used by the council text workflow to provide the corresponding application capability.</param>
    /// <param name="selectionScope">Selection scope value supplied to the council text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ProviderUnavailableSelectionNotice(
        IReadOnlyCollection<string> unavailable,
        string selectionScope,
        ILogger logger)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(unavailable);
            ArgumentException.ThrowIfNullOrWhiteSpace(selectionScope);

            var preview = string.Join("; ", unavailable.Take(3));
            var remainder = unavailable.Count > 3
                ? $"; +{unavailable.Count - 3} more"
                : string.Empty;
            return $"{selectionScope}: kept {unavailable.Count} unavailable provider-qualified route(s) visible for review ({preview}{remainder}). Refresh/reconfigure the exact host or remove the red selection; no same-name fallback will be used.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the unavailable provider-selection notice failed for {SelectionScope}.", selectionScope);
            return $"{selectionScope}: one or more provider-qualified routes are unavailable and remain visible for review. Refresh/reconfigure the exact host or remove the red selection; no same-name fallback will be used.";
        }
    }

    /// <summary>
    /// Performs provider unavailable run notice as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="unavailable">String dependency used by the council text workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ProviderUnavailableRunNotice(IReadOnlyCollection<string> unavailable, ILogger logger)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(unavailable);
            var preview = string.Join("; ", unavailable.Take(3));
            var remainder = unavailable.Count > 3 ? $"; +{unavailable.Count - 3} more" : string.Empty;
            return $"AI Council did not start because {unavailable.Count} selected provider-qualified route(s) are unavailable: {preview}{remainder}. They are marked red in Chat configuration. Refresh/reconfigure the exact provider host or remove those selections; LocalGPT will not substitute a same-name model from another endpoint.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the unavailable Council-run notice failed.");
            return "AI Council did not start because one or more selected provider routes are unavailable. Review the red entries in Chat configuration and refresh/reconfigure or remove them.";
        }
    }
}
