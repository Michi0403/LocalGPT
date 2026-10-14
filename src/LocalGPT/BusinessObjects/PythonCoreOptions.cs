namespace LocalGPT.BusinessObjects
{
    /// <summary>Configures the LocalGPT-managed Python and specialized local-AI runtime.</summary>
    public class PythonCoreOptions
    {
        /// <summary>
        /// Defines the python core constant used by <see cref="PythonCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string PythonCore = "PythonCore";
        /// <summary>
        /// Gets or sets the python executable value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The python executable value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string? PythonExecutable { get; set; }
        /// <summary>
        /// Gets or sets the python runtime value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The python runtime value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string? PythonRuntime { get; set; }
        /// <summary>
        /// Gets or sets the python home value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The python home value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string? PythonHome { get; set; }
        /// <summary>
        /// Gets or sets the virtual environment path used by this python core instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The virtual environment path value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string? VirtualEnvironmentPath { get; set; }
        /// <summary>
        /// Gets or sets the model root value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The model root value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string? ModelRoot { get; set; }
        /// <summary>
        /// Gets or sets the artifact root value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact root value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string? ArtifactRoot { get; set; }
        /// <summary>
        /// Gets or sets the hugging face token environment variable value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The hugging face token environment variable value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string HuggingFaceTokenEnvironmentVariable { get; set; } = "HF_TOKEN";
        /// <summary>
        /// Gets or sets the default device value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default device value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string DefaultDevice { get; set; } = "auto";
        /// <summary>
        /// Gets or sets the default memory strategy value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default memory strategy value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string DefaultMemoryStrategy { get; set; } = "native";
        /// <summary>
        /// Gets or sets the default whisper model value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default whisper model value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string DefaultWhisperModel { get; set; } = "base";
        /// <summary>
        /// Gets or sets the default speech language value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default speech language value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string DefaultSpeechLanguage { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the default speech task value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default speech task value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string DefaultSpeechTask { get; set; } = "transcribe";
        /// <summary>
        /// Gets or sets the default speech initial prompt value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default speech initial prompt value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string DefaultSpeechInitialPrompt { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the queue capacity value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The queue capacity value exposed by <see cref="PythonCoreOptions"/>.</value>
        public int QueueCapacity { get; set; } = 64;
        /// <summary>
        /// Gets or sets the max parallel process jobs value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max parallel process jobs value exposed by <see cref="PythonCoreOptions"/>.</value>
        public int MaxParallelProcessJobs { get; set; } = 2;
        /// <summary>
        /// Gets or sets the maximum input megabytes value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The maximum input megabytes value exposed by <see cref="PythonCoreOptions"/>.</value>
        public int MaximumInputMegabytes { get; set; } = 512;
        /// <summary>
        /// Gets or sets the maximum image pixels value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The maximum image pixels value exposed by <see cref="PythonCoreOptions"/>.</value>
        public long MaximumImagePixels { get; set; } = 100_000_000;
        /// <summary>
        /// Gets or sets the maximum audio seconds value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The maximum audio seconds value exposed by <see cref="PythonCoreOptions"/>.</value>
        public int MaximumAudioSeconds { get; set; } = 7200;
        /// <summary>
        /// Gets or sets the minimum audio bytes per second value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The minimum audio bytes per second value exposed by <see cref="PythonCoreOptions"/>.</value>
        public int MinimumAudioBytesPerSecond { get; set; } = 768;
        /// <summary>
        /// Gets or sets a value indicating whether cache models applies to the python core state.
        /// </summary>
        /// <value>The cache models value exposed by <see cref="PythonCoreOptions"/>.</value>
        public bool CacheModels { get; set; } = true;
        /// <summary>
        /// Gets or sets the package profiles collection maintained or exposed by this python core instance for downstream processing.
        /// </summary>
        /// <value>The package profiles value exposed by <see cref="PythonCoreOptions"/>.</value>
        public List<LocalAiPackageProfile> PackageProfiles { get; set; } = [];
    }
}
