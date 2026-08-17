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
    /// <summary>Fetches one explicitly selected CanIRun.ai device page and returns bounded model recommendations with attribution.</summary>
    /// <param name="deviceSlug">Device slug value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <param name="userConfirmedWebLookup">Value indicating whether user confirmed web lookup should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CanIRunModelRecommendation>> GetRecommendationsAsync(string deviceSlug, bool userConfirmedWebLookup, CancellationToken cancellationToken = default);
    /// <summary>Derives an editable initial CanIRun.ai slug from a hardware display name.</summary>
    /// <param name="hardwareName">Hardware name value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string SuggestDeviceSlug(string hardwareName);
}

/// <summary>Runs knowledge-backed local AI provider/model bootstrap operations through the common LocalGPT console engine.</summary>
public interface IAiProviderBootstrapService
{
    /// <summary>Returns provider bootstrap profiles for the current platform from the Knowledge Database.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<AiProviderBootstrapProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);
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
    /// <summary>
    /// Performs start as part of the AI provider bootstrap service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
