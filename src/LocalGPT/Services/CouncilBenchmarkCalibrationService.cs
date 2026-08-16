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
    /// Executes the maintained four-point/four-task calibration across every distinct benchmark-capable provider-qualified target,
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
            var maximumContextTokens = Math.Clamp(request.MaximumContextTokens, 4096, 32768);
            var maximumOutputTokens = Math.Clamp(request.MaximumOutputTokens, 512, 1536);
            var timeoutSeconds = Math.Clamp(request.MaxSecondsPerCall, 30, 180);
            var taskPack = BuildCuratedTaskPack(request.TaskPackText);
            var hostQueues = benchmarkTargets
                .GroupBy(target => GetBenchmarkHostKey(target.Endpoint), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.ToList())
                .ToList();
            progressMessage?.Invoke(
                $"## Deterministic all-member calibration started\n\n" +
                $"LocalGPT will execute the consolidated Task Curator assignment for every one of the {benchmarkTargets.Count} benchmark-capable provider-qualified Benchmark Subject(s). " +
                $"No representative sampling or size-bracket extrapolation is allowed. Four measured profile points are attempted per target, but the four curator tasks are executed together in one bounded provider turn per profile instead of being repeated as four separate calls. " +
                $"The {hostQueues.Count} physical/provider host queue(s) advance in parallel while each host remains sequential to avoid VRAM contention.");

            async Task<ProviderModelBenchmarkReport> RunHostQueueAsync(List<ProviderModelReference> queue)
            {
                var hostReport = new ProviderModelBenchmarkReport
                {
                    RunId = benchmarkRunId,
                    StartedAtUtc = DateTimeOffset.UtcNow
                };
                foreach (var target in queue)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progressMessage?.Invoke($"\n### Host queue {GetBenchmarkHostKey(target.Endpoint)} · Benchmark Subject {target.DisplayName}\n");
                    var targetReport = await benchmark.RunAsync(
                        new ProviderModelBenchmarkRequest
                        {
                            RunId = benchmarkRunId,
                            Targets = [target],
                            CouncilReviewers = [],
                            MaxProfilesPerModel = 4,
                            ProfileMode = ProviderModelBenchmarkProfileMode.EvenlySpaced,
                            MinimumContextTokens = 2048,
                            MinimumOutputTokens = 384,
                            MaximumContextTokens = maximumContextTokens,
                            MaximumOutputTokens = maximumOutputTokens,
                            IncludeCpuSafeControl = false,
                            StopWhenImprovementStalls = false,
                            StopAfterConsecutiveProfileFailures = 2,
                            MaxTasks = 1,
                            TaskDefinitions = [taskPack],
                            MaxCouncilReviewers = 0,
                            MaxSecondsPerCall = timeoutSeconds,
                            ImprovementThresholdPercent = 0d,
                            IncludeCouncilReview = false,
                            OwnLiveSession = false,
                            ProgressMessage = progressMessage
                        },
                        cancellationToken).ConfigureAwait(false);
                    hostReport.Targets.AddRange(targetReport.Targets);
                    hostReport.Warnings.AddRange(targetReport.Warnings);
                }
                hostReport.CompletedAtUtc = DateTimeOffset.UtcNow;
                return hostReport;
            }

            var hostReports = await Task.WhenAll(hostQueues.Select(RunHostQueueAsync)).ConfigureAwait(false);
            var report = new ProviderModelBenchmarkReport
            {
                RunId = benchmarkRunId,
                StartedAtUtc = hostReports.Min(item => item.StartedAtUtc),
                CompletedAtUtc = hostReports.Max(item => item.CompletedAtUtc),
                Targets = hostReports.SelectMany(item => item.Targets)
                    .OrderBy(item => requestedTargets.FindIndex(requested => requested.SelectionKey.Equals(item.Model.SelectionKey, StringComparison.OrdinalIgnoreCase)))
                    .ToList(),
                Warnings = hostReports.SelectMany(item => item.Warnings).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };

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

    /// <summary>Builds the single composite benchmark assignment from the Task Curator's authoritative preceding result.</summary>
    /// <param name="taskPackText">Consolidated Task Curator output.</param>
    /// <returns>A bounded composite benchmark task that executes all four numbered tasks in one provider call.</returns>
    private ProviderModelBenchmarkTaskDefinition BuildCuratedTaskPack(string taskPackText)
    {
        try
        {
            var taskPack = string.IsNullOrWhiteSpace(taskPackText)
                ? """
                  Task 1: Diagnose this C# loop intended to print 1 through 10: `for (var i = 0; i <= 10; i++) Console.WriteLine(i);`. State the exact bug and corrected loop.
                  Task 2: Explain why provider endpoint plus model name is safer than model name alone in a multi-host LocalGPT environment.
                  Task 3: Return a compact JSON object with contextTokens, outputTokens, parallelModels and reason.
                  Task 4: Give three accessibility requirements for an interactive model card covering keyboard access, labels/screen readers and focus management.
                  """
                : taskPackText.Trim();
            // Curators may accidentally include an opt-out clause (for example "Task N: UNABLE").
            // The maintained Benchmark Subject contract requires an attempt, so remove only those opt-out instruction lines
            // while preserving the curator-authored task content and acceptance criteria.
            taskPack = string.Join(Environment.NewLine, taskPack
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.Contains("UNABLE", StringComparison.OrdinalIgnoreCase)));
            if (taskPack.Length > 24_000)
                taskPack = taskPack[..24_000];

            return new ProviderModelBenchmarkTaskDefinition
            {
                Name = "Curated four-task Benchmark Subject pack",
                Prompt =
                    "Execute the following authoritative Task Curator benchmark pack. This is your assigned Benchmark Subject job. " +
                    "Attempt every task; do not return UNABLE merely because you are an AI model, do not ask another role to perform it, do not call tools, and do not discuss benchmark planning. " +
                    "Return exactly four sections labelled Task 1:, Task 2:, Task 3:, Task 4:. If uncertain, make the best bounded attempt and state uncertainty inside that task answer.\n\n" +
                    taskPack,
                ExpectedTokens = ["Task 1", "Task 2", "Task 3", "Task 4", "provider", "model", "contextTokens", "outputTokens", "parallelModels", "keyboard", "focus"],
                ExpectedSectionCount = 4,
                RequireEmbeddedJsonObject = true,
                EnforceRoleExecution = true
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the curated composite benchmark task pack failed.");
            throw;
        }
    }

    /// <summary>Returns a physical-host grouping key so independent AI hosts benchmark in parallel while each host remains sequential.</summary>
    /// <param name="endpoint">Provider endpoint associated with a benchmark target.</param>
    /// <returns>A stable host grouping key.</returns>
    private string GetBenchmarkHostKey(string endpoint)
    {
        try
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                return string.IsNullOrWhiteSpace(endpoint) ? "unknown-host" : endpoint.Trim().ToLowerInvariant();
            if (uri.IsLoopback)
                return "local-machine";
            return uri.Host.ToLowerInvariant();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving benchmark host queue failed; endpoint details were omitted.");
            throw;
        }
    }

    /// <summary>Limits one untrusted provider answer preview so aggregate Council evidence stays useful without recreating a 95-member context flood.</summary>
    /// <param name="value">Provider answer preview.</param>
    /// <returns>A single-line bounded evidence excerpt.</returns>
    private string LimitEvidence(string value)
    {
        try
        {
            var normalized = string.Join(" ", (value ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (normalized.Length > 600)
                normalized = normalized[..600] + "…";
            return normalized.Replace("|", "\\|", StringComparison.Ordinal);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Limiting provider benchmark evidence failed; response content was omitted.");
            return "[evidence formatting failed]";
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

            builder.AppendLine();
            builder.AppendLine("### Provider-qualified subject measurement matrix");
            builder.AppendLine("Each row is actual provider execution evidence. Failed/malformed answers remain evidence; they are not application JSON errors and are not silently retried forever.");
            builder.AppendLine();
            builder.AppendLine("| Subject | Successful points | Best quality | Best tok/s | Best time | Recommendation |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- |");
            foreach (var target in result.Report.Targets)
            {
                var successfulProfiles = target.Profiles.Where(profile => profile.Tasks.Any(task => task.Succeeded)).ToList();
                var best = successfulProfiles.OrderByDescending(profile => profile.AverageQualityScore).ThenByDescending(profile => profile.AverageTokensPerSecond).FirstOrDefault();
                var recommendation = string.IsNullOrWhiteSpace(target.Recommendation.ProfileName)
                    ? "no successful recommendation"
                    : $"{target.Recommendation.ContextTokens:N0} ctx / {target.Recommendation.OutputTokens:N0} out";
                builder.Append("| ").Append(target.Model.DisplayName.Replace("|", "\\|", StringComparison.Ordinal))
                    .Append(" | ").Append(successfulProfiles.Count).Append('/').Append(target.Profiles.Count)
                    .Append(" | ").Append(best?.AverageQualityScore.ToString("0.00") ?? "—")
                    .Append(" | ").Append(best?.AverageTokensPerSecond.ToString("0.0") ?? "—")
                    .Append(" | ").Append(best is null ? "—" : $"{best.AverageTotalMilliseconds / 1000d:0.0}s")
                    .Append(" | ").Append(recommendation).AppendLine(" |");
            }

            var evidence = result.Report.Targets
                .SelectMany(target => target.Profiles.SelectMany(profile => profile.Tasks.Select(task => new { Target = target, Profile = profile, Task = task })))
                .Where(item => !string.IsNullOrWhiteSpace(item.Task.ResponsePreview))
                .ToList();
            var exemplars = evidence.OrderByDescending(item => item.Task.QualityScore).ThenByDescending(item => item.Task.TokensPerSecond).Take(6).ToList();
            var weak = evidence.OrderBy(item => item.Task.QualityScore).ThenByDescending(item => item.Task.TotalMilliseconds).Take(6).ToList();
            if (exemplars.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("### Strong independent answer exemplars");
                builder.AppendLine("These remain independent subject results; later roles may learn from them without erasing the individual measurements.");
                foreach (var item in exemplars)
                    builder.Append("- **").Append(item.Target.Model.DisplayName).Append("** · ").Append(item.Profile.ProfileName)
                        .Append(" · quality ").Append(item.Task.QualityScore.ToString("0.00")).Append(": ")
                        .AppendLine(LimitEvidence(item.Task.ResponsePreview));
            }
            if (weak.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("### Weak/refusal/malformed evidence for reviewers");
                foreach (var item in weak)
                    builder.Append("- **").Append(item.Target.Model.DisplayName).Append("** · ").Append(item.Profile.ProfileName)
                        .Append(" · quality ").Append(item.Task.QualityScore.ToString("0.00")).Append(": ")
                        .AppendLine(LimitEvidence(item.Task.ResponsePreview));
            }

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
