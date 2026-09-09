using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Coordinates LocalGPT's user-driven first-run hardware, provider, model and benchmark preparation workflow.</summary>
public interface IInitialSetupAssistantService
{
    /// <summary>
    /// Retrieves snapshot as part of the initial setup assistant service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The initial setup assistant snapshot produced by the operation.</returns>
    Task<InitialSetupAssistantSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    /// <summary>Builds provider-specific model choices by resolving recommendation aliases and matching currently installed provider models.</summary>
    /// <param name="profileKey">Profile key value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="recommendations">Can i run model recommendation dependency used by the initial setup assistant workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<InitialSetupModelChoice>> BuildModelChoicesAsync(string profileKey, IReadOnlyList<CanIRunModelRecommendation> recommendations, CancellationToken cancellationToken = default);
    /// <summary>Searches the selected provider's official public model catalog after an explicit user action, independently from CanIRun.ai.</summary>
    /// <param name="profileKey">Selected platform/provider bootstrap profile.</param>
    /// <param name="query">Optional provider-catalog search text. An empty value requests the provider's default catalog view.</param>
    /// <param name="userConfirmedWebLookup">Value indicating whether the user explicitly requested the provider-owned network lookup.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>Bounded provider-safe catalog choices that may be installed through the existing provider bootstrap path.</returns>
    Task<IReadOnlyList<InitialSetupModelChoice>> SearchProviderCatalogAsync(string profileKey, string query, bool userConfirmedWebLookup, CancellationToken cancellationToken = default);
    /// <summary>Resolves one CanIRun.ai hardware-fit recommendation against the selected provider's official catalog after an explicit user action.</summary>
    /// <param name="profileKey">Selected platform/provider bootstrap profile.</param>
    /// <param name="recommendationId">Attributed CanIRun.ai recommendation identifier.</param>
    /// <param name="displayName">Human-readable recommendation name used only as an additional conservative identity candidate.</param>
    /// <param name="userConfirmedWebLookup">Value indicating whether the user explicitly requested the provider-owned network lookup.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The bounded catalog candidates together with a unique provider-safe match when one can be proven conservatively.</returns>
    Task<InitialSetupProviderCatalogResolution> ResolveProviderCatalogRecommendationAsync(string profileKey, string recommendationId, string displayName, bool userConfirmedWebLookup, CancellationToken cancellationToken = default);
    /// <summary>Loads optional attributed CanIRun.ai recommendations for each selected hardware row while preserving the local physical-host/endpoint association.</summary>
    /// <param name="devices">Initial setup hardware device dependency used by the initial setup assistant workflow to provide the corresponding application capability.</param>
    /// <param name="userConfirmedWebLookup">Value indicating whether user confirmed web lookup should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CanIRunModelRecommendation>> GetHardwareRecommendationsAsync(IReadOnlyList<InitialSetupHardwareDevice> devices, bool userConfirmedWebLookup, CancellationToken cancellationToken = default);
    /// <summary>Runs local read-only hardware probes and persists their reviewed host profile through the existing hardware service.</summary>
    /// <param name="endpoint">Endpoint value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The configured AI host hardware profile produced by the operation.</returns>
    Task<ConfiguredAiHostHardwareProfile> DetectHardwareAsync(string endpoint, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Imports a user-provided local HWiNFO text report and persists the parsed multi-GPU host profile.</summary>
    /// <param name="endpoint">Endpoint value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="reportText">Report text value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The configured AI host hardware profile produced by the operation.</returns>
    Task<ConfiguredAiHostHardwareProfile> ImportHwInfoAsync(string endpoint, string reportText, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Persists an explicit hardware device list for the physical host represented by an endpoint.</summary>
    /// <param name="endpoint">Endpoint value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="devices">Initial setup hardware device dependency used by the initial setup assistant workflow to provide the corresponding application capability.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The configured AI host hardware profile produced by the operation.</returns>
    Task<ConfiguredAiHostHardwareProfile> SaveHardwareAsync(string endpoint, IReadOnlyList<InitialSetupHardwareDevice> devices, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Persists a reviewed hardware list grouped by each device's own endpoint, using the fallback endpoint only for rows that have none.</summary>
    /// <param name="devices">Initial setup hardware device dependency used by the initial setup assistant workflow to provide the corresponding application capability.</param>
    /// <param name="fallbackEndpoint">Fallback endpoint value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<ConfiguredAiHostHardwareProfile>> SaveHardwareListAsync(IReadOnlyList<InitialSetupHardwareDevice> devices, string fallbackEndpoint, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Creates a user-owned benchmark team whose role pools use the selected provider-qualified models.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic council team definition produced by the operation.</returns>
    Task<OrganicCouncilTeamDefinition> CreateBenchmarkTeamAsync(CreateInitialBenchmarkTeamRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Fetches and parses explicitly requested CanIRun.ai hardware recommendations.</summary>
public interface ICanIRunHardwareRecommendationService
{
    /// <summary>Posts reviewed hardware facts to CanIRun.ai's JSON recommendation API and returns bounded attributed model recommendations.</summary>
    /// <param name="device">Reviewed hardware facts to send after opt-in; LocalGPT endpoint/host identifiers are not transmitted.</param>
    /// <param name="userConfirmedWebLookup">Value indicating whether user confirmed web lookup should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CanIRunModelRecommendation>> GetRecommendationsAsync(InitialSetupHardwareDevice device, bool userConfirmedWebLookup, CancellationToken cancellationToken = default);
    /// <summary>Retains the legacy editable slug helper for backwards-compatible callers; the 3.9 setup workflow no longer requires a slug.</summary>
    /// <param name="hardwareName">Hardware name value supplied to the compatibility helper.</param>
    /// <returns>A normalized legacy slug.</returns>
    string SuggestDeviceSlug(string hardwareName);
}

/// <summary>Runs knowledge-backed local AI provider/model bootstrap operations through LocalGPT runtime services and the common bounded console where appropriate.</summary>
public interface IAiProviderBootstrapService
{
    /// <summary>Returns provider bootstrap profiles for the current platform from the Knowledge Database.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<AiProviderBootstrapProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);
    /// <summary>Classifies a bootstrap profile through the provider service so UI/controller callers do not duplicate provider-key string policy.</summary>
    /// <param name="profile">Provider bootstrap profile to classify.</param>
    /// <returns><see langword="true"/> when the profile represents Ollama; otherwise <see langword="false"/>.</returns>
    bool IsOllamaProfile(AiProviderBootstrapProfile profile);
    /// <summary>Checks whether a provider's command-line runtime is available.</summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    Task<LocalConsoleCommandResult> DetectAsync(string profileKey, CancellationToken cancellationToken = default);
    /// <summary>Lists the selected provider's local model store through its knowledge-backed read-only command.</summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    Task<LocalConsoleCommandResult> ListModelsAsync(string profileKey, CancellationToken cancellationToken = default);
    /// <summary>Installs the selected provider after explicit user confirmation.</summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    Task<LocalConsoleCommandResult> InstallAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Updates the selected provider after explicit confirmation using its knowledge-backed update command, with the install command as a backward-compatible fallback.</summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    Task<LocalConsoleCommandResult> UpdateAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Starts the selected provider after explicit confirmation; Ollama uses its maintained process lifecycle service rather than a foreground <c>ollama serve</c> console command.</summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    Task<LocalConsoleCommandResult> StartAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Installs a provider-specific model after explicit user confirmation.</summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="modelId">Identifier of the model to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    Task<LocalConsoleCommandResult> InstallModelAsync(string profileKey, string modelId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Registers the provider profile endpoint in the existing LocalGPT AI provider configuration after explicit user confirmation.</summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> ConfigureEndpointAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Maps a generic recommendation identifier to a provider-specific install identifier using Knowledge Database aliases.</summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="recommendationId">Identifier of the recommendation to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> ResolveModelIdAsync(string profileKey, string recommendationId, CancellationToken cancellationToken = default);
}
