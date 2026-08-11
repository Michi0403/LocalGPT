namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a provider model benchmark request.
/// </summary>
public sealed class ProviderModelBenchmarkRequest
{
    /// <summary>
    /// Gets or sets run identifier.
    /// </summary>
    public Guid RunId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets targets.
    /// </summary>
    public List<ProviderModelReference> Targets { get; set; } = [];
    /// <summary>
    /// Gets or sets council reviewers.
    /// </summary>
    public List<ProviderModelReference> CouncilReviewers { get; set; } = [];
    /// <summary>
    /// Gets or sets max profiles per model.
    /// </summary>
    public int MaxProfilesPerModel { get; set; } = 5;
    /// <summary>
    /// Gets or sets max tasks.
    /// </summary>
    public int MaxTasks { get; set; } = 3;
    /// <summary>
    /// Gets or sets max council reviewers.
    /// </summary>
    public int MaxCouncilReviewers { get; set; } = 3;
    /// <summary>
    /// Gets or sets max seconds per call.
    /// </summary>
    public int MaxSecondsPerCall { get; set; } = 180;
    /// <summary>
    /// Gets or sets improvement threshold percent.
    /// </summary>
    public double ImprovementThresholdPercent { get; set; } = 5d;
    /// <summary>
    /// Gets or sets maximum context tokens.
    /// </summary>
    public int MaximumContextTokens { get; set; } = 32768;
    /// <summary>
    /// Gets or sets maximum output tokens.
    /// </summary>
    public int MaximumOutputTokens { get; set; } = 1024;
    /// <summary>
    /// Gets or sets include council review.
    /// </summary>
    public bool IncludeCouncilReview { get; set; } = true;
}

/// <summary>
/// Represents a provider model benchmark report.
/// </summary>
public sealed class ProviderModelBenchmarkReport
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
    /// Gets or sets targets.
    /// </summary>
    public List<ProviderModelBenchmarkTargetResult> Targets { get; set; } = [];
    /// <summary>
    /// Gets or sets council members.
    /// </summary>
    public List<string> CouncilMembers { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
    /// <summary>
    /// Gets or sets applied preset identifier.
    /// </summary>
    public Guid? AppliedPresetId { get; set; }
    /// <summary>
    /// Gets or sets applied preset name.
    /// </summary>
    public string AppliedPresetName { get; set; } = string.Empty;
}

/// <summary>
/// Represents a provider model benchmark target result.
/// </summary>
public sealed class ProviderModelBenchmarkTargetResult
{
    /// <summary>
    /// Gets or sets model.
    /// </summary>
    public ProviderModelReference Model { get; set; } = new();
    /// <summary>
    /// Gets or sets profiles.
    /// </summary>
    public List<ProviderModelBenchmarkProfileResult> Profiles { get; set; } = [];
    /// <summary>
    /// Gets or sets recommendation.
    /// </summary>
    public ProviderModelBenchmarkRecommendation Recommendation { get; set; } = new();
    /// <summary>
    /// Gets or sets council reviews.
    /// </summary>
    public List<ProviderModelCouncilReview> CouncilReviews { get; set; } = [];
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
/// Represents a provider model benchmark profile result.
/// </summary>
public sealed class ProviderModelBenchmarkProfileResult
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
    public List<ProviderModelBenchmarkTaskResult> Tasks { get; set; } = [];
}

/// <summary>
/// Represents a provider model benchmark task result.
/// </summary>
public sealed class ProviderModelBenchmarkTaskResult
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
    /// Gets or sets response preview.
    /// </summary>
    public string ResponsePreview { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents a provider model council review.
/// </summary>
public sealed class ProviderModelCouncilReview
{
    /// <summary>
    /// Gets or sets reviewer.
    /// </summary>
    public ProviderModelReference Reviewer { get; set; } = new();
    /// <summary>
    /// Gets or sets quality score.
    /// </summary>
    public double QualityScore { get; set; }
    /// <summary>
    /// Gets or sets reliability score.
    /// </summary>
    public double ReliabilityScore { get; set; }
    /// <summary>
    /// Gets or sets recommended context tokens.
    /// </summary>
    public int RecommendedContextTokens { get; set; }
    /// <summary>
    /// Gets or sets recommended output tokens.
    /// </summary>
    public int RecommendedOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets rationale.
    /// </summary>
    public string Rationale { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents a provider model benchmark recommendation.
/// </summary>
public sealed class ProviderModelBenchmarkRecommendation
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
    /// Gets or sets empirical score.
    /// </summary>
    public double EmpiricalScore { get; set; }
    /// <summary>
    /// Gets or sets council score.
    /// </summary>
    public double CouncilScore { get; set; }
    /// <summary>
    /// Gets or sets rationale.
    /// </summary>
    public string Rationale { get; set; } = string.Empty;
}

/// <summary>
/// Represents a provider model benchmark applied event.
/// </summary>
public sealed record ProviderModelBenchmarkAppliedEvent(
    ProviderModelReference Model,
    OneWireCouncilModelRoute Route,
    CouncilModelPreset Preset);

/// <summary>
/// Represents a provider model benchmark batch applied event.
/// </summary>
public sealed record ProviderModelBenchmarkBatchAppliedEvent(
    IReadOnlyList<ProviderModelReference> Models,
    IReadOnlyList<OneWireCouncilModelRoute> Routes,
    CouncilModelPreset Preset);
