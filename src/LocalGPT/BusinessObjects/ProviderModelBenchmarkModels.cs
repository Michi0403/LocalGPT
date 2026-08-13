namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for provider model benchmark, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class ProviderModelBenchmarkRequest
{
    /// <summary>
    /// Gets or sets the stable run identifier used to identify or correlate this provider model benchmark instance with related application state.
    /// </summary>
    /// <value>The run identifier value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public Guid RunId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the targets collection maintained or exposed by this provider model benchmark instance for downstream processing.
    /// </summary>
    /// <value>The targets value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public List<ProviderModelReference> Targets { get; set; } = [];
    /// <summary>
    /// Gets or sets the council reviewers collection maintained or exposed by this provider model benchmark instance for downstream processing.
    /// </summary>
    /// <value>The council reviewers value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public List<ProviderModelReference> CouncilReviewers { get; set; } = [];
    /// <summary>
    /// Gets or sets the max profiles per model value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max profiles per model value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public int MaxProfilesPerModel { get; set; } = 5;
    /// <summary>
    /// Gets or sets the max tasks value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max tasks value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public int MaxTasks { get; set; } = 3;
    /// <summary>
    /// Gets or sets the max council reviewers value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max council reviewers value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public int MaxCouncilReviewers { get; set; } = 3;
    /// <summary>
    /// Gets or sets the max seconds per call value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max seconds per call value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public int MaxSecondsPerCall { get; set; } = 180;
    /// <summary>
    /// Gets or sets the improvement threshold percent value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The improvement threshold percent value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public double ImprovementThresholdPercent { get; set; } = 5d;
    /// <summary>
    /// Gets or sets the maximum context tokens value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum context tokens value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public int MaximumContextTokens { get; set; } = 32768;
    /// <summary>
    /// Gets or sets the maximum output tokens value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum output tokens value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public int MaximumOutputTokens { get; set; } = 1024;
    /// <summary>
    /// Gets or sets a value indicating whether council review applies to the provider model benchmark state.
    /// </summary>
    /// <value>The include council review value exposed by <see cref="ProviderModelBenchmarkRequest"/>.</value>
    public bool IncludeCouncilReview { get; set; } = true;
}

