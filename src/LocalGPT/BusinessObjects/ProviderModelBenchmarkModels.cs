namespace LocalGPT.BusinessObjects;

public sealed class ProviderModelBenchmarkRequest
{
    public Guid RunId { get; set; } = Guid.NewGuid();
    public List<ProviderModelReference> Targets { get; set; } = [];
    public List<ProviderModelReference> CouncilReviewers { get; set; } = [];
    public int MaxProfilesPerModel { get; set; } = 5;
    public int MaxTasks { get; set; } = 3;
    public int MaxCouncilReviewers { get; set; } = 3;
    public int MaxSecondsPerCall { get; set; } = 180;
    public double ImprovementThresholdPercent { get; set; } = 5d;
    public int MaximumContextTokens { get; set; } = 32768;
    public int MaximumOutputTokens { get; set; } = 1024;
    public bool IncludeCouncilReview { get; set; } = true;
}

public sealed class ProviderModelBenchmarkReport
{
    public Guid RunId { get; set; } = Guid.NewGuid();
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedAtUtc { get; set; }
    public List<ProviderModelBenchmarkTargetResult> Targets { get; set; } = [];
    public List<string> CouncilMembers { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public Guid? AppliedPresetId { get; set; }
    public string AppliedPresetName { get; set; } = string.Empty;
}

public sealed class ProviderModelBenchmarkTargetResult
{
    public ProviderModelReference Model { get; set; } = new();
    public List<ProviderModelBenchmarkProfileResult> Profiles { get; set; } = [];
    public ProviderModelBenchmarkRecommendation Recommendation { get; set; } = new();
    public List<ProviderModelCouncilReview> CouncilReviews { get; set; } = [];
    public bool StoppedBecauseImprovementWasBelowThreshold { get; set; }
    public string Error { get; set; } = string.Empty;
}

public sealed class ProviderModelBenchmarkProfileResult
{
    public string ProfileName { get; set; } = string.Empty;
    public int ContextTokens { get; set; }
    public int OutputTokens { get; set; }
    public int? OllamaNumGpu { get; set; }
    public double Score { get; set; }
    public double AverageTokensPerSecond { get; set; }
    public double AverageQualityScore { get; set; }
    public double AverageTotalMilliseconds { get; set; }
    public List<ProviderModelBenchmarkTaskResult> Tasks { get; set; } = [];
}

public sealed class ProviderModelBenchmarkTaskResult
{
    public string TaskName { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public double QualityScore { get; set; }
    public double TokensPerSecond { get; set; }
    public long TotalMilliseconds { get; set; }
    public string ResponsePreview { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public sealed class ProviderModelCouncilReview
{
    public ProviderModelReference Reviewer { get; set; } = new();
    public double QualityScore { get; set; }
    public double ReliabilityScore { get; set; }
    public int RecommendedContextTokens { get; set; }
    public int RecommendedOutputTokens { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public sealed class ProviderModelBenchmarkRecommendation
{
    public string ProfileName { get; set; } = string.Empty;
    public int ContextTokens { get; set; }
    public int OutputTokens { get; set; }
    public int? OllamaNumGpu { get; set; }
    public double EmpiricalScore { get; set; }
    public double CouncilScore { get; set; }
    public string Rationale { get; set; } = string.Empty;
}

public sealed record ProviderModelBenchmarkAppliedEvent(
    ProviderModelReference Model,
    OneWireCouncilModelRoute Route,
    CouncilModelPreset Preset);

public sealed record ProviderModelBenchmarkBatchAppliedEvent(
    IReadOnlyList<ProviderModelReference> Models,
    IReadOnlyList<OneWireCouncilModelRoute> Routes,
    CouncilModelPreset Preset);
