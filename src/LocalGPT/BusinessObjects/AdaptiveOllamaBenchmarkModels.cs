using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Carries the configurable adaptive Ollama benchmark settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkOptions
{
    /// <summary>
    /// Gets or sets the endpoint that identifies the network or application endpoint associated with this adaptive Ollama benchmark state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model names collection maintained or exposed by this adaptive Ollama benchmark instance for downstream processing.
    /// </summary>
    /// <value>The model names value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public List<string> ModelNames { get; set; } = [];
    /// <summary>
    /// Gets or sets the max models value that forms part of the adaptive Ollama benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max models value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public int MaxModels { get; set; } = 3;
    /// <summary>
    /// Gets or sets the max profiles per model value that forms part of the adaptive Ollama benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max profiles per model value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public int MaxProfilesPerModel { get; set; } = 4;
    /// <summary>
    /// Gets or sets the max tasks value that forms part of the adaptive Ollama benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max tasks value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public int MaxTasks { get; set; } = 3;
    /// <summary>
    /// Gets or sets the max seconds per call value that forms part of the adaptive Ollama benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max seconds per call value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public int MaxSecondsPerCall { get; set; } = 120;
    /// <summary>
    /// Gets or sets the improvement threshold percent value that forms part of the adaptive Ollama benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The improvement threshold percent value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public double ImprovementThresholdPercent { get; set; } = 5d;
    /// <summary>
    /// Gets or sets a value indicating whether peer authored task applies to the adaptive Ollama benchmark state.
    /// </summary>
    /// <value>The include peer authored task value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public bool IncludePeerAuthoredTask { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether persist preset applies to the adaptive Ollama benchmark state.
    /// </summary>
    /// <value>The persist preset value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public bool PersistPreset { get; set; } = true;
    /// <summary>
    /// Gets or sets the preset name value that forms part of the adaptive Ollama benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preset name value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public string PresetName { get; set; } = "Adaptive Ollama Benchmark";
    /// <summary>
    /// Gets or sets a value indicating whether make default applies to the adaptive Ollama benchmark state.
    /// </summary>
    /// <value>The make default value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public bool MakeDefault { get; set; }
    /// <summary>
    /// Gets or sets the maximum context tokens value that forms part of the adaptive Ollama benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum context tokens value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public int MaximumContextTokens { get; set; } = 16384;
    /// <summary>
    /// Gets or sets the maximum output tokens value that forms part of the adaptive Ollama benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum output tokens value exposed by <see cref="AdaptiveOllamaBenchmarkOptions"/>.</value>
    public int MaximumOutputTokens { get; set; } = 512;
}

