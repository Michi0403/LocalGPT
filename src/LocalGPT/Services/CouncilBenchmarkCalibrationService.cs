using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates the deterministic benchmark engine used by the maintained initial-calibration Council workflow.
/// The social Council may design and review the work, but this service owns target coverage so models cannot silently
/// replace an all-member request with a representative sample.
/// </summary>
/// <param name="benchmark">Runs provider-qualified bounded measurements.</param>
/// <param name="performancePresets">Persists measured hardware-spooler profile sets.</param>
/// <param name="logger">Writes bounded calibration diagnostics.</param>
public sealed class CouncilBenchmarkCalibrationService(
    IProviderModelBenchmarkService benchmark,
    IHardwarePerformancePresetService performancePresets,
    ILogger<CouncilBenchmarkCalibrationService> logger) : ICouncilBenchmarkCalibrationService
{
    /// <summary>
    /// Executes the maintained four-point calibration across every distinct benchmark-capable provider-qualified target,
    /// streams bounded progress to the parent Council, stores the four measured tier profiles and returns coverage evidence.
    /// </summary>
    /// <inheritdoc />
    public async Task<CouncilBenchmarkCalibrationResult> RunAsync(
        CouncilBenchmarkCalibrationRequest request,
        Action<string>? progressMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before the deterministic all-member benchmark runs and stores performance profiles.");

            var requestedTargets = request.Targets
                .Where(model => model is not null && !string.IsNullOrWhiteSpace(model.ModelName))
                .GroupBy(model => model.SelectionKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            if (requestedTargets.Count == 0)
                throw new InvalidOperationException("The benchmark calibration has no provider-qualified Council members to test.");

            var benchmarkTargets = requestedTargets
                .Where(model => model.SupportsBenchmark)
                .ToList();
            var skipped = requestedTargets
                .Where(model => !model.SupportsBenchmark)
                .Select(model => $"{model.SelectionKey} — provider/model does not expose the maintained benchmark contract.")
                .ToList();
            if (benchmarkTargets.Count == 0)
                throw new InvalidOperationException("None of the selected provider-qualified Council members supports the maintained benchmark contract.");

            var benchmarkRunId = Guid.NewGuid();
            var maximumContextTokens = Math.Clamp(request.MaximumContextTokens, 8192, 65536);
            var maximumOutputTokens = Math.Clamp(request.MaximumOutputTokens, 1024, 8192);
            var timeoutSeconds = Math.Clamp(request.MaxSecondsPerCall, 30, 900);
            progressMessage?.Invoke(
                $"## Deterministic all-member calibration started\n\n" +
                $"LocalGPT, not a model prompt, will benchmark every one of the {benchmarkTargets.Count} benchmark-capable provider-qualified member(s) selected for this Council. " +
                $"No representative sampling or size-bracket extrapolation is allowed. Four measured profile points are attempted per target.");

            var report = await benchmark.RunAsync(
                new ProviderModelBenchmarkRequest
                {
                    RunId = benchmarkRunId,
                    Targets = benchmarkTargets,
                    CouncilReviewers = [],
                    MaxProfilesPerModel = 4,
                    ProfileMode = ProviderModelBenchmarkProfileMode.EvenlySpaced,
                    MinimumContextTokens = 2048,
                    MinimumOutputTokens = 256,
                    MaximumContextTokens = maximumContextTokens,
                    MaximumOutputTokens = maximumOutputTokens,
                    IncludeCpuSafeControl = false,
                    StopWhenImprovementStalls = false,
                    MaxTasks = 1,
                    MaxCouncilReviewers = 0,
                    MaxSecondsPerCall = timeoutSeconds,
                    ImprovementThresholdPercent = 0d,
                    IncludeCouncilReview = false,
                    OwnLiveSession = false,
                    ProgressMessage = progressMessage
                },
                cancellationToken).ConfigureAwait(false);

            var successfulTargets = report.Targets.Count(target =>
                string.IsNullOrWhiteSpace(target.Error) &&
                !string.IsNullOrWhiteSpace(target.Recommendation.ProfileName));
            var baseName = string.IsNullOrWhiteSpace(request.PresetBaseName)
                ? $"Initial calibration {DateTimeOffset.Now:yyyy-MM-dd HHmmss}"
                : request.PresetBaseName.Trim();
            var presets = await performancePresets.SaveBenchmarkProfileSetAsync(
                report,
                baseName,
                userConfirmed: true,
                cancellationToken).ConfigureAwait(false);

            var result = new CouncilBenchmarkCalibrationResult
            {
                CouncilRunId = request.CouncilRunId,
                BenchmarkRunId = benchmarkRunId,
                RequestedTargetCount = requestedTargets.Count,
                BenchmarkTargetCount = benchmarkTargets.Count,
                SuccessfulTargetCount = successfulTargets,
                SkippedTargets = skipped,
                Report = report,
                Presets = presets.ToList()
            };
            result.SummaryMarkdown = BuildSummary(result);
            progressMessage?.Invoke(result.SummaryMarkdown);
            logger.LogInformation(
                "Council calibration for parent run {CouncilRunId} benchmarked {BenchmarkTargetCount}/{RequestedTargetCount} distinct provider-qualified member(s); {SuccessfulTargetCount} produced measured recommendations and {PresetCount} profile(s) were stored.",
                request.CouncilRunId,
                benchmarkTargets.Count,
                requestedTargets.Count,
                successfulTargets,
                presets.Count);
            return result;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Council benchmark calibration was cancelled for parent run {CouncilRunId}.", request?.CouncilRunId);
            else
                logger.LogError(exception, "Council benchmark calibration failed for parent run {CouncilRunId}.", request?.CouncilRunId);
            throw;
        }
    }

    /// <summary>Builds the bounded coverage and measured-profile summary persisted into the parent Council transcript.</summary>
    /// <param name="result">Completed deterministic calibration.</param>
    /// <returns>Markdown evidence for later Council rounds and the final chat transcript.</returns>
    private string BuildSummary(CouncilBenchmarkCalibrationResult result)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("## Deterministic benchmark calibration evidence");
            builder.AppendLine();
            builder.AppendLine($"- Parent Council: `{result.CouncilRunId:N}`");
            builder.AppendLine($"- Provider benchmark: `{result.BenchmarkRunId:N}`");
            builder.AppendLine($"- Requested distinct provider-qualified members: **{result.RequestedTargetCount}**");
            builder.AppendLine($"- Benchmark-capable members attempted: **{result.BenchmarkTargetCount}**");
            builder.AppendLine($"- Members with at least one successful measured recommendation: **{result.SuccessfulTargetCount}**");
            builder.AppendLine($"- Stored measured profiles: **{result.Presets.Count}**");
            builder.AppendLine();
            builder.AppendLine("### Stored initial profiles");
            foreach (var preset in result.Presets)
                builder.AppendLine($"- `{preset.Name}`");
            if (result.SkippedTargets.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("### Selected members not accepted by the benchmark engine");
                foreach (var item in result.SkippedTargets)
                    builder.AppendLine($"- {item}");
            }

            builder.AppendLine();
            builder.AppendLine("### Per-member measured coverage");
            builder.AppendLine("| Provider-qualified member | Successful points | Attempted points | Recommendation | Status |");
            builder.AppendLine("|---|---:|---:|---|---|");
            foreach (var target in result.Report.Targets)
            {
                var successfulProfiles = target.Profiles.Count(profile => profile.Tasks.Any(task => task.Succeeded));
                var recommendation = string.IsNullOrWhiteSpace(target.Recommendation.ProfileName)
                    ? "—"
                    : $"{target.Recommendation.ContextTokens:N0} ctx / {target.Recommendation.OutputTokens:N0} out";
                var status = string.IsNullOrWhiteSpace(target.Error) ? "measured" : target.Error.Replace("|", "/");
                builder.AppendLine($"| `{target.Model.SelectionKey.Replace("|", "\\|")}` | {successfulProfiles} | {target.Profiles.Count} | {recommendation} | {status} |");
            }

            builder.AppendLine();
            builder.AppendLine(result.SuccessfulTargetCount == result.BenchmarkTargetCount
                ? "**Coverage gate: PASS.** Every benchmark-capable selected member produced measured evidence. Social review may now compare and synthesize the four initial profiles."
                : "**Coverage gate: PARTIAL.** Every benchmark-capable selected member was attempted, but one or more did not produce a successful measurement. Later roles must preserve those failures as inconclusive evidence and must not pretend unmeasured members were benchmarked.");
            return builder.ToString().Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building Council benchmark calibration summary failed.");
            throw;
        }
    }
}
