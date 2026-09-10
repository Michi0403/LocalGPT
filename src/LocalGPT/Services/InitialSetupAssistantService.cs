using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;

namespace LocalGPT.Services;

/// <summary>Coordinates the reopenable AI-guided setup workflow without creating a second provider, hardware, or Council persistence model.</summary>
/// <param name="hardwareInventory">Provides read-only local hardware discovery.</param>
/// <param name="configuredHardware">Owns the durable physical-host hardware profile.</param>
/// <param name="providerBootstrap">Provides knowledge-backed provider installation profiles for the current platform.</param>
/// <param name="canIRun">Provides optional attributed hardware/model compatibility evidence after explicit user opt-in.</param>
/// <param name="providerModels">Returns provider-qualified models from configured/reachable endpoints.</param>
/// <param name="onboarding">Returns the existing first-run completion state.</param>
/// <param name="teams">Owns user-confirmed Council team configuration persistence.</param>
/// <param name="reviewerPolicy">Provides the shared benchmark reviewer ranking used to avoid weak default curator assignments.</param>
/// <param name="jsonText">Serializes maintained team templates for a detached deep clone.</param>
/// <param name="httpClientFactory">Creates the bounded client used only for explicit official provider-catalog searches.</param>
/// <param name="logger">Writes bounded setup diagnostics.</param>
public sealed class InitialSetupAssistantService(
    IHardwareInventoryService hardwareInventory,
    IConfiguredAiHostHardwareService configuredHardware,
    IAiProviderBootstrapService providerBootstrap,
    ICanIRunHardwareRecommendationService canIRun,
    IProviderModelRuntimeService providerModels,
    IFirstRunOnboardingService onboarding,
    ICouncilTeamConfigurationService teams,
    IProviderModelReviewerPolicyService reviewerPolicy,
    IJsonTextService jsonText,
    IHttpClientFactory httpClientFactory,
    ILogger<InitialSetupAssistantService> logger) : IInitialSetupAssistantService
{
    /// <summary>Synchronizes the last successful provider candidate snapshot within this scoped setup workflow so one UI refresh does not repeat slow remote-provider probes.</summary>
    private readonly object providerCandidateCacheSync = new();
    /// <summary>Stores the last successful provider candidate discovery for reuse by model mapping and explicit provider-catalog annotation in the same setup scope.</summary>
    private IReadOnlyList<MultiModelCouncilModelCandidate>? cachedProviderCandidates;

    /// <summary>Builds current hardware/provider/model state without changing the machine.</summary>
    /// <inheritdoc />
    public async Task<InitialSetupAssistantSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<InitialSetupHardwareDevice> hardware;
            try
            {
                hardware = await BuildHardwareListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Optional hardware discovery was unavailable while building initial setup snapshot; provider, Ollama, model, and recommendation controls remain usable.");
                hardware = [];
            }
            IReadOnlyList<AiProviderBootstrapProfile> profiles;
            try
            {
                profiles = await providerBootstrap.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Provider bootstrap profiles were unavailable while building initial setup snapshot; hardware and installed-model state remain usable.");
                profiles = [];
            }
            IReadOnlyList<MultiModelCouncilModelCandidate> candidates;
            try
            {
                candidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
                RememberProviderCandidates(candidates);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Provider model discovery was unavailable while building initial setup snapshot; setup remains usable for provider installation.");
                candidates = [];
            }
            var onboardingCompleted = false;
            try
            {
                onboardingCompleted = (await onboarding.GetStatusAsync(refreshConnectivity: false, cancellationToken).ConfigureAwait(false)).IsCompleted;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "First-run review status was unavailable while building initial setup snapshot; provider and model setup remain usable.");
            }
            var installedModels = candidates.Where(item => item.IsInstalled).Select(item => item.ToReference()).ToList();
            var recommendedCurators = installedModels
                .OrderBy(reviewerPolicy.GetPriority)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Min(3, installedModels.Count))
                .Select(item => item.SelectionKey)
                .ToList();
            return new InitialSetupAssistantSnapshot
            {
                Hardware = hardware.ToList(),
                ProviderProfiles = profiles.ToList(),
                InstalledModels = installedModels,
                RecommendedCuratorModelKeys = recommendedCurators,
                Platform = RuntimeInformation.OSDescription,
                IsOnboardingCompleted = onboardingCompleted,
                CanStartAiGuidedSetup = installedModels.Count > 0,
                AiGuidedSetupRoute = "/chat?team=initial-setup-assistant&starter=initial-setup-council-start&autoStartCouncil=true&newCouncil=true"
            };
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Building initial setup snapshot was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building initial setup snapshot failed.");
            throw;
        }
    }

    /// <summary>Loads optional CanIRun.ai compatibility evidence per selected hardware row without collapsing recommendations across physical hosts.</summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<CanIRunModelRecommendation>> GetHardwareRecommendationsAsync(
        IReadOnlyList<InitialSetupHardwareDevice> devices,
        bool userConfirmedWebLookup,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmedWebLookup)
                throw new InvalidOperationException("CanIRun.ai lookup requires explicit user opt-in for this web request.");
            ArgumentNullException.ThrowIfNull(devices);
            var combined = new List<CanIRunModelRecommendation>();
            Exception? firstLookupFailure = null;
            var lookupCount = 0;
            foreach (var device in devices.Where(item => item.Selected && (!string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.CpuName))).Take(32))
            {
                cancellationToken.ThrowIfCancellationRequested();
                lookupCount++;
                var endpoint = device.Endpoint ?? string.Empty;
                var hostKey = string.IsNullOrWhiteSpace(device.HostKey) && !string.IsNullOrWhiteSpace(endpoint)
                    ? configuredHardware.GetHostKey(endpoint)
                    : device.HostKey;
                try
                {
                    var items = await canIRun.GetRecommendationsAsync(device, userConfirmedWebLookup: true, cancellationToken).ConfigureAwait(false);
                    foreach (var item in items)
                    {
                        combined.Add(new CanIRunModelRecommendation
                        {
                            ModelId = item.ModelId,
                            ModelName = item.ModelName,
                            Grade = item.Grade,
                            Status = item.Status,
                            Score = item.Score,
                            Quantization = item.Quantization,
                            RequiredVramGiB = item.RequiredVramGiB,
                            Publisher = item.Publisher,
                            OllamaModelId = item.OllamaModelId,
                            LmStudioModelId = item.LmStudioModelId,
                            DeviceSlug = item.DeviceSlug,
                            DeviceEndpoint = endpoint,
                            HostKey = hostKey ?? string.Empty,
                            SourceUrl = item.SourceUrl
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    firstLookupFailure ??= exception;
                    logger.LogWarning(exception, "One optional CanIRun.ai hardware lookup failed; other selected hardware rows will continue and hardware values remain omitted from logs.");
                }
            }
            if (lookupCount > 0 && combined.Count == 0 && firstLookupFailure is not null)
                throw new InvalidOperationException("CanIRun.ai did not return recommendations for any selected hardware row.", firstLookupFailure);
            var result = combined
                .GroupBy(item => $"{item.HostKey}|{item.DeviceEndpoint}|{item.ModelId}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.Score).First())
                .OrderBy(item => item.HostKey, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(item => item.Score)
                .ThenBy(item => item.ModelName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            logger.LogInformation("Loaded {RecommendationCount} attributed hardware recommendation(s) across {HostCount} local physical host(s).", result.Count, result.Select(item => item.HostKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            return result;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading multi-host CanIRun.ai recommendations was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading multi-host CanIRun.ai recommendations failed; hardware and web response values were omitted.");
            throw;
        }
    }

    /// <summary>Builds the selected provider's installed/catalog model list and enriches it with optional CanIRun.ai hardware-fit discoveries.</summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<InitialSetupModelChoice>> BuildModelChoicesAsync(
        string profileKey,
        IReadOnlyList<CanIRunModelRecommendation> recommendations,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(profileKey);
            ArgumentNullException.ThrowIfNull(recommendations);
            var profiles = await providerBootstrap.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
            var profile = profiles.FirstOrDefault(item => item.Key.Equals(profileKey, StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException($"AI-provider bootstrap profile '{profileKey}' was not found.");
            var isOllamaProfile = providerBootstrap.IsOllamaProfile(profile);
            var hasInstallModelAction = !string.IsNullOrWhiteSpace(profile.InstallModelCommandTemplate);
            IReadOnlyList<MultiModelCouncilModelCandidate> providerCandidates;
            try
            {
                providerCandidates = await GetProviderCandidatesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Provider model discovery was unavailable while building setup choices; knowledge-backed install choices and CanIRun.ai discoveries remain usable.");
                providerCandidates = [];
            }

            var candidates = providerCandidates
                .Where(item => item.ProviderKind.Equals(profile.ProviderKind, StringComparison.OrdinalIgnoreCase)
                    && ProviderEndpointsEquivalent(item.ProviderKind, item.Endpoint, profile.Endpoint))
                .ToList();
            var choices = new Dictionary<string, InitialSetupModelChoice>(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates.Where(item => item.IsInstalled))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var providerId = candidate.ModelName.Trim();
                choices[providerId] = new InitialSetupModelChoice
                {
                    RecommendationId = string.Empty,
                    ProviderModelId = providerId,
                    DisplayName = candidate.ModelName,
                    SelectionKey = candidate.SelectionKey,
                    CanInstall = hasInstallModelAction,
                    MappingStatus = "Installed model reported by the selected provider.",
                    IsInstalled = true,
                    IsProviderInventory = true,
                    IsHardwareRecommended = false,
                    CanCheckUpdate = isOllamaProfile && hasInstallModelAction,
                    ProviderCatalogUrl = BuildProviderCatalogModelUrl(profile, providerId),
                    IsProviderCatalogUrlExact = true,
                    Selected = true
                };
            }

            foreach (var alias in profile.ModelAliases.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var providerId = alias.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(providerId))
                    continue;
                var installedCandidate = candidates.FirstOrDefault(item => item.IsInstalled && ModelNamesEquivalent(item.ModelName, providerId));
                if (!choices.TryGetValue(providerId, out var choice))
                {
                    choice = new InitialSetupModelChoice
                    {
                        RecommendationId = alias.Key,
                        ProviderModelId = providerId,
                        DisplayName = alias.Key,
                        SelectionKey = installedCandidate?.SelectionKey ?? string.Empty,
                        CanInstall = hasInstallModelAction,
                        MappingStatus = "Knowledge-backed provider catalog mapping.",
                        IsInstalled = installedCandidate is not null,
                        IsProviderInventory = installedCandidate is not null,
                        CanCheckUpdate = installedCandidate is not null && isOllamaProfile && hasInstallModelAction,
                        ProviderCatalogUrl = BuildProviderCatalogModelUrl(profile, providerId),
                        IsProviderCatalogUrlExact = true,
                        Selected = installedCandidate is not null
                    };
                    choices[providerId] = choice;
                }
            }

            var endpointScopedRecommendations = recommendations
                .Where(item => string.IsNullOrWhiteSpace(item.DeviceEndpoint)
                    || ProviderEndpointsEquivalent(profile.ProviderKind, item.DeviceEndpoint, profile.Endpoint))
                .ToList();
            var effectiveRecommendations = endpointScopedRecommendations.Count > 0 ? endpointScopedRecommendations : recommendations.ToList();
            foreach (var recommendation in effectiveRecommendations
                .GroupBy(item => item.ModelId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.Score).First())
                .OrderByDescending(item => item.Score))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var providerId = ResolveRecommendationProviderId(profile, recommendation);
                var hasProviderId = !string.IsNullOrWhiteSpace(providerId);
                var canInstall = hasProviderId && hasInstallModelAction;
                if (!hasProviderId)
                    providerId = recommendation.ModelId.Trim();
                var installedCandidate = candidates.FirstOrDefault(item => item.IsInstalled && ModelNamesEquivalent(item.ModelName, providerId));
                var lookupKey = string.IsNullOrWhiteSpace(providerId) ? recommendation.ModelId : providerId;
                if (!choices.TryGetValue(lookupKey, out var choice))
                {
                    choice = new InitialSetupModelChoice
                    {
                        RecommendationId = recommendation.ModelId,
                        ProviderModelId = providerId,
                        DisplayName = recommendation.ModelName,
                        SelectionKey = installedCandidate?.SelectionKey ?? string.Empty,
                        CanInstall = canInstall,
                        MappingStatus = canInstall ? "CanIRun.ai hardware-fit discovery mapped to the selected provider." : "Provider install ID needs review.",
                        IsInstalled = installedCandidate is not null,
                        IsProviderInventory = installedCandidate is not null,
                        CanCheckUpdate = installedCandidate is not null && canInstall && isOllamaProfile,
                        ProviderCatalogUrl = hasProviderId
                            ? BuildProviderCatalogModelUrl(profile, providerId)
                            : BuildProviderCatalogSearchUrl(profile, BuildProviderCatalogResolutionQuery(recommendation.ModelId, recommendation.ModelName)),
                        IsProviderCatalogUrlExact = hasProviderId,
                        Selected = installedCandidate is not null && recommendation.Score >= 40
                    };
                    choices[lookupKey] = choice;
                }
                choice.RecommendationId = recommendation.ModelId;
                choice.DisplayName = string.IsNullOrWhiteSpace(recommendation.ModelName) ? choice.DisplayName : recommendation.ModelName;
                choice.IsHardwareRecommended = true;
                choice.RecommendationScore = recommendation.Score;
                choice.RecommendationGrade = recommendation.Grade;
                choice.RecommendationStatus = recommendation.Status;
                choice.Quantization = recommendation.Quantization;
                choice.RequiredVramGiB = recommendation.RequiredVramGiB;
                choice.SourceUrl = recommendation.SourceUrl;
                if (installedCandidate is not null)
                {
                    choice.IsInstalled = true;
                    choice.IsProviderInventory = true;
                    choice.SelectionKey = installedCandidate.SelectionKey;
                    choice.CanCheckUpdate = canInstall && isOllamaProfile;
                }
            }

            return choices.Values
                .OrderByDescending(item => item.IsInstalled)
                .ThenByDescending(item => item.IsHardwareRecommended)
                .ThenByDescending(item => item.RecommendationScore)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Building initial setup model choices was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building initial setup model choices failed; model identifiers were omitted from logs.");
            throw;
        }
    }

    /// <summary>Resolves one optional hardware-fit discovery to a provider-safe model identifier without throwing for an ordinary unmapped recommendation.</summary>
    /// <param name="profile">Selected provider bootstrap profile.</param>
    /// <param name="recommendation">Attributed CanIRun.ai recommendation.</param>
    /// <returns>A provider-safe model identifier, or an empty string when manual provider selection is required.</returns>
    private string ResolveRecommendationProviderId(AiProviderBootstrapProfile profile, CanIRunModelRecommendation recommendation)
    {
        try
        {
            if (providerBootstrap.IsOllamaProfile(profile))
            {
                if (!string.IsNullOrWhiteSpace(recommendation.OllamaModelId))
                    return recommendation.OllamaModelId.Trim();
                if (profile.ModelAliases.TryGetValue(recommendation.ModelId, out var mappedOllama) && !string.IsNullOrWhiteSpace(mappedOllama))
                    return mappedOllama.Trim();
                return TryResolveKnownOllamaRecommendationId(recommendation.ModelId) ?? string.Empty;
            }

            if (profile.DisplayName.Contains("LM Studio", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(recommendation.LmStudioModelId))
                    return recommendation.LmStudioModelId.Trim();
                if (profile.ModelAliases.TryGetValue(recommendation.ModelId, out var mappedLmStudio) && !string.IsNullOrWhiteSpace(mappedLmStudio))
                    return mappedLmStudio.Trim();
                return string.Empty;
            }

            return profile.ModelAliases.TryGetValue(recommendation.ModelId, out var mapped) && !string.IsNullOrWhiteSpace(mapped)
                ? mapped.Trim()
                : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Resolving one provider-specific hardware-fit recommendation ID failed; manual provider ID entry remains available.");
            return string.Empty;
        }
    }

    /// <summary>Returns the last successful provider candidate snapshot for this scoped setup workflow, querying providers only when no snapshot has been established yet.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop a required provider discovery.</param>
    /// <returns>The cached or newly discovered provider candidates.</returns>
    private async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetProviderCandidatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            lock (providerCandidateCacheSync)
            {
                if (cachedProviderCandidates is not null)
                    return cachedProviderCandidates;
            }

            var candidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            RememberProviderCandidates(candidates);
            return candidates;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Provider candidate discovery failed while establishing the scoped initial-setup snapshot.");
            throw;
        }
    }

    /// <summary>Stores one detached provider candidate snapshot for reuse by subsequent setup mapping/catalog operations in the same scoped service instance.</summary>
    /// <param name="candidates">Successfully discovered provider candidates.</param>
    private void RememberProviderCandidates(IReadOnlyList<MultiModelCouncilModelCandidate> candidates)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(candidates);
            var detached = candidates.ToArray();
            lock (providerCandidateCacheSync)
                cachedProviderCandidates = detached;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Remembering the scoped initial-setup provider candidate snapshot failed.");
        }
    }

    /// <summary>Compares provider endpoints while tolerating historic loopback spelling and OpenAI-compatible <c>/v1</c> normalization.</summary>
    /// <param name="providerKind">Provider kind controlling endpoint path normalization.</param>
    /// <param name="left">First endpoint.</param>
    /// <param name="right">Second endpoint.</param>
    /// <returns><see langword="true"/> when both endpoints identify the same provider host route.</returns>
    private bool ProviderEndpointsEquivalent(string providerKind, string? left, string? right)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            var leftValue = providerKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                ? identity.NormalizeOpenAiCompatibleEndpoint(left)
                : identity.NormalizeEndpoint(left);
            var rightValue = providerKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                ? identity.NormalizeOpenAiCompatibleEndpoint(right)
                : identity.NormalizeEndpoint(right);
            return leftValue.Equals(rightValue, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Comparing setup provider endpoints failed.");
            return false;
        }
    }

    /// <summary>Searches one provider-owned public model catalog only after an explicit user action; hardware, LocalGPT endpoint, and CanIRun state are not sent.</summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<InitialSetupModelChoice>> SearchProviderCatalogAsync(
        string profileKey,
        string query,
        bool userConfirmedWebLookup,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmedWebLookup)
                throw new InvalidOperationException("Official provider catalog lookup requires an explicit user action.");
            ArgumentException.ThrowIfNullOrWhiteSpace(profileKey);
            var boundedQuery = (query ?? string.Empty).Trim();
            if (boundedQuery.Length > 120)
                throw new InvalidDataException("Provider catalog search text is limited to 120 characters.");

            var profile = (await providerBootstrap.GetProfilesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => item.Key.Equals(profileKey.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException("The selected provider bootstrap profile is unavailable.");
            using var lookupTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lookupTimeout.CancelAfter(TimeSpan.FromSeconds(20));
            var lookupToken = lookupTimeout.Token;
            var catalogUri = BuildProviderCatalogSearchUri(profile, boundedQuery);
            var html = await DownloadProviderCatalogPageAsync(catalogUri, lookupToken).ConfigureAwait(false);
            var providerIds = await LoadProviderCatalogModelIdsAsync(profile, html, boundedQuery, lookupToken).ConfigureAwait(false);

            IReadOnlyList<MultiModelCouncilModelCandidate> candidates;
            try
            {
                candidates = await GetProviderCandidatesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogInformation(exception, "Installed provider models were unavailable while annotating explicit provider-catalog search results.");
                candidates = [];
            }

            var scopedCandidates = candidates
                .Where(item => item.ProviderKind.Equals(profile.ProviderKind, StringComparison.OrdinalIgnoreCase)
                    && ProviderEndpointsEquivalent(profile.ProviderKind, item.Endpoint, profile.Endpoint))
                .ToList();
            var isOllamaProfile = providerBootstrap.IsOllamaProfile(profile);
            var canInstall = !string.IsNullOrWhiteSpace(profile.InstallModelCommandTemplate);
            var choices = new List<InitialSetupModelChoice>();
            foreach (var providerId in providerIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var installedCandidate = scopedCandidates.FirstOrDefault(item => item.IsInstalled && ModelNamesEquivalent(item.ModelName, providerId));
                choices.Add(new InitialSetupModelChoice
                {
                    RecommendationId = providerId,
                    ProviderModelId = providerId,
                    DisplayName = providerId,
                    SelectionKey = installedCandidate?.SelectionKey ?? string.Empty,
                    CanInstall = canInstall,
                    MappingStatus = "Official provider catalog search result.",
                    IsInstalled = installedCandidate is not null,
                    IsProviderInventory = installedCandidate is not null,
                    IsProviderCatalogEntry = true,
                    CanCheckUpdate = installedCandidate is not null && canInstall && isOllamaProfile,
                    ProviderCatalogUrl = BuildProviderCatalogModelUrl(profile, providerId),
                    IsProviderCatalogUrlExact = true,
                    Selected = installedCandidate is not null
                });
            }

            logger.LogInformation(
                "Loaded {ModelCount} bounded model identifier(s) from the explicitly requested official provider catalog for profile {ProfileKey}.",
                choices.Count,
                profile.Key);
            return choices;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Official provider catalog search was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Official provider catalog search failed; query text and response content were omitted.");
            throw;
        }
    }

    /// <summary>Resolves one hardware-fit recommendation against the selected provider's official catalog without guessing an install identifier.</summary>
    /// <inheritdoc />
    public async Task<InitialSetupProviderCatalogResolution> ResolveProviderCatalogRecommendationAsync(
        string profileKey,
        string recommendationId,
        string displayName,
        bool userConfirmedWebLookup,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmedWebLookup)
                throw new InvalidOperationException("Provider catalog recommendation resolution requires an explicit user action.");
            ArgumentException.ThrowIfNullOrWhiteSpace(profileKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(recommendationId);
            var query = BuildProviderCatalogResolutionQuery(recommendationId, displayName);
            var candidates = await SearchProviderCatalogAsync(profileKey, query, true, cancellationToken).ConfigureAwait(false);
            var matches = candidates
                .Where(candidate => IsConservativeProviderCatalogMatch(candidate.ProviderModelId, recommendationId, displayName))
                .ToList();
            return new InitialSetupProviderCatalogResolution
            {
                Match = matches.Count == 1 ? matches[0] : null,
                Candidates = candidates.ToList(),
                Query = query
            };
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Resolving a hardware-fit recommendation through the official provider catalog was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Resolving a hardware-fit recommendation through the official provider catalog failed; recommendation and catalog identifiers were omitted.");
            throw;
        }
    }

    /// <summary>Builds a bounded provider-catalog search query from a recommendation slug without treating architecture suffixes as provider syntax.</summary>
    /// <param name="recommendationId">CanIRun.ai recommendation identifier.</param>
    /// <param name="displayName">Human-readable recommendation display name used only as a fallback.</param>
    /// <returns>A bounded provider family/search token.</returns>
    private string BuildProviderCatalogResolutionQuery(string recommendationId, string displayName)
    {
        try
        {
            var value = string.IsNullOrWhiteSpace(recommendationId) ? displayName : recommendationId;
            value = (value ?? string.Empty).Trim();
            var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var index = parts.Length - 1; index >= 0; index--)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(parts[index], @"^\d+(?:\.\d+)?b$", System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
                    continue;
                var family = string.Join('-', parts.Take(index));
                if (!string.IsNullOrWhiteSpace(family))
                    value = family;
                break;
            }
            return value[..Math.Min(value.Length, 120)];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Building provider catalog resolution search text failed; recommendation values were omitted.");
            return string.Empty;
        }
    }

    /// <summary>Matches only provider identifiers that normalize exactly to the recommendation identity, with one tolerated trailing MoE active-parameter suffix.</summary>
    /// <param name="providerModelId">Provider-owned model identifier from the official catalog.</param>
    /// <param name="recommendationId">CanIRun.ai recommendation identifier.</param>
    /// <param name="displayName">Human-readable CanIRun.ai model name.</param>
    /// <returns><see langword="true"/> only when an exact conservative identity match is available.</returns>
    private bool IsConservativeProviderCatalogMatch(string providerModelId, string recommendationId, string displayName)
    {
        try
        {
            var recommendationKeys = BuildModelIdentityKeys(recommendationId)
                .Concat(BuildModelIdentityKeys(displayName))
                .Where(item => item.Length > 0)
                .ToHashSet(StringComparer.Ordinal);
            var providerKeys = BuildModelIdentityKeys(providerModelId)
                .Concat(BuildModelIdentityKeys(providerModelId.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty))
                .Where(item => item.Length > 0)
                .ToHashSet(StringComparer.Ordinal);
            return providerKeys.Overlaps(recommendationKeys);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Comparing official provider and hardware-fit model identities failed; model identifiers were omitted.");
            return false;
        }
    }

    /// <summary>Builds punctuation-insensitive exact identity keys while tolerating a trailing MoE active-parameter descriptor such as A3B.</summary>
    /// <param name="value">Provider or recommendation model identity.</param>
    /// <returns>Normalized exact-match keys.</returns>
    private IReadOnlyList<string> BuildModelIdentityKeys(string value)
    {
        try
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0)
                return [];
            var keys = new HashSet<string>(StringComparer.Ordinal)
            {
                string.Concat(text.Where(char.IsLetterOrDigit)).ToLowerInvariant()
            };
            var withoutArchitectureSuffix = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"(?:[-_\s]+a\d+b)\s*$",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));
            keys.Add(string.Concat(withoutArchitectureSuffix.Where(char.IsLetterOrDigit)).ToLowerInvariant());
            return keys.Where(item => item.Length > 0).ToList();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Normalizing one provider model identity failed; model text was omitted.");
            return [];
        }
    }

    /// <summary>Builds a provider-owned catalog/search URL for a recommendation whose exact provider identifier is not yet known.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <param name="query">Bounded provider family/search text.</param>
    /// <returns>The provider-owned catalog/search URL.</returns>
    private string BuildProviderCatalogSearchUrl(AiProviderBootstrapProfile profile, string query)
    {
        try
        {
            if (providerBootstrap.IsOllamaProfile(profile))
                return $"https://ollama.com/search?q={Uri.EscapeDataString(query ?? string.Empty)}";
            if (profile.DisplayName.Contains("LM Studio", StringComparison.OrdinalIgnoreCase))
                return "https://lmstudio.ai/models";
            return profile.ModelCatalogUrl;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Building provider catalog search URL failed; search text was omitted.");
            return profile.ModelCatalogUrl;
        }
    }

    /// <summary>Builds the fixed HTTPS provider-catalog request URI without trusting persisted URLs as network destinations.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <param name="query">Bounded user search text.</param>
    /// <returns>The provider-owned HTTPS catalog URI.</returns>
    private Uri BuildProviderCatalogSearchUri(AiProviderBootstrapProfile profile, string query)
    {
        try
        {
            if (providerBootstrap.IsOllamaProfile(profile))
                return new Uri($"https://ollama.com/search?q={Uri.EscapeDataString(query)}", UriKind.Absolute);
            if (profile.DisplayName.Contains("LM Studio", StringComparison.OrdinalIgnoreCase))
                return new Uri("https://lmstudio.ai/models", UriKind.Absolute);
            throw new InvalidOperationException("The selected provider does not have a maintained in-app catalog search adapter.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Building official provider catalog URI failed for profile {ProfileKey}.", profile.Key);
            throw;
        }
    }

    /// <summary>Downloads one fixed provider-owned catalog page through the bounded provider-catalog client.</summary>
    /// <param name="uri">Validated provider-owned HTTPS URI.</param>
    /// <param name="cancellationToken">Cancellation token for the explicit lookup.</param>
    /// <returns>Bounded provider-owned HTML.</returns>
    private async Task<string> DownloadProviderCatalogPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || (!uri.Host.Equals("ollama.com", StringComparison.OrdinalIgnoreCase)
                    && !uri.Host.Equals("www.ollama.com", StringComparison.OrdinalIgnoreCase)
                    && !uri.Host.Equals("lmstudio.ai", StringComparison.OrdinalIgnoreCase)
                    && !uri.Host.Equals("www.lmstudio.ai", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Provider catalog requests are restricted to maintained HTTPS provider hosts.");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("LocalGPT/4.0.4");
            var client = httpClientFactory.CreateClient("LocalGPTProviderCatalog");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await ReadBoundedCatalogTextAsync(response.Content, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Downloading official provider catalog page was cancelled or reached the bounded lookup timeout.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Downloading official provider catalog page failed; request path and response content were omitted.");
            throw;
        }
    }

    /// <summary>Expands a bounded set of provider family pages so the UI receives concrete Ollama tags and LM Studio catalog identifiers instead of only family names.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <param name="landingHtml">Bounded provider catalog/search HTML.</param>
    /// <param name="query">Optional user search text.</param>
    /// <param name="cancellationToken">Cancellation token for the bounded provider lookup.</param>
    /// <returns>At most sixty concrete provider model identifiers.</returns>
    private async Task<IReadOnlyList<string>> LoadProviderCatalogModelIdsAsync(
        AiProviderBootstrapProfile profile,
        string landingHtml,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var families = ExtractProviderCatalogFamilyIds(profile, landingHtml, query).Take(6).ToList();
            if (providerBootstrap.IsOllamaProfile(profile))
            {
                foreach (var family in families)
                    values.Add(family);
            }

            foreach (var family in families)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var familyUri = BuildProviderCatalogFamilyUri(profile, family);
                var familyHtml = await DownloadProviderCatalogPageAsync(familyUri, cancellationToken).ConfigureAwait(false);
                foreach (var providerId in ExtractProviderCatalogModelIds(profile, familyHtml, query))
                {
                    values.Add(providerId);
                    if (values.Count >= 60)
                        break;
                }
                if (values.Count >= 60)
                    break;
            }

            if (values.Count == 0)
            {
                foreach (var providerId in ExtractProviderCatalogModelIds(profile, landingHtml, query))
                {
                    values.Add(providerId);
                    if (values.Count >= 60)
                        break;
                }
            }
            return values.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).Take(60).ToList();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Expanding official provider catalog families was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Expanding official provider catalog families failed; provider response content was omitted.");
            throw;
        }
    }

    /// <summary>Extracts bounded provider-family identifiers from the provider's catalog landing/search page.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <param name="html">Bounded provider HTML.</param>
    /// <param name="query">Optional user search text.</param>
    /// <returns>Provider family identifiers suitable only for constructing fixed-origin detail-page requests.</returns>
    private IReadOnlyList<string> ExtractProviderCatalogFamilyIds(AiProviderBootstrapProfile profile, string html, string query)
    {
        try
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var isOllama = providerBootstrap.IsOllamaProfile(profile);
            foreach (var href in ExtractHrefValues(html))
            {
                if (values.Count >= 40)
                    break;
                var decoded = WebUtility.HtmlDecode(href).Trim();
                string candidate;
                if (isOllama)
                {
                    const string prefix = "/library/";
                    var index = decoded.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                        continue;
                    var path = decoded[(index + prefix.Length)..].Split('?', '#')[0].Trim('/');
                    var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 1 || parts[0].Contains(':'))
                        continue;
                    candidate = parts[0];
                }
                else if (profile.DisplayName.Contains("LM Studio", StringComparison.OrdinalIgnoreCase))
                {
                    const string prefix = "/models/";
                    var index = decoded.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                        continue;
                    var path = decoded[(index + prefix.Length)..].Split('?', '#')[0].Trim('/');
                    var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 1)
                        continue;
                    candidate = parts[0];
                }
                else
                {
                    continue;
                }

                candidate = Uri.UnescapeDataString(candidate).Trim();
                if (!string.IsNullOrWhiteSpace(candidate) && CatalogQueryMatches(candidate, query))
                    values.Add(candidate);
            }
            return values.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Extracting provider catalog family identifiers failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Builds one fixed-origin provider family page URI from a bounded catalog family identifier.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <param name="family">Provider family identifier extracted from the provider-owned landing page.</param>
    /// <returns>Provider-owned HTTPS family URI.</returns>
    private Uri BuildProviderCatalogFamilyUri(AiProviderBootstrapProfile profile, string family)
    {
        try
        {
            var escaped = Uri.EscapeDataString(family);
            if (providerBootstrap.IsOllamaProfile(profile))
                return new Uri($"https://ollama.com/library/{escaped}", UriKind.Absolute);
            if (profile.DisplayName.Contains("LM Studio", StringComparison.OrdinalIgnoreCase))
                return new Uri($"https://lmstudio.ai/models/{escaped}", UriKind.Absolute);
            throw new InvalidOperationException("The selected provider does not have a maintained family catalog adapter.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Building provider catalog family URI failed for profile {ProfileKey}.", profile.Key);
            throw;
        }
    }

    /// <summary>Reads a provider catalog response through a strict character bound so external HTML cannot expand setup memory without limit.</summary>
    /// <param name="content">Provider HTTP response content.</param>
    /// <param name="cancellationToken">Cancellation token for the explicit lookup.</param>
    /// <returns>At most two million characters of provider-owned HTML.</returns>
    private async Task<string> ReadBoundedCatalogTextAsync(HttpContent content, CancellationToken cancellationToken)
    {
        try
        {
            const int maximumCharacters = 2_000_000;
            await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var text = new StringBuilder(Math.Min(maximumCharacters, 64 * 1024));
            var buffer = new char[8192];
            while (text.Length < maximumCharacters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remaining = Math.Min(buffer.Length, maximumCharacters - text.Length);
                var read = await reader.ReadAsync(buffer.AsMemory(0, remaining), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                text.Append(buffer, 0, read);
            }
            return text.ToString();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Reading official provider catalog response was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Reading bounded official provider catalog response failed.");
            throw;
        }
    }

    /// <summary>Extracts provider-specific install identifiers from provider-owned links without executing or trusting HTML script content.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <param name="html">Bounded provider HTML.</param>
    /// <param name="query">Optional search text used to filter providers whose landing page has no server-side query endpoint.</param>
    /// <returns>At most sixty provider model identifiers.</returns>
    private IReadOnlyList<string> ExtractProviderCatalogModelIds(AiProviderBootstrapProfile profile, string html, string query)
    {
        try
        {
            var hrefs = ExtractHrefValues(html);
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var isOllama = providerBootstrap.IsOllamaProfile(profile);
            foreach (var href in hrefs)
            {
                if (values.Count >= 60)
                    break;
                var decoded = WebUtility.HtmlDecode(href).Trim();
                string candidate;
                if (isOllama)
                {
                    const string prefix = "/library/";
                    var index = decoded.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                        continue;
                    candidate = decoded[(index + prefix.Length)..].Split('?', '#')[0].Trim('/');
                    if (candidate.Contains('/'))
                        candidate = candidate.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                }
                else if (profile.DisplayName.Contains("LM Studio", StringComparison.OrdinalIgnoreCase))
                {
                    const string prefix = "/models/";
                    var index = decoded.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                        continue;
                    var path = decoded[(index + prefix.Length)..].Split('?', '#')[0].Trim('/');
                    var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        continue;
                    candidate = $"{parts[0]}/{parts[1]}";
                }
                else
                {
                    continue;
                }

                candidate = Uri.UnescapeDataString(candidate).Trim();
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                if (!CatalogQueryMatches(candidate, query))
                    continue;
                values.Add(candidate);
            }
            return values.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Extracting model identifiers from official provider catalog HTML failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Extracts quoted href attribute values with a bounded, non-regex scan so provider markup changes fail closed.</summary>
    /// <param name="html">Bounded provider HTML.</param>
    /// <returns>Quoted link destinations found in the response.</returns>
    private IReadOnlyList<string> ExtractHrefValues(string html)
    {
        try
        {
            var values = new List<string>();
            var offset = 0;
            while (offset < html.Length && values.Count < 4000)
            {
                var hrefIndex = html.IndexOf("href", offset, StringComparison.OrdinalIgnoreCase);
                if (hrefIndex < 0)
                    break;
                var equalsIndex = html.IndexOf('=', hrefIndex + 4);
                if (equalsIndex < 0 || equalsIndex - hrefIndex > 16)
                {
                    offset = hrefIndex + 4;
                    continue;
                }
                var quoteIndex = equalsIndex + 1;
                while (quoteIndex < html.Length && char.IsWhiteSpace(html[quoteIndex]))
                    quoteIndex++;
                if (quoteIndex >= html.Length || (html[quoteIndex] != '"' && html[quoteIndex] != '\''))
                {
                    offset = equalsIndex + 1;
                    continue;
                }
                var quote = html[quoteIndex];
                var endIndex = html.IndexOf(quote, quoteIndex + 1);
                if (endIndex < 0)
                    break;
                if (endIndex > quoteIndex + 1)
                    values.Add(html[(quoteIndex + 1)..endIndex]);
                offset = endIndex + 1;
            }
            return values;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Scanning official provider catalog links failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Compares provider identifiers to optional human search text while tolerating spaces and punctuation differences such as Qwen 3.5 versus qwen3.5.</summary>
    /// <param name="candidate">Provider family or model identifier.</param>
    /// <param name="query">Optional user search text.</param>
    /// <returns><see langword="true"/> when the candidate matches the bounded search text.</returns>
    private bool CatalogQueryMatches(string candidate, string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;
            var normalizedCandidate = string.Concat(candidate.Where(char.IsLetterOrDigit)).ToLowerInvariant();
            var normalizedQuery = string.Concat(query.Where(char.IsLetterOrDigit)).ToLowerInvariant();
            return normalizedQuery.Length == 0 || normalizedCandidate.Contains(normalizedQuery, StringComparison.Ordinal);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Comparing provider catalog search text failed; search text was omitted.");
            return false;
        }
    }

    /// <summary>Builds a provider-owned model detail URL for one safe catalog identifier.</summary>
    /// <param name="profile">Selected provider profile.</param>
    /// <param name="providerModelId">Provider model identifier extracted from the official catalog.</param>
    /// <returns>The provider-owned model-detail URL.</returns>
    private string BuildProviderCatalogModelUrl(AiProviderBootstrapProfile profile, string providerModelId)
    {
        try
        {
            if (providerBootstrap.IsOllamaProfile(profile))
                return $"https://ollama.com/library/{Uri.EscapeDataString(providerModelId).Replace("%3A", ":", StringComparison.OrdinalIgnoreCase)}";
            if (profile.DisplayName.Contains("LM Studio", StringComparison.OrdinalIgnoreCase))
            {
                var escaped = string.Join("/", providerModelId.Split('/').Select(Uri.EscapeDataString));
                return $"https://lmstudio.ai/models/{escaped}";
            }
            return profile.ModelCatalogUrl;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Building official provider model URL failed; model identifier was omitted.");
            return profile.ModelCatalogUrl;
        }
    }

    /// <summary>Runs the existing local hardware probe as one confirmed setup action.</summary>
    /// <inheritdoc />
    public async Task<ConfiguredAiHostHardwareProfile> DetectHardwareAsync(string endpoint, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Local hardware detection persistence requires explicit user confirmation.");
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
            var saved = await configuredHardware.DetectLocalAsync(endpoint, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Detected and persisted local hardware through the initial setup assistant for host {HostKey}.", saved.HostKey);
            return saved;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Initial setup hardware detection was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Initial setup hardware detection failed; hardware values were omitted from logs.");
            throw;
        }
    }

    /// <summary>Imports a local HWiNFO text report through the existing configured-host hardware service.</summary>
    /// <inheritdoc />
    public async Task<ConfiguredAiHostHardwareProfile> ImportHwInfoAsync(string endpoint, string reportText, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("HWiNFO hardware import requires explicit user confirmation.");
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
            ArgumentException.ThrowIfNullOrWhiteSpace(reportText);
            if (reportText.Length > 4 * 1024 * 1024)
                throw new InvalidDataException("HWiNFO text is larger than the maintained 4 MiB setup import limit.");
            var saved = await configuredHardware.ImportHwInfoAsync(endpoint, reportText, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Imported HWiNFO hardware through the initial setup assistant for host {HostKey}; report text omitted.", saved.HostKey);
            return saved;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Initial setup HWiNFO import was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Initial setup HWiNFO import failed; report and hardware values were omitted from logs.");
            throw;
        }
    }

    /// <summary>Persists the user-reviewed hardware device list through the existing configured-host hardware service.</summary>
    /// <inheritdoc />
    public async Task<ConfiguredAiHostHardwareProfile> SaveHardwareAsync(
        string endpoint,
        IReadOnlyList<InitialSetupHardwareDevice> devices,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Saving the initial setup hardware list requires explicit user confirmation.");
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
            ArgumentNullException.ThrowIfNull(devices);
            var existing = await configuredHardware.GetForEndpointAsync(endpoint, cancellationToken).ConfigureAwait(false);
            var draft = configuredHardware.CreateDraft(endpoint, existing);
            var selected = devices.Where(item => item.Selected && (!string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.CpuName))).Take(32).ToList();
            draft.Gpus = selected.Where(item => !string.IsNullOrWhiteSpace(item.Name)).Select((item, index) => new ConfiguredAiHostGpu
            {
                Index = index,
                Name = item.Name.Trim(),
                Vendor = (item.Vendor ?? string.Empty).Trim(),
                DedicatedMemoryBytes = ToBytes(item.DedicatedVramGiB)
            }).ToList();
            var primary = selected.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Name));
            if (primary is not null)
            {
                draft.GpuName = primary.Name.Trim();
                draft.GpuVendor = (primary.Vendor ?? string.Empty).Trim();
                draft.DedicatedVramGiB = primary.DedicatedVramGiB;
            }
            var reviewedCpuName = selected.Select(item => item.CpuName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (!string.IsNullOrWhiteSpace(reviewedCpuName))
                draft.CpuName = reviewedCpuName.Trim();
            var reviewedSystemMemoryGiB = selected.Select(item => item.SystemMemoryGiB).FirstOrDefault(value => value is > 0);
            if (reviewedSystemMemoryGiB is > 0)
                draft.SystemMemoryGiB = reviewedSystemMemoryGiB;
            draft.SourceKind = selected.Select(item => item.Source).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? "Manual";
            draft.Confidence = "UserConfirmed";
            var saved = await configuredHardware.SaveAsync(draft, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved {GpuCount} user-reviewed GPU(s) through the initial setup assistant for host {HostKey}.", saved.Gpus.Count, saved.HostKey);
            return saved;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Saving initial setup hardware was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving initial setup hardware failed; hardware values were omitted from logs.");
            throw;
        }
    }

    /// <summary>Persists a reviewed multi-host hardware list without collapsing devices from different provider endpoints onto one machine.</summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfiguredAiHostHardwareProfile>> SaveHardwareListAsync(
        IReadOnlyList<InitialSetupHardwareDevice> devices,
        string fallbackEndpoint,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Saving the initial setup multi-host hardware list requires explicit user confirmation.");
            ArgumentNullException.ThrowIfNull(devices);
            var selected = devices.Where(item => item.Selected && (!string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.CpuName))).Take(64).ToList();
            if (selected.Count == 0)
                throw new InvalidOperationException("Select at least one physical host hardware row before saving the setup list.");
            var grouped = selected
                .Select(item => new { Item = item, Endpoint = string.IsNullOrWhiteSpace(item.Endpoint) ? fallbackEndpoint : item.Endpoint })
                .Where(item => !string.IsNullOrWhiteSpace(item.Endpoint))
                .GroupBy(item => configuredHardware.GetHostKey(item.Endpoint), StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (grouped.Count == 0)
                throw new InvalidOperationException("Each selected hardware device needs a provider endpoint, or a fallback endpoint must be supplied.");

            var savedProfiles = new List<ConfiguredAiHostHardwareProfile>();
            foreach (var group in grouped)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var endpoint = group.Select(item => item.Endpoint).First();
                var groupDevices = group.Select(item => item.Item).ToList();
                savedProfiles.Add(await SaveHardwareAsync(endpoint, groupDevices, userConfirmed: true, cancellationToken).ConfigureAwait(false));
            }
            logger.LogInformation("Saved {HostCount} physical host hardware profile(s) from {HardwareRowCount} reviewed setup row(s).", savedProfiles.Count, selected.Count);
            return savedProfiles;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Saving initial setup multi-host hardware list was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving initial setup multi-host hardware list failed; endpoint/hardware values omitted.");
            throw;
        }
    }

    /// <summary>Creates or refreshes a user-owned benchmark team based on the supplied ordered model pools.</summary>
    /// <inheritdoc />
    public async Task<OrganicCouncilTeamDefinition> CreateBenchmarkTeamAsync(CreateInitialBenchmarkTeamRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Creating the hardware-curated benchmark team requires explicit user confirmation.");
            var selected = request.ModelSelectionKeys.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (selected.Count == 0)
                throw new InvalidOperationException("Select at least one installed provider-qualified model before creating the benchmark team.");
            var preferred = request.PreferredCuratorModelKeys
                .Where(item => selected.Contains(item, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (preferred.Count == 0)
                preferred.Add(selected[0]);

            var template = (await teams.GetDefaultTemplatesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => item.Key.Equals("adaptive-model-benchmark", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("The maintained adaptive-model-benchmark template is unavailable.");
            var cloneJson = jsonText.Serialize(template);
            var team = jsonText.Deserialize<OrganicCouncilTeamDefinition>(cloneJson)
                ?? throw new InvalidDataException("The benchmark team template could not be cloned.");
            team.Key = "hardware-initial-benchmark";
            team.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "Hardware-curated initial benchmark" : request.DisplayName.Trim();
            team.Purpose = "User-owned initial hardware benchmark. Broad benchmark subjects use all selected installed models; curator/director/reviewer roles use the user's stronger preferred model pool.";
            team.IsSystemSeed = false;
            team.IsUserModified = true;
            team.IsDeleted = false;
            team.IsEnabled = true;

            foreach (var role in team.Roles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var usePreferred = IsCuratorRole(role.Role);
                var pool = usePreferred ? preferred : selected;
                role.AiSelectionMode = CouncilRoleAiSelectionMode.AssignedModels;
                role.AssignedModelKeys = pool.ToList();
                role.MinimumAiParticipants = 1;
                role.MaximumAiParticipants = usePreferred ? Math.Min(Math.Max(1, pool.Count), 2) : Math.Max(1, pool.Count);
            }

            var saved = await teams.SaveAsync(new SaveCouncilTeamConfigurationRequest
            {
                Team = team,
                IsEnabled = true,
                UserConfirmed = true
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Created hardware-curated initial benchmark team with {ModelCount} subject model(s) and {PreferredCount} preferred curator model(s).",
                selected.Count,
                preferred.Count);
            return saved;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Creating hardware-curated initial benchmark team was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating hardware-curated initial benchmark team failed; model identities were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Builds hardware list as part of the initial setup assistant service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<InitialSetupHardwareDevice>> BuildHardwareListAsync(CancellationToken cancellationToken)
    {
        try
        {
            var profiles = await configuredHardware.GetAllAsync(cancellationToken).ConfigureAwait(false);
            long? localSystemMemoryBytes = null;
            var localCpuName = string.Empty;
            if (profiles.Any(profile => profile.HostKey.Equals("local-machine", StringComparison.OrdinalIgnoreCase) && profile.SystemMemoryBytes is not > 0))
                localSystemMemoryBytes = await hardwareInventory.GetSystemMemoryBytesAsync(cancellationToken).ConfigureAwait(false);
            if (profiles.Any(profile => profile.HostKey.Equals("local-machine", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(profile.CpuName)))
                localCpuName = await hardwareInventory.GetCpuNameAsync(cancellationToken).ConfigureAwait(false);

            var configured = new List<InitialSetupHardwareDevice>();
            foreach (var profile in profiles)
            {
                var endpoint = ResolveProfileEndpoint(profile);
                var profileCpuName = string.IsNullOrWhiteSpace(profile.CpuName) && profile.HostKey.Equals("local-machine", StringComparison.OrdinalIgnoreCase)
                    ? localCpuName
                    : profile.CpuName;
                var memoryGiB = profile.SystemMemoryBytes is > 0
                    ? profile.SystemMemoryBytes.Value / 1024d / 1024d / 1024d
                    : profile.HostKey.Equals("local-machine", StringComparison.OrdinalIgnoreCase) && localSystemMemoryBytes is > 0
                        ? localSystemMemoryBytes.Value / 1024d / 1024d / 1024d
                        : (double?)null;
                if (profile.Gpus.Count == 0)
                {
                    configured.Add(new InitialSetupHardwareDevice
                    {
                        Key = $"{profile.HostKey}:host",
                        Endpoint = endpoint,
                        HostKey = profile.HostKey,
                        CpuName = profileCpuName,
                        SystemMemoryGiB = memoryGiB,
                        Source = profile.SourceKind,
                        Selected = true
                    });
                    continue;
                }
                foreach (var gpu in profile.Gpus)
                {
                    configured.Add(new InitialSetupHardwareDevice
                    {
                        Key = $"{profile.HostKey}:{gpu.Index}",
                        Endpoint = endpoint,
                        HostKey = profile.HostKey,
                        CpuName = profileCpuName,
                        Name = gpu.Name,
                        Vendor = gpu.Vendor,
                        DedicatedVramGiB = gpu.DedicatedMemoryBytes is > 0 ? gpu.DedicatedMemoryBytes.Value / 1024d / 1024d / 1024d : null,
                        SystemMemoryGiB = memoryGiB,
                        Source = profile.SourceKind,
                        Selected = true
                    });
                }
            }
            if (configured.Count > 0)
                return configured;

            var detected = await hardwareInventory.GetHardwareAsync(cancellationToken).ConfigureAwait(false);
            var systemMemoryBytes = await hardwareInventory.GetSystemMemoryBytesAsync(cancellationToken).ConfigureAwait(false);
            var systemMemoryGiB = systemMemoryBytes is > 0 ? systemMemoryBytes.Value / 1024d / 1024d / 1024d : (double?)null;
            var cpu = detected.FirstOrDefault(item => item.Kind == OneWireHardwareKind.Cpu);
            var cpuName = cpu?.Name ?? await hardwareInventory.GetCpuNameAsync(cancellationToken).ConfigureAwait(false);
            var gpus = detected.Where(item => item.Kind == OneWireHardwareKind.Gpu).ToList();
            if (gpus.Count == 0)
            {
                return
                [
                    new InitialSetupHardwareDevice
                    {
                        Key = "local:host",
                        Endpoint = "http://127.0.0.1:11434",
                        HostKey = "local-machine",
                        CpuName = cpuName,
                        SystemMemoryGiB = systemMemoryGiB,
                        Source = "LocalProbe",
                        Selected = true
                    }
                ];
            }
            return gpus.Select((gpu, index) => new InitialSetupHardwareDevice
            {
                Key = $"local:{index}",
                Endpoint = "http://127.0.0.1:11434",
                HostKey = "local-machine",
                CpuName = cpuName,
                Name = gpu.Name,
                Vendor = gpu.Vendor,
                DedicatedVramGiB = gpu.DedicatedMemoryBytes is > 0 ? gpu.DedicatedMemoryBytes.Value / 1024d / 1024d / 1024d : null,
                SystemMemoryGiB = systemMemoryGiB,
                Source = "LocalProbe",
                Selected = true
            }).ToList();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Building initial hardware list was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building initial hardware list failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves profile endpoint as part of the initial setup assistant service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveProfileEndpoint(ConfiguredAiHostHardwareProfile profile)
    {
        try
        {
            var endpoints = jsonText.Deserialize<List<string>>(profile.ProviderEndpointsJson) ?? [];
            var endpoint = endpoints.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
            if (!string.IsNullOrWhiteSpace(endpoint))
                return endpoint;
            return profile.HostKey.Equals("local-machine", StringComparison.OrdinalIgnoreCase)
                ? "http://127.0.0.1:11434"
                : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Resolving a configured hardware profile endpoint failed for host {HostKey}; an editable blank endpoint will be shown.", profile.HostKey);
            return string.Empty;
        }
    }

    /// <summary>Resolves conservative CanIRun.ai slug patterns whose Ollama family/tag syntax is stable and unambiguous.</summary>
    /// <param name="recommendationId">CanIRun.ai recommendation identifier.</param>
    /// <returns>The Ollama model token, or <see langword="null"/> when LocalGPT cannot infer one safely.</returns>
    private string? TryResolveKnownOllamaRecommendationId(string recommendationId)
    {
        try
        {
            var value = recommendationId?.Trim() ?? string.Empty;
            var separator = value.LastIndexOf('-');
            if (separator <= 0 || separator >= value.Length - 1)
                return null;
            var family = value[..separator];
            var size = value[(separator + 1)..];
            if (!System.Text.RegularExpressions.Regex.IsMatch(size, @"^\d+(?:\.\d+)?b$", System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
                return null;
            var knownFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "gpt-oss",
                "qwen3",
                "qwen3.5",
                "qwen2.5",
                "qwen2.5-coder",
                "llama3.1",
                "llama3.2",
                "llama3.3",
                "deepseek-r1",
                "gemma2",
                "gemma3",
                "codegemma",
                "codellama",
                "deepscaler"
            };
            return knownFamilies.Contains(family) ? $"{family}:{size}" : null;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Inferring a conservative Ollama recommendation identifier failed.");
            return null;
        }
    }

    /// <summary>
    /// Performs model names equivalent as part of the initial setup assistant service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="installedName">Installed name value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="requestedName">Requested name value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool ModelNamesEquivalent(string installedName, string requestedName)
    {
        try
        {
            var installed = (installedName ?? string.Empty).Trim();
            var requested = (requestedName ?? string.Empty).Trim();
            if (installed.Equals(requested, StringComparison.OrdinalIgnoreCase))
                return true;
            var installedWithoutLatest = installed.EndsWith(":latest", StringComparison.OrdinalIgnoreCase) ? installed[..^7] : installed;
            var requestedWithoutLatest = requested.EndsWith(":latest", StringComparison.OrdinalIgnoreCase) ? requested[..^7] : requested;
            return installedWithoutLatest.Equals(requestedWithoutLatest, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Comparing provider model identifiers failed; identifiers omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether curator role as part of the initial setup assistant service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="roleName">Role name value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsCuratorRole(string roleName)
    {
        try
        {
            var value = roleName ?? string.Empty;
            return value.Contains("Curator", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Director", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Reviewer", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Auditor", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Analyst", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Synthesizer", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Classifying benchmark role for preferred-model assignment failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs to bytes as part of the initial setup assistant service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gib">Gib value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <returns>The long produced by the operation.</returns>
    private long? ToBytes(double? gib)
    {
        try
        {
            if (gib is null || gib <= 0d) return null;
            return checked((long)Math.Round(gib.Value * 1024d * 1024d * 1024d, MidpointRounding.AwayFromZero));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Converting initial setup VRAM GiB to bytes failed.");
            throw;
        }
    }
}
