using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Carries one deterministic installed-model calibration request from a configured Council workflow into the benchmark service.
/// </summary>
public sealed class CouncilBenchmarkCalibrationRequest
{
    /// <summary>Gets or sets the parent Council run identifier used for diagnostics and transcript correlation.</summary>
    /// <value>The parent Council run identifier.</value>
    public Guid CouncilRunId { get; set; }

    /// <summary>Gets or sets the exact provider-qualified Council members selected for calibration.</summary>
    /// <value>The selected provider-qualified model identities.</value>
    public List<ProviderModelReference> Targets { get; set; } = [];

    /// <summary>Gets or sets the largest context window the initial four-point calibration may attempt.</summary>
    /// <value>The upper context-token bound supplied by the initiating Council configuration.</value>
    public int MaximumContextTokens { get; set; } = 32768;

    /// <summary>Gets or sets the largest output budget the initial four-point calibration may attempt.</summary>
    /// <value>The upper output-token bound supplied by the initiating Council configuration.</value>
    public int MaximumOutputTokens { get; set; } = 2048;

    /// <summary>Gets or sets the maximum duration allowed for one bounded provider call.</summary>
    /// <value>The per-call timeout in seconds.</value>
    public int MaxSecondsPerCall { get; set; } = 180;

    /// <summary>Gets or sets the base name used when the measured Low, Middle, High and Expert performance profiles are stored.</summary>
    /// <value>The user-visible performance-profile base name.</value>
    public string PresetBaseName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the workflow's human checkpoint approved running and persisting the benchmark.</summary>
    /// <value><see langword="true"/> only after the configured Council human boundary has completed.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Describes the outcome of one deterministic installed-model Council calibration, including measured coverage and the four stored performance profiles.
/// </summary>
public sealed class CouncilBenchmarkCalibrationResult
{
    /// <summary>Correlates the deterministic calibration outcome with the parent social Council session that requested the measurements.</summary>
    /// <value>The parent Council run identifier retained for transcript and diagnostic correlation.</value>
    public Guid CouncilRunId { get; set; }

    /// <summary>Identifies the dedicated provider-benchmark measurement run whose evidence produced the stored calibration tiers.</summary>
    /// <value>The child benchmark run identifier used by measured profile persistence.</value>
    public Guid BenchmarkRunId { get; set; }

    /// <summary>Gets or sets the number of distinct provider-qualified Council members requested for calibration.</summary>
    /// <value>The requested member count.</value>
    public int RequestedTargetCount { get; set; }

    /// <summary>Gets or sets the number of requested members that support the provider benchmark contract.</summary>
    /// <value>The benchmark-capable target count.</value>
    public int BenchmarkTargetCount { get; set; }

    /// <summary>Gets or sets the number of benchmark targets that produced at least one successful measured recommendation.</summary>
    /// <value>The successful measured target count.</value>
    public int SuccessfulTargetCount { get; set; }

    /// <summary>Gets or sets selected Council members that could not enter the benchmark engine, with a bounded reason for each.</summary>
    /// <value>The skipped-target evidence collection.</value>
    public List<string> SkippedTargets { get; set; } = [];

    /// <summary>Gets or sets the complete provider-qualified benchmark report used by later Council review rounds.</summary>
    /// <value>The measured provider benchmark report.</value>
    [JsonIgnore]
    public ProviderModelBenchmarkReport Report { get; set; } = new();

    /// <summary>Gets or sets the measured Low, Middle, High and Expert hardware-spooler profiles stored for this calibration.</summary>
    /// <value>The stored performance profiles.</value>
    public List<HardwarePerformancePreset> Presets { get; set; } = [];

    /// <summary>Gets or sets the bounded Markdown evidence inserted into the parent Council transcript.</summary>
    /// <value>The visible calibration summary.</value>
    public string SummaryMarkdown { get; set; } = string.Empty;
}
