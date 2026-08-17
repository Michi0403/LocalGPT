using System.Runtime.InteropServices;
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
    ILogger<InitialSetupAssistantService> logger) : IInitialSetupAssistantService
{
    /// <summary>Builds current hardware/provider/model state without changing the machine.</summary>
    /// <inheritdoc />
    public async Task<InitialSetupAssistantSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var hardware = await BuildHardwareListAsync(cancellationToken).ConfigureAwait(false);
            var profiles = await providerBootstrap.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<MultiModelCouncilModelCandidate> candidates;
            try
            {
                candidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Provider model discovery was unavailable while building initial setup snapshot; setup remains usable for provider installation.");
                candidates = [];
            }
            var status = await onboarding.GetStatusAsync(refreshConnectivity: false, cancellationToken).ConfigureAwait(false);
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
                IsOnboardingCompleted = status.IsCompleted,
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
            foreach (var device in devices.Where(item => item.Selected && !string.IsNullOrWhiteSpace(item.CanIRunSlug)).Take(32))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var endpoint = device.Endpoint ?? string.Empty;
                var hostKey = string.IsNullOrWhiteSpace(device.HostKey) && !string.IsNullOrWhiteSpace(endpoint)
                    ? configuredHardware.GetHostKey(endpoint)
                    : device.HostKey;
                var items = await canIRun.GetRecommendationsAsync(device.CanIRunSlug, userConfirmedWebLookup: true, cancellationToken).ConfigureAwait(false);
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
                        DeviceSlug = item.DeviceSlug,
                        DeviceEndpoint = endpoint,
                        HostKey = hostKey ?? string.Empty,
                        SourceUrl = item.SourceUrl
                    });
                }
            }
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

    /// <summary>Resolves recommendation IDs through the selected provider profile and matches them against the authoritative provider-model runtime.</summary>
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
            var identity = new ProviderModelIdentity();
            var profileEndpoint = identity.NormalizeEndpoint(profile.Endpoint);
            var candidates = (await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false))
                .Where(item => item.ProviderKind.Equals(profile.ProviderKind, StringComparison.OrdinalIgnoreCase)
                    && identity.NormalizeEndpoint(item.Endpoint).Equals(profileEndpoint, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var endpointScopedRecommendations = recommendations
                .Where(item => string.IsNullOrWhiteSpace(item.DeviceEndpoint)
                    || identity.NormalizeEndpoint(item.DeviceEndpoint).Equals(profileEndpoint, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var effectiveRecommendations = endpointScopedRecommendations.Count > 0 ? endpointScopedRecommendations : recommendations.ToList();
            var choices = new List<InitialSetupModelChoice>();
            foreach (var recommendation in effectiveRecommendations
                .GroupBy(item => item.ModelId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.Score).First())
                .OrderByDescending(item => item.Score)
                .Take(96))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var providerId = await providerBootstrap.ResolveModelIdAsync(profileKey, recommendation.ModelId, cancellationToken).ConfigureAwait(false);
                var installedCandidate = candidates.FirstOrDefault(item => item.IsInstalled && ModelNamesEquivalent(item.ModelName, providerId));
                var installed = installedCandidate is not null;
                choices.Add(new InitialSetupModelChoice
                {
                    RecommendationId = recommendation.ModelId,
                    ProviderModelId = providerId,
                    DisplayName = recommendation.ModelName,
                    SelectionKey = installedCandidate?.SelectionKey ?? string.Empty,
                    IsInstalled = installed,
                    RecommendationScore = recommendation.Score,
                    RecommendationGrade = recommendation.Grade,
                    Selected = installed && recommendation.Score >= 40
                });
            }
            return choices;
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
            var selected = devices.Where(item => item.Selected && !string.IsNullOrWhiteSpace(item.Name)).Take(32).ToList();
            draft.Gpus = selected.Select((item, index) => new ConfiguredAiHostGpu
            {
                Index = index,
                Name = item.Name.Trim(),
                Vendor = (item.Vendor ?? string.Empty).Trim(),
                DedicatedMemoryBytes = ToBytes(item.DedicatedVramGiB)
            }).ToList();
            var primary = selected.FirstOrDefault();
            if (primary is not null)
            {
                draft.GpuName = primary.Name.Trim();
                draft.GpuVendor = (primary.Vendor ?? string.Empty).Trim();
                draft.DedicatedVramGiB = primary.DedicatedVramGiB;
            }
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
            var selected = devices.Where(item => item.Selected && !string.IsNullOrWhiteSpace(item.Name)).Take(64).ToList();
            if (selected.Count == 0)
                throw new InvalidOperationException("Select at least one hardware device before saving the setup list.");
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
            logger.LogInformation("Saved {HostCount} physical host hardware profile(s) from {GpuCount} reviewed setup device row(s).", savedProfiles.Count, selected.Count);
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
            var selected = request.ModelSelectionKeys.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).Take(128).ToList();
            if (selected.Count == 0)
                throw new InvalidOperationException("Select at least one installed provider-qualified model before creating the benchmark team.");
            var preferred = request.PreferredCuratorModelKeys
                .Where(item => selected.Contains(item, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4)
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
            var configured = profiles
                .SelectMany(profile => profile.Gpus.Select(gpu => new InitialSetupHardwareDevice
                {
                    Key = $"{profile.HostKey}:{gpu.Index}",
                    Endpoint = ResolveProfileEndpoint(profile),
                    HostKey = profile.HostKey,
                    Name = gpu.Name,
                    Vendor = gpu.Vendor,
                    DedicatedVramGiB = gpu.DedicatedMemoryBytes is > 0 ? gpu.DedicatedMemoryBytes.Value / 1024d / 1024d / 1024d : null,
                    Source = profile.SourceKind,
                    Selected = true
                }))
                .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                .ToList();
            if (configured.Count > 0)
                return configured;

            var detected = await hardwareInventory.GetHardwareAsync(cancellationToken).ConfigureAwait(false);
            return detected
                .Where(item => item.Kind == OneWireHardwareKind.Gpu)
                .Select((gpu, index) => new InitialSetupHardwareDevice
                {
                    Key = $"local:{index}",
                    Endpoint = "http://127.0.0.1:11434",
                    HostKey = "local-machine",
                    Name = gpu.Name,
                    Vendor = gpu.Vendor,
                    DedicatedVramGiB = gpu.DedicatedMemoryBytes is > 0 ? gpu.DedicatedMemoryBytes.Value / 1024d / 1024d / 1024d : null,
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
            var installedBase = installed.Split(':', 2)[0];
            var requestedBase = requested.Split(':', 2)[0];
            return installedBase.Equals(requestedBase, StringComparison.OrdinalIgnoreCase);
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
