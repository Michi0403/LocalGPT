using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Keeps provider editing transactional. Installer edits operate on detached copies and a normal save is additive for
/// Ollama hosts; an existing endpoint is removed only through an explicit remove action.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class AiProviderConfigurationRegistryService(
    ILogger<AiProviderConfigurationRegistryService> logger) : IAiProviderConfigurationRegistryService
{
    /// <summary>
    /// Creates detached draft as part of the AI provider configuration registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the AI provider configuration registry operation and used when producing its result.</param>
    /// <returns>The AI core options produced by the operation.</returns>
    public AICoreOptions CreateDetachedDraft(AICoreOptions? source)
    {
        try
        {
            source ??= new AICoreOptions();

            OllamaCoreOptions CloneOllama(OllamaCoreOptions? item) => new()
            {
                Uri = item?.Uri ?? string.Empty,
                ModelName = item?.ModelName ?? string.Empty,
                ResponseProtocol = item?.ResponseProtocol ?? ChatResponseProtocol.Auto
            };

            ChatGPTLocalCoreOptions CloneLocalOpenAi(ChatGPTLocalCoreOptions? item) => new()
            {
                Endpoint = item?.Endpoint ?? string.Empty,
                ApiKey = item?.ApiKey ?? string.Empty,
                ModelName = item?.ModelName ?? string.Empty,
                AutoStartServer = item?.AutoStartServer ?? false,
                PythonEnvironment = item?.PythonEnvironment,
                StartScript = item?.StartScript,
                WorkingDir = item?.WorkingDir,
                StartCommand = item?.StartCommand,
                HealthTimeoutSeconds = item?.HealthTimeoutSeconds ?? 45
            };

            OpenAICompatOptions CloneOpenAi(OpenAICompatOptions? item) => new()
            {
                Endpoint = item?.Endpoint ?? string.Empty,
                ApiKey = item?.ApiKey ?? string.Empty,
                ModelName = item?.ModelName ?? string.Empty
            };

            OpenAIServiceCoreOptions CloneAzure(OpenAIServiceCoreOptions? item) => new()
            {
                Endpoint = item?.Endpoint ?? string.Empty,
                Key = item?.Key ?? string.Empty,
                DeploymentName = item?.DeploymentName ?? string.Empty
            };

            var providerIdentity = new ProviderModelIdentity();
            var detachedPrimaryOllama = CloneOllama(source.OllamaCore);
            detachedPrimaryOllama.Uri = providerIdentity.NormalizeEndpoint(detachedPrimaryOllama.Uri);
            var detachedAdditionalOllamas = new List<OllamaCoreOptions>();
            var detachedOllamaEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(detachedPrimaryOllama.Uri))
                detachedOllamaEndpoints.Add(detachedPrimaryOllama.Uri);
            foreach (var configured in source.OllamaCores ?? [])
            {
                var detached = CloneOllama(configured);
                detached.Uri = providerIdentity.NormalizeEndpoint(detached.Uri);
                if (string.IsNullOrWhiteSpace(detached.Uri) || !detachedOllamaEndpoints.Add(detached.Uri))
                    continue;
                detachedAdditionalOllamas.Add(detached);
            }

            var draft = new AICoreOptions
            {
                OllamaCore = detachedPrimaryOllama,
                OllamaCores = detachedAdditionalOllamas,
                ChatGPTLocalCore = CloneLocalOpenAi(source.ChatGPTLocalCore),
                ChatGPTLocalCores = (source.ChatGPTLocalCores ?? []).Select(CloneLocalOpenAi).ToList(),
                OpenAICore = CloneOpenAi(source.OpenAICore),
                OpenAIServiceCore = CloneAzure(source.OpenAIServiceCore)
            };

            logger.LogDebug(
                "Created detached AI provider configuration draft with {OllamaHostCount} Ollama host(s) and {OpenAiHostCount} local OpenAI-compatible host(s).",
                (string.IsNullOrWhiteSpace(draft.OllamaCore.Uri) ? 0 : 1) + draft.OllamaCores.Count,
                (string.IsNullOrWhiteSpace(draft.ChatGPTLocalCore.Endpoint) ? 0 : 1) + draft.ChatGPTLocalCores.Count);
            return draft;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a detached AI provider configuration draft failed.");
            throw;
        }
    }

    /// <summary>
    /// Applies detached draft as part of the AI provider configuration registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="target">Target value supplied to the AI provider configuration registry operation and used when producing its result.</param>
    /// <param name="draft">Draft value supplied to the AI provider configuration registry operation and used when producing its result.</param>
    /// <param name="removedOllamaEndpoints">String dependency used by the AI provider configuration registry workflow to provide the corresponding application capability.</param>
    /// <param name="explicitOllamaPrimaryEndpoint">Explicit ollama primary endpoint value supplied to the AI provider configuration registry operation and used when producing its result.</param>
    public void ApplyDetachedDraft(
        AICoreOptions target,
        AICoreOptions draft,
        IReadOnlyCollection<string> removedOllamaEndpoints,
        string? explicitOllamaPrimaryEndpoint)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(draft);
            removedOllamaEndpoints ??= Array.Empty<string>();

            var identity = new ProviderModelIdentity();

            string NormalizeOllama(string? endpoint) => string.IsNullOrWhiteSpace(endpoint)
                ? string.Empty
                : identity.NormalizeEndpoint(endpoint);

            OllamaCoreOptions CloneOllama(OllamaCoreOptions item) => new()
            {
                Uri = NormalizeOllama(item.Uri),
                ModelName = item.ModelName?.Trim() ?? string.Empty,
                ResponseProtocol = item.ResponseProtocol
            };

            ChatGPTLocalCoreOptions CloneLocalOpenAi(ChatGPTLocalCoreOptions? item) => new()
            {
                Endpoint = item?.Endpoint ?? string.Empty,
                ApiKey = item?.ApiKey ?? string.Empty,
                ModelName = item?.ModelName ?? string.Empty,
                AutoStartServer = item?.AutoStartServer ?? false,
                PythonEnvironment = item?.PythonEnvironment,
                StartScript = item?.StartScript,
                WorkingDir = item?.WorkingDir,
                StartCommand = item?.StartCommand,
                HealthTimeoutSeconds = item?.HealthTimeoutSeconds ?? 45
            };

            OpenAICompatOptions CloneOpenAi(OpenAICompatOptions? item) => new()
            {
                Endpoint = item?.Endpoint ?? string.Empty,
                ApiKey = item?.ApiKey ?? string.Empty,
                ModelName = item?.ModelName ?? string.Empty
            };

            OpenAIServiceCoreOptions CloneAzure(OpenAIServiceCoreOptions? item) => new()
            {
                Endpoint = item?.Endpoint ?? string.Empty,
                Key = item?.Key ?? string.Empty,
                DeploymentName = item?.DeploymentName ?? string.Empty
            };

            var removed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var endpoint in removedOllamaEndpoints)
            {
                var normalized = NormalizeOllama(endpoint);
                if (!string.IsNullOrWhiteSpace(normalized))
                    removed.Add(normalized);
            }

            var persistedPrimaryEndpoint = NormalizeOllama(target.OllamaCore?.Uri);
            var draftPrimaryEndpoint = NormalizeOllama(draft.OllamaCore?.Uri);
            var registry = new Dictionary<string, OllamaCoreOptions>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            void Store(OllamaCoreOptions? item, bool fromDraft)
            {
                if (item is null)
                    return;

                var endpoint = NormalizeOllama(item.Uri);
                if (string.IsNullOrWhiteSpace(endpoint))
                    return;

                if (!fromDraft && removed.Contains(endpoint))
                    return;

                if (fromDraft)
                    removed.Remove(endpoint);

                var incoming = CloneOllama(item);
                if (registry.TryGetValue(endpoint, out var existing))
                {
                    if (fromDraft)
                    {
                        if (string.IsNullOrWhiteSpace(incoming.ModelName) && !string.IsNullOrWhiteSpace(existing.ModelName))
                            incoming.ModelName = existing.ModelName;
                        registry[endpoint] = incoming;
                    }
                    return;
                }

                registry.Add(endpoint, incoming);
                order.Add(endpoint);
            }

            Store(target.OllamaCore, fromDraft: false);
            foreach (var item in target.OllamaCores ?? [])
                Store(item, fromDraft: false);
            Store(draft.OllamaCore, fromDraft: true);
            foreach (var item in draft.OllamaCores ?? [])
                Store(item, fromDraft: true);

            var explicitPrimary = NormalizeOllama(explicitOllamaPrimaryEndpoint);
            string primaryEndpoint;
            if (!string.IsNullOrWhiteSpace(explicitPrimary) && registry.ContainsKey(explicitPrimary))
                primaryEndpoint = explicitPrimary;
            else if (!string.IsNullOrWhiteSpace(persistedPrimaryEndpoint) && registry.ContainsKey(persistedPrimaryEndpoint))
                primaryEndpoint = persistedPrimaryEndpoint;
            else if (!string.IsNullOrWhiteSpace(draftPrimaryEndpoint) && registry.ContainsKey(draftPrimaryEndpoint))
                primaryEndpoint = draftPrimaryEndpoint;
            else
                primaryEndpoint = order.FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(primaryEndpoint))
            {
                target.OllamaCore = new OllamaCoreOptions { Uri = string.Empty, ModelName = string.Empty };
                target.OllamaCores = [];
            }
            else
            {
                target.OllamaCore = CloneOllama(registry[primaryEndpoint]);
                target.OllamaCores = order
                    .Where(endpoint => !endpoint.Equals(primaryEndpoint, StringComparison.OrdinalIgnoreCase))
                    .Select(endpoint => CloneOllama(registry[endpoint]))
                    .ToList();
            }

            // Other provider families remain normal detached editor fields. The important distinction is that none of
            // these objects alias IOptionsMonitor.CurrentValue while the user is editing the page.
            target.ChatGPTLocalCore = CloneLocalOpenAi(draft.ChatGPTLocalCore);
            target.ChatGPTLocalCores = (draft.ChatGPTLocalCores ?? []).Select(CloneLocalOpenAi).ToList();
            target.OpenAICore = CloneOpenAi(draft.OpenAICore);
            target.OpenAIServiceCore = CloneAzure(draft.OpenAIServiceCore);

            logger.LogInformation(
                "Applied detached AI provider draft; Ollama primary endpoint {PrimaryEndpoint} with {AdditionalCount} additional host(s) and {RemovedCount} explicit removal request(s).",
                target.OllamaCore.Uri,
                target.OllamaCores.Count,
                removedOllamaEndpoints.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying the detached AI provider configuration draft failed.");
            throw;
        }
    }
}
