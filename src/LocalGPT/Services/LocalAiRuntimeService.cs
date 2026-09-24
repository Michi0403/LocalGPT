using LocalGPT.BusinessObjects;
using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;
using LocalGPT.Interfaces;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>Coordinates LocalGPT-managed Python environments, specialized local models and media inference.</summary>
public sealed partial class LocalAiRuntimeService(
    IOptionsMonitor<LocalGptConfigurationRoot> options,
    IConfigurationWriter configurationWriter,
    IToolchainDiscoveryService toolchainDiscovery,
    IToolchainExecutionProfileService toolchainProfiles,
    IProjectMaintenanceService projectMaintenance,
    IPythonNetRuntimeCoordinator python,
    ILocalAiArtifactService artifacts,
    IChatUploadWorkspaceService uploads,
    IPlatformRuntimeService platform,
    IWebHostEnvironment environment,
    ILogger<LocalAiRuntimeService> logger) : ILocalAiRuntimeService
{
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly HashSet<LocalAiCapability> ExecutableCapabilities =
    [
        LocalAiCapability.ImageGeneration,
        LocalAiCapability.ImageEditing,
        LocalAiCapability.TextToVideo,
        LocalAiCapability.ImageToVideo,
        LocalAiCapability.SpeechRecognition
    ];
    private const string RuntimePolicyProfileKey = "localai.runtime-policy";
    private PythonCoreOptions? configuredOverride;
    private bool runtimePolicyRestartRequired;
    private bool runtimePolicyLoaded;
    private readonly SemaphoreSlim runtimePolicyLoadGate = new(1, 1);

    public async Task<IReadOnlyList<PythonRuntimeCandidate>> DiscoverPythonAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            var discovered = await toolchainDiscovery.DiscoverAsync(maximumCandidates: 128, cancellationToken: cancellationToken).ConfigureAwait(false);
            var result = new List<PythonRuntimeCandidate>();
            var seen = new HashSet<string>(platform.PathComparer);
            foreach (var candidate in discovered.Where(item => item.ProfileKey.Equals("python", StringComparison.OrdinalIgnoreCase)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var probed = await ProbePythonCandidateAsync(candidate.ExecutablePath, candidate.DiscoverySource, cancellationToken).ConfigureAwait(false);
                if (probed is not null && seen.Add(probed.ExecutablePath))
                    result.Add(probed);
            }
            return result.OrderByDescending(item => item.RuntimeLibraryDetected)
                .ThenByDescending(item => ParsePythonVersion(item.Version))
                .ThenBy(item => item.ExecutablePath, platform.PathComparer)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(DiscoverPythonAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(DiscoverPythonAsync)} failed.");
        throw;
    }
}

    public async Task<LocalAiRuntimeStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            var config = CurrentConfig();
            var environmentPath = ResolveEnvironmentPath(config);
            var modelCount = GetInstalledModels().Count;
            return new LocalAiRuntimeStatus
            {
                Configured = !string.IsNullOrWhiteSpace(config.PythonExecutable) && !string.IsNullOrWhiteSpace(config.PythonRuntime),
                PythonExecutableExists = !string.IsNullOrWhiteSpace(config.PythonExecutable) && File.Exists(config.PythonExecutable),
                PythonRuntimeLibraryExists = !string.IsNullOrWhiteSpace(config.PythonRuntime) && File.Exists(config.PythonRuntime),
                VirtualEnvironmentExists = Directory.Exists(environmentPath),
                PythonNetAvailable = true,
                InterpreterInitialized = python.IsInitialized,
                RestartRequired = python.RestartRequired || runtimePolicyRestartRequired,
                QueueLength = python.QueueLength,
                InstalledModelCount = modelCount,
                Message = python.RestartRequired || runtimePolicyRestartRequired
                    ? "Python runtime binding or queue policy changed after interpreter initialization; restart LocalGPT before relying on the changed startup-only settings."
                    : "Local AI runtime status loaded."
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GetStatusAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GetStatusAsync)} failed.");
        throw;
    }
}

    public async Task<LocalAiRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
            if (!status.Configured || !status.VirtualEnvironmentExists || !status.PythonNetAvailable)
                return status;
            var result = await python.ExecuteAsync(new LocalAiRuntimeJobRequest
            {
                Operation = "runtime_probe",
                Parameters = new Dictionary<string, object?> { ["device"] = CurrentConfig().DefaultDevice }
            }, cancellationToken).ConfigureAwait(false);
            status.InterpreterInitialized = python.IsInitialized;
            status.RestartRequired = python.RestartRequired;
            status.QueueLength = python.QueueLength;
            status.PythonVersion = result.Metadata.GetValueOrDefault("python_version", string.Empty);
            var device = result.Metadata.GetValueOrDefault("device", string.Empty);
            var backend = result.Metadata.GetValueOrDefault("backend", string.Empty);
            var deviceName = result.Metadata.GetValueOrDefault("device_name", string.Empty);
            var backendLabel = string.IsNullOrWhiteSpace(backend) || backend.Equals(device, StringComparison.OrdinalIgnoreCase) ? device : $"{backend}/{device}";
            status.DeviceSummary = string.IsNullOrWhiteSpace(deviceName) ? backendLabel : $"{backendLabel} · {deviceName}";
            status.Message = result.Succeeded ? "Embedded Python.NET runtime probe completed." : result.Message;
            return status;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ProbeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ProbeAsync)} failed.");
        throw;
    }
}

    public async Task<LocalAiRuntimeConfiguration> GetRuntimeConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            return BuildRuntimeConfiguration(CurrentConfig());
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading the LocalGPT Python execution policy was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the LocalGPT Python execution policy failed; runtime paths and environment values were omitted from logs.");
            throw;
        }
    }

    public async Task<LocalAiRuntimeConfiguration> UpdateRuntimeConfigurationAsync(LocalAiRuntimeConfigurationChangeRequest request, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireConfirmation(userConfirmed, "changing the LocalGPT Python execution policy");
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var device = NormalizeRuntimeDevice(request.DefaultDevice);
            var whisperModel = NormalizeWhisperVariant(request.DefaultWhisperModel);
            var speechTask = NormalizeSpeechTask(request.DefaultSpeechTask);
            if (request.DefaultSpeechLanguage?.Length > 40)
                throw new ArgumentException("Default speech language must not exceed 40 characters.", nameof(request));
            if (request.DefaultSpeechInitialPrompt?.Length > 4000)
                throw new ArgumentException("Default speech initial prompt must not exceed 4000 characters.", nameof(request));
            if (request.QueueCapacity is < 1 or > 1024)
                throw new ArgumentOutOfRangeException(nameof(request), "Python queue capacity must be between 1 and 1024.");
            if (request.MaximumInputMegabytes is < 1 or > 4096)
                throw new ArgumentOutOfRangeException(nameof(request), "Maximum input size must be between 1 and 4096 MiB.");
            if (request.MaximumImagePixels is < 1_000_000 or > 1_000_000_000)
                throw new ArgumentOutOfRangeException(nameof(request), "Maximum image pixels must be between 1,000,000 and 1,000,000,000.");
            if (request.MaximumAudioSeconds is < 1 or > 86_400)
                throw new ArgumentOutOfRangeException(nameof(request), "Maximum audio duration must be between 1 and 86,400 seconds.");
            if (request.MinimumAudioBytesPerSecond is < 1 or > 1_048_576)
                throw new ArgumentOutOfRangeException(nameof(request), "Minimum audio byte density is outside the supported range.");

            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            var previous = CurrentConfig();
            var updated = ClonePythonCore(previous);
            updated.DefaultDevice = device;
            updated.DefaultWhisperModel = whisperModel;
            updated.DefaultSpeechLanguage = (request.DefaultSpeechLanguage ?? string.Empty).Trim();
            updated.DefaultSpeechTask = speechTask;
            updated.DefaultSpeechInitialPrompt = (request.DefaultSpeechInitialPrompt ?? string.Empty).Trim();
            updated.QueueCapacity = request.QueueCapacity;
            updated.MaximumInputMegabytes = request.MaximumInputMegabytes;
            updated.MaximumImagePixels = request.MaximumImagePixels;
            updated.MaximumAudioSeconds = request.MaximumAudioSeconds;
            updated.MinimumAudioBytesPerSecond = request.MinimumAudioBytesPerSecond;
            updated.CacheModels = request.CacheModels;

            if (python.IsInitialized && previous.QueueCapacity != updated.QueueCapacity)
                runtimePolicyRestartRequired = true;

            var persistedPolicy = new LocalAiRuntimeConfigurationChangeRequest
            {
                DefaultDevice = updated.DefaultDevice,
                DefaultWhisperModel = updated.DefaultWhisperModel,
                DefaultSpeechLanguage = updated.DefaultSpeechLanguage,
                DefaultSpeechTask = updated.DefaultSpeechTask,
                DefaultSpeechInitialPrompt = updated.DefaultSpeechInitialPrompt,
                QueueCapacity = updated.QueueCapacity,
                MaximumInputMegabytes = updated.MaximumInputMegabytes,
                MaximumImagePixels = updated.MaximumImagePixels,
                MaximumAudioSeconds = updated.MaximumAudioSeconds,
                MinimumAudioBytesPerSecond = updated.MinimumAudioBytesPerSecond,
                CacheModels = updated.CacheModels
            };
            await toolchainProfiles.SaveProfileAsync(new SaveToolchainExecutionProfileRequest
            {
                UserConfirmed = true,
                Profile = new ToolchainExecutionProfile
                {
                    ProfileKey = RuntimePolicyProfileKey,
                    Name = "Local AI runtime policy",
                    CapabilityKey = "localai.runtime.policy",
                    ExecutionKind = ToolchainExecutionKind.ToolchainExecutable,
                    ConfigurationJson = JsonSerializer.Serialize(persistedPolicy, JsonOptions),
                    IsEnabled = true,
                    IsDefaultForCapability = true,
                    RequiresApproval = false,
                    UpdatedBy = "Local AI runtime service"
                }
            }, cancellationToken).ConfigureAwait(false);
            configuredOverride = updated;
            runtimePolicyLoaded = true;
            logger.LogInformation("Saved LocalGPT local-AI execution policy in the database-backed toolchain profile store; prompt text, paths and environment values were omitted from logs.");
            return BuildRuntimeConfiguration(updated);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Updating the LocalGPT Python execution policy was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Updating the LocalGPT Python execution policy failed; runtime paths, prompts and environment values were omitted from logs.");
            throw;
        }
    }

    public async Task ConfigurePythonAsync(PythonRuntimeCandidate candidate, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(candidate);
            if (!File.Exists(candidate.ExecutablePath))
                throw new FileNotFoundException("The selected Python executable no longer exists.", candidate.ExecutablePath);
            if (string.IsNullOrWhiteSpace(candidate.PythonRuntimeLibrary) || !File.Exists(candidate.PythonRuntimeLibrary))
                throw new InvalidOperationException("The selected Python installation has no resolved shared runtime library. Python.NET cannot bind it safely.");

            var root = CloneConfiguration(options.CurrentValue);
            root.PythonCore ??= new PythonCoreOptions();
            root.PythonCore.PythonExecutable = Path.GetFullPath(candidate.ExecutablePath);
            root.PythonCore.PythonRuntime = Path.GetFullPath(candidate.PythonRuntimeLibrary);
            root.PythonCore.PythonHome = Path.GetFullPath(candidate.PythonHome);
            root.PythonCore.VirtualEnvironmentPath = ResolveEnvironmentPath(root.PythonCore);
            root.PythonCore.ModelRoot = ResolveModelRoot(root.PythonCore);
            root.PythonCore.ArtifactRoot = ResolveArtifactRoot(root.PythonCore);
            await configurationWriter.SaveAsync(root, cancellationToken).ConfigureAwait(false);
            configuredOverride = ClonePythonCore(root.PythonCore);

            var existingPython = (await projectMaintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => platform.PathComparer.Equals(Path.GetFullPath(item.ExecutablePath), Path.GetFullPath(candidate.ExecutablePath)));
            var installation = await projectMaintenance.SaveCompilerInstallationAsync(new SaveProjectCompilerInstallationRequest
            {
                Id = existingPython?.Id,
                Name = $"Python {candidate.Version}".Trim(),
                Language = "Python",
                ExecutablePath = candidate.ExecutablePath,
                CompilerHomePath = candidate.PythonHome,
                Version = candidate.Version,
                Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                DiscoverySource = candidate.DiscoverySource,
                ToolchainKind = "runtime",
                DetectedPlatform = platform.ProviderBootstrapToken,
                ValidationArguments = "--version",
                KnowledgeProfileKey = "python",
                IsEnabled = true,
                IsDefaultForLanguage = true,
                UserConfirmed = true
            }, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Saved a reviewed Python runtime binding in configuration and the generic toolchain database; executable and shared-library paths were omitted from logs.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ConfigurePythonAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ConfigurePythonAsync)} failed.");
        throw;
    }
}

    public async Task CreateManagedEnvironmentAsync(bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            RequireConfirmation(userConfirmed, "creating the LocalGPT-managed Python environment");
            var config = CurrentConfig();
            var pythonExecutable = RequireExistingFile(config.PythonExecutable, "Python executable");
            var environmentPath = ResolveEnvironmentPath(config);
            Directory.CreateDirectory(Path.GetDirectoryName(environmentPath)!);
            if (!Directory.Exists(environmentPath) || !File.Exists(ResolveEnvironmentPython(environmentPath)))
            {
                var create = await RunProcessAsync(pythonExecutable, ["-m", "venv", environmentPath], Path.GetDirectoryName(environmentPath), TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
                EnsureProcessSuccess(create, "Python virtual-environment creation");
            }
            var profile = GetPackageProfiles().FirstOrDefault(item => item.Key.Equals("core", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("The PythonCore package profile is missing the required 'core' profile.");
            await InstallPackagesAsync(profile, cancellationToken).ConfigureAwait(false);
            await EnsureManagedPythonToolchainAsync(config.DefaultWhisperModel, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("LocalGPT-managed Python environment is ready; environment path and package output were omitted from logs.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(CreateManagedEnvironmentAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(CreateManagedEnvironmentAsync)} failed.");
        throw;
    }
}

    public async Task InstallPackageProfileAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            RequireConfirmation(userConfirmed, "installing Python packages");
            var profile = GetPackageProfiles().FirstOrDefault(item => item.Key.Equals(profileKey, StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException($"Python package profile '{profileKey}' was not found.");
            await InstallPackagesAsync(profile, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(InstallPackageProfileAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(InstallPackageProfileAsync)} failed.");
        throw;
    }
}

    public IReadOnlyList<LocalAiPackageProfile> GetPackageProfiles() {
    try
    {
        return CurrentConfig().PackageProfiles
        .Where(item => !string.IsNullOrWhiteSpace(item.Key))
        .GroupBy(item => item.Key.Trim(), StringComparer.OrdinalIgnoreCase)
        .Select(group => group.Last())
        .Select(item => new LocalAiPackageProfile
        {
            Key = item.Key,
            DisplayName = item.DisplayName,
            Description = item.Description,
            Packages = [.. item.Packages],
            IncludesTorch = item.IncludesTorch
        }).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GetPackageProfiles)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GetPackageProfiles)} failed.");
        throw;
    }
}

    public string FormatPackageList(IEnumerable<string> packages)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(packages);
            return string.Join(" · ", packages);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(FormatPackageList)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(FormatPackageList)} failed.");
            throw;
        }
    }

    public string FormatCapabilities(IEnumerable<LocalAiCapability> capabilities)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(capabilities);
            var values = capabilities.Where(value => value != LocalAiCapability.Unknown).Select(value => value.ToString()).ToArray();
            return values.Length == 0 ? "unclassified" : string.Join(", ", values);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(FormatCapabilities)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(FormatCapabilities)} failed.");
            throw;
        }
    }

    public IReadOnlyList<LocalAiModelInstallation> GetInstalledModels()
    {
        var modelRoot = ResolveModelRoot(CurrentConfig());
        var manifests = Path.Combine(modelRoot, ".localgpt", "manifests");
        if (!Directory.Exists(manifests))
            return [];
        var result = new List<LocalAiModelInstallation>();
        foreach (var file in Directory.EnumerateFiles(manifests, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var model = JsonSerializer.Deserialize<LocalAiModelInstallation>(File.ReadAllText(file), JsonOptions);
                if (model is not null && !string.IsNullOrWhiteSpace(model.InstallationId) && Directory.Exists(model.LocalPath) && platform.IsSameOrDescendantPath(modelRoot, model.LocalPath))
                    result.Add(model);
            }
            catch (Exception exception)
            {
                logger.LogWarning("Skipped an unreadable local-AI model manifest after {ExceptionType}; exception text, path and model identity were omitted from logs.", exception.GetType().Name);
            }
        }
        return result.OrderByDescending(item => item.InstalledAtUtc).ToList();
    }

    public async Task<LocalAiModelInstallation> InstallModelAsync(LocalAiModelInstallRequest request, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(request);
        RequireConfirmation(userConfirmed, "installing a Hugging Face model snapshot");
        if (string.IsNullOrWhiteSpace(request.ModelId) || request.ModelId.Length > 300)
            throw new ArgumentException("A bounded Hugging Face model id is required.", nameof(request));

        var capabilities = request.Capabilities
            .Where(value => value != LocalAiCapability.Unknown)
            .Distinct()
            .ToList();
        if (capabilities.Count == 0)
            throw new InvalidOperationException("A specialized model must be bound to at least one executable LocalGPT capability.");
        var unsupported = capabilities.Where(value => !ExecutableCapabilities.Contains(value)).ToArray();
        if (unsupported.Length > 0)
            throw new NotSupportedException($"This LocalGPT release does not yet execute capability binding(s): {string.Join(", ", unsupported)}.");
        var config = CurrentConfig();
        if (request.RequiresAuthentication)
        {
            var tokenName = config.HuggingFaceTokenEnvironmentVariable?.Trim();
            if (string.IsNullOrWhiteSpace(tokenName) || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(tokenName)))
                throw new InvalidOperationException("This gated/private model requires the configured Hugging Face token environment variable to be present in the LocalGPT process.");
        }

        var revision = string.IsNullOrWhiteSpace(request.Revision) ? "main" : request.Revision.Trim();
        if (revision.Length > 200)
            throw new ArgumentException("The Hugging Face revision is too long.", nameof(request));
        var modelRoot = ResolveModelRoot(config);
        Directory.CreateDirectory(modelRoot);
        var installationId = BuildInstallationId(request.ModelId.Trim(), revision);
        var localPath = Path.Combine(modelRoot, installationId);
        if (!platform.IsSameOrDescendantPath(modelRoot, localPath))
            throw new InvalidOperationException("Resolved model path escaped the managed model root.");
        if (Directory.Exists(localPath) || File.Exists(ManifestPath(modelRoot, installationId)))
            throw new InvalidOperationException("This model revision is already installed or has an existing managed path. Remove it before reinstalling.");

        var incomingRoot = Path.Combine(modelRoot, ".localgpt", "incoming");
        Directory.CreateDirectory(incomingRoot);
        var incomingPath = Path.Combine(incomingRoot, $"{installationId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(incomingPath);
        var promotedToModelRoot = false;
        try
        {
            var result = await RunBridgeProcessAsync("hf_snapshot_download", new Dictionary<string, object?>
            {
                ["model_id"] = request.ModelId.Trim(),
                ["revision"] = revision,
                ["local_dir"] = incomingPath,
                ["token_environment_variable"] = config.HuggingFaceTokenEnvironmentVariable
            }, TimeSpan.FromHours(12), cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Message) ? "Hugging Face model snapshot installation failed." : result.Message);

            cancellationToken.ThrowIfCancellationRequested();
            Directory.Move(incomingPath, localPath);
            promotedToModelRoot = true;
            var installation = new LocalAiModelInstallation
            {
                InstallationId = installationId,
                ModelId = request.ModelId.Trim(),
                Revision = revision,
                LocalPath = localPath,
                Adapter = string.IsNullOrWhiteSpace(request.Adapter) ? InferAdapter(capabilities) : request.Adapter.Trim(),
                Capabilities = capabilities,
                TrustRemoteCode = request.TrustRemoteCode,
                SourceProvider = "Hugging Face Hub",
                SourceReference = request.ModelId.Trim(),
                RuntimeModelName = request.ModelId.Trim(),
                InstalledAtUtc = DateTime.UtcNow
            };
            SaveManifest(modelRoot, installation);
            logger.LogInformation("Installed specialized local-AI model {InstallationId} with {CapabilityCount} capability binding(s); model identity and path were omitted from logs.", installationId, installation.Capabilities.Count);
            return installation;
        }
        catch
        {
            if (promotedToModelRoot)
                TryDeleteDirectory(localPath);
            throw;
        }
        finally
        {
            TryDeleteDirectory(incomingPath);
        }
    }

    public async Task<LocalAiModelInstallation> InstallOpenAiWhisperAsync(string sourceArchivePath, string variant, bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            RequireConfirmation(userConfirmed, "installing OpenAI Whisper from reviewed GitHub source and downloading its selected model weights");
            if (python.IsInitialized)
                throw new InvalidOperationException("The embedded Python runtime is already initialized. Restart LocalGPT before installing Python packages or a new Whisper source build.");
            var sourceRoot = LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Sources");
            var fullSourcePath = Path.GetFullPath(sourceArchivePath ?? string.Empty);
            if (!File.Exists(fullSourcePath) || !platform.IsSameOrDescendantPath(sourceRoot, fullSourcePath) || !Path.GetExtension(fullSourcePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("OpenAI Whisper installation accepts only a reviewed source ZIP inside LocalGPT's managed LocalAiRuntime/Sources root.");
            var modelName = (variant ?? string.Empty).Trim().ToLowerInvariant();
            var supportedVariants = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tiny", "base", "small", "medium", "large-v3", "turbo" };
            if (!supportedVariants.Contains(modelName))
                throw new ArgumentException("The selected OpenAI Whisper model variant is not in LocalGPT's reviewed catalog.", nameof(variant));

            var environmentPath = ResolveEnvironmentPath(CurrentConfig());
            var environmentPython = RequireExistingFile(ResolveEnvironmentPython(environmentPath), "Managed environment Python executable");
            var supportProfile = GetPackageProfiles().FirstOrDefault(item => item.Key.Equals("whisper-runtime", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("The PythonCore package profile is missing the required 'whisper-runtime' profile.");
            await InstallPackagesAsync(supportProfile, cancellationToken).ConfigureAwait(false);
            var torchProbe = await RunProcessAsync(environmentPython, ["-c", "import torch; print(torch.__version__)"], environmentPath, TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            if (torchProbe.ExitCode != 0)
                throw new InvalidOperationException("OpenAI Whisper requires PyTorch. Install the PyTorch backend appropriate for this machine from the Python capability profiles before installing Whisper.");

            var sourceInstall = await RunProcessAsync(
                environmentPython,
                ["-m", "pip", "install", "--disable-pip-version-check", "--no-deps", fullSourcePath],
                environmentPath,
                TimeSpan.FromMinutes(30),
                cancellationToken).ConfigureAwait(false);
            EnsureProcessSuccess(sourceInstall, "OpenAI Whisper source installation");

            var modelRoot = ResolveModelRoot(CurrentConfig());
            Directory.CreateDirectory(modelRoot);
            var installationId = BuildInstallationId("openai/whisper", modelName);
            var localPath = Path.Combine(modelRoot, installationId);
            if (!platform.IsSameOrDescendantPath(modelRoot, localPath))
                throw new InvalidOperationException("Resolved Whisper model path escaped the managed model root.");
            if (Directory.Exists(localPath) || File.Exists(ManifestPath(modelRoot, installationId)))
                throw new InvalidOperationException("This OpenAI Whisper variant is already installed or has an existing managed path. Remove it before reinstalling.");

            var incomingRoot = Path.Combine(modelRoot, ".localgpt", "incoming");
            Directory.CreateDirectory(incomingRoot);
            var incomingPath = Path.Combine(incomingRoot, $"{installationId}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(incomingPath);
            var promoted = false;
            try
            {
                var download = await RunBridgeProcessAsync("openai_whisper_download", new Dictionary<string, object?>
                {
                    ["model_name"] = modelName,
                    ["local_dir"] = incomingPath,
                    ["device"] = CurrentConfig().DefaultDevice
                }, TimeSpan.FromHours(12), cancellationToken).ConfigureAwait(false);
                if (!download.Succeeded)
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(download.Message) ? "OpenAI Whisper model-weight download failed." : download.Message);
                cancellationToken.ThrowIfCancellationRequested();
                Directory.Move(incomingPath, localPath);
                promoted = true;
                var installation = new LocalAiModelInstallation
                {
                    InstallationId = installationId,
                    ModelId = $"openai/whisper:{modelName}",
                    Revision = "upstream-main",
                    LocalPath = localPath,
                    Adapter = "openai-whisper",
                    Capabilities = [LocalAiCapability.SpeechRecognition],
                    TrustRemoteCode = false,
                    SourceProvider = "GitHub + Python",
                    SourceReference = "https://github.com/openai/whisper",
                    RuntimeModelName = modelName,
                    InstalledAtUtc = DateTime.UtcNow
                };
                SaveManifest(modelRoot, installation);
                await EnsureManagedPythonToolchainAsync(modelName, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Installed OpenAI Whisper variant through the reviewed GitHub/Python path; variant, source path and model path were omitted from logs.");
                return installation;
            }
            catch
            {
                if (promoted)
                    TryDeleteDirectory(localPath);
                throw;
            }
            finally
            {
                TryDeleteDirectory(incomingPath);
            }
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(InstallOpenAiWhisperAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(InstallOpenAiWhisperAsync)} failed; source, model identity and paths were omitted.");
        throw;
    }
}

    public async Task RemoveModelAsync(string installationId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
        RequireConfirmation(userConfirmed, "removing a managed local-AI model");
        var model = FindInstallation(installationId);
        var modelRoot = ResolveModelRoot(CurrentConfig());
        if (!platform.IsSameOrDescendantPath(modelRoot, model.LocalPath))
            throw new InvalidOperationException("The installed model path is outside the managed model root.");
        try
        {
            await python.ExecuteAsync(new LocalAiRuntimeJobRequest
            {
                Operation = "model_evict",
                Parameters = new Dictionary<string, object?> { ["model_path"] = model.LocalPath }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            // Model removal must remain available even if Python.NET is not configured or the
            // embedded runtime cannot initialize. An active in-process pipeline may retain native
            // memory until restart, but the managed snapshot and manifest can still be removed.
            logger.LogWarning("Could not evict a model from the embedded Python cache before removal after {ExceptionType}; exception text, model identity and paths were omitted.", exception.GetType().Name);
        }
        if (Directory.Exists(model.LocalPath))
            Directory.Delete(model.LocalPath, recursive: true);
        var manifest = ManifestPath(modelRoot, model.InstallationId);
        if (File.Exists(manifest))
            File.Delete(manifest);
        logger.LogInformation("Removed managed local-AI model {InstallationId}; model identity and path were omitted from logs.", installationId);
    }

    public async Task ClearModelCacheAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!python.IsInitialized)
                return;
            var result = await python.ExecuteAsync(new LocalAiRuntimeJobRequest
            {
                Operation = "cache_clear"
            }, cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Message) ? "The embedded model cache could not be cleared." : result.Message);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ClearModelCacheAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ClearModelCacheAsync)} failed.");
        throw;
    }
}

    public async Task<LocalAiRuntimeJobResult> GenerateImageAsync(LocalAiImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(request);
            var model = RequireCapability(request.ModelInstallationId, LocalAiCapability.ImageGeneration);
            if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Length > 16000)
                throw new ArgumentException("Image prompt must contain between 1 and 16000 characters.", nameof(request));
            var config = CurrentConfig();
            var result = await python.ExecuteAsync(new LocalAiRuntimeJobRequest
            {
                Operation = "image_generate",
                ModelInstallationId = model.InstallationId,
                Parameters = new Dictionary<string, object?>
                {
                    ["model_path"] = model.LocalPath,
                    ["trust_remote_code"] = model.TrustRemoteCode,
                    ["cache_models"] = config.CacheModels,
                    ["device"] = config.DefaultDevice,
                    ["prompt"] = request.Prompt,
                    ["negative_prompt"] = request.NegativePrompt,
                    ["width"] = Math.Clamp(request.Width, 256, 4096),
                    ["height"] = Math.Clamp(request.Height, 256, 4096),
                    ["steps"] = Math.Clamp(request.Steps, 1, 200),
                    ["guidance_scale"] = Math.Clamp(request.GuidanceScale, 0, 30),
                    ["seed"] = request.Seed
                }
            }, cancellationToken).ConfigureAwait(false);
            return await PublishGeneratedArtifactAsync(result, "localgpt-image.png", cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GenerateImageAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GenerateImageAsync)} failed.");
        throw;
    }
}

    public async Task<LocalAiRuntimeJobResult> EditWorkspaceImageAsync(LocalAiImageEditRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(request);
            var model = RequireCapability(request.ModelInstallationId, LocalAiCapability.ImageEditing);
            if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Length > 16000)
                throw new ArgumentException("Image edit prompt must contain between 1 and 16000 characters.", nameof(request));
            var sourcePath = ResolveWorkspaceInput(request.WorkspaceName, request.RelativePath);
            var config = CurrentConfig();
            var maximumBytes = (long)Math.Clamp(config.MaximumInputMegabytes, 1, 4096) * 1024 * 1024;
            EnsureInputSize(sourcePath, maximumBytes);
            var inputDirectory = LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "InputScratch", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inputDirectory);
            var privateCopy = Path.Combine(inputDirectory, Path.GetFileName(sourcePath));
            try
            {
                await CopyPrivateInputAsync(sourcePath, privateCopy, cancellationToken).ConfigureAwait(false);
                var result = await python.ExecuteAsync(new LocalAiRuntimeJobRequest
                {
                    Operation = "image_edit",
                    ModelInstallationId = model.InstallationId,
                    Parameters = new Dictionary<string, object?>
                    {
                        ["model_path"] = model.LocalPath,
                        ["adapter"] = model.Adapter,
                        ["runtime_model_name"] = model.RuntimeModelName,
                        ["trust_remote_code"] = model.TrustRemoteCode,
                        ["cache_models"] = config.CacheModels,
                        ["device"] = config.DefaultDevice,
                        ["input_path"] = privateCopy,
                        ["maximum_input_bytes"] = maximumBytes,
                        ["maximum_image_pixels"] = Math.Clamp(config.MaximumImagePixels, 1_000_000L, 1_000_000_000L),
                        ["prompt"] = request.Prompt,
                        ["negative_prompt"] = request.NegativePrompt,
                        ["width"] = Math.Clamp(request.Width, 256, 4096),
                        ["height"] = Math.Clamp(request.Height, 256, 4096),
                        ["steps"] = Math.Clamp(request.Steps, 1, 200),
                        ["guidance_scale"] = Math.Clamp(request.GuidanceScale, 0, 30),
                        ["seed"] = request.Seed
                    }
                }, cancellationToken).ConfigureAwait(false);
                return await PublishGeneratedArtifactAsync(result, "localgpt-image-edit.png", cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                TryDeleteDirectory(inputDirectory);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(EditWorkspaceImageAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(EditWorkspaceImageAsync)} failed.");
        throw;
    }
}

    public async Task<LocalAiRuntimeJobResult> GenerateVideoAsync(LocalAiVideoGenerationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(request);
            var model = RequireCapability(request.ModelInstallationId, LocalAiCapability.TextToVideo);
            if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Length > 16000)
                throw new ArgumentException("Video prompt must contain between 1 and 16000 characters.", nameof(request));
            var config = CurrentConfig();
            var result = await python.ExecuteAsync(new LocalAiRuntimeJobRequest
            {
                Operation = "video_generate",
                ModelInstallationId = model.InstallationId,
                Parameters = new Dictionary<string, object?>
                {
                    ["model_path"] = model.LocalPath,
                    ["trust_remote_code"] = model.TrustRemoteCode,
                    ["cache_models"] = config.CacheModels,
                    ["device"] = config.DefaultDevice,
                    ["prompt"] = request.Prompt,
                    ["width"] = Math.Clamp(request.Width, 256, 1920),
                    ["height"] = Math.Clamp(request.Height, 256, 1080),
                    ["frames"] = Math.Clamp(request.Frames, 8, 241),
                    ["steps"] = Math.Clamp(request.Steps, 1, 200),
                    ["guidance_scale"] = Math.Clamp(request.GuidanceScale, 0, 30),
                    ["fps"] = Math.Clamp(request.FramesPerSecond, 1, 60),
                    ["seed"] = request.Seed
                }
            }, cancellationToken).ConfigureAwait(false);
            return await PublishGeneratedArtifactAsync(result, "localgpt-video.mp4", cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GenerateVideoAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GenerateVideoAsync)} failed.");
        throw;
    }
}

    public async Task<LocalAiRuntimeJobResult> GenerateVideoFromWorkspaceImageAsync(LocalAiImageToVideoRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(request);
            var model = RequireCapability(request.ModelInstallationId, LocalAiCapability.ImageToVideo);
            if (request.Prompt.Length > 16000)
                throw new ArgumentException("Image-to-video prompt cannot exceed 16000 characters.", nameof(request));
            var sourcePath = ResolveWorkspaceInput(request.WorkspaceName, request.RelativePath);
            var config = CurrentConfig();
            var maximumBytes = (long)Math.Clamp(config.MaximumInputMegabytes, 1, 4096) * 1024 * 1024;
            EnsureInputSize(sourcePath, maximumBytes);
            var inputDirectory = LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "InputScratch", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inputDirectory);
            var privateCopy = Path.Combine(inputDirectory, Path.GetFileName(sourcePath));
            try
            {
                await CopyPrivateInputAsync(sourcePath, privateCopy, cancellationToken).ConfigureAwait(false);
                var result = await python.ExecuteAsync(new LocalAiRuntimeJobRequest
                {
                    Operation = "image_to_video",
                    ModelInstallationId = model.InstallationId,
                    Parameters = new Dictionary<string, object?>
                    {
                        ["model_path"] = model.LocalPath,
                        ["trust_remote_code"] = model.TrustRemoteCode,
                        ["cache_models"] = config.CacheModels,
                        ["device"] = config.DefaultDevice,
                        ["input_path"] = privateCopy,
                        ["maximum_input_bytes"] = maximumBytes,
                        ["maximum_image_pixels"] = Math.Clamp(config.MaximumImagePixels, 1_000_000L, 1_000_000_000L),
                        ["prompt"] = request.Prompt,
                        ["width"] = Math.Clamp(request.Width, 256, 1920),
                        ["height"] = Math.Clamp(request.Height, 256, 1080),
                        ["frames"] = Math.Clamp(request.Frames, 8, 241),
                        ["steps"] = Math.Clamp(request.Steps, 1, 200),
                        ["guidance_scale"] = Math.Clamp(request.GuidanceScale, 0, 30),
                        ["fps"] = Math.Clamp(request.FramesPerSecond, 1, 60),
                        ["seed"] = request.Seed
                    }
                }, cancellationToken).ConfigureAwait(false);
                return await PublishGeneratedArtifactAsync(result, "localgpt-image-video.mp4", cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                TryDeleteDirectory(inputDirectory);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GenerateVideoFromWorkspaceImageAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(GenerateVideoFromWorkspaceImageAsync)} failed.");
        throw;
    }
}

    public async Task<LocalAiRuntimeJobResult> TranscribeWorkspaceAudioAsync(LocalAiSpeechRecognitionRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureRuntimePolicyLoadedAsync(cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(request);
            var model = RequireCapability(request.ModelInstallationId, LocalAiCapability.SpeechRecognition);
            var sourcePath = ResolveWorkspaceInput(request.WorkspaceName, request.RelativePath);
            var config = CurrentConfig();
            var maximumBytes = (long)Math.Clamp(config.MaximumInputMegabytes, 1, 4096) * 1024 * 1024;
            EnsureInputSize(sourcePath, maximumBytes);
            var language = string.IsNullOrWhiteSpace(request.Language) ? config.DefaultSpeechLanguage : request.Language;
            var task = NormalizeSpeechTask(string.IsNullOrWhiteSpace(request.Task) ? config.DefaultSpeechTask : request.Task);
            var initialPrompt = string.IsNullOrWhiteSpace(request.InitialPrompt) ? config.DefaultSpeechInitialPrompt : request.InitialPrompt;
            var device = NormalizeRuntimeDevice(string.IsNullOrWhiteSpace(request.Device) ? config.DefaultDevice : request.Device);
            if (language?.Length > 40) throw new ArgumentException("Speech language must not exceed 40 characters.", nameof(request));
            if (initialPrompt?.Length > 4000) throw new ArgumentException("Speech initial prompt must not exceed 4000 characters.", nameof(request));

            var inputDirectory = LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "InputScratch", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inputDirectory);
            var privateCopy = Path.Combine(inputDirectory, Path.GetFileName(sourcePath));
            try
            {
                await CopyPrivateInputAsync(sourcePath, privateCopy, cancellationToken).ConfigureAwait(false);
                var result = await python.ExecuteAsync(new LocalAiRuntimeJobRequest
                {
                    Operation = "speech_recognize",
                    ModelInstallationId = model.InstallationId,
                    Parameters = new Dictionary<string, object?>
                    {
                        ["model_path"] = model.LocalPath,
                        ["adapter"] = model.Adapter,
                        ["runtime_model_name"] = model.RuntimeModelName,
                        ["trust_remote_code"] = model.TrustRemoteCode,
                        ["cache_models"] = request.CacheModel ?? config.CacheModels,
                        ["device"] = device,
                        ["input_path"] = privateCopy,
                        ["language"] = language,
                        ["task"] = task,
                        ["initial_prompt"] = initialPrompt,
                        ["maximum_input_bytes"] = maximumBytes,
                        ["maximum_audio_seconds"] = Math.Clamp(config.MaximumAudioSeconds, 1, 86400),
                        ["minimum_audio_bytes_per_second"] = Math.Clamp(config.MinimumAudioBytesPerSecond, 1, 1024 * 1024)
                    }
                }, cancellationToken).ConfigureAwait(false);
                return result;
            }
            finally
            {
                TryDeleteDirectory(inputDirectory);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(TranscribeWorkspaceAudioAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(TranscribeWorkspaceAudioAsync)} failed.");
        throw;
    }
}

    private string ResolveWorkspaceInput(string workspaceName, string relativePath)
    {
    try
    {
            var workspace = uploads.ResolveWorkspacePath(workspaceName)
                ?? throw new DirectoryNotFoundException("The selected upload workspace does not exist.");
            var sourcePath = Path.GetFullPath(Path.Combine(workspace, relativePath ?? string.Empty));
            if (!platform.IsSameOrDescendantPath(workspace, sourcePath) || !File.Exists(sourcePath))
                throw new FileNotFoundException("The selected input file is not inside the bounded upload workspace.");
            return sourcePath;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveWorkspaceInput)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveWorkspaceInput)} failed.");
        throw;
    }
}

    private void EnsureInputSize(string sourcePath, long maximumBytes)
    {
    try
    {
            var info = new FileInfo(sourcePath);
            if (info.Length <= 0 || info.Length > maximumBytes)
                throw new InvalidOperationException("The selected input file is empty or exceeds the configured admission limit.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(EnsureInputSize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(EnsureInputSize)} failed.");
        throw;
    }
}

    private async Task CopyPrivateInputAsync(string sourcePath, string privateCopy, CancellationToken cancellationToken)
    {
    try
    {
            var source = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using (source.ConfigureAwait(false))
            {
                var destination = File.Open(privateCopy, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await using (destination.ConfigureAwait(false))
                    await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(CopyPrivateInputAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(CopyPrivateInputAsync)} failed.");
        throw;
    }
}

    private async Task<LocalAiRuntimeJobResult> RunBridgeProcessAsync(
        string operation,
        Dictionary<string, object?> parameters,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
    try
    {
            if (!operation.Equals("hf_snapshot_download", StringComparison.Ordinal)
                && !operation.Equals("web_extract", StringComparison.Ordinal)
                && !operation.Equals("openai_whisper_download", StringComparison.Ordinal))
                throw new InvalidOperationException("Only reviewed non-inference bridge operations may run outside the embedded Python.NET lane.");

            var environmentPath = ResolveEnvironmentPath(CurrentConfig());
            var environmentPython = RequireExistingFile(ResolveEnvironmentPython(environmentPath), "Managed environment Python executable");
            var bridgePath = Path.Combine(environment.ContentRootPath, "Runtime", "Python", "localgpt_runtime_bridge.py");
            if (!File.Exists(bridgePath))
                throw new FileNotFoundException("The packaged LocalGPT Python runtime bridge is missing.", bridgePath);

            var jobId = Guid.NewGuid();
            var scratchDirectory = LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "InstallScratch", jobId.ToString("N"));
            Directory.CreateDirectory(scratchDirectory);
            var requestPath = Path.Combine(scratchDirectory, "request.json");
            var resultPath = Path.Combine(scratchDirectory, "result.json");
            var cancelPath = Path.Combine(scratchDirectory, "cancel");
            File.WriteAllText(requestPath, JsonSerializer.Serialize(new { operation, parameters }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            using var cancellationRegistration = cancellationToken.Register(static state =>
            {
                try { File.WriteAllText((string)state!, "cancel"); } catch { }
            }, cancelPath);

            try
            {
                var processResult = await RunProcessAsync(
                    environmentPython,
                    [bridgePath, requestPath, resultPath, cancelPath],
                    environmentPath,
                    timeout,
                    cancellationToken).ConfigureAwait(false);

                if (!File.Exists(resultPath))
                {
                    EnsureProcessSuccess(processResult, $"Local AI bridge operation '{operation}'");
                    throw new InvalidOperationException("The managed Python bridge returned without a result envelope.");
                }

                using var document = JsonDocument.Parse(File.ReadAllText(resultPath));
                var root = document.RootElement;
                var result = new LocalAiRuntimeJobResult
                {
                    JobId = jobId,
                    Succeeded = root.TryGetProperty("succeeded", out var succeeded) && succeeded.ValueKind == JsonValueKind.True,
                    Status = root.TryGetProperty("status", out var status) ? status.GetString() ?? string.Empty : string.Empty,
                    Message = root.TryGetProperty("message", out var message) ? message.GetString() ?? string.Empty : string.Empty,
                    Text = root.TryGetProperty("text", out var text) ? text.GetString() ?? string.Empty : string.Empty
                };
                if (root.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in metadata.EnumerateObject())
                        result.Metadata[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();
                }
                if (processResult.ExitCode != 0 && result.Succeeded)
                    EnsureProcessSuccess(processResult, $"Local AI bridge operation '{operation}'");
                return result;
            }
            finally
            {
                TryDeleteDirectory(scratchDirectory);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RunBridgeProcessAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RunBridgeProcessAsync)} failed.");
        throw;
    }
}

    private async Task InstallPackagesAsync(LocalAiPackageProfile profile, CancellationToken cancellationToken)
    {
    try
    {
            if (python.IsInitialized)
                throw new InvalidOperationException("The embedded Python runtime is already initialized. Restart LocalGPT before changing packages in its managed environment so loaded Python/native modules are not mutated underneath active jobs.");
            if (profile.Packages.Count == 0)
                throw new InvalidOperationException($"Python package profile '{profile.Key}' contains no packages.");
            var environmentPath = ResolveEnvironmentPath(CurrentConfig());
            var environmentPython = RequireExistingFile(ResolveEnvironmentPython(environmentPath), "Managed environment Python executable");
            var arguments = new List<string> { "-m", "pip", "install", "--disable-pip-version-check" };
            arguments.AddRange(profile.Packages.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
            var result = await RunProcessAsync(environmentPython, arguments, environmentPath, TimeSpan.FromMinutes(45), cancellationToken).ConfigureAwait(false);
            EnsureProcessSuccess(result, $"Python package profile '{profile.Key}' installation");
            logger.LogInformation("Installed Python package profile {ProfileKey}; package output and environment path were omitted from logs.", profile.Key);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(InstallPackagesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(InstallPackagesAsync)} failed.");
        throw;
    }
}

    private async Task<PythonRuntimeCandidate?> ProbePythonCandidateAsync(string executablePath, string discoverySource, CancellationToken cancellationToken)
    {
        try
        {
            const string script = "import json,sys,sysconfig,os; print(json.dumps({'executable':sys.executable,'base_prefix':sys.base_prefix,'version':'.'.join(map(str,sys.version_info[:3])),'ldlibrary':sysconfig.get_config_var('LDLIBRARY') or '', 'libdir':sysconfig.get_config_var('LIBDIR') or ''}))";
            var result = await RunProcessAsync(executablePath, ["-c", script], Path.GetDirectoryName(executablePath), TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
                return null;
            var jsonLine = result.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (string.IsNullOrWhiteSpace(jsonLine))
                return null;
            using var document = JsonDocument.Parse(jsonLine);
            var root = document.RootElement;
            var executable = root.GetProperty("executable").GetString() ?? executablePath;
            var home = root.GetProperty("base_prefix").GetString() ?? Path.GetDirectoryName(executable) ?? string.Empty;
            var version = root.GetProperty("version").GetString() ?? string.Empty;
            var libraryName = root.GetProperty("ldlibrary").GetString() ?? string.Empty;
            var libraryDirectory = root.GetProperty("libdir").GetString() ?? string.Empty;
            var runtime = ResolvePythonRuntimeLibrary(home, version, libraryDirectory, libraryName);
            return new PythonRuntimeCandidate
            {
                ExecutablePath = Path.GetFullPath(executable),
                PythonHome = Path.GetFullPath(home),
                PythonRuntimeLibrary = runtime,
                Version = version,
                DiscoverySource = discoverySource
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogDebug("A discovered Python candidate could not be probed after {ExceptionType}; exception text and executable path were omitted from logs.", exception.GetType().Name);
            return null;
        }
    }

    private string ResolvePythonRuntimeLibrary(string home, string version, string libDirectory, string libraryName)
    {
    try
    {
            var direct = string.IsNullOrWhiteSpace(libraryName) ? string.Empty : Path.Combine(string.IsNullOrWhiteSpace(libDirectory) ? home : libDirectory, libraryName);
            if (!string.IsNullOrWhiteSpace(direct) && File.Exists(direct))
                return Path.GetFullPath(direct);
            var versionParts = version.Split('.');
            if (platform.ToolchainPlatform == ToolchainPlatformKind.Windows && versionParts.Length >= 2)
            {
                var windowsDll = Path.Combine(home, $"python{versionParts[0]}{versionParts[1]}.dll");
                if (File.Exists(windowsDll))
                    return Path.GetFullPath(windowsDll);
            }
            try
            {
                var patterns = platform.ToolchainPlatform == ToolchainPlatformKind.Windows ? new[] { "python*.dll" } : platform.ToolchainPlatform == ToolchainPlatformKind.MacOS ? new[] { "libpython*.dylib" } : new[] { "libpython*.so", "libpython*.so.*" };
                foreach (var root in new[] { home, Path.Combine(home, "lib") }.Where(Directory.Exists))
                foreach (var pattern in patterns)
                {
                    var match = Directory.EnumerateFiles(root, pattern, SearchOption.TopDirectoryOnly).OrderBy(path => path.Length).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(match))
                        return Path.GetFullPath(match);
                }
            }
            catch { }
            return string.Empty;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolvePythonRuntimeLibrary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolvePythonRuntimeLibrary)} failed.");
        throw;
    }
}

    private async Task<LocalAiRuntimeJobResult> PublishGeneratedArtifactAsync(LocalAiRuntimeJobResult result, string fileName, CancellationToken cancellationToken)
    {
    try
    {
            if (!result.Succeeded)
                return result;
            if (!result.Metadata.TryGetValue("artifactPath", out var stagedPath) || !File.Exists(stagedPath))
                throw new InvalidOperationException("The local AI runtime reported success without a staged artifact.");
            try
            {
                var artifact = await artifacts.PublishAsync(stagedPath, fileName, cancellationToken).ConfigureAwait(false);
                result.ArtifactId = artifact.ArtifactId;
                result.ArtifactFileName = artifact.FileName;
                result.ArtifactUrl = artifact.DownloadUrl;
                result.ArtifactMarkdown = artifact.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                    ? $"![Generated local AI image]({artifact.DownloadUrl})"
                    : artifact.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                        ? $"[Open generated local AI video]({artifact.DownloadUrl})"
                        : $"[Open generated local AI artifact]({artifact.DownloadUrl})";
                result.Metadata.Remove("artifactPath");
                return result;
            }
            finally
            {
                TryDeleteDirectory(Path.GetDirectoryName(stagedPath)!);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(PublishGeneratedArtifactAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(PublishGeneratedArtifactAsync)} failed.");
        throw;
    }
}

    private LocalAiModelInstallation RequireCapability(string installationId, LocalAiCapability capability)
    {
    try
    {
            var installation = FindInstallation(installationId);
            if (!installation.Capabilities.Contains(capability))
                throw new InvalidOperationException($"The selected model installation is not bound to {capability}.");
            return installation;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RequireCapability)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RequireCapability)} failed.");
        throw;
    }
}

    private LocalAiModelInstallation FindInstallation(string installationId) {
    try
    {
        return GetInstalledModels().FirstOrDefault(item => item.InstallationId.Equals(installationId, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException("The selected local-AI model installation was not found.");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(FindInstallation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(FindInstallation)} failed.");
        throw;
    }
}

    private void SaveManifest(string modelRoot, LocalAiModelInstallation installation)
    {
    try
    {
            var manifests = Path.Combine(modelRoot, ".localgpt", "manifests");
            Directory.CreateDirectory(manifests);
            var path = ManifestPath(modelRoot, installation.InstallationId);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(installation, JsonOptions));
            File.Move(temporary, path, overwrite: true);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(SaveManifest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(SaveManifest)} failed.");
        throw;
    }
}

    private string ManifestPath(string modelRoot, string installationId) {
    try
    {
        return Path.Combine(modelRoot, ".localgpt", "manifests", installationId + ".json");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ManifestPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ManifestPath)} failed.");
        throw;
    }
}

    private string BuildInstallationId(string modelId, string revision)
    {
    try
    {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(modelId + "\n" + revision));
            return Convert.ToHexString(hash)[..20].ToLowerInvariant();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(BuildInstallationId)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(BuildInstallationId)} failed.");
        throw;
    }
}

    private string InferAdapter(IReadOnlyCollection<LocalAiCapability> capabilities)
    {
    try
    {
            if (capabilities.Contains(LocalAiCapability.ImageGeneration)) return "diffusers-image";
            if (capabilities.Contains(LocalAiCapability.TextToVideo)) return "diffusers-video";
            if (capabilities.Contains(LocalAiCapability.SpeechRecognition)) return "transformers-asr";
            return "specialized-python";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(InferAdapter)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(InferAdapter)} failed.");
        throw;
    }
}

    private Version ParsePythonVersion(string value) {
    try
    {
        return Version.TryParse(value, out var parsed) ? parsed : new Version(0, 0);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ParsePythonVersion)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ParsePythonVersion)} failed.");
        throw;
    }
}

    private LocalAiRuntimeConfiguration BuildRuntimeConfiguration(PythonCoreOptions config)
    {
        try
        {
            return new LocalAiRuntimeConfiguration
            {
                DefaultDevice = NormalizeRuntimeDevice(config.DefaultDevice),
                DefaultWhisperModel = NormalizeWhisperVariant(config.DefaultWhisperModel),
                DefaultSpeechLanguage = config.DefaultSpeechLanguage ?? string.Empty,
                DefaultSpeechTask = NormalizeSpeechTask(config.DefaultSpeechTask),
                DefaultSpeechInitialPrompt = config.DefaultSpeechInitialPrompt ?? string.Empty,
                QueueCapacity = Math.Clamp(config.QueueCapacity, 1, 1024),
                MaximumInputMegabytes = Math.Clamp(config.MaximumInputMegabytes, 1, 4096),
                MaximumImagePixels = Math.Clamp(config.MaximumImagePixels, 1_000_000, 1_000_000_000),
                MaximumAudioSeconds = Math.Clamp(config.MaximumAudioSeconds, 1, 86_400),
                MinimumAudioBytesPerSecond = Math.Clamp(config.MinimumAudioBytesPerSecond, 1, 1_048_576),
                CacheModels = config.CacheModels,
                InterpreterInitialized = python.IsInitialized,
                QueueLength = python.QueueLength,
                RestartRequired = python.RestartRequired || runtimePolicyRestartRequired,
                RestartReason = python.RestartRequired
                    ? "Python executable/runtime binding changed after interpreter initialization."
                    : runtimePolicyRestartRequired
                        ? "Queue capacity is created with the embedded runtime coordinator and will apply after restart."
                        : string.Empty
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the LocalGPT local-AI runtime configuration failed; runtime paths and environment values were omitted from logs.");
            throw;
        }
    }

    private string NormalizeRuntimeDevice(string? value)
    {
        try
        {
            var normalized = (value ?? "auto").Trim().ToLowerInvariant();
            return normalized is "auto" or "cpu" or "cuda" or "mps" ? normalized : throw new ArgumentException("Device must be auto, cpu, cuda or mps.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing the LocalGPT local-AI runtime device failed; the supplied value was omitted from logs.");
            throw;
        }
    }

    private string NormalizeSpeechTask(string? value)
    {
        try
        {
            var normalized = (value ?? "transcribe").Trim().ToLowerInvariant();
            return normalized is "transcribe" or "translate" ? normalized : throw new ArgumentException("Speech task must be transcribe or translate.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing the LocalGPT speech task failed; the supplied value was omitted from logs.");
            throw;
        }
    }

    private string NormalizeWhisperVariant(string? value)
    {
        try
        {
            var normalized = (value ?? "base").Trim().ToLowerInvariant();
            return normalized is "tiny" or "base" or "small" or "medium" or "large-v3" or "turbo"
                ? normalized
                : throw new ArgumentException("Whisper model must be tiny, base, small, medium, large-v3 or turbo.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing the LocalGPT Whisper model variant failed; the supplied value was omitted from logs.");
            throw;
        }
    }

    private async Task EnsureRuntimePolicyLoadedAsync(CancellationToken cancellationToken)
    {
        var lockTaken = false;
        try
        {
            if (runtimePolicyLoaded)
                return;
            await runtimePolicyLoadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            if (runtimePolicyLoaded)
                return;

            var profile = await toolchainProfiles.GetProfileAsync(RuntimePolicyProfileKey, cancellationToken).ConfigureAwait(false);
            if (profile is not null && !string.IsNullOrWhiteSpace(profile.ConfigurationJson))
            {
                var persisted = JsonSerializer.Deserialize<LocalAiRuntimeConfigurationChangeRequest>(profile.ConfigurationJson, JsonOptions);
                if (persisted is not null)
                {
                    var config = ClonePythonCore(configuredOverride ?? options.CurrentValue.PythonCore ?? new PythonCoreOptions());
                    config.DefaultDevice = NormalizeRuntimeDevice(persisted.DefaultDevice);
                    config.DefaultWhisperModel = NormalizeWhisperVariant(persisted.DefaultWhisperModel);
                    config.DefaultSpeechLanguage = (persisted.DefaultSpeechLanguage ?? string.Empty).Trim();
                    config.DefaultSpeechTask = NormalizeSpeechTask(persisted.DefaultSpeechTask);
                    config.DefaultSpeechInitialPrompt = persisted.DefaultSpeechInitialPrompt ?? string.Empty;
                    config.QueueCapacity = Math.Clamp(persisted.QueueCapacity, 1, 1024);
                    config.MaximumInputMegabytes = Math.Clamp(persisted.MaximumInputMegabytes, 1, 4096);
                    config.MaximumImagePixels = Math.Clamp(persisted.MaximumImagePixels, 1_000_000, 1_000_000_000);
                    config.MaximumAudioSeconds = Math.Clamp(persisted.MaximumAudioSeconds, 1, 86_400);
                    config.MinimumAudioBytesPerSecond = Math.Clamp(persisted.MinimumAudioBytesPerSecond, 1, 1_048_576);
                    config.CacheModels = persisted.CacheModels;
                    configuredOverride = config;
                }
            }
            runtimePolicyLoaded = true;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading the database-backed LocalGPT local-AI runtime policy was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the database-backed LocalGPT local-AI runtime policy failed; configuration content was omitted from logs.");
            throw;
        }
        finally
        {
            if (lockTaken)
                runtimePolicyLoadGate.Release();
        }
    }

    private PythonCoreOptions CurrentConfig() {
    try
    {
        return configuredOverride ?? options.CurrentValue.PythonCore ?? new PythonCoreOptions();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(CurrentConfig)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(CurrentConfig)} failed.");
        throw;
    }
}
    private string ResolveEnvironmentPath(PythonCoreOptions config) {
    try
    {
        return string.IsNullOrWhiteSpace(config.VirtualEnvironmentPath) ? LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "PythonEnv") : Path.GetFullPath(config.VirtualEnvironmentPath);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveEnvironmentPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveEnvironmentPath)} failed.");
        throw;
    }
}
    private string ResolveModelRoot(PythonCoreOptions config) {
    try
    {
        return string.IsNullOrWhiteSpace(config.ModelRoot) ? LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Models") : Path.GetFullPath(config.ModelRoot);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveModelRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveModelRoot)} failed.");
        throw;
    }
}
    private string ResolveArtifactRoot(PythonCoreOptions config) {
    try
    {
        return string.IsNullOrWhiteSpace(config.ArtifactRoot) ? LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Artifacts") : Path.GetFullPath(config.ArtifactRoot);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveArtifactRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveArtifactRoot)} failed.");
        throw;
    }
}

    private string ResolveEnvironmentPython(string environmentPath) {
    try
    {
        return platform.ToolchainPlatform == ToolchainPlatformKind.Windows ? Path.Combine(environmentPath, "Scripts", "python.exe") : Path.Combine(environmentPath, "bin", "python");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveEnvironmentPython)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ResolveEnvironmentPython)} failed.");
        throw;
    }
}


    private string RequireExistingFile(string? path, string label)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new InvalidOperationException($"{label} is not configured or no longer exists.");
            return Path.GetFullPath(path);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RequireExistingFile)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RequireExistingFile)} failed.");
        throw;
    }
}

    private void RequireConfirmation(bool userConfirmed, string operation)
    {
    try
    {
            if (!userConfirmed)
                throw new InvalidOperationException($"Explicit human confirmation is required before {operation}.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RequireConfirmation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RequireConfirmation)} failed.");
        throw;
    }
}

    private void EnsureProcessSuccess(ProcessResult result, string operation)
    {
    try
    {
            if (result.ExitCode == 0)
                return;
            var tail = string.Join(' ', (result.StdErr + " " + result.StdOut).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).TakeLast(4));
            if (tail.Length > 600) tail = tail[^600..];
            throw new InvalidOperationException($"{operation} failed with exit code {result.ExitCode}. {tail}".Trim());
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(EnsureProcessSuccess)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(EnsureProcessSuccess)} failed.");
        throw;
    }
}

    private async Task<ProcessResult> RunProcessAsync(string executable, IEnumerable<string> arguments, string? workingDirectory, TimeSpan timeout, CancellationToken cancellationToken)
    {
    try
    {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            foreach (var argument in arguments)
                process.StartInfo.ArgumentList.Add(argument);
            if (!process.Start())
                throw new InvalidOperationException("The reviewed Python process could not be started.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
                throw;
            }
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return new ProcessResult(process.ExitCode, TrimOutput(stdout), TrimOutput(stderr));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RunProcessAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(RunProcessAsync)} failed.");
        throw;
    }
}

    private string TrimOutput(string value) {
    try
    {
        return value.Length <= 20000 ? value : value[^20000..];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(TrimOutput)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(TrimOutput)} failed.");
        throw;
    }
}
    private void TryDeleteDirectory(string path) {
    try
    {
     try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { } 
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(TryDeleteDirectory)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(TryDeleteDirectory)} failed.");
        throw;
    }
}

    private LocalGptConfigurationRoot CloneConfiguration(LocalGptConfigurationRoot source) {
    try
    {
        return JsonSerializer.Deserialize<LocalGptConfigurationRoot>(JsonSerializer.Serialize(source)) ?? new LocalGptConfigurationRoot();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(CloneConfiguration)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(CloneConfiguration)} failed.");
        throw;
    }
}
    private PythonCoreOptions ClonePythonCore(PythonCoreOptions source) {
    try
    {
        return JsonSerializer.Deserialize<PythonCoreOptions>(JsonSerializer.Serialize(source)) ?? new PythonCoreOptions();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ClonePythonCore)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiRuntimeService)}.{nameof(ClonePythonCore)} failed.");
        throw;
    }
}
    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
}
