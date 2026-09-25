using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IPythonNetRuntimeCoordinator
{
    bool IsInitialized { get; }
    bool RestartRequired { get; }
    int QueueLength { get; }
    Task<LocalAiRuntimeJobResult> ExecuteAsync(LocalAiRuntimeJobRequest request, CancellationToken cancellationToken = default);
}

public interface ILocalAiRuntimeService
{
    Task<IReadOnlyList<PythonRuntimeCandidate>> DiscoverPythonAsync(CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeConfiguration> GetRuntimeConfigurationAsync(CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeConfiguration> UpdateRuntimeConfigurationAsync(LocalAiRuntimeConfigurationChangeRequest request, bool userConfirmed, CancellationToken cancellationToken = default);
    Task ConfigurePythonAsync(PythonRuntimeCandidate candidate, CancellationToken cancellationToken = default);
    Task CreateManagedEnvironmentAsync(bool userConfirmed, CancellationToken cancellationToken = default);
    Task InstallPackageProfileAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default);
    IReadOnlyList<LocalAiPackageProfile> GetPackageProfiles();
    string FormatPackageList(IEnumerable<string> packages);
    string FormatCapabilities(IEnumerable<LocalAiCapability> capabilities);
    IReadOnlyList<LocalAiModelInstallation> GetInstalledModels();
    Task<LocalAiModelInstallation> InstallModelAsync(LocalAiModelInstallRequest request, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<LocalAiModelInstallation> InstallOpenAiWhisperAsync(string sourceArchivePath, string variant, bool userConfirmed, CancellationToken cancellationToken = default);
    Task RemoveModelAsync(string installationId, bool userConfirmed, CancellationToken cancellationToken = default);
    Task ClearModelCacheAsync(CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeJobResult> GenerateImageAsync(LocalAiImageGenerationRequest request, CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeJobResult> EditWorkspaceImageAsync(LocalAiImageEditRequest request, CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeJobResult> GenerateVideoAsync(LocalAiVideoGenerationRequest request, CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeJobResult> GenerateVideoFromWorkspaceImageAsync(LocalAiImageToVideoRequest request, CancellationToken cancellationToken = default);
    Task<LocalAiRuntimeJobResult> TranscribeWorkspaceAudioAsync(LocalAiSpeechRecognitionRequest request, CancellationToken cancellationToken = default);
    /// <summary>Persists one bounded browser microphone WAV as a chat-upload workspace and downloadable media artifact without requiring Whisper.</summary>
    /// <param name="audio">Readable WAV stream admitted from the browser capture.</param>
    /// <param name="length">Declared audio length used by upload admission.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop persistence.</param>
    /// <returns>The persisted microphone recording and its downloadable artifact.</returns>
    Task<LocalAiMicrophoneRecording> SaveMicrophoneRecordingAsync(Stream audio, long length, CancellationToken cancellationToken = default);
    /// <summary>Transcribes one bounded browser microphone WAV through the selected installed speech-recognition model.</summary>
    /// <param name="audio">Readable WAV stream admitted from the browser capture.</param>
    /// <param name="length">Declared audio length used by upload admission.</param>
    /// <param name="installationId">Identifier of the installed speech-recognition model to use.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop transcription.</param>
    /// <returns>The managed Local AI runtime result containing the transcription outcome.</returns>
    Task<LocalAiRuntimeJobResult> TranscribeMicrophoneAsync(Stream audio, long length, string installationId, CancellationToken cancellationToken = default);
    /// <summary>Installs the managed Playwright Chromium browser after the dispatcher has obtained explicit approval.</summary>
    /// <param name="userConfirmed">Value indicating whether exact-action human confirmation has been granted by the dispatcher.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop browser installation.</param>
    /// <returns>A task that completes when browser installation has finished.</returns>
    Task InstallWebBrowserAsync(bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Extracts bounded rendered evidence from one approved public web request through the managed browser profile.</summary>
    /// <param name="request">Validated web-content request containing URL and bounded reveal/scroll settings.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop web extraction.</param>
    /// <returns>The managed Local AI runtime result containing bounded page evidence and source metadata.</returns>
    Task<LocalAiRuntimeJobResult> ExtractWebContentAsync(WebContentRequest request, CancellationToken cancellationToken = default);
}

public interface IHuggingFaceModelCatalogService
{
    Task<IReadOnlyList<HuggingFaceModelSearchResult>> SearchAsync(HuggingFaceModelSearchRequest request, CancellationToken cancellationToken = default);
}

public interface ILocalAiArtifactService
{
    string ArtifactRoot { get; }
    Task<LocalAiArtifactDescriptor> PublishAsync(string sourcePath, string preferredFileName, CancellationToken cancellationToken = default);
    LocalAiArtifactDescriptor? Resolve(string artifactId, string fileName);
}
