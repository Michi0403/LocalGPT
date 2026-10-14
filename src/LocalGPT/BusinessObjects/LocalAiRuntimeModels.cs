using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported local AI capability values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum LocalAiCapability
{
    /// <summary>
    /// Selects the unknown option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Selects the image generation option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ImageGeneration,
    /// <summary>
    /// Selects the image editing option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ImageEditing,
    /// <summary>
    /// Selects the text to video option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TextToVideo,
    /// <summary>
    /// Selects the image to video option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ImageToVideo,
    /// <summary>
    /// Selects the speech recognition option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SpeechRecognition,
    /// <summary>
    /// Selects the speech synthesis option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SpeechSynthesis,
    /// <summary>
    /// Selects the audio generation option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AudioGeneration,
    /// <summary>
    /// Selects the image understanding option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ImageUnderstanding,
    /// <summary>
    /// Selects the video understanding option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    VideoUnderstanding,
    /// <summary>
    /// Selects the embeddings option for <see cref="LocalAiCapability"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Embeddings
}

/// <summary>
/// Represents a local AI package profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalAiPackageProfile
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this local AI package profile instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="LocalAiPackageProfile"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the local AI package profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="LocalAiPackageProfile"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the local AI package profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="LocalAiPackageProfile"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the packages collection maintained or exposed by this local AI package profile instance for downstream processing.
    /// </summary>
    /// <value>The packages value exposed by <see cref="LocalAiPackageProfile"/>.</value>
    public List<string> Packages { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether includes torch applies to the local AI package profile state.
    /// </summary>
    /// <value>The includes torch value exposed by <see cref="LocalAiPackageProfile"/>.</value>
    public bool IncludesTorch { get; set; }
}

/// <summary>
/// Represents a python runtime candidate application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PythonRuntimeCandidate
{
    /// <summary>
    /// Gets or sets the executable path used by this python runtime candidate instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The executable path value exposed by <see cref="PythonRuntimeCandidate"/>.</value>
    public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the python home value that forms part of the python runtime candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The python home value exposed by <see cref="PythonRuntimeCandidate"/>.</value>
    public string PythonHome { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the python runtime library value that forms part of the python runtime candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The python runtime library value exposed by <see cref="PythonRuntimeCandidate"/>.</value>
    public string PythonRuntimeLibrary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version value that forms part of the python runtime candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="PythonRuntimeCandidate"/>.</value>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the discovery source value that forms part of the python runtime candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The discovery source value exposed by <see cref="PythonRuntimeCandidate"/>.</value>
    public string DiscoverySource { get; set; } = string.Empty;
    /// <summary>
    /// Gets a value indicating whether runtime library detected applies to the python runtime candidate state.
    /// </summary>
    /// <value>The runtime library detected value exposed by <see cref="PythonRuntimeCandidate"/>.</value>
    public bool RuntimeLibraryDetected => !string.IsNullOrWhiteSpace(PythonRuntimeLibrary);
}

/// <summary>
/// Represents a local AI runtime status application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalAiRuntimeStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether configured applies to the local AI runtime status state.
    /// </summary>
    /// <value>The configured value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public bool Configured { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether python executable exists applies to the local AI runtime status state.
    /// </summary>
    /// <value>The python executable exists value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public bool PythonExecutableExists { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether python runtime library exists applies to the local AI runtime status state.
    /// </summary>
    /// <value>The python runtime library exists value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public bool PythonRuntimeLibraryExists { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether virtual environment exists applies to the local AI runtime status state.
    /// </summary>
    /// <value>The virtual environment exists value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public bool VirtualEnvironmentExists { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether python net available applies to the local AI runtime status state.
    /// </summary>
    /// <value>The python net available value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public bool PythonNetAvailable { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether interpreter initialized applies to the local AI runtime status state.
    /// </summary>
    /// <value>The interpreter initialized value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public bool InterpreterInitialized { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether restart required applies to the local AI runtime status state.
    /// </summary>
    /// <value>The restart required value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public bool RestartRequired { get; set; }
    /// <summary>
    /// Gets or sets the python version value that forms part of the local AI runtime status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The python version value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public string PythonVersion { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the device summary value that forms part of the local AI runtime status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The device summary value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public string DeviceSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the message value that forms part of the local AI runtime status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the queue length that quantifies the associated local AI runtime status data.
    /// </summary>
    /// <value>The queue length value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public int QueueLength { get; set; }
    /// <summary>
    /// Gets or sets the installed model count that quantifies the associated local AI runtime status data.
    /// </summary>
    /// <value>The installed model count value exposed by <see cref="LocalAiRuntimeStatus"/>.</value>
    public int InstalledModelCount { get; set; }
}

