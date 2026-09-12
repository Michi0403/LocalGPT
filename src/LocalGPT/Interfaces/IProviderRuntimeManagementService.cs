using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Provides provider-supported model lifecycle, storage, token, and local-network controls for the LocalGPT provider workbench.</summary>
public interface IProviderRuntimeManagementService
{
    /// <summary>Loads one detached management snapshot for the selected provider profile.</summary>
    /// <param name="profile">Selected provider bootstrap profile.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider-management snapshot.</returns>
    Task<ProviderRuntimeManagementSnapshot> GetSnapshotAsync(AiProviderBootstrapProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists local GPT token defaults as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="maxOutputTokens">Default maximum response-token budget.</param>
    /// <param name="contextTokens">Default context-token budget.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes after both values are persisted.</returns>
    Task SaveLocalGptTokenDefaultsAsync(int maxOutputTokens, int contextTokens, CancellationToken cancellationToken = default);

    /// <summary>Persists LocalGPT-owned Ollama launch settings without moving existing provider files implicitly.</summary>
    /// <param name="options">Reviewed Ollama launch settings.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes after durable configuration is written.</returns>
    Task SaveOllamaRuntimeOptionsAsync(OllamaRuntimeManagementOptions options, CancellationToken cancellationToken = default);

    /// <summary>Persists LocalGPT-owned LM Studio load/server defaults.</summary>
    /// <param name="options">Reviewed LM Studio management settings.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes after durable configuration is written.</returns>
    Task SaveLmStudioRuntimeOptionsAsync(LmStudioRuntimeManagementOptions options, CancellationToken cancellationToken = default);

    /// <summary>Unloads one model from provider memory without deleting its downloaded files.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <param name="modelId">Provider-native model identifier.</param>
    /// <param name="userConfirmed">Whether the human explicitly confirmed the operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The model-management result.</returns>
    Task<ProviderModelManagementResult> UnloadModelAsync(AiProviderBootstrapProfile profile, string modelId, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Permanently deletes one Ollama model through Ollama's documented model-delete API.</summary>
    /// <param name="profile">Selected Ollama provider profile.</param>
    /// <param name="modelId">Provider-native model identifier.</param>
    /// <param name="userConfirmed">Whether the human explicitly confirmed permanent deletion.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The model-management result.</returns>
    Task<ProviderModelManagementResult> DeleteModelAsync(AiProviderBootstrapProfile profile, string modelId, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Estimates LM Studio memory requirements using the documented <c>lms load --estimate-only</c> path.</summary>
    /// <param name="profile">Selected LM Studio provider profile.</param>
    /// <param name="modelId">Downloaded LM Studio model key.</param>
    /// <param name="options">Reviewed LM Studio load settings.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider estimation output.</returns>
    Task<ProviderModelManagementResult> EstimateLmStudioModelAsync(AiProviderBootstrapProfile profile, string modelId, LmStudioRuntimeManagementOptions options, CancellationToken cancellationToken = default);

    /// <summary>Loads one LM Studio model using reviewed context, GPU-offload, and TTL defaults.</summary>
    /// <param name="profile">Selected LM Studio provider profile.</param>
    /// <param name="modelId">Downloaded LM Studio model key.</param>
    /// <param name="options">Reviewed LM Studio load settings.</param>
    /// <param name="userConfirmed">Whether the human explicitly confirmed loading the model.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The model-management result.</returns>
    Task<ProviderModelManagementResult> LoadLmStudioModelAsync(AiProviderBootstrapProfile profile, string modelId, LmStudioRuntimeManagementOptions options, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Restarts the LM Studio HTTP server through the documented <c>lms server</c> CLI using reviewed bind/CORS settings.</summary>
    /// <param name="profile">Selected LM Studio provider profile.</param>
    /// <param name="options">Reviewed LM Studio server settings.</param>
    /// <param name="userConfirmed">Whether the human explicitly confirmed the server restart.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider-management result.</returns>
    Task<ProviderModelManagementResult> RestartLmStudioServerAsync(AiProviderBootstrapProfile profile, LmStudioRuntimeManagementOptions options, bool userConfirmed, CancellationToken cancellationToken = default);
}
