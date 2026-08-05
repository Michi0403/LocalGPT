using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

public sealed class AdaptiveOllamaBenchmarkOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public List<string> ModelNames { get; set; } = [];
    public int MaxModels { get; set; } = 3;
    public int MaxProfilesPerModel { get; set; } = 4;
    public int MaxTasks { get; set; } = 3;
    public int MaxSecondsPerCall { get; set; } = 120;
    public double ImprovementThresholdPercent { get; set; } = 5d;
    public bool IncludePeerAuthoredTask { get; set; } = true;
    public bool PersistPreset { get; set; } = true;
    public string PresetName { get; set; } = "Adaptive Ollama Benchmark";
    public bool MakeDefault { get; set; }
    public int MaximumContextTokens { get; set; } = 16384;
    public int MaximumOutputTokens { get; set; } = 512;
}

public sealed class AdaptiveOllamaBenchmarkReport
{
    public Guid RunId { get; set; } = Guid.NewGuid();
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedAtUtc { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HardwareSummary { get; set; } = string.Empty;
    public string PeerAuthoredTask { get; set; } = string.Empty;
    public List<AdaptiveOllamaBenchmarkModelResult> Models { get; set; } = [];
    public string BestModel { get; set; } = string.Empty;
    public string BestProfile { get; set; } = string.Empty;
    public double BestScore { get; set; }
    public Guid? SavedPresetId { get; set; }
    public string SavedPresetName { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
}

public sealed class AdaptiveOllamaBenchmarkModelResult
{
    public string ModelName { get; set; } = string.Empty;
    public long? InstalledSizeBytes { get; set; }
    public string ParameterSize { get; set; } = string.Empty;
    public List<AdaptiveOllamaBenchmarkProfileResult> Profiles { get; set; } = [];
    public string BestProfile { get; set; } = string.Empty;
    public double BestScore { get; set; }
    public bool StoppedBecauseImprovementWasBelowThreshold { get; set; }
    public string Error { get; set; } = string.Empty;
}

public sealed class AdaptiveOllamaBenchmarkProfileResult
{
    public string ProfileName { get; set; } = string.Empty;
    public int ContextTokens { get; set; }
    public int OutputTokens { get; set; }
    public int? OllamaNumGpu { get; set; }
    public int NumBatch { get; set; }
    public double Score { get; set; }
    public double AverageTokensPerSecond { get; set; }
    public double AverageQualityScore { get; set; }
    public double AverageTotalMilliseconds { get; set; }
    public List<AdaptiveOllamaBenchmarkTaskResult> Tasks { get; set; } = [];
}

public sealed class AdaptiveOllamaBenchmarkTaskResult
{
    public string TaskName { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public double QualityScore { get; set; }
    public double TokensPerSecond { get; set; }
    public long TotalMilliseconds { get; set; }
    public int EvaluatedTokens { get; set; }
    public string ResponsePreview { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public sealed class AdaptiveOllamaBenchmarkTask
{
    public string Name { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public List<string> ExpectedTokens { get; set; } = [];
    public bool ExpectJson { get; set; }
}

public sealed class AdaptiveOllamaTuningProfile
{
    public string Name { get; set; } = string.Empty;
    public int ContextTokens { get; set; }
    public int OutputTokens { get; set; }
    public int NumBatch { get; set; }
    public int? OllamaNumGpu { get; set; }
}

public sealed class OllamaBenchmarkTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaBenchmarkModelInfo> Models { get; set; } = [];
}

public sealed class OllamaBenchmarkModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("details")]
    public OllamaBenchmarkModelDetails Details { get; set; } = new();
}

public sealed class OllamaBenchmarkModelDetails
{
    [JsonPropertyName("parameter_size")]
    public string ParameterSize { get; set; } = string.Empty;

    [JsonPropertyName("quantization_level")]
    public string QuantizationLevel { get; set; } = string.Empty;
}

public sealed class OllamaBenchmarkGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("keep_alive")]
    public string KeepAlive { get; set; } = "5m";

    [JsonPropertyName("options")]
    public Dictionary<string, object?> Options { get; set; } = new(StringComparer.Ordinal);
}

public sealed class OllamaBenchmarkGenerateResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("total_duration")]
    public long TotalDurationNanoseconds { get; set; }

    [JsonPropertyName("load_duration")]
    public long LoadDurationNanoseconds { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvaluationCount { get; set; }

    [JsonPropertyName("prompt_eval_duration")]
    public long PromptEvaluationDurationNanoseconds { get; set; }

    [JsonPropertyName("eval_count")]
    public int EvaluationCount { get; set; }

    [JsonPropertyName("eval_duration")]
    public long EvaluationDurationNanoseconds { get; set; }
}