/// <summary>
/// Represents a provider model benchmark report application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProviderModelBenchmarkReport
{
    /// <summary>
    /// Gets or sets the stable run identifier used to identify or correlate this provider model benchmark report instance with related application state.
    /// </summary>
    /// <value>The run identifier value exposed by <see cref="ProviderModelBenchmarkReport"/>.</value>
    public Guid RunId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the started at UTC associated with this provider model benchmark report state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="ProviderModelBenchmarkReport"/>.</value>
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the completed at UTC associated with this provider model benchmark report state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="ProviderModelBenchmarkReport"/>.</value>
    public DateTimeOffset CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the targets collection maintained or exposed by this provider model benchmark report instance for downstream processing.
    /// </summary>
    /// <value>The targets value exposed by <see cref="ProviderModelBenchmarkReport"/>.</value>
    public List<ProviderModelBenchmarkTargetResult> Targets { get; set; } = [];
    /// <summary>
    /// Gets or sets the council members collection maintained or exposed by this provider model benchmark report instance for downstream processing.
    /// </summary>
    /// <value>The council members value exposed by <see cref="ProviderModelBenchmarkReport"/>.</value>
    public List<string> CouncilMembers { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this provider model benchmark report instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="ProviderModelBenchmarkReport"/>.</value>
    public List<string> Warnings { get; set; } = [];
    /// <summary>
    /// Gets or sets the stable applied preset identifier used to identify or correlate this provider model benchmark report instance with related application state.
    /// </summary>
    /// <value>The applied preset identifier value exposed by <see cref="ProviderModelBenchmarkReport"/>.</value>
    public Guid? AppliedPresetId { get; set; }
    /// <summary>
    /// Gets or sets the applied preset name value that forms part of the provider model benchmark report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The applied preset name value exposed by <see cref="ProviderModelBenchmarkReport"/>.</value>
    public string AppliedPresetName { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of provider model benchmark target, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class ProviderModelBenchmarkTargetResult
{
    /// <summary>
    /// Gets or sets the model value that forms part of the provider model benchmark target state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model value exposed by <see cref="ProviderModelBenchmarkTargetResult"/>.</value>
    public ProviderModelReference Model { get; set; } = new();
    /// <summary>
    /// Gets or sets the profiles collection maintained or exposed by this provider model benchmark target instance for downstream processing.
    /// </summary>
    /// <value>The profiles value exposed by <see cref="ProviderModelBenchmarkTargetResult"/>.</value>
    public List<ProviderModelBenchmarkProfileResult> Profiles { get; set; } = [];
    /// <summary>
    /// Gets or sets the recommendation value that forms part of the provider model benchmark target state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recommendation value exposed by <see cref="ProviderModelBenchmarkTargetResult"/>.</value>
    public ProviderModelBenchmarkRecommendation Recommendation { get; set; } = new();
    /// <summary>
    /// Gets or sets the council reviews collection maintained or exposed by this provider model benchmark target instance for downstream processing.
    /// </summary>
    /// <value>The council reviews value exposed by <see cref="ProviderModelBenchmarkTargetResult"/>.</value>
    public List<ProviderModelCouncilReview> CouncilReviews { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether stopped because improvement was below threshold applies to the provider model benchmark target state.
    /// </summary>
    /// <value>The stopped because improvement was below threshold value exposed by <see cref="ProviderModelBenchmarkTargetResult"/>.</value>
    public bool StoppedBecauseImprovementWasBelowThreshold { get; set; }
    /// <summary>
    /// Gets or sets the error value that forms part of the provider model benchmark target state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="ProviderModelBenchmarkTargetResult"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of provider model benchmark profile, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class ProviderModelBenchmarkProfileResult
{
    /// <summary>
    /// Gets or sets the profile name value that forms part of the provider model benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The profile name value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public string ProfileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the context tokens value that forms part of the provider model benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The context tokens value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public int ContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the output tokens value that forms part of the provider model benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output tokens value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public int OutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the Ollama num GPU value that forms part of the provider model benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama num GPU value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public int? OllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets the score value that forms part of the provider model benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The score value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public double Score { get; set; }
    /// <summary>
    /// Gets or sets the average tokens per second value that forms part of the provider model benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The average tokens per second value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public double AverageTokensPerSecond { get; set; }
    /// <summary>
    /// Gets or sets the average quality score value that forms part of the provider model benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The average quality score value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public double AverageQualityScore { get; set; }
    /// <summary>
    /// Gets or sets the average total milliseconds value that forms part of the provider model benchmark profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The average total milliseconds value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public double AverageTotalMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the tasks collection maintained or exposed by this provider model benchmark profile instance for downstream processing.
    /// </summary>
    /// <value>The tasks value exposed by <see cref="ProviderModelBenchmarkProfileResult"/>.</value>
    public List<ProviderModelBenchmarkTaskResult> Tasks { get; set; } = [];
}

/// <summary>
/// Represents the outcome of provider model benchmark task, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class ProviderModelBenchmarkTaskResult
{
    /// <summary>
    /// Gets or sets the task name value that forms part of the provider model benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The task name value exposed by <see cref="ProviderModelBenchmarkTaskResult"/>.</value>
    public string TaskName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the provider model benchmark task state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="ProviderModelBenchmarkTaskResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the quality score value that forms part of the provider model benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quality score value exposed by <see cref="ProviderModelBenchmarkTaskResult"/>.</value>
    public double QualityScore { get; set; }
    /// <summary>
    /// Gets or sets the tokens per second value that forms part of the provider model benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tokens per second value exposed by <see cref="ProviderModelBenchmarkTaskResult"/>.</value>
    public double TokensPerSecond { get; set; }
    /// <summary>
    /// Gets or sets the total milliseconds value that forms part of the provider model benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The total milliseconds value exposed by <see cref="ProviderModelBenchmarkTaskResult"/>.</value>
    public long TotalMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the response preview value that forms part of the provider model benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The response preview value exposed by <see cref="ProviderModelBenchmarkTaskResult"/>.</value>
    public string ResponsePreview { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the provider model benchmark task state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="ProviderModelBenchmarkTaskResult"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents a provider model council review application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProviderModelCouncilReview
{
    /// <summary>
    /// Gets or sets the reviewer value that forms part of the provider model council review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reviewer value exposed by <see cref="ProviderModelCouncilReview"/>.</value>
    public ProviderModelReference Reviewer { get; set; } = new();
    /// <summary>
    /// Gets or sets the quality score value that forms part of the provider model council review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quality score value exposed by <see cref="ProviderModelCouncilReview"/>.</value>
    public double QualityScore { get; set; }
    /// <summary>
    /// Gets or sets the reliability score value that forms part of the provider model council review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reliability score value exposed by <see cref="ProviderModelCouncilReview"/>.</value>
    public double ReliabilityScore { get; set; }
    /// <summary>
    /// Gets or sets the recommended context tokens value that forms part of the provider model council review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recommended context tokens value exposed by <see cref="ProviderModelCouncilReview"/>.</value>
    public int RecommendedContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the recommended output tokens value that forms part of the provider model council review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recommended output tokens value exposed by <see cref="ProviderModelCouncilReview"/>.</value>
    public int RecommendedOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the rationale value that forms part of the provider model council review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rationale value exposed by <see cref="ProviderModelCouncilReview"/>.</value>
    public string Rationale { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the provider model council review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="ProviderModelCouncilReview"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents a provider model benchmark recommendation application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProviderModelBenchmarkRecommendation
{
    /// <summary>
    /// Gets or sets the profile name value that forms part of the provider model benchmark recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The profile name value exposed by <see cref="ProviderModelBenchmarkRecommendation"/>.</value>
    public string ProfileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the context tokens value that forms part of the provider model benchmark recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The context tokens value exposed by <see cref="ProviderModelBenchmarkRecommendation"/>.</value>
    public int ContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the output tokens value that forms part of the provider model benchmark recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output tokens value exposed by <see cref="ProviderModelBenchmarkRecommendation"/>.</value>
    public int OutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the Ollama num GPU value that forms part of the provider model benchmark recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama num GPU value exposed by <see cref="ProviderModelBenchmarkRecommendation"/>.</value>
    public int? OllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets the empirical score value that forms part of the provider model benchmark recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The empirical score value exposed by <see cref="ProviderModelBenchmarkRecommendation"/>.</value>
    public double EmpiricalScore { get; set; }
    /// <summary>
    /// Gets or sets the council score value that forms part of the provider model benchmark recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council score value exposed by <see cref="ProviderModelBenchmarkRecommendation"/>.</value>
    public double CouncilScore { get; set; }
    /// <summary>
    /// Gets or sets the rationale value that forms part of the provider model benchmark recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rationale value exposed by <see cref="ProviderModelBenchmarkRecommendation"/>.</value>
    public string Rationale { get; set; } = string.Empty;
}

/// <summary>
/// Represents a provider model benchmark applied event application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Model">Model value supplied to the provider model benchmark applied event operation and used when producing its result.</param>
/// <param name="Route">Route value supplied to the provider model benchmark applied event operation and used when producing its result.</param>
/// <param name="Preset">Preset value supplied to the provider model benchmark applied event operation and used when producing its result.</param>
public sealed record ProviderModelBenchmarkAppliedEvent(
    ProviderModelReference Model,
    OneWireCouncilModelRoute Route,
    CouncilModelPreset Preset);

/// <summary>
/// Represents a provider model benchmark batch applied event application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Models">Provider model reference dependency used by the provider model benchmark batch applied event workflow to provide the corresponding application capability.</param>
/// <param name="Routes">One wire council model route dependency used by the provider model benchmark batch applied event workflow to provide the corresponding application capability.</param>
/// <param name="Preset">Preset value supplied to the provider model benchmark batch applied event operation and used when producing its result.</param>
public sealed record ProviderModelBenchmarkBatchAppliedEvent(
    IReadOnlyList<ProviderModelReference> Models,
    IReadOnlyList<OneWireCouncilModelRoute> Routes,
    CouncilModelPreset Preset);
