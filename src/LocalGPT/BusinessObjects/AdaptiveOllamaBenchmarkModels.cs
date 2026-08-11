using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an adaptive ollama benchmark options.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkOptions
{
    /// <summary>
    /// Gets or sets endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets model names.
    /// </summary>
    public List<string> ModelNames { get; set; } = [];
    /// <summary>
    /// Gets or sets max models.
    /// </summary>
    public int MaxModels { get; set; } = 3;
    /// <summary>
    /// Gets or sets max profiles per model.
    /// </summary>
    public int MaxProfilesPerModel { get; set; } = 4;
    /// <summary>
    /// Gets or sets max tasks.
    /// </summary>
    public int MaxTasks { get; set; } = 3;
    /// <summary>
    /// Gets or sets max seconds per call.
    /// </summary>
    public int MaxSecondsPerCall { get; set; } = 120;
    /// <summary>
    /// Gets or sets improvement threshold percent.
    /// </summary>
    public double ImprovementThresholdPercent { get; set; } = 5d;
    /// <summary>
    /// Gets or sets include peer authored task.
    /// </summary>
    public bool IncludePeerAuthoredTask { get; set; } = true;
    /// <summary>
    /// Gets or sets persist preset.
    /// </summary>
    public bool PersistPreset { get; set; } = true;
    /// <summary>
    /// Gets or sets preset name.
    /// </summary>
    public string PresetName { get; set; } = "Adaptive Ollama Benchmark";
    /// <summary>
    /// Gets or sets make default.
    /// </summary>
    public bool MakeDefault { get; set; }
    /// <summary>
    /// Gets or sets maximum context tokens.
    /// </summary>
    public int MaximumContextTokens { get; set; } = 16384;
    /// <summary>
    /// Gets or sets maximum output tokens.
    /// </summary>
    public int MaximumOutputTokens { get; set; } = 512;
}

