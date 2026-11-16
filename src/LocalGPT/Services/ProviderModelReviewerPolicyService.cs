using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>Owns the provider-model reviewer ranking policy so frontend defaults and benchmark execution cannot drift.</summary>
/// <param name="optionsRoot">Application configuration whose ordered PreferredCouncilModelKeys rank reviewers.</param>
/// <param name="logger">Logger used for reviewer-policy diagnostics.</param>
public sealed class ProviderModelReviewerPolicyService(
    IOptionsMonitor<BusinessObjects.ConfigurationRoot> optionsRoot,
    ILogger<ProviderModelReviewerPolicyService> logger) : IProviderModelReviewerPolicyService
{
    /// <summary>
    /// Retrieves priority as part of the provider model reviewer policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public int GetPriority(ProviderModelReference model)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(model);
            // The owner's ordered "Preferred Council models" decide the reviewer order. No model family is
            // favoured in code; models that are not in the preferred list share one neutral rank after them.
            var preferred = optionsRoot.CurrentValue.AICore?.PreferredCouncilModelKeys ?? [];
            var index = preferred.FindIndex(key =>
                string.Equals(key, model.SelectionKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, model.ModelName, StringComparison.OrdinalIgnoreCase));
            return index >= 0 ? index : preferred.Count;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ranking a provider-qualified reviewer failed; provider identity was omitted.");
            throw;
        }
    }
}
