using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>Loads user-maintainable provider bootstrap commands from LocalGPT knowledge and executes them only through the shared bounded console service.</summary>
/// <param name="knowledge">Reads approved/user-edited provider profile articles from the LocalGPT Knowledge Database.</param>
/// <param name="regexPatterns">Parses profile blocks and validates provider model identifiers through database-backed regexes.</param>
/// <param name="console">Runs read-only and explicitly approved provider commands through the common console feed.</param>
/// <param name="jsonText">Applies the maintained LocalGPT JSON policy when reading provider profiles from Knowledge.</param>
/// <param name="platformRuntime">Platform runtime service used to select the operating-system-specific provider bootstrap token.</param>
/// <param name="ollamaPlatform">Platform-owned Ollama executable resolver used to keep Finder/desktop launches independent of shell PATH setup.</param>
/// <param name="lmStudioPlatform">Platform-owned LM Studio/llmster executable resolver used to keep Finder/desktop launches independent of shell PATH setup.</param>
/// <param name="logger">Writes bounded provider-bootstrap diagnostics without logging command text.</param>
/// <param name="options">Options containing the caller-supplied values that control this operation.</param>
/// <param name="configurationWriter">Configuration writer dependency used by the AI provider bootstrap workflow to provide the corresponding application capability.</param>
/// <param name="providerRegistry">Ai provider configuration registry service dependency used by the AI provider bootstrap workflow to provide the corresponding application capability.</param>
public sealed class AiProviderBootstrapService(
    ICouncilKnowledgeService knowledge,
    IRegexPatternService regexPatterns,
    IConsoleCommandService console,
    IJsonTextService jsonText,
    IOptionsMonitor<global::LocalGPT.BusinessObjects.ConfigurationRoot> options,
    IConfigurationWriter configurationWriter,
    IAiProviderConfigurationRegistryService providerRegistry,
    IPlatformRuntimeService platformRuntime,
    IOllamaPlatformService ollamaPlatform,
    ILmStudioPlatformService lmStudioPlatform,
    ILogger<AiProviderBootstrapService> logger) : IAiProviderBootstrapService
{

    /// <summary>Returns provider bootstrap profiles from local knowledge for the current operating-system family.</summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<AiProviderBootstrapProfile>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var blockRegex = await regexPatterns.GetRegexAsync("builtin.ai-provider-bootstrap-block").ConfigureAwait(false)
                ?? throw new InvalidOperationException("The AI-provider bootstrap profile regex is unavailable.");
            var platform = platformRuntime.ProviderBootstrapToken;
            var entries = await knowledge.GetEntriesAsync(includeArchived: false, take: 500, cancellationToken).ConfigureAwait(false);
            var profiles = new List<AiProviderBootstrapProfile>();
            foreach (var entry in entries.OrderByDescending(item => item.IsUserApproved).ThenByDescending(item => item.UpdatedAtUtc))
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (System.Text.RegularExpressions.Match match in blockRegex.Matches(entry.Content ?? string.Empty))
                {
                    try
                    {
                        var profile = jsonText.Deserialize<AiProviderBootstrapProfile>(match.Groups["json"].Value);
                        if (profile is null || string.IsNullOrWhiteSpace(profile.Key) || !string.Equals(profile.Platform, platform, StringComparison.OrdinalIgnoreCase))
                            continue;
                        NormalizeProfile(profile);
                        profiles.Add(profile);
                    }
                    catch (JsonException exception)
                    {
                        logger.LogWarning(exception, "Ignored malformed AI-provider bootstrap profile in knowledge entry {KnowledgeEntryId}; profile content was omitted.", entry.Id);
                    }
                }
            }
            var result = profiles
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            logger.LogInformation("Loaded {ProfileCount} knowledge-backed AI-provider bootstrap profile(s) for platform {Platform}.", result.Count, platform);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading AI-provider bootstrap profiles was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading AI-provider bootstrap profiles failed.");
            throw;
        }
    }

    /// <summary>Runs the profile's read-only local detection command.</summary>
    /// <inheritdoc />
    public async Task<LocalConsoleCommandResult> DetectAsync(string profileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await RequireProfileAsync(profileKey, cancellationToken).ConfigureAwait(false);
            return await ExecuteProfileCommandAsync(profile, "Detect provider", profile.DetectCommand, isReadOnly: true, userConfirmed: false, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider detection was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider detection failed for profile {ProfileKey}; command text was omitted.", profileKey); throw; }
    }

    /// <summary>Runs the profile's read-only local model-listing command.</summary>
    /// <inheritdoc />
    public async Task<LocalConsoleCommandResult> ListModelsAsync(string profileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await RequireProfileAsync(profileKey, cancellationToken).ConfigureAwait(false);
            return await ExecuteProfileCommandAsync(profile, "List provider models", profile.ListModelsCommand, isReadOnly: true, userConfirmed: false, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider model listing was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider model listing failed for profile {ProfileKey}; command/output text was omitted.", profileKey); throw; }
    }

    /// <summary>Runs the profile's user-confirmed installation command.</summary>
    /// <inheritdoc />
    public async Task<LocalConsoleCommandResult> InstallAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await RequireProfileAsync(profileKey, cancellationToken).ConfigureAwait(false);
            return await ExecuteProfileCommandAsync(profile, "Install provider", profile.InstallCommand, isReadOnly: false, userConfirmed, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider installation was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider installation failed for profile {ProfileKey}; command text was omitted.", profileKey); throw; }
    }

    /// <summary>Runs the profile's user-confirmed runtime start command.</summary>
    /// <inheritdoc />
    public async Task<LocalConsoleCommandResult> StartAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await RequireProfileAsync(profileKey, cancellationToken).ConfigureAwait(false);
            return await ExecuteProfileCommandAsync(profile, "Start provider", profile.StartCommand, isReadOnly: false, userConfirmed, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider startup was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider startup failed for profile {ProfileKey}; command text was omitted.", profileKey); throw; }
    }

    /// <summary>Runs a user-confirmed provider model-install command after validating and alias-resolving the model identifier.</summary>
    /// <inheritdoc />
    public async Task<LocalConsoleCommandResult> InstallModelAsync(string profileKey, string modelId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await RequireProfileAsync(profileKey, cancellationToken).ConfigureAwait(false);
            var resolved = await ResolveModelIdAsync(profileKey, modelId, cancellationToken).ConfigureAwait(false);
            var command = profile.InstallModelCommandTemplate.Replace("{{model}}", resolved, StringComparison.Ordinal);
            return await ExecuteProfileCommandAsync(profile, $"Install model {resolved}", command, isReadOnly: false, userConfirmed, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider model installation was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider model installation failed for profile {ProfileKey}; model/command values were omitted.", profileKey); throw; }
    }

    /// <summary>Registers the profile endpoint in LocalGPT's existing provider configuration after explicit confirmation.</summary>
    /// <inheritdoc />
    public async Task<string> ConfigureEndpointAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Registering an AI provider endpoint requires explicit user confirmation.");
            var profile = await RequireProfileAsync(profileKey, cancellationToken).ConfigureAwait(false);
            if (!Uri.TryCreate(profile.Endpoint, UriKind.Absolute, out var endpointUri) || !endpointUri.IsLoopback)
                throw new InvalidOperationException("Initial provider bootstrap may only register a loopback endpoint. Remote providers remain user-configured separately.");
            var current = options.CurrentValue;
            var draft = providerRegistry.CreateDetachedDraft(current.AICore);
            if (profile.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                || profile.ProviderKind.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                var exists = draft.OllamaCore.Uri.Equals(profile.Endpoint, StringComparison.OrdinalIgnoreCase)
                    || draft.OllamaCores.Any(item => item.Uri.Equals(profile.Endpoint, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    if (string.IsNullOrWhiteSpace(draft.OllamaCore.Uri))
                        draft.OllamaCore = new OllamaCoreOptions { Uri = profile.Endpoint, ModelName = string.Empty };
                    else
                        draft.OllamaCores.Add(new OllamaCoreOptions { Uri = profile.Endpoint, ModelName = string.Empty });
                }
            }
            else if (profile.ProviderKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                || profile.ProviderKind.Equals("ChatGPTLocal", StringComparison.OrdinalIgnoreCase))
            {
                var exists = draft.ChatGPTLocalCore.Endpoint.Equals(profile.Endpoint, StringComparison.OrdinalIgnoreCase)
                    || draft.ChatGPTLocalCores.Any(item => item.Endpoint.Equals(profile.Endpoint, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    if (string.IsNullOrWhiteSpace(draft.ChatGPTLocalCore.Endpoint))
                        draft.ChatGPTLocalCore = new ChatGPTLocalCoreOptions { Endpoint = profile.Endpoint, ApiKey = string.Empty, ModelName = string.Empty };
                    else
                        draft.ChatGPTLocalCores.Add(new ChatGPTLocalCoreOptions { Endpoint = profile.Endpoint, ApiKey = string.Empty, ModelName = string.Empty });
                }
            }
            else
            {
                throw new InvalidOperationException($"Provider kind '{profile.ProviderKind}' is not supported by the initial local-provider bootstrap configuration bridge.");
            }

            await configurationWriter.SaveAsync(new global::LocalGPT.BusinessObjects.ConfigurationRoot
            {
                LoggingCore = current.LoggingCore,
                PythonCore = current.PythonCore,
                ConnectionStringsCore = current.ConnectionStringsCore,
                AICore = draft,
                LocalGPT = current.LocalGPT
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Registered loopback AI provider profile {ProfileKey} in LocalGPT user configuration.", profile.Key);
            return profile.Endpoint;
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Registering provider endpoint was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Registering provider endpoint failed for profile {ProfileKey}; endpoint details omitted.", profileKey); throw; }
    }

    /// <summary>Maps a generic recommendation identifier to the provider's user-maintainable alias and validates it as a single command token.</summary>
    /// <inheritdoc />
    public async Task<string> ResolveModelIdAsync(string profileKey, string recommendationId, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(recommendationId);
            var profile = await RequireProfileAsync(profileKey, cancellationToken).ConfigureAwait(false);
            var candidate = profile.ModelAliases.TryGetValue(recommendationId.Trim(), out var alias) ? alias : recommendationId.Trim();
            var modelRegex = await regexPatterns.GetRegexAsync("builtin.provider-model-token-pattern").ConfigureAwait(false)
                ?? throw new InvalidOperationException("The provider model-token regex is unavailable.");
            if (!modelRegex.IsMatch(candidate))
                throw new InvalidDataException("The provider model identifier is not a safe single command token. Update the knowledge alias instead of embedding shell syntax.");
            return candidate;
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Resolving provider model identifier was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Resolving provider model identifier failed; identifier text was omitted."); throw; }
    }

    /// <summary>
    /// Performs require profile as part of the AI provider bootstrap service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileKey">Profile key value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The AI provider bootstrap profile produced by the operation.</returns>
    private async Task<AiProviderBootstrapProfile> RequireProfileAsync(string profileKey, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(profileKey);
            var profile = (await GetProfilesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => item.Key.Equals(profileKey.Trim(), StringComparison.OrdinalIgnoreCase));
            return profile ?? throw new KeyNotFoundException($"AI-provider bootstrap profile '{profileKey}' was not found for the current platform.");
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Resolving provider bootstrap profile was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Resolving provider bootstrap profile {ProfileKey} failed.", profileKey); throw; }
    }

    /// <summary>
    /// Executes profile command as part of the AI provider bootstrap service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="command">Command value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    /// <param name="isReadOnly">Value indicating whether is read only should apply to this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    private async Task<LocalConsoleCommandResult> ExecuteProfileCommandAsync(
        AiProviderBootstrapProfile profile,
        string displayName,
        string command,
        bool isReadOnly,
        bool userConfirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new InvalidOperationException("The selected provider knowledge profile does not define this command for the current platform.");
            return await console.ExecuteAsync(new LocalConsoleCommandRequest
            {
                DisplayName = $"{displayName}: {profile.DisplayName}",
                Shell = profile.Shell,
                CommandText = command,
                Environment = BuildProviderCommandEnvironment(profile),
                IsReadOnly = isReadOnly,
                UserConfirmed = userConfirmed,
                TimeoutSeconds = isReadOnly ? 30 : 600
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Executing provider bootstrap command was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Executing provider bootstrap command failed; command text was omitted."); throw; }
    }


    /// <summary>Builds a bounded PATH override for a provider CLI discovered through the platform service.</summary>
    /// <param name="profile">Selected provider bootstrap profile.</param>
    /// <returns>Environment entries passed only to the reviewed provider command.</returns>
    private List<ToolchainEnvironmentVariableSetting> BuildProviderCommandEnvironment(AiProviderBootstrapProfile profile)
    {
        try
        {
            var executable = ResolveProviderExecutable(profile);
            if (string.IsNullOrWhiteSpace(executable))
                return [];

            var directory = Path.GetDirectoryName(executable);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return [];

            var inheritedPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var pathEntries = inheritedPath
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(item => !string.Equals(item, directory, platformRuntime.PathComparison));
            var value = string.Join(Path.PathSeparator, new[] { directory }.Concat(pathEntries));
            return
            [
                new ToolchainEnvironmentVariableSetting
                {
                    Name = "PATH",
                    Value = value,
                    Source = $"LocalGPT {profile.DisplayName} platform discovery",
                    IsEnabled = true
                }
            ];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not enrich provider command PATH for profile {ProfileKey}; inherited PATH will be used.", profile.Key);
            return [];
        }
    }

    /// <summary>Resolves the CLI executable associated with a built-in local provider profile.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <returns>Resolved executable path, or <see langword="null"/> when the provider is not installed yet.</returns>
    private string? ResolveProviderExecutable(AiProviderBootstrapProfile profile)
    {
        try
        {
            if (profile.ProviderKind.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
                || profile.Key.StartsWith("ollama-", StringComparison.OrdinalIgnoreCase))
                return ollamaPlatform.ResolveExecutable();

            if (profile.Key.StartsWith("lmstudio-", StringComparison.OrdinalIgnoreCase)
                || profile.DisplayName.Contains("LM Studio", StringComparison.OrdinalIgnoreCase)
                || profile.DisplayName.Contains("llmster", StringComparison.OrdinalIgnoreCase))
                return lmStudioPlatform.ResolveExecutable();

            return null;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not resolve provider executable for profile {ProfileKey}; inherited PATH will be used.", profile.Key);
            return null;
        }
    }

    /// <summary>
    /// Normalizes profile as part of the AI provider bootstrap service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the AI provider bootstrap operation and used when producing its result.</param>
    private void NormalizeProfile(AiProviderBootstrapProfile profile)
    {
        try
        {
            profile.Key = profile.Key.Trim().ToLowerInvariant();
            profile.DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Key : profile.DisplayName.Trim();
            profile.ProviderKind = profile.ProviderKind.Trim();
            profile.Platform = profile.Platform.Trim().ToLowerInvariant();
            profile.Endpoint = profile.Endpoint.Trim();
            profile.SourceUrl = profile.SourceUrl.Trim();
            profile.ModelAliases = new Dictionary<string, string>(profile.ModelAliases ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing provider bootstrap profile failed; command text was omitted.");
            throw;
        }
    }


}