/// <summary>
/// Represents an adaptive ollama benchmark report.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkReport
{
    /// <summary>
    /// Gets or sets run identifier.
    /// </summary>
    public Guid RunId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets started at UTC.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets completed at UTC.
    /// </summary>
    public DateTimeOffset CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets hardware summary.
    /// </summary>
    public string HardwareSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets peer authored task.
    /// </summary>
    public string PeerAuthoredTask { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets models.
    /// </summary>
    public List<AdaptiveOllamaBenchmarkModelResult> Models { get; set; } = [];
    /// <summary>
    /// Gets or sets best model.
    /// </summary>
    public string BestModel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets best profile.
    /// </summary>
    public string BestProfile { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets best score.
    /// </summary>
    public double BestScore { get; set; }
    /// <summary>
    /// Gets or sets saved preset identifier.
    /// </summary>
    public Guid? SavedPresetId { get; set; }
    /// <summary>
    /// Gets or sets saved preset name.
    /// </summary>
    public string SavedPresetName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Represents an adaptive ollama benchmark model result.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkModelResult
{
    /// <summary>
    /// Gets or sets model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets installed size bytes.
    /// </summary>
    public long? InstalledSizeBytes { get; set; }
    /// <summary>
    /// Gets or sets parameter size.
    /// </summary>
    public string ParameterSize { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets profiles.
    /// </summary>
    public List<AdaptiveOllamaBenchmarkProfileResult> Profiles { get; set; } = [];
    /// <summary>
    /// Gets or sets best profile.
    /// </summary>
    public string BestProfile { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets best score.
    /// </summary>
    public double BestScore { get; set; }
    /// <summary>
    /// Gets or sets stopped because improvement was below threshold.
    /// </summary>
    public bool StoppedBecauseImprovementWasBelowThreshold { get; set; }
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents an adaptive ollama benchmark profile result.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkProfileResult
{
    /// <summary>
    /// Gets or sets profile name.
    /// </summary>
    public string ProfileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets context tokens.
    /// </summary>
    public int ContextTokens { get; set; }
    /// <summary>
    /// Gets or sets output tokens.
    /// </summary>
    public int OutputTokens { get; set; }
    /// <summary>
    /// Gets or sets ollama num gpu.
    /// </summary>
    public int? OllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets num batch.
    /// </summary>
    public int NumBatch { get; set; }
    /// <summary>
    /// Gets or sets score.
    /// </summary>
    public double Score { get; set; }
    /// <summary>
    /// Gets or sets average tokens per second.
    /// </summary>
    public double AverageTokensPerSecond { get; set; }
    /// <summary>
    /// Gets or sets average quality score.
    /// </summary>
    public double AverageQualityScore { get; set; }
    /// <summary>
    /// Gets or sets average total milliseconds.
    /// </summary>
    public double AverageTotalMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets tasks.
    /// </summary>
    public List<AdaptiveOllamaBenchmarkTaskResult> Tasks { get; set; } = [];
}

/// <summary>
/// Represents an adaptive ollama benchmark task result.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkTaskResult
{
    /// <summary>
    /// Gets or sets task name.
    /// </summary>
    public string TaskName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets succeeded.
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets quality score.
    /// </summary>
    public double QualityScore { get; set; }
    /// <summary>
    /// Gets or sets tokens per second.
    /// </summary>
    public double TokensPerSecond { get; set; }
    /// <summary>
    /// Gets or sets total milliseconds.
    /// </summary>
    public long TotalMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets evaluated tokens.
    /// </summary>
    public int EvaluatedTokens { get; set; }
    /// <summary>
    /// Gets or sets response preview.
    /// </summary>
    public string ResponsePreview { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents an adaptive ollama benchmark task.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkTask
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets prompt.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets expected tokens.
    /// </summary>
    public List<string> ExpectedTokens { get; set; } = [];
    /// <summary>
    /// Gets or sets expect JSON.
    /// </summary>
    public bool ExpectJson { get; set; }
}

/// <summary>
/// Represents an adaptive ollama tuning profile.
/// </summary>
public sealed class AdaptiveOllamaTuningProfile
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets context tokens.
    /// </summary>
    public int ContextTokens { get; set; }
    /// <summary>
    /// Gets or sets output tokens.
    /// </summary>
    public int OutputTokens { get; set; }
    /// <summary>
    /// Gets or sets num batch.
    /// </summary>
    public int NumBatch { get; set; }
    /// <summary>
    /// Gets or sets ollama num gpu.
    /// </summary>
    public int? OllamaNumGpu { get; set; }
}

/// <summary>
/// Represents an ollama benchmark tags response.
/// </summary>
public sealed class OllamaBenchmarkTagsResponse
{
    /// <summary>
    /// Gets or sets models.
    /// </summary>
    [JsonPropertyName("models")]
    public List<OllamaBenchmarkModelInfo> Models { get; set; } = [];
}

/// <summary>
/// Represents an ollama benchmark model info.
/// </summary>
public sealed class OllamaBenchmarkModelInfo
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets model.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets size.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets details.
    /// </summary>
    [JsonPropertyName("details")]
    public OllamaBenchmarkModelDetails Details { get; set; } = new();
}

/// <summary>
/// Represents an ollama benchmark model details.
/// </summary>
public sealed class OllamaBenchmarkModelDetails
{
    /// <summary>
    /// Gets or sets parameter size.
    /// </summary>
    [JsonPropertyName("parameter_size")]
    public string ParameterSize { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets quantization level.
    /// </summary>
    [JsonPropertyName("quantization_level")]
    public string QuantizationLevel { get; set; } = string.Empty;
}

/// <summary>
/// Represents an ollama benchmark generate request.
/// </summary>
public sealed class OllamaBenchmarkGenerateRequest
{
    /// <summary>
    /// Gets or sets model.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets prompt.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets stream.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets keep alive.
    /// </summary>
    [JsonPropertyName("keep_alive")]
    public string KeepAlive { get; set; } = "5m";

    /// <summary>
    /// Gets or sets options.
    /// </summary>
    [JsonPropertyName("options")]
    public Dictionary<string, object?> Options { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Represents an ollama benchmark generate response.
/// </summary>
public sealed class OllamaBenchmarkGenerateResponse
{
    /// <summary>
    /// Gets or sets response.
    /// </summary>
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets done.
    /// </summary>
    [JsonPropertyName("done")]
    public bool Done { get; set; }

    /// <summary>
    /// Gets or sets total duration nanoseconds.
    /// </summary>
    [JsonPropertyName("total_duration")]
    public long TotalDurationNanoseconds { get; set; }

    /// <summary>
    /// Gets or sets load duration nanoseconds.
    /// </summary>
    [JsonPropertyName("load_duration")]
    public long LoadDurationNanoseconds { get; set; }

    /// <summary>
    /// Gets or sets prompt evaluation count.
    /// </summary>
    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvaluationCount { get; set; }

    /// <summary>
    /// Gets or sets prompt evaluation duration nanoseconds.
    /// </summary>
    [JsonPropertyName("prompt_eval_duration")]
    public long PromptEvaluationDurationNanoseconds { get; set; }

    /// <summary>
    /// Gets or sets evaluation count.
    /// </summary>
    [JsonPropertyName("eval_count")]
    public int EvaluationCount { get; set; }

    /// <summary>
    /// Gets or sets evaluation duration nanoseconds.
    /// </summary>
    [JsonPropertyName("eval_duration")]
    public long EvaluationDurationNanoseconds { get; set; }
}
