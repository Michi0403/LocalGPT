using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Owns the provider-model reviewer ranking policy so frontend defaults and benchmark execution cannot drift.</summary>
/// <param name="logger">Logger used for reviewer-policy diagnostics.</param>
public sealed class ProviderModelReviewerPolicyService(ILogger<ProviderModelReviewerPolicyService> logger) : IProviderModelReviewerPolicyService
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
            var name = model.ModelName ?? string.Empty;
            if (name.Equals("gpt-oss:20b", StringComparison.OrdinalIgnoreCase)) return 0;
            if (name.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase)) return 1;
            if (name.Contains("qwen", StringComparison.OrdinalIgnoreCase) && name.Contains("coder", StringComparison.OrdinalIgnoreCase)) return 2;
            if (name.Contains("deepseek", StringComparison.OrdinalIgnoreCase) && name.Contains("coder", StringComparison.OrdinalIgnoreCase)) return 3;
            if (name.Contains("openthinker", StringComparison.OrdinalIgnoreCase) || name.Contains("qwen", StringComparison.OrdinalIgnoreCase) || name.Contains("gemma", StringComparison.OrdinalIgnoreCase)) return 4;
            if (name.Contains("deepscaler", StringComparison.OrdinalIgnoreCase) || name.Contains("1.5b", StringComparison.OrdinalIgnoreCase) || name.Contains("0.8b", StringComparison.OrdinalIgnoreCase)) return 20;
            return 10;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ranking a provider-qualified reviewer failed; provider identity was omitted.");
            throw;
        }
    }
}
