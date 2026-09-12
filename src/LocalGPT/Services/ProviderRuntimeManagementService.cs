using System.Net.Http.Json;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>Coordinates documented Ollama and LM Studio model lifecycle, storage, token, and local-network controls for the provider-management UI.</summary>
/// <param name="httpClientFactory">Factory used for provider-local HTTP lifecycle calls.</param>
/// <param name="console">Shared bounded command service used for documented LM Studio CLI operations.</param>
/// <param name="ollamaPlatform">Platform boundary used to resolve Ollama's default model store.</param>
/// <param name="lmStudioPlatform">Platform boundary used to resolve the LM Studio CLI executable.</param>
/// <param name="optionsRoot">Current LocalGPT configuration.</param>
/// <param name="configurationWriter">Durable LocalGPT user-configuration writer.</param>
/// <param name="providerRegistry">Provider configuration registry used to create detached safe drafts.</param>
/// <param name="variables">Database-backed LocalGPT system-variable store.</param>
/// <param name="systemVariables">Strongly typed system-variable definitions.</param>
/// <param name="logger">Logger used for bounded diagnostics that omit model identifiers and paths where appropriate.</param>
public sealed class ProviderRuntimeManagementService(
    IHttpClientFactory httpClientFactory,
    IConsoleCommandService console,
    IOllamaPlatformService ollamaPlatform,
    ILmStudioPlatformService lmStudioPlatform,
    IOptionsMonitor<LocalGptConfigurationRoot> optionsRoot,
    IConfigurationWriter configurationWriter,
    IAiProviderConfigurationRegistryService providerRegistry,
    IVariableStoreService variables,
    ISystemVariableDefinitionService systemVariables,
    ILogger<ProviderRuntimeManagementService> logger) : IProviderRuntimeManagementService
{
    /// <summary>Ollama generation route used for unload requests that set <c>keep_alive</c> to zero.</summary>
    private const string OllamaGenerateRoute = "/api/generate";
    /// <summary>Ollama model deletion route used only after explicit destructive-action confirmation.</summary>
    private const string OllamaDeleteRoute = "/api/delete";

    /// <summary>
    /// Retrieves snapshot as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProviderRuntimeManagementSnapshot> GetSnapshotAsync(AiProviderBootstrapProfile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(profile);
            var isOllama = IsOllama(profile);
            var isLmStudio = IsLmStudio(profile);
            var current = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
            var snapshot = new ProviderRuntimeManagementSnapshot
            {
                ProfileKey = profile.Key,
                ProviderName = profile.DisplayName,
                IsOllama = isOllama,
                IsLmStudio = isLmStudio,
                Ollama = Clone(current.OllamaRuntime),
                LmStudio = Clone(current.LmStudioRuntime),
                DefaultMaxOutputTokens = await variables.GetAsync<int>(systemVariables.DefaultMaxOutputTokens.Name, cancellationToken).ConfigureAwait(false),
                DefaultContextTokens = await variables.GetAsync<int>(systemVariables.DefaultContextTokens.Name, cancellationToken).ConfigureAwait(false)
            };

            if (isOllama)
                await PopulateOllamaAsync(profile, snapshot, cancellationToken).ConfigureAwait(false);
            else if (isLmStudio)
                await PopulateLmStudioAsync(snapshot, cancellationToken).ConfigureAwait(false);
            else
                snapshot.Status = "This provider is OpenAI-compatible, but LocalGPT has no provider-specific local disk lifecycle API for it.";

            snapshot.InstalledModelBytes = snapshot.Models.Where(item => item.SizeBytes.HasValue).Sum(item => item.SizeBytes!.Value);
            PopulateDiskCapacity(snapshot);
            return snapshot;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Refreshing provider runtime management was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Refreshing provider runtime management failed; provider paths and model identifiers were omitted.");
            throw;
        }
    }

    /// <summary>
    /// Persists local GPT token defaults as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task SaveLocalGptTokenDefaultsAsync(int maxOutputTokens, int contextTokens, CancellationToken cancellationToken = default)
    {
        try
        {
            var minOutput = await variables.GetAsync<int>(systemVariables.MinOutputTokens.Name, cancellationToken).ConfigureAwait(false);
            var maxOutput = await variables.GetAsync<int>(systemVariables.MaxOutputTokens.Name, cancellationToken).ConfigureAwait(false);
            var minContext = await variables.GetAsync<int>(systemVariables.MinContextTokens.Name, cancellationToken).ConfigureAwait(false);
            var maxContext = await variables.GetAsync<int>(systemVariables.MaxContextTokens.Name, cancellationToken).ConfigureAwait(false);
            if (maxOutputTokens < minOutput || maxOutputTokens > maxOutput)
                throw new ArgumentOutOfRangeException(nameof(maxOutputTokens), $"Output tokens must be between {minOutput} and {maxOutput}.");
            if (contextTokens < minContext || contextTokens > maxContext)
                throw new ArgumentOutOfRangeException(nameof(contextTokens), $"Context tokens must be between {minContext} and {maxContext}.");
            await variables.SetAsync(systemVariables.DefaultMaxOutputTokens.Name, maxOutputTokens, cancellationToken).ConfigureAwait(false);
            await variables.SetAsync(systemVariables.DefaultContextTokens.Name, contextTokens, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Saving LocalGPT token defaults was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving LocalGPT token defaults failed.");
            throw;
        }
    }

    /// <summary>
    /// Persists Ollama runtime options as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task SaveOllamaRuntimeOptionsAsync(OllamaRuntimeManagementOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(options);
            ValidatePort(options.Port);
            ValidateNonNegative(options.ContextLengthTokens, nameof(options.ContextLengthTokens));
            ValidateNonNegative(options.MaxLoadedModels, nameof(options.MaxLoadedModels));
            ValidateNonNegative(options.ParallelRequests, nameof(options.ParallelRequests));
            ValidateNonNegative(options.MaxQueue, nameof(options.MaxQueue));
            if (!string.IsNullOrWhiteSpace(options.ModelDirectory))
            {
                var fullPath = NormalizeModelDirectory(options.ModelDirectory);
                Directory.CreateDirectory(fullPath);
                options.ModelDirectory = fullPath;
            }
            options.BindAddress = NormalizeBindAddress(options.BindAddress);
            options.KeepAlive = options.KeepAlive.Trim();
            await SaveAiCoreAsync(draft =>
            {
                var previousPort = draft.OllamaRuntime?.Port is > 0 and <= 65535 ? draft.OllamaRuntime.Port : 11434;
                draft.OllamaRuntime = Clone(options);
                SynchronizeLocalOllamaEndpoints(draft, previousPort, options);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Saving Ollama runtime options was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving Ollama runtime options failed; model directory was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Persists lm studio runtime options as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task SaveLmStudioRuntimeOptionsAsync(LmStudioRuntimeManagementOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(options);
            ValidatePort(options.Port);
            ValidateNonNegative(options.ContextLengthTokens, nameof(options.ContextLengthTokens));
            ValidateNonNegative(options.TtlSeconds, nameof(options.TtlSeconds));
            options.BindAddress = NormalizeBindAddress(options.BindAddress);
            options.GpuOffload = NormalizeGpuOffload(options.GpuOffload);
            await SaveAiCoreAsync(draft =>
            {
                var previousPort = draft.LmStudioRuntime?.Port is > 0 and <= 65535 ? draft.LmStudioRuntime.Port : 1234;
                draft.LmStudioRuntime = Clone(options);
                SynchronizeLocalLmStudioEndpoints(draft, previousPort, options);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Saving LM Studio runtime options was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving LM Studio runtime options failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs unload model as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProviderModelManagementResult> UnloadModelAsync(AiProviderBootstrapProfile profile, string modelId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireConfirmation(userConfirmed);
            ValidateModelId(modelId);
            if (IsOllama(profile))
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, BuildProviderUri(profile, OllamaGenerateRoute))
                {
                    Content = JsonContent.Create(new { model = modelId.Trim(), keep_alive = 0 })
                };
                using var response = await SendProviderRequestAsync(request, cancellationToken).ConfigureAwait(false);
                return FromResponse(response, "Ollama model unloaded from memory.");
            }
            if (IsLmStudio(profile))
                return await ExecuteLmStudioAsync("Unload LM Studio model", ["unload", modelId.Trim()], isReadOnly: false, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            throw new NotSupportedException("The selected provider does not expose a documented LocalGPT unload bridge.");
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Unloading provider model was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unloading provider model failed; model identifier was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Deletes model as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProviderModelManagementResult> DeleteModelAsync(AiProviderBootstrapProfile profile, string modelId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireConfirmation(userConfirmed);
            ValidateModelId(modelId);
            if (!IsOllama(profile))
                throw new NotSupportedException("Permanent model deletion is exposed here only for Ollama because LM Studio does not document a downloaded-model delete CLI/API. Use LM Studio My Models for permanent LM Studio removal.");
            using var request = new HttpRequestMessage(HttpMethod.Delete, BuildProviderUri(profile, OllamaDeleteRoute))
            {
                Content = JsonContent.Create(new { model = modelId.Trim() })
            };
            using var response = await SendProviderRequestAsync(request, cancellationToken).ConfigureAwait(false);
            return FromResponse(response, "Ollama model permanently removed from the local model store.");
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Deleting provider model was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deleting provider model failed; model identifier was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs estimate lm studio model as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProviderModelManagementResult> EstimateLmStudioModelAsync(AiProviderBootstrapProfile profile, string modelId, LmStudioRuntimeManagementOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireLmStudio(profile);
            ArgumentNullException.ThrowIfNull(options);
            ValidateModelId(modelId);
            var args = BuildLmStudioLoadArguments(modelId, options, estimateOnly: true);
            return await ExecuteLmStudioAsync("Estimate LM Studio model resources", args, isReadOnly: true, userConfirmed: false, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Estimating LM Studio model resources was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Estimating LM Studio model resources failed; model identifier was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Loads lm studio model as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProviderModelManagementResult> LoadLmStudioModelAsync(AiProviderBootstrapProfile profile, string modelId, LmStudioRuntimeManagementOptions options, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireConfirmation(userConfirmed);
            RequireLmStudio(profile);
            ArgumentNullException.ThrowIfNull(options);
            ValidateModelId(modelId);
            var args = BuildLmStudioLoadArguments(modelId, options, estimateOnly: false);
            return await ExecuteLmStudioAsync("Load LM Studio model", args, isReadOnly: false, userConfirmed: true, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading LM Studio model was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading LM Studio model failed; model identifier was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs restart lm studio server as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProviderModelManagementResult> RestartLmStudioServerAsync(AiProviderBootstrapProfile profile, LmStudioRuntimeManagementOptions options, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireConfirmation(userConfirmed);
            RequireLmStudio(profile);
            ArgumentNullException.ThrowIfNull(options);
            ValidatePort(options.Port);
            var bind = NormalizeBindAddress(options.BindAddress);
            _ = await ExecuteLmStudioAsync("Stop LM Studio server", ["server", "stop"], isReadOnly: false, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            var args = new List<string> { "server", "start", "--bind", bind, "--port", options.Port.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            if (options.EnableCors)
                args.Add("--cors");
            return await ExecuteLmStudioAsync("Start LM Studio server", args, isReadOnly: false, userConfirmed: true, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Restarting LM Studio server was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Restarting LM Studio server failed.");
            throw;
        }
    }

    /// <summary>Populates Ollama downloaded and running model inventory through documented local HTTP endpoints.</summary>
    /// <param name="profile">Profile value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="snapshot">Snapshot value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PopulateOllamaAsync(AiProviderBootstrapProfile profile, ProviderRuntimeManagementSnapshot snapshot, CancellationToken cancellationToken)
    {
    try
    {
                snapshot.ModelDirectoryEditable = true;
                snapshot.ModelDirectory = string.IsNullOrWhiteSpace(snapshot.Ollama.ModelDirectory)
                    ? ollamaPlatform.ResolveDefaultModelDirectory() ?? string.Empty
                    : snapshot.Ollama.ModelDirectory;
                snapshot.ModelDirectoryGuidance = "Changing the directory affects LocalGPT-started Ollama after restart. Existing model files are not moved implicitly; relocate them deliberately while Ollama is stopped.";
                try
                {
                    using var tagsRequest = new HttpRequestMessage(HttpMethod.Get, BuildProviderUri(profile, "/api/tags"));
                    using var tagsResponse = await SendProviderRequestAsync(tagsRequest, cancellationToken).ConfigureAwait(false);
                    if (!tagsResponse.IsSuccessStatusCode)
                    {
                        snapshot.Status = $"Ollama inventory returned HTTP {(int)tagsResponse.StatusCode}.";
                        return;
                    }
                    using var tagsDoc = JsonDocument.Parse(await tagsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                    var loaded = await ReadOllamaLoadedModelsAsync(profile, cancellationToken).ConfigureAwait(false);
                    if (tagsDoc.RootElement.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in models.EnumerateArray())
                        {
                            var id = GetString(item, "name", "model");
                            if (string.IsNullOrWhiteSpace(id))
                                continue;
                            var details = item.TryGetProperty("details", out var detailsElement) ? detailsElement : default;
                            snapshot.Models.Add(new ProviderManagedModelInfo
                            {
                                ModelId = id,
                                DisplayName = id,
                                SizeBytes = GetInt64(item, "size"),
                                ParameterSize = details.ValueKind == JsonValueKind.Object ? GetString(details, "parameter_size") : string.Empty,
                                Architecture = details.ValueKind == JsonValueKind.Object ? GetString(details, "family", "families") : string.Empty,
                                IsLoaded = loaded.Contains(id),
                                CanUnload = loaded.Contains(id),
                                CanDelete = true
                            });
                        }
                    }
                    snapshot.Status = $"Loaded {snapshot.Models.Count} Ollama model(s); {snapshot.Models.Count(item => item.IsLoaded)} currently in memory.";
                }
                catch (HttpRequestException)
                {
                    snapshot.Status = "Ollama is not reachable; saved runtime/storage settings remain editable and will apply to a LocalGPT-started Ollama process.";
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    snapshot.Status = "Ollama did not answer before the local provider timeout; saved runtime/storage settings remain editable and will apply to a LocalGPT-started Ollama process.";
                }
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.PopulateOllamaAsync failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Populates LM Studio inventory through the documented machine-readable CLI without assuming its on-disk directory layout.</summary>
    /// <param name="snapshot">Snapshot value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PopulateLmStudioAsync(ProviderRuntimeManagementSnapshot snapshot, CancellationToken cancellationToken)
    {
    try
    {
                snapshot.ModelDirectoryEditable = false;
                snapshot.ModelDirectory = "Managed by LM Studio → My Models";
                snapshot.ModelDirectoryGuidance = "LM Studio's documented CLI reflects the directory selected in My Models, but does not expose a supported command to change or permanently delete downloaded model files. LocalGPT therefore never reverse-engineers or deletes that storage behind LM Studio's back.";
                var executable = lmStudioPlatform.ResolveExecutable();
                if (string.IsNullOrWhiteSpace(executable))
                {
                    snapshot.Status = "LM Studio CLI (lms) was not found. Install/enable the LM Studio CLI to use model inventory and lifecycle controls.";
                    return;
                }

                var list = await ExecuteLmStudioRawAsync("List LM Studio downloaded models", ["ls", "--json", "--detailed"], isReadOnly: true, userConfirmed: false, cancellationToken).ConfigureAwait(false);
                if (!list.Succeeded)
                {
                    snapshot.Status = "LM Studio downloaded-model inventory failed; review the shared ASCII console.";
                    return;
                }
                var loadedResult = await ExecuteLmStudioRawAsync("List loaded LM Studio models", ["ps", "--json"], isReadOnly: true, userConfirmed: false, cancellationToken).ConfigureAwait(false);
                var loadedKeys = loadedResult.Succeeded ? ReadLmStudioIdentifiers(loadedResult.StandardOutput) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in ReadLmStudioDownloadedModels(list.StandardOutput))
                {
                    item.IsLoaded = loadedKeys.Contains(item.ModelId) || (!string.IsNullOrWhiteSpace(item.ProviderPath) && loadedKeys.Contains(item.ProviderPath));
                    item.CanUnload = item.IsLoaded;
                    item.CanDelete = false;
                    snapshot.Models.Add(item);
                }
                snapshot.Status = $"Loaded {snapshot.Models.Count} LM Studio downloaded model(s); {snapshot.Models.Count(item => item.IsLoaded)} currently in memory.";
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.PopulateLmStudioAsync failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Reads the currently loaded Ollama model names from the documented <c>/api/ps</c> endpoint.</summary>
    /// <param name="profile">Profile value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The hash set string produced by the operation.</returns>
    private async Task<HashSet<string>> ReadOllamaLoadedModelsAsync(AiProviderBootstrapProfile profile, CancellationToken cancellationToken)
    {
    try
    {
                var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using var request = new HttpRequestMessage(HttpMethod.Get, BuildProviderUri(profile, "/api/ps"));
                using var response = await SendProviderRequestAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return values;
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                if (!document.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                    return values;
                foreach (var item in models.EnumerateArray())
                {
                    var id = GetString(item, "name", "model");
                    if (!string.IsNullOrWhiteSpace(id))
                        values.Add(id);
                }
                return values;
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.ReadOllamaLoadedModelsAsync failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Returns model entries from LM Studio JSON in either array or object-wrapped shapes.</summary>
    /// <param name="json">Json value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<ProviderManagedModelInfo> ReadLmStudioDownloadedModels(string json)
    {
    try
    {
                var values = new List<ProviderManagedModelInfo>();
                if (string.IsNullOrWhiteSpace(json))
                    return values;
                using var document = JsonDocument.Parse(json);
                foreach (var item in EnumerateModelObjects(document.RootElement))
                {
                    var id = GetString(item, "modelKey", "model_key", "identifier", "key");
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    values.Add(new ProviderManagedModelInfo
                    {
                        ModelId = id,
                        DisplayName = GetString(item, "displayName", "display_name") is { Length: > 0 } name ? name : id,
                        ProviderPath = GetString(item, "path"),
                        SizeBytes = GetInt64(item, "sizeBytes", "size_bytes", "size"),
                        ParameterSize = GetString(item, "paramsString", "params_string", "params"),
                        Architecture = GetString(item, "architecture"),
                        MaxContextLengthTokens = GetInt32(item, "maxContextLength", "max_context_length"),
                        CanDelete = false
                    });
                }
                return values
                    .GroupBy(item => item.ModelId, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderByDescending(item => item.SizeBytes ?? 0)
                    .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.ReadLmStudioDownloadedModels failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Returns loaded LM Studio identifiers/path keys from machine-readable process output.</summary>
    /// <param name="json">Json value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The hash set string produced by the operation.</returns>
    private HashSet<string> ReadLmStudioIdentifiers(string json)
    {
    try
    {
                var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (string.IsNullOrWhiteSpace(json))
                    return values;
                using var document = JsonDocument.Parse(json);
                foreach (var item in EnumerateModelObjects(document.RootElement))
                {
                    foreach (var property in new[] { "identifier", "modelKey", "model_key", "path", "key" })
                    {
                        var value = GetString(item, property);
                        if (!string.IsNullOrWhiteSpace(value))
                            values.Add(value);
                    }
                }
                return values;
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.ReadLmStudioIdentifiers failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Collects model-shaped objects from direct arrays or common object wrappers without binding to an undocumented LM Studio JSON wrapper.</summary>
    /// <param name="root">Root JSON value whose direct or wrapped arrays may contain model objects.</param>
    /// <returns>A materialized list of model objects that remains valid for the lifetime of the owning JSON document.</returns>
    private IReadOnlyList<JsonElement> EnumerateModelObjects(JsonElement root)
    {
        try
        {
            var items = new List<JsonElement>();
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        items.Add(item);
                }
                return items;
            }

            if (root.ValueKind != JsonValueKind.Object)
                return items;

            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var item in property.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        items.Add(item);
                }
            }
            return items;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.EnumerateModelObjects failed while reading provider model inventory JSON.");
            throw;
        }
    }

    /// <summary>Builds the documented LM Studio load command without shell string interpolation.</summary>
    /// <param name="modelId">Identifier of the model to use for this operation.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="estimateOnly">Value indicating whether estimate only should apply to this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> BuildLmStudioLoadArguments(string modelId, LmStudioRuntimeManagementOptions options, bool estimateOnly)
    {
    try
    {
                var args = new List<string> { "load", modelId.Trim() };
                if (estimateOnly)
                    args.Add("--estimate-only");
                if (options.ContextLengthTokens > 0)
                {
                    args.Add("--context-length");
                    args.Add(options.ContextLengthTokens.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                var gpu = NormalizeGpuOffload(options.GpuOffload);
                if (!gpu.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    args.Add("--gpu");
                    args.Add(gpu);
                }
                if (options.TtlSeconds > 0 && !estimateOnly)
                {
                    args.Add("--ttl");
                    args.Add(options.TtlSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                return args;
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.BuildLmStudioLoadArguments failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Executes one direct LM Studio CLI operation and converts it to a bounded UI result.</summary>
    /// <param name="displayName">Display name value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="arguments">Arguments value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="isReadOnly">Value indicating whether is read only should apply to this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider model management result produced by the operation.</returns>
    private async Task<ProviderModelManagementResult> ExecuteLmStudioAsync(string displayName, List<string> arguments, bool isReadOnly, bool userConfirmed, CancellationToken cancellationToken)
    {
    try
    {
                var result = await ExecuteLmStudioRawAsync(displayName, arguments, isReadOnly, userConfirmed, cancellationToken).ConfigureAwait(false);
                var output = string.IsNullOrWhiteSpace(result.StandardOutput) ? result.StandardError : result.StandardOutput;
                return new ProviderModelManagementResult
                {
                    Succeeded = result.Succeeded,
                    Message = string.IsNullOrWhiteSpace(output) ? result.Status : output.Trim()
                };
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.ExecuteLmStudioAsync failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Executes one LM Studio CLI operation through the direct executable mode so model identifiers never become shell syntax.</summary>
    /// <param name="displayName">Display name value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="arguments">Arguments value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="isReadOnly">Value indicating whether is read only should apply to this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    private async Task<LocalConsoleCommandResult> ExecuteLmStudioRawAsync(string displayName, List<string> arguments, bool isReadOnly, bool userConfirmed, CancellationToken cancellationToken)
    {
    try
    {
                var executable = lmStudioPlatform.ResolveExecutable() ?? throw new FileNotFoundException("LM Studio CLI (lms) was not found in a supported location or PATH.");
                return await console.ExecuteAsync(new LocalConsoleCommandRequest
                {
                    DisplayName = displayName,
                    Shell = LocalConsoleShellKind.Direct,
                    Executable = executable,
                    Arguments = arguments,
                    IsReadOnly = isReadOnly,
                    UserConfirmed = userConfirmed,
                    TimeoutSeconds = isReadOnly ? 60 : 1800
                }, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.ExecuteLmStudioRawAsync failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Sends one bounded local-provider HTTP request without following a provider-supplied redirect to another host.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP response message produced by the operation.</returns>
    private async Task<HttpResponseMessage> SendProviderRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
    try
    {
                var client = httpClientFactory.CreateClient("LocalGPTProviderRuntime");
                return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.SendProviderRequestAsync failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Builds one provider-local API URI from a reviewed absolute profile endpoint.</summary>
    /// <param name="profile">Profile value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="relativePath">Relative path value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The URI produced by the operation.</returns>
    private Uri BuildProviderUri(AiProviderBootstrapProfile profile, string relativePath)
    {
    try
    {
                if (!Uri.TryCreate(profile.Endpoint, UriKind.Absolute, out var baseUri))
                    throw new InvalidOperationException("The selected provider endpoint is not an absolute URI.");
                if (!baseUri.IsLoopback)
                    throw new InvalidOperationException("Provider model lifecycle management is limited to loopback provider profiles. Configure remote hosts separately.");
                return new Uri(baseUri, relativePath);
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.BuildProviderUri failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>
    /// Performs from response as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="response">Response value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="successMessage">Success message value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The provider model management result produced by the operation.</returns>
    private ProviderModelManagementResult FromResponse(HttpResponseMessage response, string successMessage) {
    try
    {
        return new()
    {
        Succeeded = response.IsSuccessStatusCode,
        Message = response.IsSuccessStatusCode ? successMessage : $"Provider returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase})."
    };
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.FromResponse failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Persists one detached AICore mutation without replacing unrelated top-level configuration sections.</summary>
    /// <param name="mutate">Mutate value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveAiCoreAsync(Action<AICoreOptions> mutate, CancellationToken cancellationToken)
    {
    try
    {
                var current = optionsRoot.CurrentValue;
                var draft = providerRegistry.CreateDetachedDraft(current.AICore);
                mutate(draft);
                await configurationWriter.SaveAsync(new LocalGptConfigurationRoot
                {
                    LoggingCore = current.LoggingCore,
                    PythonCore = current.PythonCore,
                    ConnectionStringsCore = current.ConnectionStringsCore,
                    AICore = draft,
                    LocalGPT = current.LocalGPT
                }, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.SaveAiCoreAsync failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Populates disk capacity using the longest mounted-drive path that contains the configured Ollama model directory.</summary>
    /// <param name="snapshot">Snapshot value supplied to the provider runtime management operation and used when producing its result.</param>
    private void PopulateDiskCapacity(ProviderRuntimeManagementSnapshot snapshot)
    {
    try
    {
                if (!snapshot.ModelDirectoryEditable || string.IsNullOrWhiteSpace(snapshot.ModelDirectory))
                    return;
                try
                {
                    var full = Path.GetFullPath(snapshot.ModelDirectory);
                    var drive = DriveInfo.GetDrives()
                        .Where(item => item.IsReady && full.StartsWith(Path.GetFullPath(item.RootDirectory.FullName), StringComparison.Ordinal))
                        .OrderByDescending(item => item.RootDirectory.FullName.Length)
                        .FirstOrDefault();
                    if (drive is null)
                        return;
                    snapshot.DiskFreeBytes = drive.AvailableFreeSpace;
                    snapshot.DiskTotalBytes = drive.TotalSize;
                }
                catch
                {
                    // Disk capacity is informational only and must never block provider management.
                }
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.PopulateDiskCapacity failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Normalizes one Ollama model directory without moving or deleting provider-owned files.</summary>
    /// <param name="value">Value value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeModelDirectory(string value)
    {
    try
    {
                var trimmed = value.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    return string.Empty;
                var expanded = Environment.ExpandEnvironmentVariables(trimmed);
                if (expanded.Equals("~", StringComparison.Ordinal) || expanded.StartsWith("~/", StringComparison.Ordinal) || expanded.StartsWith("~\\", StringComparison.Ordinal))
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (string.IsNullOrWhiteSpace(home))
                        throw new InvalidOperationException("The current user's home directory could not be resolved for the Ollama model store.");
                    expanded = expanded.Length == 1 ? home : Path.Combine(home, expanded[2..]);
                }
                var fullPath = Path.GetFullPath(expanded);
                if (fullPath == Path.GetPathRoot(fullPath))
                    throw new InvalidOperationException("The filesystem root cannot be used as an Ollama model store through LocalGPT.");
                return fullPath;
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.NormalizeModelDirectory failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Synchronizes local Ollama provider bindings when LocalGPT changes the port of the LocalGPT-managed runtime.</summary>
    /// <param name="draft">Detached AI-core draft being persisted.</param>
    /// <param name="previousPort">Previously configured LocalGPT-managed Ollama port.</param>
    /// <param name="options">New reviewed Ollama runtime settings.</param>
    private void SynchronizeLocalOllamaEndpoints(AICoreOptions draft, int previousPort, OllamaRuntimeManagementOptions options)
    {
    try
    {
                var endpoint = BuildLocalClientEndpoint(options.BindAddress, options.Port, openAiCompatible: false);
                if (IsManagedLoopbackEndpoint(draft.OllamaCore?.Uri, previousPort, openAiCompatible: false))
                    draft.OllamaCore!.Uri = endpoint;
                foreach (var item in draft.OllamaCores ?? [])
                    if (IsManagedLoopbackEndpoint(item.Uri, previousPort, openAiCompatible: false))
                        item.Uri = endpoint;
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.SynchronizeLocalOllamaEndpoints failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Synchronizes LM Studio-tagged local OpenAI-compatible bindings when its LocalGPT-managed server port changes.</summary>
    /// <param name="draft">Detached AI-core draft being persisted.</param>
    /// <param name="previousPort">Previously configured LocalGPT-managed LM Studio port.</param>
    /// <param name="options">New reviewed LM Studio runtime settings.</param>
    private void SynchronizeLocalLmStudioEndpoints(AICoreOptions draft, int previousPort, LmStudioRuntimeManagementOptions options)
    {
    try
    {
                var endpoint = BuildLocalClientEndpoint(options.BindAddress, options.Port, openAiCompatible: true);
                bool IsLmStudioBinding(ChatGPTLocalCoreOptions item, int oldPort) =>
                    string.Equals(item.ApiKey, "lm-studio", StringComparison.OrdinalIgnoreCase)
                    || IsManagedLoopbackEndpoint(item.Endpoint, oldPort, openAiCompatible: true);

                if (draft.ChatGPTLocalCore is not null && IsLmStudioBinding(draft.ChatGPTLocalCore, previousPort))
                    draft.ChatGPTLocalCore.Endpoint = endpoint;
                foreach (var item in draft.ChatGPTLocalCores ?? [])
                    if (IsLmStudioBinding(item, previousPort))
                        item.Endpoint = endpoint;
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.SynchronizeLocalLmStudioEndpoints failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Builds the client endpoint LocalGPT should use for a locally managed provider after a bind/port change.</summary>
    /// <param name="bindAddress">Reviewed provider bind address.</param>
    /// <param name="port">Reviewed provider port.</param>
    /// <param name="openAiCompatible">Whether the endpoint requires the OpenAI-compatible <c>/v1</c> suffix.</param>
    /// <returns>The absolute local provider endpoint.</returns>
    private string BuildLocalClientEndpoint(string bindAddress, int port, bool openAiCompatible)
    {
    try
    {
                var host = bindAddress.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : bindAddress;
                return $"http://{host}:{port}{(openAiCompatible ? "/v1" : string.Empty)}";
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.BuildLocalClientEndpoint failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Determines whether a configured provider endpoint is the local binding owned by the previous runtime port.</summary>
    /// <param name="value">Configured provider endpoint.</param>
    /// <param name="port">Previously configured runtime port.</param>
    /// <param name="openAiCompatible">Whether an OpenAI-compatible <c>/v1</c> endpoint is expected.</param>
    /// <returns><see langword="true"/> when the endpoint is loopback and matches the previous managed port.</returns>
    private bool IsManagedLoopbackEndpoint(string? value, int port, bool openAiCompatible)
    {
    try
    {
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Port != port)
                    return false;
                if (!openAiCompatible)
                    return true;
                return uri.AbsolutePath.TrimEnd('/').Equals("/v1", StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath is "" or "/";
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.IsManagedLoopbackEndpoint failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Normalizes a bind address while rejecting URI/path syntax.</summary>
    /// <param name="value">Value value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeBindAddress(string value)
    {
    try
    {
                var trimmed = string.IsNullOrWhiteSpace(value) ? "127.0.0.1" : value.Trim();
                if (trimmed.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    return "127.0.0.1";
                if (trimmed is "127.0.0.1" or "0.0.0.0")
                    return trimmed;
                throw new InvalidOperationException("LocalGPT-managed provider servers may bind to 127.0.0.1 (this Mac/PC only) or 0.0.0.0 (LAN/all IPv4 interfaces). Configure arbitrary remote hosts as separate provider endpoints instead.");
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.NormalizeBindAddress failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Normalizes an LM Studio GPU-offload setting to the documented value family.</summary>
    /// <param name="value">Value value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeGpuOffload(string value)
    {
    try
    {
                var trimmed = string.IsNullOrWhiteSpace(value) ? "auto" : value.Trim().ToLowerInvariant();
                if (trimmed is "auto" or "off" or "max")
                    return trimmed;
                if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fraction)
                    && fraction is >= 0 and <= 1)
                    return fraction.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                throw new InvalidOperationException("LM Studio GPU offload must be auto, off, max, or a value from 0 through 1.");
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.NormalizeGpuOffload failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Validates a provider model key as one non-control argument rather than shell content.</summary>
    /// <param name="modelId">Identifier of the model to use for this operation.</param>
    private void ValidateModelId(string modelId)
    {
    try
    {
                ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
                if (modelId.Length > 512 || modelId.Any(char.IsControl))
                    throw new InvalidDataException("The provider model identifier is not a valid direct-process argument.");
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.ValidateModelId failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Requires explicit human confirmation for consequential lifecycle actions.</summary>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    private void RequireConfirmation(bool userConfirmed)
    {
    try
    {
                if (!userConfirmed)
                    throw new InvalidOperationException("This provider lifecycle action requires explicit user confirmation.");
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.RequireConfirmation failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Requires an LM Studio provider profile.</summary>
    /// <param name="profile">Profile value supplied to the provider runtime management operation and used when producing its result.</param>
    private void RequireLmStudio(AiProviderBootstrapProfile profile)
    {
    try
    {
                if (!IsLmStudio(profile))
                    throw new NotSupportedException("The selected provider profile is not LM Studio.");
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.RequireLmStudio failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>
    /// Validates port as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="port">Port value supplied to the provider runtime management operation and used when producing its result.</param>
    private void ValidatePort(int port)
    {
    try
    {
                if (port is <= 0 or > 65535)
                    throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.ValidatePort failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>
    /// Validates non negative as part of the provider runtime management service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="parameterName">Parameter name value supplied to the provider runtime management operation and used when producing its result.</param>
    private void ValidateNonNegative(int value, string parameterName)
    {
    try
    {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.ValidateNonNegative failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Returns whether one bootstrap profile describes Ollama.</summary>
    /// <param name="profile">Profile value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsOllama(AiProviderBootstrapProfile profile) {
    try
    {
        return profile.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
        || profile.ProviderKind.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
        || profile.Key.StartsWith("ollama-", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.IsOllama failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Returns whether one bootstrap profile describes LM Studio rather than another OpenAI-compatible host.</summary>
    /// <param name="profile">Profile value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsLmStudio(AiProviderBootstrapProfile profile) {
    try
    {
        return profile.Key.StartsWith("lmstudio-", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.IsLmStudio failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Clones Ollama runtime settings for UI editing.</summary>
    /// <param name="source">Source value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The Ollama runtime management options produced by the operation.</returns>
    private OllamaRuntimeManagementOptions Clone(OllamaRuntimeManagementOptions? source) {
    try
    {
        return new()
    {
        ModelDirectory = source?.ModelDirectory ?? string.Empty,
        BindAddress = source?.BindAddress ?? "127.0.0.1",
        Port = source?.Port ?? 11434,
        ContextLengthTokens = source?.ContextLengthTokens ?? 0,
        KeepAlive = source?.KeepAlive ?? string.Empty,
        MaxLoadedModels = source?.MaxLoadedModels ?? 0,
        ParallelRequests = source?.ParallelRequests ?? 0,
        MaxQueue = source?.MaxQueue ?? 0,
        DisableCloud = source?.DisableCloud ?? false
    };
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.Clone failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Clones LM Studio runtime settings for UI editing.</summary>
    /// <param name="source">Source value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The lm studio runtime management options produced by the operation.</returns>
    private LmStudioRuntimeManagementOptions Clone(LmStudioRuntimeManagementOptions? source) {
    try
    {
        return new()
    {
        BindAddress = source?.BindAddress ?? "127.0.0.1",
        Port = source?.Port ?? 1234,
        EnableCors = source?.EnableCors ?? false,
        ContextLengthTokens = source?.ContextLengthTokens ?? 0,
        GpuOffload = source?.GpuOffload ?? "auto",
        TtlSeconds = source?.TtlSeconds ?? 0
    };
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.Clone failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Gets the first string-valued property matching one of the requested JSON names.</summary>
    /// <param name="element">Element value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="names">Names value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GetString(JsonElement element, params string[] names)
    {
    try
    {
                if (element.ValueKind != JsonValueKind.Object)
                    return string.Empty;
                foreach (var name in names)
                {
                    if (!element.TryGetProperty(name, out var value))
                        continue;
                    if (value.ValueKind == JsonValueKind.String)
                        return value.GetString() ?? string.Empty;
                    if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                        return value.ToString();
                }
                return string.Empty;
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.GetString failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Gets the first 32-bit integral JSON property matching one of the requested names.</summary>
    /// <param name="element">JSON object containing provider inventory metadata.</param>
    /// <param name="names">Candidate property names ordered by preference.</param>
    /// <returns>The first integral value that fits in a 32-bit integer, or <see langword="null"/> when none is present.</returns>
    private int? GetInt32(JsonElement element, params string[] names)
    {
        try
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;
            foreach (var name in names)
            {
                if (!element.TryGetProperty(name, out var value))
                    continue;
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                    return number;
            }
            return null;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.GetInt32 failed; caller-controlled path/model values were omitted.");
            throw;
        }
    }

    /// <summary>Gets the first integral JSON property matching one of the requested names.</summary>
    /// <param name="element">Element value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <param name="names">Names value supplied to the provider runtime management operation and used when producing its result.</param>
    /// <returns>The long produced by the operation.</returns>
    private long? GetInt64(JsonElement element, params string[] names)
    {
    try
    {
                if (element.ValueKind != JsonValueKind.Object)
                    return null;
                foreach (var name in names)
                {
                    if (!element.TryGetProperty(name, out var value))
                        continue;
                    if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
                        return number;
                }
                return null;
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper ProviderRuntimeManagementService.GetInt64 failed; caller-controlled path/model values were omitted.");
        throw;
    }
}
}
