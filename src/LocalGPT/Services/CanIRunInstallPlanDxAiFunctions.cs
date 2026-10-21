using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Converts source-attributed CanIRun model identifiers into verified local provider installation candidates, without guessing a repository or initiating a download.</summary>
/// <param name="providers">Operator-configured, knowledge-backed local provider installers.</param>
/// <param name="json">Shared DXFunction parameter binding and result serialization.</param>
public sealed class CanIRunInstallPlanFunction(IAiProviderBootstrapService providers, IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    /// <summary>Read-only model/provider join; actual provider installation remains independently human-approved.</summary>
    /// <value>The registered DXFunction description.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.canirun.install.plan", "POST", "/api/dxai/functions/initial.setup.canirun.install.plan/invoke",
        "Matches a CanIRun recommendation's explicit provider-qualified model id with installed-platform Knowledge Database provider profiles. Returns exact approved-installer arguments only when the provider mapping is known; does not claim arbitrary model compatibility or install automatically.",
        "recommendation is the unmodified attributed CanIRunModelRecommendation returned by the approved lookup.",
        "Read-only configuration inspection; execute the proposed initial.setup.provider.model.install through its separate human approval gate.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["recommendation"],"properties":{"recommendation":{"type":"object"}},"additionalProperties":false}""");

    /// <summary>Joins the recommended Ollama token to real supported local installer profiles.</summary>
    /// <param name="request">Source-attributed recommendation supplied by the AI, not trusted as executable input.</param>
    /// <param name="cancellationToken">Cancels knowledge-backed provider discovery.</param>
    /// <returns>A non-executing candidate list with explicit unverified-capability warnings.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<CanIRunInstallPlanParameters>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        var recommendation = binding.Value.Recommendation;
        if (string.IsNullOrWhiteSpace(recommendation.ModelId))
            return json.InvalidParameters("The CanIRun recommendation needs a nonempty source model id.");
        var profiles = await providers.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
        List<CanIRunProviderInstallCandidate> candidates = string.IsNullOrWhiteSpace(recommendation.OllamaModelId)
            ? []
            : profiles.Where(providers.IsOllamaProfile)
                .Select(profile => new CanIRunProviderInstallCandidate
                {
                    ProfileKey = profile.Key,
                    ModelId = recommendation.OllamaModelId,
                    FunctionName = "initial.setup.provider.model.install"
                }).ToList();
        return json.Success(new
        {
            recommendation.ModelId, recommendation.ModelName, recommendation.OllamaModelId,
            recommendation.LmStudioModelId, recommendation.Grade, recommendation.Score,
            recommendation.RequiredVramGiB, recommendation.MemoryPlanSummary, recommendation.SourceUrl,
            ProviderInstallCandidates = candidates,
            HasApprovedExecutableProviderMapping = candidates.Count > 0,
            RequiresFreshInstallationApproval = true,
            HasVerifiedRuntimeCompatibility = false,
            UnsupportedDirectInstall = candidates.Count == 0,
            Diagnostic = candidates.Count > 0
                ? "Provider-specific model token is available; verify actual runtime availability after separately approved installation. CanIRun is a suitability estimate, not a runtime probe."
                : "No executable provider-specific mapping could be established. Inspect provider/source adapters; do not fabricate a Hugging Face id or a model install command."
        });
    }
}

/// <summary>Parameters for the non-mutating CanIRun-to-provider installer join.</summary>
public sealed class CanIRunInstallPlanParameters
{
    /// <summary>Complete recommendation with source-attributed model and provider tokens.</summary>
    /// <value>The recommendation returned by the separately approved lookup.</value>
    public CanIRunModelRecommendation Recommendation { get; set; } = new();
}

/// <summary>Ready-to-review provider-model install arguments; no execution takes place.</summary>
public sealed class CanIRunProviderInstallCandidate
{
    /// <summary>Operator-configured knowledge-backed local provider profile key.</summary>
    /// <value>Provider configuration key.</value>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>Source-attributed provider-specific model token.</summary>
    /// <value>Exact install model id.</value>
    public string ModelId { get; set; } = string.Empty;
    /// <summary>Existing approval-gated DXFunction for this provider-model installation.</summary>
    /// <value>Function registry key.</value>
    public string FunctionName { get; set; } = string.Empty;
}
