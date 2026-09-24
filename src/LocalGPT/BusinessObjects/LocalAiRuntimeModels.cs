using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

public enum LocalAiCapability
{
    Unknown = 0,
    ImageGeneration,
    ImageEditing,
    TextToVideo,
    ImageToVideo,
    SpeechRecognition,
    SpeechSynthesis,
    AudioGeneration,
    ImageUnderstanding,
    VideoUnderstanding,
    Embeddings
}

public sealed class LocalAiPackageProfile
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Packages { get; set; } = [];
    public bool IncludesTorch { get; set; }
}

public sealed class PythonRuntimeCandidate
{
    public string ExecutablePath { get; set; } = string.Empty;
    public string PythonHome { get; set; } = string.Empty;
    public string PythonRuntimeLibrary { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string DiscoverySource { get; set; } = string.Empty;
    public bool RuntimeLibraryDetected => !string.IsNullOrWhiteSpace(PythonRuntimeLibrary);
}

public sealed class LocalAiRuntimeStatus
{
    public bool Configured { get; set; }
    public bool PythonExecutableExists { get; set; }
    public bool PythonRuntimeLibraryExists { get; set; }
    public bool VirtualEnvironmentExists { get; set; }
    public bool PythonNetAvailable { get; set; }
    public bool InterpreterInitialized { get; set; }
    public bool RestartRequired { get; set; }
    public string PythonVersion { get; set; } = string.Empty;
    public string DeviceSummary { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int QueueLength { get; set; }
    public int InstalledModelCount { get; set; }
}

public sealed class HuggingFaceModelSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public LocalAiCapability Capability { get; set; } = LocalAiCapability.Unknown;
    public int Limit { get; set; } = 20;
}

public sealed class HuggingFaceModelSearchResult
{
    public string ModelId { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string PipelineTag { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public string Revision { get; set; } = "main";
    public bool IsPrivate { get; set; }
    public bool IsGated { get; set; }
    public long Downloads { get; set; }
    public int Likes { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<LocalAiCapability> Capabilities { get; set; } = [];
}

public sealed class LocalAiModelInstallRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string Revision { get; set; } = "main";
    public string Adapter { get; set; } = string.Empty;
    public List<LocalAiCapability> Capabilities { get; set; } = [];
    public bool TrustRemoteCode { get; set; }
    public bool RequiresAuthentication { get; set; }
}

public sealed class LocalAiModelInstallation
{
    public string InstallationId { get; set; } = Guid.NewGuid().ToString("N");
    public string ModelId { get; set; } = string.Empty;
    public string Revision { get; set; } = "main";
    public string LocalPath { get; set; } = string.Empty;
    public string Adapter { get; set; } = string.Empty;
    public List<LocalAiCapability> Capabilities { get; set; } = [];
    public bool TrustRemoteCode { get; set; }
    public string SourceProvider { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string RuntimeModelName { get; set; } = string.Empty;
    public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class LocalAiRuntimeJobRequest
{
    public string Operation { get; set; } = string.Empty;
    public string ModelInstallationId { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LocalAiRuntimeJobResult
{
    public Guid JobId { get; set; }
    public bool Succeeded { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ArtifactId { get; set; } = string.Empty;
    public string ArtifactFileName { get; set; } = string.Empty;
    public string ArtifactUrl { get; set; } = string.Empty;
    public string ArtifactMarkdown { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LocalAiArtifactDescriptor
{
    public string ArtifactId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string FullPath { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long Length { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class LocalAiImageGenerationRequest
{
    public string ModelInstallationId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string NegativePrompt { get; set; } = string.Empty;
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 1024;
    public int Steps { get; set; } = 28;
    public double GuidanceScale { get; set; } = 4.0;
    public long? Seed { get; set; }
}

public sealed class LocalAiVideoGenerationRequest
{
    public string ModelInstallationId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public int Width { get; set; } = 832;
    public int Height { get; set; } = 480;
    public int Frames { get; set; } = 49;
    public int Steps { get; set; } = 30;
    public double GuidanceScale { get; set; } = 5.0;
    public int FramesPerSecond { get; set; } = 16;
    public long? Seed { get; set; }
}


public sealed class LocalAiImageEditRequest
{
    public string ModelInstallationId { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string NegativePrompt { get; set; } = string.Empty;
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 1024;
    public int Steps { get; set; } = 28;
    public double GuidanceScale { get; set; } = 4.0;
    public long? Seed { get; set; }
}

public sealed class LocalAiImageToVideoRequest
{
    public string ModelInstallationId { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public int Width { get; set; } = 832;
    public int Height { get; set; } = 480;
    public int Frames { get; set; } = 49;
    public int Steps { get; set; } = 30;
    public double GuidanceScale { get; set; } = 5.0;
    public int FramesPerSecond { get; set; } = 16;
    public long? Seed { get; set; }
}

public sealed class LocalAiSpeechRecognitionRequest
{
    public string ModelInstallationId { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public string InitialPrompt { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public bool? CacheModel { get; set; }
}

/// <summary>Mutable LocalGPT Python execution policy exposed to the local user and approval-gated AI functions.</summary>
public sealed class LocalAiRuntimeConfiguration
{
    public string DefaultDevice { get; set; } = "auto";
    public string DefaultWhisperModel { get; set; } = "base";
    public string DefaultSpeechLanguage { get; set; } = string.Empty;
    public string DefaultSpeechTask { get; set; } = "transcribe";
    public string DefaultSpeechInitialPrompt { get; set; } = string.Empty;
    public int QueueCapacity { get; set; } = 64;
    public int MaximumInputMegabytes { get; set; } = 512;
    public long MaximumImagePixels { get; set; } = 100_000_000;
    public int MaximumAudioSeconds { get; set; } = 7200;
    public int MinimumAudioBytesPerSecond { get; set; } = 768;
    public bool CacheModels { get; set; } = true;
    public bool InterpreterInitialized { get; set; }
    public int QueueLength { get; set; }
    public bool RestartRequired { get; set; }
    public string RestartReason { get; set; } = string.Empty;
}

/// <summary>Approval-gated update for the LocalGPT-managed Python execution policy.</summary>
public sealed class LocalAiRuntimeConfigurationChangeRequest
{
    public string DefaultDevice { get; set; } = "auto";
    public string DefaultWhisperModel { get; set; } = "base";
    public string DefaultSpeechLanguage { get; set; } = string.Empty;
    public string DefaultSpeechTask { get; set; } = "transcribe";
    public string DefaultSpeechInitialPrompt { get; set; } = string.Empty;
    public int QueueCapacity { get; set; } = 64;
    public int MaximumInputMegabytes { get; set; } = 512;
    public long MaximumImagePixels { get; set; } = 100_000_000;
    public int MaximumAudioSeconds { get; set; } = 7200;
    public int MinimumAudioBytesPerSecond { get; set; } = 768;
    public bool CacheModels { get; set; } = true;
    public bool UserConfirmed { get; set; }
}