/// <summary>Defines supported Hugging Face metadata ordering modes.</summary>
public enum HuggingFaceModelSort
{
    /// <summary>Orders models by download count.</summary>
    Downloads,
    /// <summary>Orders models by Hugging Face likes.</summary>
    Likes,
    /// <summary>Orders models by most recent metadata modification.</summary>
    Recent,
    /// <summary>Ranks recently modified models by likes within the configured recent window.</summary>
    RecentLiked
}

/// <summary>
/// Represents the input contract for hugging face model search, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class HuggingFaceModelSearchRequest
{
    /// <summary>
    /// Gets or sets the query value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The query value exposed by <see cref="HuggingFaceModelSearchRequest"/>.</value>
    public string Query { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the capability value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capability value exposed by <see cref="HuggingFaceModelSearchRequest"/>.</value>
    public LocalAiCapability Capability { get; set; } = LocalAiCapability.Unknown;
    /// <summary>
    /// Gets or sets the limit value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The limit value exposed by <see cref="HuggingFaceModelSearchRequest"/>.</value>
    public int Limit { get; set; } = 20;
    /// <summary>
    /// Gets or sets the sort value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sort value exposed by <see cref="HuggingFaceModelSearchRequest"/>.</value>
    public HuggingFaceModelSort Sort { get; set; } = HuggingFaceModelSort.Downloads;
    /// <summary>
    /// Gets or sets the recent window days value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recent window days value exposed by <see cref="HuggingFaceModelSearchRequest"/>.</value>
    public int RecentWindowDays { get; set; } = 30;
}

/// <summary>
/// Represents the outcome of hugging face model search, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class HuggingFaceModelSearchResult
{
    /// <summary>
    /// Gets or sets the stable model identifier used to identify or correlate this hugging face model search instance with related application state.
    /// </summary>
    /// <value>The model identifier value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public string ModelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the author value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The author value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public string Author { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the pipeline tag value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pipeline tag value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public string PipelineTag { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the library name value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The library name value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public string LibraryName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the revision value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public string Revision { get; set; } = "main";
    /// <summary>
    /// Gets or sets a value indicating whether private applies to the hugging face model search state.
    /// </summary>
    /// <value>The is private value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public bool IsPrivate { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether gated applies to the hugging face model search state.
    /// </summary>
    /// <value>The is gated value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public bool IsGated { get; set; }
    /// <summary>
    /// Gets or sets the downloads value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The downloads value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public long Downloads { get; set; }
    /// <summary>
    /// Gets or sets the likes value that forms part of the hugging face model search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The likes value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public int Likes { get; set; }
    /// <summary>
    /// Gets or sets the last modified UTC associated with this hugging face model search state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last modified UTC value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public DateTime? LastModifiedUtc { get; set; }
    /// <summary>
    /// Gets or sets the tags collection maintained or exposed by this hugging face model search instance for downstream processing.
    /// </summary>
    /// <value>The tags value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public List<string> Tags { get; set; } = [];
    /// <summary>
    /// Gets or sets the capabilities collection maintained or exposed by this hugging face model search instance for downstream processing.
    /// </summary>
    /// <value>The capabilities value exposed by <see cref="HuggingFaceModelSearchResult"/>.</value>
    public List<LocalAiCapability> Capabilities { get; set; } = [];
}

/// <summary>
/// Represents the input contract for local AI model install, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalAiModelInstallRequest
{
    /// <summary>
    /// Gets or sets the stable model identifier used to identify or correlate this local AI model install instance with related application state.
    /// </summary>
    /// <value>The model identifier value exposed by <see cref="LocalAiModelInstallRequest"/>.</value>
    public string ModelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the revision value that forms part of the local AI model install state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="LocalAiModelInstallRequest"/>.</value>
    public string Revision { get; set; } = "main";
    /// <summary>
    /// Gets or sets the adapter value that forms part of the local AI model install state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The adapter value exposed by <see cref="LocalAiModelInstallRequest"/>.</value>
    public string Adapter { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the capabilities collection maintained or exposed by this local AI model install instance for downstream processing.
    /// </summary>
    /// <value>The capabilities value exposed by <see cref="LocalAiModelInstallRequest"/>.</value>
    public List<LocalAiCapability> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether trust remote code applies to the local AI model install state.
    /// </summary>
    /// <value>The trust remote code value exposed by <see cref="LocalAiModelInstallRequest"/>.</value>
    public bool TrustRemoteCode { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires authentication applies to the local AI model install state.
    /// </summary>
    /// <value>The requires authentication value exposed by <see cref="LocalAiModelInstallRequest"/>.</value>
    public bool RequiresAuthentication { get; set; }
}

/// <summary>
/// Represents a local AI model installation application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalAiModelInstallation
{
    /// <summary>
    /// Gets or sets the stable installation identifier used to identify or correlate this local AI model installation instance with related application state.
    /// </summary>
    /// <value>The installation identifier value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public string InstallationId { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the stable model identifier used to identify or correlate this local AI model installation instance with related application state.
    /// </summary>
    /// <value>The model identifier value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public string ModelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the revision value that forms part of the local AI model installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public string Revision { get; set; } = "main";
    /// <summary>
    /// Gets or sets the local path used by this local AI model installation instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The local path value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public string LocalPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the adapter value that forms part of the local AI model installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The adapter value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public string Adapter { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the capabilities collection maintained or exposed by this local AI model installation instance for downstream processing.
    /// </summary>
    /// <value>The capabilities value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public List<LocalAiCapability> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether trust remote code applies to the local AI model installation state.
    /// </summary>
    /// <value>The trust remote code value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public bool TrustRemoteCode { get; set; }
    /// <summary>
    /// Gets or sets the source provider value that forms part of the local AI model installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source provider value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public string SourceProvider { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source reference value that forms part of the local AI model installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source reference value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public string SourceReference { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the runtime model name value that forms part of the local AI model installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The runtime model name value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public string RuntimeModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the installed at UTC associated with this local AI model installation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The installed at UTC value exposed by <see cref="LocalAiModelInstallation"/>.</value>
    public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the input contract for local AI runtime job, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalAiRuntimeJobRequest
{
    /// <summary>
    /// Gets or sets the operation value that forms part of the local AI runtime job state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The operation value exposed by <see cref="LocalAiRuntimeJobRequest"/>.</value>
    public string Operation { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable model installation identifier used to identify or correlate this local AI runtime job instance with related application state.
    /// </summary>
    /// <value>The model installation identifier value exposed by <see cref="LocalAiRuntimeJobRequest"/>.</value>
    public string ModelInstallationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameters collection maintained or exposed by this local AI runtime job instance for downstream processing.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="LocalAiRuntimeJobRequest"/>.</value>
    public Dictionary<string, object?> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents the outcome of local AI runtime job, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class LocalAiRuntimeJobResult
{
    /// <summary>
    /// Gets or sets the stable job identifier used to identify or correlate this local AI runtime job instance with related application state.
    /// </summary>
    /// <value>The job identifier value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the local AI runtime job state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the local AI runtime job state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the message value that forms part of the local AI runtime job state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the text value that forms part of the local AI runtime job state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable artifact identifier used to identify or correlate this local AI runtime job instance with related application state.
    /// </summary>
    /// <value>The artifact identifier value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public string ArtifactId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the artifact file name used by this local AI runtime job instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The artifact file name value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public string ArtifactFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the artifact URL that identifies the network or application endpoint associated with this local AI runtime job state.
    /// </summary>
    /// <value>The artifact URL value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public string ArtifactUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the artifact markdown value that forms part of the local AI runtime job state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The artifact markdown value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public string ArtifactMarkdown { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this local AI runtime job instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="LocalAiRuntimeJobResult"/>.</value>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents local AI artifact state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class LocalAiArtifactDescriptor
{
    /// <summary>
    /// Gets or sets the stable artifact identifier used to identify or correlate this local AI artifact instance with related application state.
    /// </summary>
    /// <value>The artifact identifier value exposed by <see cref="LocalAiArtifactDescriptor"/>.</value>
    public string ArtifactId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the file name used by this local AI artifact instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file name value exposed by <see cref="LocalAiArtifactDescriptor"/>.</value>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content type value that forms part of the local AI artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content type value exposed by <see cref="LocalAiArtifactDescriptor"/>.</value>
    public string ContentType { get; set; } = "application/octet-stream";
    /// <summary>
    /// Gets or sets the full path used by this local AI artifact instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The full path value exposed by <see cref="LocalAiArtifactDescriptor"/>.</value>
    public string FullPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the download URL that identifies the network or application endpoint associated with this local AI artifact state.
    /// </summary>
    /// <value>The download URL value exposed by <see cref="LocalAiArtifactDescriptor"/>.</value>
    public string DownloadUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the length that quantifies the associated local AI artifact data.
    /// </summary>
    /// <value>The length value exposed by <see cref="LocalAiArtifactDescriptor"/>.</value>
    public long Length { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this local AI artifact state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="LocalAiArtifactDescriptor"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Describes one browser microphone recording after LocalGPT has persisted the audio independently from optional speech transcription.</summary>
public sealed class LocalAiMicrophoneRecording
{
    /// <summary>Gets or sets the admitted chat-upload workspace that owns the original WAV evidence.</summary>
    /// <value>The upload workspace containing the original browser-recorded WAV.</value>
    public ChatUploadWorkspaceResult Workspace { get; set; } = new(string.Empty, string.Empty, string.Empty, string.Empty, DateTimeOffset.MinValue, [], [], string.Empty);
    /// <summary>Gets or sets the published media artifact that keeps the recording downloadable from the chat history.</summary>
    /// <value>The durable local media artifact exposed through the LocalGPT artifact route.</value>
    public LocalAiArtifactDescriptor Artifact { get; set; } = new();
    /// <summary>Gets or sets the workspace-relative WAV path used by optional speech recognition.</summary>
    /// <value>The relative path to the persisted WAV inside its admitted upload workspace.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional speech-to-text transcript. An empty value means the audio remains attached without transcription.</summary>
    /// <value>The optional recognized speech text.</value>
    public string Transcript { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional transcription status shown to the local user without exposing recorded content to logs.</summary>
    /// <value>The bounded operator-facing status for optional transcription.</value>
    public string TranscriptionStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether transcription succeeded applies to the local AI microphone recording state.
    /// </summary>
    /// <value><see langword="true"/> when optional transcription produced usable text; otherwise <see langword="false"/>.</value>
    public bool TranscriptionSucceeded { get; set; }
}

/// <summary>
/// Represents the input contract for local AI image generation, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalAiImageGenerationRequest
{
    /// <summary>
    /// Gets or sets the stable model installation identifier used to identify or correlate this local AI image generation instance with related application state.
    /// </summary>
    /// <value>The model installation identifier value exposed by <see cref="LocalAiImageGenerationRequest"/>.</value>
    public string ModelInstallationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt value that forms part of the local AI image generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="LocalAiImageGenerationRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the negative prompt value that forms part of the local AI image generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The negative prompt value exposed by <see cref="LocalAiImageGenerationRequest"/>.</value>
    public string NegativePrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the width value that forms part of the local AI image generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="LocalAiImageGenerationRequest"/>.</value>
    public int Width { get; set; } = 1024;
    /// <summary>
    /// Gets or sets the height value that forms part of the local AI image generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="LocalAiImageGenerationRequest"/>.</value>
    public int Height { get; set; } = 1024;
    /// <summary>
    /// Gets or sets the steps value that forms part of the local AI image generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The steps value exposed by <see cref="LocalAiImageGenerationRequest"/>.</value>
    public int Steps { get; set; } = 28;
    /// <summary>
    /// Gets or sets the guidance scale value that forms part of the local AI image generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The guidance scale value exposed by <see cref="LocalAiImageGenerationRequest"/>.</value>
    public double GuidanceScale { get; set; } = 4.0;
    /// <summary>
    /// Gets or sets the seed value that forms part of the local AI image generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The seed value exposed by <see cref="LocalAiImageGenerationRequest"/>.</value>
    public long? Seed { get; set; }
}

/// <summary>
/// Represents the input contract for local AI video generation, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalAiVideoGenerationRequest
{
    /// <summary>
    /// Gets or sets the stable model installation identifier used to identify or correlate this local AI video generation instance with related application state.
    /// </summary>
    /// <value>The model installation identifier value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public string ModelInstallationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt value that forms part of the local AI video generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the width value that forms part of the local AI video generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public int Width { get; set; } = 832;
    /// <summary>
    /// Gets or sets the height value that forms part of the local AI video generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public int Height { get; set; } = 480;
    /// <summary>
    /// Gets or sets the frames value that forms part of the local AI video generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frames value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public int Frames { get; set; } = 49;
    /// <summary>
    /// Gets or sets the steps value that forms part of the local AI video generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The steps value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public int Steps { get; set; } = 30;
    /// <summary>
    /// Gets or sets the guidance scale value that forms part of the local AI video generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The guidance scale value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public double GuidanceScale { get; set; } = 5.0;
    /// <summary>
    /// Gets or sets the frames per second value that forms part of the local AI video generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frames per second value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public int FramesPerSecond { get; set; } = 16;
    /// <summary>
    /// Gets or sets the seed value that forms part of the local AI video generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The seed value exposed by <see cref="LocalAiVideoGenerationRequest"/>.</value>
    public long? Seed { get; set; }
}


/// <summary>
/// Represents the input contract for local AI image edit, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalAiImageEditRequest
{
    /// <summary>
    /// Gets or sets the stable model installation identifier used to identify or correlate this local AI image edit instance with related application state.
    /// </summary>
    /// <value>The model installation identifier value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public string ModelInstallationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the local AI image edit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the relative path used by this local AI image edit instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt value that forms part of the local AI image edit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the negative prompt value that forms part of the local AI image edit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The negative prompt value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public string NegativePrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the width value that forms part of the local AI image edit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public int Width { get; set; } = 1024;
    /// <summary>
    /// Gets or sets the height value that forms part of the local AI image edit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public int Height { get; set; } = 1024;
    /// <summary>
    /// Gets or sets the steps value that forms part of the local AI image edit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The steps value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public int Steps { get; set; } = 28;
    /// <summary>
    /// Gets or sets the guidance scale value that forms part of the local AI image edit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The guidance scale value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public double GuidanceScale { get; set; } = 4.0;
    /// <summary>
    /// Gets or sets the seed value that forms part of the local AI image edit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The seed value exposed by <see cref="LocalAiImageEditRequest"/>.</value>
    public long? Seed { get; set; }
}

/// <summary>
/// Represents the input contract for local AI image to video, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalAiImageToVideoRequest
{
    /// <summary>
    /// Gets or sets the stable model installation identifier used to identify or correlate this local AI image to video instance with related application state.
    /// </summary>
    /// <value>The model installation identifier value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public string ModelInstallationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the relative path used by this local AI image to video instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the width value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public int Width { get; set; } = 832;
    /// <summary>
    /// Gets or sets the height value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public int Height { get; set; } = 480;
    /// <summary>
    /// Gets or sets the frames value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frames value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public int Frames { get; set; } = 49;
    /// <summary>
    /// Gets or sets the steps value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The steps value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public int Steps { get; set; } = 30;
    /// <summary>
    /// Gets or sets the guidance scale value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The guidance scale value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public double GuidanceScale { get; set; } = 5.0;
    /// <summary>
    /// Gets or sets the frames per second value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frames per second value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public int FramesPerSecond { get; set; } = 16;
    /// <summary>
    /// Gets or sets the seed value that forms part of the local AI image to video state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The seed value exposed by <see cref="LocalAiImageToVideoRequest"/>.</value>
    public long? Seed { get; set; }
}

/// <summary>
/// Represents the input contract for local AI speech recognition, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalAiSpeechRecognitionRequest
{
    /// <summary>
    /// Gets or sets the stable model installation identifier used to identify or correlate this local AI speech recognition instance with related application state.
    /// </summary>
    /// <value>The model installation identifier value exposed by <see cref="LocalAiSpeechRecognitionRequest"/>.</value>
    public string ModelInstallationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the local AI speech recognition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="LocalAiSpeechRecognitionRequest"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the relative path used by this local AI speech recognition instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="LocalAiSpeechRecognitionRequest"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the language value that forms part of the local AI speech recognition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The language value exposed by <see cref="LocalAiSpeechRecognitionRequest"/>.</value>
    public string Language { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the task value that forms part of the local AI speech recognition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The task value exposed by <see cref="LocalAiSpeechRecognitionRequest"/>.</value>
    public string Task { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the initial prompt value that forms part of the local AI speech recognition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The initial prompt value exposed by <see cref="LocalAiSpeechRecognitionRequest"/>.</value>
    public string InitialPrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the device value that forms part of the local AI speech recognition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The device value exposed by <see cref="LocalAiSpeechRecognitionRequest"/>.</value>
    public string Device { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the cache model value that forms part of the local AI speech recognition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The cache model value exposed by <see cref="LocalAiSpeechRecognitionRequest"/>.</value>
    public bool? CacheModel { get; set; }
}

/// <summary>Mutable LocalGPT Python execution policy exposed to the local user and approval-gated AI functions.</summary>
public sealed class LocalAiRuntimeConfiguration
{
    /// <summary>
    /// Gets or sets the default device value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default device value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public string DefaultDevice { get; set; } = "auto";
    /// <summary>
    /// Gets or sets the default memory strategy value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default memory strategy value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public string DefaultMemoryStrategy { get; set; } = "native";
    /// <summary>
    /// Gets or sets the default whisper model value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default whisper model value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public string DefaultWhisperModel { get; set; } = "base";
    /// <summary>
    /// Gets or sets the default speech language value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default speech language value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public string DefaultSpeechLanguage { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the default speech task value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default speech task value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public string DefaultSpeechTask { get; set; } = "transcribe";
    /// <summary>
    /// Gets or sets the default speech initial prompt value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default speech initial prompt value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public string DefaultSpeechInitialPrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the queue capacity value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The queue capacity value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public int QueueCapacity { get; set; } = 64;
    /// <summary>
    /// Gets or sets the max parallel process jobs value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max parallel process jobs value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public int MaxParallelProcessJobs { get; set; } = 2;
    /// <summary>
    /// Gets or sets the maximum input megabytes value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum input megabytes value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public int MaximumInputMegabytes { get; set; } = 512;
    /// <summary>
    /// Gets or sets the maximum image pixels value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum image pixels value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public long MaximumImagePixels { get; set; } = 100_000_000;
    /// <summary>
    /// Gets or sets the maximum audio seconds value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum audio seconds value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public int MaximumAudioSeconds { get; set; } = 7200;
    /// <summary>
    /// Gets or sets the minimum audio bytes per second value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum audio bytes per second value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public int MinimumAudioBytesPerSecond { get; set; } = 768;
    /// <summary>
    /// Gets or sets a value indicating whether cache models applies to the local AI runtime state.
    /// </summary>
    /// <value>The cache models value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public bool CacheModels { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether interpreter initialized applies to the local AI runtime state.
    /// </summary>
    /// <value>The interpreter initialized value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public bool InterpreterInitialized { get; set; }
    /// <summary>
    /// Gets or sets the queue length that quantifies the associated local AI runtime data.
    /// </summary>
    /// <value>The queue length value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public int QueueLength { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether restart required applies to the local AI runtime state.
    /// </summary>
    /// <value>The restart required value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public bool RestartRequired { get; set; }
    /// <summary>
    /// Gets or sets the restart reason value that forms part of the local AI runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The restart reason value exposed by <see cref="LocalAiRuntimeConfiguration"/>.</value>
    public string RestartReason { get; set; } = string.Empty;
}

/// <summary>Approval-gated update for the LocalGPT-managed Python execution policy.</summary>
public sealed class LocalAiRuntimeConfigurationChangeRequest
{
    /// <summary>
    /// Gets or sets the default device value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default device value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public string DefaultDevice { get; set; } = "auto";
    /// <summary>
    /// Gets or sets the default memory strategy value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default memory strategy value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public string DefaultMemoryStrategy { get; set; } = "native";
    /// <summary>
    /// Gets or sets the default whisper model value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default whisper model value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public string DefaultWhisperModel { get; set; } = "base";
    /// <summary>
    /// Gets or sets the default speech language value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default speech language value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public string DefaultSpeechLanguage { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the default speech task value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default speech task value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public string DefaultSpeechTask { get; set; } = "transcribe";
    /// <summary>
    /// Gets or sets the default speech initial prompt value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default speech initial prompt value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public string DefaultSpeechInitialPrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the queue capacity value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The queue capacity value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public int QueueCapacity { get; set; } = 64;
    /// <summary>
    /// Gets or sets the max parallel process jobs value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max parallel process jobs value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public int MaxParallelProcessJobs { get; set; } = 2;
    /// <summary>
    /// Gets or sets the maximum input megabytes value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum input megabytes value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public int MaximumInputMegabytes { get; set; } = 512;
    /// <summary>
    /// Gets or sets the maximum image pixels value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum image pixels value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public long MaximumImagePixels { get; set; } = 100_000_000;
    /// <summary>
    /// Gets or sets the maximum audio seconds value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum audio seconds value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public int MaximumAudioSeconds { get; set; } = 7200;
    /// <summary>
    /// Gets or sets the minimum audio bytes per second value that forms part of the local AI runtime configuration change state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum audio bytes per second value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public int MinimumAudioBytesPerSecond { get; set; } = 768;
    /// <summary>
    /// Gets or sets a value indicating whether cache models applies to the local AI runtime configuration change state.
    /// </summary>
    /// <value>The cache models value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public bool CacheModels { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the local AI runtime configuration change state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="LocalAiRuntimeConfigurationChangeRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}
