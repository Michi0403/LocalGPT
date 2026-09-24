namespace LocalGPT.BusinessObjects
{
    /// <summary>Configures the LocalGPT-managed Python and specialized local-AI runtime.</summary>
    public class PythonCoreOptions
    {
        public const string PythonCore = "PythonCore";
        public string? PythonExecutable { get; set; }
        public string? PythonRuntime { get; set; }
        public string? PythonHome { get; set; }
        public string? VirtualEnvironmentPath { get; set; }
        public string? ModelRoot { get; set; }
        public string? ArtifactRoot { get; set; }
        public string HuggingFaceTokenEnvironmentVariable { get; set; } = "HF_TOKEN";
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
        public List<LocalAiPackageProfile> PackageProfiles { get; set; } = [];
    }
}
