using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for python net runtime behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPythonNetRuntimeCoordinator
{
    /// <summary>
    /// Gets a value indicating whether initialized applies to the python net runtime state.
    /// </summary>
    /// <value>The is initialized value exposed by <see cref="IPythonNetRuntimeCoordinator"/>.</value>
    bool IsInitialized { get; }
    /// <summary>
    /// Gets a value indicating whether restart required applies to the python net runtime state.
    /// </summary>
    /// <value>The restart required value exposed by <see cref="IPythonNetRuntimeCoordinator"/>.</value>
    bool RestartRequired { get; }
    /// <summary>
    /// Gets the queue length that quantifies the associated python net runtime data.
    /// </summary>
    /// <value>The queue length value exposed by <see cref="IPythonNetRuntimeCoordinator"/>.</value>
    int QueueLength { get; }
    /// <summary>
    /// Performs execute for <see cref="IPythonNetRuntimeCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding python net runtime workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime job result produced by the operation.</returns>
    Task<LocalAiRuntimeJobResult> ExecuteAsync(LocalAiRuntimeJobRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for local AI runtime behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalAiRuntimeService
{
    /// <summary>
    /// Discovers python as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<PythonRuntimeCandidate>> DiscoverPythonAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves status as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime status produced by the operation.</returns>
    Task<LocalAiRuntimeStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs probe as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime status produced by the operation.</returns>
    Task<LocalAiRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves runtime configuration as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime configuration produced by the operation.</returns>
    Task<LocalAiRuntimeConfiguration> GetRuntimeConfigurationAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates runtime configuration as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime configuration produced by the operation.</returns>
    Task<LocalAiRuntimeConfiguration> UpdateRuntimeConfigurationAsync(LocalAiRuntimeConfigurationChangeRequest request, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs configure python as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="candidate">Candidate value supplied to the local AI runtime operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ConfigurePythonAsync(PythonRuntimeCandidate candidate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates managed environment as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task CreateManagedEnvironmentAsync(bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs install package profile as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileKey">Profile key value supplied to the local AI runtime operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task InstallPackageProfileAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves package profiles as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<LocalAiPackageProfile> GetPackageProfiles();
    /// <summary>
    /// Performs format package list as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="packages">String dependency used by the local AI runtime workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    string FormatPackageList(IEnumerable<string> packages);
    /// <summary>
    /// Performs format capabilities as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="capabilities">Local ai capability dependency used by the local AI runtime workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    string FormatCapabilities(IEnumerable<LocalAiCapability> capabilities);
    /// <summary>
    /// Retrieves installed models as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<LocalAiModelInstallation> GetInstalledModels();
    /// <summary>
    /// Performs install model as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI model installation produced by the operation.</returns>
    Task<LocalAiModelInstallation> InstallModelAsync(LocalAiModelInstallRequest request, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs install open AI whisper as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourceArchivePath">Source archive path value supplied to the local AI runtime operation and used when producing its result.</param>
    /// <param name="variant">Variant value supplied to the local AI runtime operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI model installation produced by the operation.</returns>
    Task<LocalAiModelInstallation> InstallOpenAiWhisperAsync(string sourceArchivePath, string variant, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Removes model as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="installationId">Identifier of the installation to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RemoveModelAsync(string installationId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs clear model cache as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ClearModelCacheAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Generates image as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime job result produced by the operation.</returns>
    Task<LocalAiRuntimeJobResult> GenerateImageAsync(LocalAiImageGenerationRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs edit workspace image as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime job result produced by the operation.</returns>
    Task<LocalAiRuntimeJobResult> EditWorkspaceImageAsync(LocalAiImageEditRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Generates video as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime job result produced by the operation.</returns>
    Task<LocalAiRuntimeJobResult> GenerateVideoAsync(LocalAiVideoGenerationRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Generates video from workspace image as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime job result produced by the operation.</returns>
    Task<LocalAiRuntimeJobResult> GenerateVideoFromWorkspaceImageAsync(LocalAiImageToVideoRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs transcribe workspace audio as part of the local AI runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI runtime job result produced by the operation.</returns>
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

/// <summary>
/// Defines the contract for hugging face model catalog behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IHuggingFaceModelCatalogService
{
    /// <summary>
    /// Performs search as part of the hugging face model catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<HuggingFaceModelSearchResult>> SearchAsync(HuggingFaceModelSearchRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for local AI artifact behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalAiArtifactService
{
    /// <summary>
    /// Gets the artifact root value that forms part of the local AI artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The artifact root value exposed by <see cref="ILocalAiArtifactService"/>.</value>
    string ArtifactRoot { get; }
    /// <summary>
    /// Performs publish as part of the local AI artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourcePath">Source path value supplied to the local AI artifact operation and used when producing its result.</param>
    /// <param name="preferredFileName">Preferred file name value supplied to the local AI artifact operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local AI artifact descriptor produced by the operation.</returns>
    Task<LocalAiArtifactDescriptor> PublishAsync(string sourcePath, string preferredFileName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs resolve as part of the local AI artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="artifactId">Identifier of the artifact to use for this operation.</param>
    /// <param name="fileName">File name value supplied to the local AI artifact operation and used when producing its result.</param>
    /// <returns>The local AI artifact descriptor produced by the operation.</returns>
    LocalAiArtifactDescriptor? Resolve(string artifactId, string fileName);
}
