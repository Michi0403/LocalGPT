using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Components.Pages
{
    public partial class Install
    {
        private LocalAiRuntimeStatus? LocalAiStatus;
        private LocalAiRuntimeConfiguration? LocalAiRuntimePolicy;
        private IReadOnlyList<PythonRuntimeCandidate> LocalAiPythonCandidates = [];
        private IReadOnlyList<LocalAiPackageProfile> LocalAiPackageProfiles = [];
        private IReadOnlyList<LocalAiModelInstallation> LocalAiInstalledModels = [];
        private IReadOnlyList<LocalAiKnownModelDefinition> LocalAiKnownModels = [];
        private IReadOnlyList<HuggingFaceModelSearchResult> LocalAiSearchResults = [];
        private string LocalAiWhisperVariant = "base";
        private IReadOnlyList<string> LocalAiDeviceOptions { get; } = ["auto", "cpu", "cuda", "mps"];
        private IReadOnlyList<string> LocalAiSpeechTaskOptions { get; } = ["transcribe", "translate"];
        private IReadOnlyList<string> LocalAiWhisperVariantOptions => LocalAiKnownModels
            .FirstOrDefault(item => item.Key.Equals("openai-whisper", StringComparison.OrdinalIgnoreCase))?.Variants
            .Select(item => item.Key)
            .ToList() ?? ["tiny", "base", "small", "medium", "large-v3", "turbo"];
        private IReadOnlyList<LocalAiCapability> LocalAiCapabilityOptions { get; } =
        [
            LocalAiCapability.Unknown,
            LocalAiCapability.ImageGeneration,
            LocalAiCapability.ImageEditing,
            LocalAiCapability.TextToVideo,
            LocalAiCapability.ImageToVideo,
            LocalAiCapability.SpeechRecognition
        ];
        private LocalAiCapability LocalAiSearchCapability = LocalAiCapability.Unknown;
        private LocalAiCapability LocalAiManualCapability = LocalAiCapability.Unknown;
        private string LocalAiSearchQuery = string.Empty;
        private string LocalAiStatusText = string.Empty;
        private bool LocalAiBusy;
        private bool LocalAiTrustRemoteCode;

        private async Task RefreshLocalAiRuntimeAsync()
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatus = await LocalAiRuntime.GetStatusAsync().ConfigureAwait(false);
                LocalAiRuntimePolicy = await LocalAiRuntime.GetRuntimeConfigurationAsync().ConfigureAwait(false);
                LocalAiWhisperVariant = LocalAiRuntimePolicy.DefaultWhisperModel;
                LocalAiPackageProfiles = LocalAiRuntime.GetPackageProfiles();
                LocalAiInstalledModels = LocalAiRuntime.GetInstalledModels();
                LocalAiKnownModels = LocalAiAcquisition.GetKnownModels();
                LocalAiStatusText = LocalAiStatus.Message;
            }
            catch (Exception exception)
            {
                Logger.LogError("Refreshing the Local AI runtime workbench failed with {ExceptionType}; exception text and runtime paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = "Local AI runtime status could not be loaded. Review LocalGPT logs.";
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }


        private async Task SaveLocalAiRuntimePolicyAsync()
        {
            if (LocalAiRuntimePolicy is null)
                return;
            try
            {
                LocalAiBusy = true;
                LocalAiStatusText = "Saving managed Python/Whisper execution policy...";
                LocalAiRuntimePolicy = await LocalAiRuntime.UpdateRuntimeConfigurationAsync(new LocalAiRuntimeConfigurationChangeRequest
                {
                    DefaultDevice = LocalAiRuntimePolicy.DefaultDevice,
                    DefaultWhisperModel = LocalAiRuntimePolicy.DefaultWhisperModel,
                    DefaultSpeechLanguage = LocalAiRuntimePolicy.DefaultSpeechLanguage,
                    DefaultSpeechTask = LocalAiRuntimePolicy.DefaultSpeechTask,
                    DefaultSpeechInitialPrompt = LocalAiRuntimePolicy.DefaultSpeechInitialPrompt,
                    QueueCapacity = LocalAiRuntimePolicy.QueueCapacity,
                    MaximumInputMegabytes = LocalAiRuntimePolicy.MaximumInputMegabytes,
                    MaximumImagePixels = LocalAiRuntimePolicy.MaximumImagePixels,
                    MaximumAudioSeconds = LocalAiRuntimePolicy.MaximumAudioSeconds,
                    MinimumAudioBytesPerSecond = LocalAiRuntimePolicy.MinimumAudioBytesPerSecond,
                    CacheModels = LocalAiRuntimePolicy.CacheModels,
                    UserConfirmed = true
                }, userConfirmed: true).ConfigureAwait(false);
                LocalAiStatus = await LocalAiRuntime.GetStatusAsync().ConfigureAwait(false);
                LocalAiWhisperVariant = LocalAiRuntimePolicy.DefaultWhisperModel;
                LocalAiStatusText = LocalAiRuntimePolicy.RestartRequired
                    ? $"Runtime policy saved. Restart required: {LocalAiRuntimePolicy.RestartReason}"
                    : "Runtime policy saved and available to LocalGPT users and AI-team DXFunctions.";
            }
            catch (Exception exception)
            {
                Logger.LogError("Saving Local AI runtime policy failed with {ExceptionType}; prompt text, values and paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task DiscoverLocalAiPythonAsync()
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatusText = "Discovering Python runtimes from LocalGPT toolchain knowledge...";
                LocalAiPythonCandidates = await LocalAiRuntime.DiscoverPythonAsync().ConfigureAwait(false);
                LocalAiStatusText = LocalAiPythonCandidates.Count == 0
                    ? "No Python runtime with a usable executable was detected. Install Python, then run discovery again or use the Toolchains section to review search roots."
                    : $"Detected {LocalAiPythonCandidates.Count} Python runtime candidate(s). Select one with a resolved shared runtime library for Python.NET.";
            }
            catch (Exception exception)
            {
                Logger.LogError("Python runtime discovery failed with {ExceptionType}; exception text and discovered paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = "Python runtime discovery failed. Review LocalGPT logs.";
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task SelectLocalAiPythonAsync(PythonRuntimeCandidate candidate)
        {
            try
            {
                LocalAiBusy = true;
                await LocalAiRuntime.ConfigurePythonAsync(candidate).ConfigureAwait(false);
                LocalAiStatusText = "Python runtime binding saved. Create the managed environment next. If Python.NET was already initialized with another runtime, restart LocalGPT before executing jobs.";
                await RefreshLocalAiRuntimeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError("Saving the Local AI Python binding failed with {ExceptionType}; exception text and runtime paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task CreateLocalAiEnvironmentAsync()
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatusText = "Creating the managed Python environment and installing the reviewed core profile...";
                await LocalAiRuntime.CreateManagedEnvironmentAsync(userConfirmed: true).ConfigureAwait(false);
                LocalAiStatusText = "Managed Python environment created. Install the PyTorch backend appropriate for this machine, then the image/video/speech profiles you need.";
                await RefreshLocalAiRuntimeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError("Creating the Local AI managed environment failed with {ExceptionType}; exception text, command output and paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task InstallLocalAiPackageProfileAsync(string profileKey)
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatusText = $"Installing Python package profile {profileKey}...";
                await LocalAiRuntime.InstallPackageProfileAsync(profileKey, userConfirmed: true).ConfigureAwait(false);
                LocalAiStatusText = $"Python package profile {profileKey} installed.";
                await RefreshLocalAiRuntimeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError("Installing Local AI Python package profile {ProfileKey} failed with {ExceptionType}; exception text, package output and paths were omitted from logs.", profileKey, exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task ProbeLocalAiRuntimeAsync()
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatus = await LocalAiRuntime.ProbeAsync().ConfigureAwait(false);
                LocalAiStatusText = LocalAiStatus.Message;
            }
            catch (Exception exception)
            {
                Logger.LogError("Local AI runtime probe failed with {ExceptionType}; exception text and runtime paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task ClearLocalAiModelCacheAsync()
        {
            try
            {
                LocalAiBusy = true;
                await LocalAiRuntime.ClearModelCacheAsync().ConfigureAwait(false);
                LocalAiStatusText = "Cached specialized model pipelines unloaded. Installed snapshots and published artifacts were kept.";
                LocalAiStatus = await LocalAiRuntime.GetStatusAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError("Clearing the Local AI model cache failed with {ExceptionType}; exception text was omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task DownloadKnownLocalAiSourceAsync(string sourceKey)
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatusText = "Downloading reviewed upstream AI source directly over HTTPS...";
                var result = await LocalAiAcquisition.DownloadSourceAsync(sourceKey, userConfirmed: true).ConfigureAwait(false);
                LocalAiStatusText = $"Reviewed source downloaded: {result.Bytes:N0} bytes, SHA-256 {result.Sha256}.";
            }
            catch (Exception exception)
            {
                Logger.LogError("Downloading a reviewed local AI source failed with {ExceptionType}; source identity, URL and path were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task InstallKnownLocalAiModelAsync(string sourceKey)
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatusText = "Installing reviewed upstream source and real model weights through the managed Python environment...";
                await LocalAiAcquisition.InstallKnownModelAsync(new LocalAiKnownModelInstallRequest
                {
                    SourceKey = sourceKey,
                    Variant = sourceKey.Equals("openai-whisper", StringComparison.OrdinalIgnoreCase) ? LocalAiWhisperVariant : string.Empty,
                    UserConfirmed = true
                }).ConfigureAwait(false);
                LocalAiInstalledModels = LocalAiRuntime.GetInstalledModels();
                LocalAiStatusText = "Known local-AI model installed and registered for its bounded DXFunction capability.";
            }
            catch (Exception exception)
            {
                Logger.LogError("Installing a reviewed direct local AI model failed with {ExceptionType}; source, model identity and paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task SearchHuggingFaceAsync()
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatusText = "Searching Hugging Face model metadata...";
                LocalAiSearchResults = await HuggingFaceCatalog.SearchAsync(new HuggingFaceModelSearchRequest
                {
                    Query = LocalAiSearchQuery,
                    Capability = LocalAiSearchCapability,
                    Limit = 24
                }).ConfigureAwait(false);
                LocalAiStatusText = $"Found {LocalAiSearchResults.Count} compatible model metadata result(s). Search does not download or execute repository code.";
            }
            catch (Exception exception)
            {
                Logger.LogError("Hugging Face model metadata search failed with {ExceptionType}; exception text, query and credentials were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = "Hugging Face model search failed. Review LocalGPT logs.";
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task InstallHuggingFaceModelAsync(HuggingFaceModelSearchResult model)
        {
            try
            {
                LocalAiBusy = true;
                LocalAiStatusText = "Installing the reviewed model snapshot. Large models can take a while; LocalGPT will not execute repository setup scripts.";
                var capabilities = model.Capabilities
                    .Where(value => value != LocalAiCapability.Unknown && LocalAiCapabilityOptions.Contains(value))
                    .Distinct()
                    .ToList();
                if (LocalAiManualCapability != LocalAiCapability.Unknown && !capabilities.Contains(LocalAiManualCapability))
                    capabilities.Add(LocalAiManualCapability);
                await LocalAiRuntime.InstallModelAsync(new LocalAiModelInstallRequest
                {
                    ModelId = model.ModelId,
                    Revision = model.Revision,
                    Capabilities = capabilities,
                    TrustRemoteCode = LocalAiTrustRemoteCode,
                    RequiresAuthentication = model.IsPrivate || model.IsGated
                }, userConfirmed: true).ConfigureAwait(false);
                LocalAiInstalledModels = LocalAiRuntime.GetInstalledModels();
                LocalAiStatusText = "Model snapshot installed and registered in the specialized capability inventory.";
            }
            catch (Exception exception)
            {
                Logger.LogError("Installing a Hugging Face model snapshot failed with {ExceptionType}; exception text, model identity, credentials and paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task RemoveLocalAiModelAsync(string installationId)
        {
            try
            {
                LocalAiBusy = true;
                await LocalAiRuntime.RemoveModelAsync(installationId, userConfirmed: true).ConfigureAwait(false);
                LocalAiInstalledModels = LocalAiRuntime.GetInstalledModels();
                LocalAiStatusText = "Managed specialized model removed.";
            }
            catch (Exception exception)
            {
                Logger.LogError("Removing a Local AI model failed with {ExceptionType}; exception text, model identity and paths were omitted from logs.", exception.GetType().Name);
                LocalAiStatusText = exception.Message;
            }
            finally
            {
                LocalAiBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private bool CanInstallLocalAiModel(HuggingFaceModelSearchResult model) =>
            LocalAiManualCapability != LocalAiCapability.Unknown || model.Capabilities.Any(value => LocalAiCapabilityOptions.Contains(value) && value != LocalAiCapability.Unknown);

    }
}