/// <summary>
/// Represents an adaptive Ollama benchmark report application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkReport
{
    /// <summary>
    /// Gets or sets the stable run identifier used to identify or correlate this adaptive Ollama benchmark report instance with related application state.
    /// </summary>
    /// <value>The run identifier value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public Guid RunId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the started at UTC associated with this adaptive Ollama benchmark report state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the completed at UTC associated with this adaptive Ollama benchmark report state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public DateTimeOffset CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the endpoint that identifies the network or application endpoint associated with this adaptive Ollama benchmark report state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the hardware summary value that forms part of the adaptive Ollama benchmark report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware summary value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public string HardwareSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the peer authored task value that forms part of the adaptive Ollama benchmark report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The peer authored task value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public string PeerAuthoredTask { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the models collection maintained or exposed by this adaptive Ollama benchmark report instance for downstream processing.
    /// </summary>
    /// <value>The models value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public List<AdaptiveOllamaBenchmarkModelResult> Models { get; set; } = [];
    /// <summary>
    /// Gets or sets the best model value that forms part of the adaptive Ollama benchmark report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The best model value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public string BestModel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the best profile value that forms part of the adaptive Ollama benchmark report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The best profile value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public string BestProfile { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the best score value that forms part of the adaptive Ollama benchmark report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The best score value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public double BestScore { get; set; }
    /// <summary>
    /// Gets or sets the stable saved preset identifier used to identify or correlate this adaptive Ollama benchmark report instance with related application state.
    /// </summary>
    /// <value>The saved preset identifier value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public Guid? SavedPresetId { get; set; }
    /// <summary>
    /// Gets or sets the saved preset name value that forms part of the adaptive Ollama benchmark report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The saved preset name value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public string SavedPresetName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this adaptive Ollama benchmark report instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="AdaptiveOllamaBenchmarkReport"/>.</value>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Represents the outcome of adaptive Ollama benchmark model, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkModelResult
{
    /// <summary>
    /// Gets or sets the model name value that forms part of the adaptive Ollama benchmark model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="AdaptiveOllamaBenchmarkModelResult"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the installed size bytes value that forms part of the adaptive Ollama benchmark model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installed size bytes value exposed by <see cref="AdaptiveOllamaBenchmarkModelResult"/>.</value>
    public long? InstalledSizeBytes { get; set; }
    /// <summary>
    /// Gets or sets the parameter size that quantifies the associated adaptive Ollama benchmark model data.
    /// </summary>
    /// <value>The parameter size value exposed by <see cref="AdaptiveOllamaBenchmarkModelResult"/>.</value>
    public string ParameterSize { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the profiles collection maintained or exposed by this adaptive Ollama benchmark model instance for downstream processing.
    /// </summary>
    /// <value>The profiles value exposed by <see cref="AdaptiveOllamaBenchmarkModelResult"/>.</value>
    public List<AdaptiveOllamaBenchmarkProfileResult> Profiles { get; set; } = [];
    /// <summary>
    /// Gets or sets the best profile value that forms part of the adaptive Ollama benchmark model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The best profile value exposed by <see cref="AdaptiveOllamaBenchmarkModelResult"/>.</value>
    public string BestProfile { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the best score value that forms part of the adaptive Ollama benchmark model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The best score value exposed by <see cref="AdaptiveOllamaBenchmarkModelResult"/>.</value>
    public double BestScore { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether stopped because improvement was below threshold applies to the adaptive Ollama benchmark model state.
    /// </summary>
    /// <value>The stopped because improvement was below threshold value exposed by <see cref="AdaptiveOllamaBenchmarkModelResult"/>.</value>
    public bool StoppedBecauseImprovementWasBelowThreshold { get; set; }
    /// <summary>
    /// Gets or sets the error value that forms part of the adaptive Ollama benchmark model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="AdaptiveOllamaBenchmarkModelResult"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of adaptive Ollama benchmark profile, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkProfileResult
{
    /// <summary>
    /// Gets or sets the profile name value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The profile name value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public string ProfileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the context tokens value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The context tokens value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public int ContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the output tokens value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output tokens value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public int OutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the Ollama num GPU value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama num GPU value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public int? OllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets the num batch value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The num batch value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public int NumBatch { get; set; }
    /// <summary>
    /// Gets or sets the score value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The score value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public double Score { get; set; }
    /// <summary>
    /// Gets or sets the average tokens per second value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The average tokens per second value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public double AverageTokensPerSecond { get; set; }
    /// <summary>
    /// Gets or sets the average quality score value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The average quality score value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public double AverageQualityScore { get; set; }
    /// <summary>
    /// Gets or sets the average total milliseconds value that forms part of the adaptive Ollama benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The average total milliseconds value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public double AverageTotalMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the tasks collection maintained or exposed by this adaptive Ollama benchmark profile instance for downstream processing.
    /// </summary>
    /// <value>The tasks value exposed by <see cref="AdaptiveOllamaBenchmarkProfileResult"/>.</value>
    public List<AdaptiveOllamaBenchmarkTaskResult> Tasks { get; set; } = [];
}

/// <summary>
/// Represents the outcome of adaptive Ollama benchmark task, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkTaskResult
{
    /// <summary>
    /// Gets or sets the task name value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The task name value exposed by <see cref="AdaptiveOllamaBenchmarkTaskResult"/>.</value>
    public string TaskName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the adaptive Ollama benchmark task state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="AdaptiveOllamaBenchmarkTaskResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the quality score value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quality score value exposed by <see cref="AdaptiveOllamaBenchmarkTaskResult"/>.</value>
    public double QualityScore { get; set; }
    /// <summary>
    /// Gets or sets the tokens per second value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tokens per second value exposed by <see cref="AdaptiveOllamaBenchmarkTaskResult"/>.</value>
    public double TokensPerSecond { get; set; }
    /// <summary>
    /// Gets or sets the total milliseconds value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The total milliseconds value exposed by <see cref="AdaptiveOllamaBenchmarkTaskResult"/>.</value>
    public long TotalMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the evaluated tokens value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The evaluated tokens value exposed by <see cref="AdaptiveOllamaBenchmarkTaskResult"/>.</value>
    public int EvaluatedTokens { get; set; }
    /// <summary>
    /// Gets or sets the response preview value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The response preview value exposed by <see cref="AdaptiveOllamaBenchmarkTaskResult"/>.</value>
    public string ResponsePreview { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="AdaptiveOllamaBenchmarkTaskResult"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents an adaptive Ollama benchmark task application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkTask
{
    /// <summary>
    /// Gets or sets the name value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="AdaptiveOllamaBenchmarkTask"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt value that forms part of the adaptive Ollama benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="AdaptiveOllamaBenchmarkTask"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the expected tokens collection maintained or exposed by this adaptive Ollama benchmark task instance for downstream processing.
    /// </summary>
    /// <value>The expected tokens value exposed by <see cref="AdaptiveOllamaBenchmarkTask"/>.</value>
    public List<string> ExpectedTokens { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether expect JSON applies to the adaptive Ollama benchmark task state.
    /// </summary>
    /// <value>The expect JSON value exposed by <see cref="AdaptiveOllamaBenchmarkTask"/>.</value>
    public bool ExpectJson { get; set; }
}

/// <summary>
/// Represents an adaptive Ollama tuning profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class AdaptiveOllamaTuningProfile
{
    /// <summary>
    /// Gets or sets the name value that forms part of the adaptive Ollama tuning profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="AdaptiveOllamaTuningProfile"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the context tokens value that forms part of the adaptive Ollama tuning profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The context tokens value exposed by <see cref="AdaptiveOllamaTuningProfile"/>.</value>
    public int ContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the output tokens value that forms part of the adaptive Ollama tuning profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output tokens value exposed by <see cref="AdaptiveOllamaTuningProfile"/>.</value>
    public int OutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the num batch value that forms part of the adaptive Ollama tuning profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The num batch value exposed by <see cref="AdaptiveOllamaTuningProfile"/>.</value>
    public int NumBatch { get; set; }
    /// <summary>
    /// Gets or sets the Ollama num GPU value that forms part of the adaptive Ollama tuning profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama num GPU value exposed by <see cref="AdaptiveOllamaTuningProfile"/>.</value>
    public int? OllamaNumGpu { get; set; }
}

/// <summary>
/// Represents the outcome of Ollama benchmark tags, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class OllamaBenchmarkTagsResponse
{
    /// <summary>
    /// Gets or sets the models collection maintained or exposed by this Ollama benchmark tags instance for downstream processing.
    /// </summary>
    /// <value>The models value exposed by <see cref="OllamaBenchmarkTagsResponse"/>.</value>
    [JsonPropertyName("models")]
    public List<OllamaBenchmarkModelInfo> Models { get; set; } = [];
}

/// <summary>
/// Represents an Ollama benchmark model info application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaBenchmarkModelInfo
{
    /// <summary>
    /// Gets or sets the name value that forms part of the Ollama benchmark model info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OllamaBenchmarkModelInfo"/>.</value>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model value that forms part of the Ollama benchmark model info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model value exposed by <see cref="OllamaBenchmarkModelInfo"/>.</value>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size that quantifies the associated Ollama benchmark model info data.
    /// </summary>
    /// <value>The size value exposed by <see cref="OllamaBenchmarkModelInfo"/>.</value>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the details value that forms part of the Ollama benchmark model info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The details value exposed by <see cref="OllamaBenchmarkModelInfo"/>.</value>
    [JsonPropertyName("details")]
    public OllamaBenchmarkModelDetails Details { get; set; } = new();
}

/// <summary>
/// Represents an Ollama benchmark model details application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaBenchmarkModelDetails
{
    /// <summary>
    /// Gets or sets the parameter size that quantifies the associated Ollama benchmark model details data.
    /// </summary>
    /// <value>The parameter size value exposed by <see cref="OllamaBenchmarkModelDetails"/>.</value>
    [JsonPropertyName("parameter_size")]
    public string ParameterSize { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantization level value that forms part of the Ollama benchmark model details state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quantization level value exposed by <see cref="OllamaBenchmarkModelDetails"/>.</value>
    [JsonPropertyName("quantization_level")]
    public string QuantizationLevel { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for Ollama benchmark generate, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class OllamaBenchmarkGenerateRequest
{
    /// <summary>
    /// Gets or sets the model value that forms part of the Ollama benchmark generate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model value exposed by <see cref="OllamaBenchmarkGenerateRequest"/>.</value>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt value that forms part of the Ollama benchmark generate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="OllamaBenchmarkGenerateRequest"/>.</value>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether stream applies to the Ollama benchmark generate state.
    /// </summary>
    /// <value>The stream value exposed by <see cref="OllamaBenchmarkGenerateRequest"/>.</value>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets the keep alive value that forms part of the Ollama benchmark generate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The keep alive value exposed by <see cref="OllamaBenchmarkGenerateRequest"/>.</value>
    [JsonPropertyName("keep_alive")]
    public string KeepAlive { get; set; } = "5m";

    /// <summary>
    /// Gets or sets the options collection maintained or exposed by this Ollama benchmark generate instance for downstream processing.
    /// </summary>
    /// <value>The options value exposed by <see cref="OllamaBenchmarkGenerateRequest"/>.</value>
    [JsonPropertyName("options")]
    public Dictionary<string, object?> Options { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Represents the outcome of Ollama benchmark generate, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class OllamaBenchmarkGenerateResponse
{
    /// <summary>
    /// Gets or sets the response value that forms part of the Ollama benchmark generate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The response value exposed by <see cref="OllamaBenchmarkGenerateResponse"/>.</value>
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether done applies to the Ollama benchmark generate state.
    /// </summary>
    /// <value>The done value exposed by <see cref="OllamaBenchmarkGenerateResponse"/>.</value>
    [JsonPropertyName("done")]
    public bool Done { get; set; }

    /// <summary>
    /// Gets or sets the total duration nanoseconds value that forms part of the Ollama benchmark generate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The total duration nanoseconds value exposed by <see cref="OllamaBenchmarkGenerateResponse"/>.</value>
    [JsonPropertyName("total_duration")]
    public long TotalDurationNanoseconds { get; set; }

    /// <summary>
    /// Gets or sets the load duration nanoseconds value that forms part of the Ollama benchmark generate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The load duration nanoseconds value exposed by <see cref="OllamaBenchmarkGenerateResponse"/>.</value>
    [JsonPropertyName("load_duration")]
    public long LoadDurationNanoseconds { get; set; }

    /// <summary>
    /// Gets or sets the prompt evaluation count that quantifies the associated Ollama benchmark generate data.
    /// </summary>
    /// <value>The prompt evaluation count value exposed by <see cref="OllamaBenchmarkGenerateResponse"/>.</value>
    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvaluationCount { get; set; }

    /// <summary>
    /// Gets or sets the prompt evaluation duration nanoseconds value that forms part of the Ollama benchmark generate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt evaluation duration nanoseconds value exposed by <see cref="OllamaBenchmarkGenerateResponse"/>.</value>
    [JsonPropertyName("prompt_eval_duration")]
    public long PromptEvaluationDurationNanoseconds { get; set; }

    /// <summary>
    /// Gets or sets the evaluation count that quantifies the associated Ollama benchmark generate data.
    /// </summary>
    /// <value>The evaluation count value exposed by <see cref="OllamaBenchmarkGenerateResponse"/>.</value>
    [JsonPropertyName("eval_count")]
    public int EvaluationCount { get; set; }

    /// <summary>
    /// Gets or sets the evaluation duration nanoseconds value that forms part of the Ollama benchmark generate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The evaluation duration nanoseconds value exposed by <see cref="OllamaBenchmarkGenerateResponse"/>.</value>
    [JsonPropertyName("eval_duration")]
    public long EvaluationDurationNanoseconds { get; set; }
}
