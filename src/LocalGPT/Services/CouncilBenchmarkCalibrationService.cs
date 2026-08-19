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
/// <param name="catalog">Supplies database-backed token bounds instead of machine-specific benchmark constants.</param>
/// <param name="liveSessions">Publishes every measured Benchmark Subject as a rich live Council lane.</param>
/// <param name="logger">Writes bounded calibration diagnostics.</param>
public sealed class CouncilBenchmarkCalibrationService(
    IProviderModelBenchmarkService benchmark,
    IHardwarePerformancePresetService performancePresets,
    LocalGptCatalogService catalog,
    ICouncilLiveSessionService liveSessions,
    ILogger<CouncilBenchmarkCalibrationService> logger) : ICouncilBenchmarkCalibrationService
{
    /// <summary>
    /// Executes the maintained all-target/four-task calibration across every distinct benchmark-capable provider-qualified target,
    /// streams each actual provider measurement through the parent Council's live lanes, stores the requested measured tier profiles and returns coverage evidence.
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
            var profileCount = Math.Clamp(request.ProfileCount, 1, 16);
            var maximumContextTokens = Math.Clamp(request.MaximumContextTokens, catalog.MinContextTokens, catalog.MaxContextTokens);
            var minimumContextTokens = Math.Clamp(request.MinimumContextTokens, catalog.MinContextTokens, maximumContextTokens);
            var maximumOutputTokens = Math.Clamp(request.MaximumOutputTokens, catalog.MinOutputTokens, catalog.MaxOutputTokens);
            var minimumOutputTokens = Math.Clamp(request.MinimumOutputTokens, catalog.MinOutputTokens, maximumOutputTokens);
            var timeoutSeconds = Math.Clamp(request.MaxSecondsPerCall, 10, 900);
            var failureStop = Math.Clamp(request.StopAfterConsecutiveProfileFailures, 0, profileCount);
            var taskPack = BuildCuratedTaskPack(request.TaskPackText);
            var profileNames = BuildProfileNames(profileCount);
            var hostQueues = benchmarkTargets
                .GroupBy(target => GetBenchmarkHostKey(target.Endpoint), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.ToList())
                .ToList();
            var activityKeys = benchmarkTargets
                .Select((target, index) => new { target.SelectionKey, ActivityKey = $"benchmark-subject-{index + 1:D3}" })
                .ToDictionary(item => item.SelectionKey, item => item.ActivityKey, StringComparer.OrdinalIgnoreCase);

            foreach (var target in benchmarkTargets)
            {
                var activityKey = activityKeys[target.SelectionKey];
                liveSessions.BeginParticipantActivity(
                    request.CouncilRunId,
                    activityKey,
                    target.DisplayName,
                    "Measurement",
                    "Benchmark Subject",
                    $"{GetBenchmarkHostKey(target.Endpoint)} · {target.ProviderName}");
                liveSessions.SetParticipantActivityStatus(
                    request.CouncilRunId,
                    activityKey,
                    $"Queued in the shared all-model measurement phase · {profileCount} measured profile point(s).");
            }
            liveSessions.Touch(request.CouncilRunId);
            progressMessage?.Invoke(
                $"## One deterministic all-model measurement phase\n\n" +
                $"Frozen target coverage: **{benchmarkTargets.Count} benchmark-capable subject(s)** from **{requestedTargets.Count} selected provider-qualified Council member(s)**. " +
                $"LocalGPT executes the same consolidated four-section suite against every subject and attempts **{profileCount} measured profile point(s)** ({string.Join(", ", profileNames)}). " +
                $"These are parameter measurements of one target set, never model packs or representative subsets. " +
                $"The {hostQueues.Count} independent physical/provider host queue(s) advance in parallel; models sharing one host remain sequential to avoid VRAM contention.\n\n" +
                "**Measurement provenance:** benchmark timing, throughput and quality are produced only by live provider calls to each provider-qualified endpoint/model. " +
                "CanIRun.ai and other hardware guidance may help setup recommendations, but advisory website values are not benchmark scores and are not substituted for provider execution.");

            async Task<ProviderModelBenchmarkReport> RunHostQueueAsync(List<ProviderModelReference> queue)
            {
                var hostReport = new ProviderModelBenchmarkReport
                {
                    RunId = benchmarkRunId,
                    StartedAtUtc = DateTimeOffset.UtcNow
                };
                for (var targetIndex = 0; targetIndex < queue.Count; targetIndex++)
                {
                    var target = queue[targetIndex];
                    var activityKey = activityKeys[target.SelectionKey];
                    cancellationToken.ThrowIfCancellationRequested();
                    liveSessions.SetParticipantActivityStatus(
                        request.CouncilRunId,
                        activityKey,
                        $"Measuring {targetIndex + 1}/{queue.Count} on host queue {GetBenchmarkHostKey(target.Endpoint)} · live provider calls in progress.");
                    liveSessions.AppendParticipantActivity(
                        request.CouncilRunId,
                        activityKey,
                        $"_Provider-qualified subject: `{target.ProviderKind}` · `{target.Endpoint}` · `{target.ModelName}`._\n\n");
                    liveSessions.Touch(request.CouncilRunId);

                    try
                    {
                        var targetReport = await benchmark.RunAsync(
                            new ProviderModelBenchmarkRequest
                            {
                                RunId = benchmarkRunId,
                                Targets = [target],
                                CouncilReviewers = [],
                                MaxProfilesPerModel = profileCount,
                                ProfileMode = ProviderModelBenchmarkProfileMode.EvenlySpaced,
                                ProfileNames = [.. profileNames],
                                MinimumContextTokens = minimumContextTokens,
                                MinimumOutputTokens = minimumOutputTokens,
                                MaximumContextTokens = maximumContextTokens,
                                MaximumOutputTokens = maximumOutputTokens,
                                IncludeCpuSafeControl = false,
                                StopWhenImprovementStalls = false,
                                StopAfterConsecutiveProfileFailures = failureStop,
                                RepetitionRecoveryAttempts = Math.Clamp(request.RepetitionRecoveryAttempts, 0, 8),
                                MaxTasks = 1,
                                TaskDefinitions = [taskPack],
                                MaxCouncilReviewers = 0,
                                MaxSecondsPerCall = timeoutSeconds,
                                ImprovementThresholdPercent = 0d,
                                IncludeCouncilReview = false,
                                OwnLiveSession = false,
                                ProgressMessage = message =>
                                {
                                    if (string.IsNullOrWhiteSpace(message))
                                        return;
                                    liveSessions.AppendParticipantActivity(request.CouncilRunId, activityKey, message.EndsWith('\n') ? message : message + Environment.NewLine);
                                    liveSessions.Touch(request.CouncilRunId);
                                },
                                ProviderStream = fragment =>
                                {
                                    if (string.IsNullOrEmpty(fragment))
                                        return;
                                    // AppendParticipantActivity already updates the run timestamp and coalesces
                                    // its Changed notification. A second Touch per streamed fragment only doubles
                                    // circuit churn during long reasoning-model runs.
                                    liveSessions.AppendParticipantActivity(request.CouncilRunId, activityKey, fragment);
                                }
                            },
                            cancellationToken).ConfigureAwait(false);
                        hostReport.Targets.AddRange(targetReport.Targets);
                        hostReport.Warnings.AddRange(targetReport.Warnings);
                        var targetResult = targetReport.Targets.FirstOrDefault(item =>
                            item.Model.SelectionKey.Equals(target.SelectionKey, StringComparison.OrdinalIgnoreCase));
                        if (targetResult is null)
                        {
                            targetResult = new ProviderModelBenchmarkTargetResult
                            {
                                Model = target,
                                Error = "Provider benchmark returned no target result for this frozen provider-qualified identity."
                            };
                            hostReport.Targets.Add(targetResult);
                            hostReport.Warnings.Add($"{target.DisplayName}: {targetResult.Error}");
                        }
                        var laneResult = BuildBenchmarkLaneResult(targetResult, profileCount);
                        liveSessions.SetParticipantActivityResult(request.CouncilRunId, activityKey, laneResult);
                        liveSessions.CompleteParticipantActivity(
                            request.CouncilRunId,
                            activityKey,
                            string.IsNullOrWhiteSpace(targetResult.Error)
                                ? "Benchmark Subject completed with live provider measurement evidence."
                                : "Benchmark Subject completed with explicit provider failure evidence.");
                    }
                    catch (OperationCanceledException)
                    {
                        liveSessions.SetParticipantActivityStatus(request.CouncilRunId, activityKey, "Benchmark Subject cancelled by the owning Council run.");
                        throw;
                    }
                    catch (Exception exception)
                    {
                        var boundedError = LimitEvidence(exception.Message);
                        var failedTarget = new ProviderModelBenchmarkTargetResult
                        {
                            Model = target,
                            Error = $"Unexpected benchmark engine failure: {boundedError}"
                        };
                        hostReport.Targets.Add(failedTarget);
                        hostReport.Warnings.Add($"{target.DisplayName}: {failedTarget.Error}");
                        liveSessions.AppendParticipantActivity(
                            request.CouncilRunId,
                            activityKey,
                            $"\n**Measurement engine error:** {boundedError}\n");
                        liveSessions.SetParticipantActivityResult(request.CouncilRunId, activityKey, BuildBenchmarkLaneResult(failedTarget, profileCount));
                        liveSessions.CompleteParticipantActivity(
                            request.CouncilRunId,
                            activityKey,
                            "Benchmark Subject failed unexpectedly; failure was retained and the host queue continued.");
                        logger.LogError(
                            exception,
                            "Benchmark Subject {SelectionKey} failed unexpectedly inside the all-target host queue; remaining subjects will continue.",
                            target.SelectionKey);
                    }
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

            var measuredSelectionKeys = report.Targets
                .Select(target => target.Model.SelectionKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingBenchmarkTargets = benchmarkTargets
                .Where(target => !measuredSelectionKeys.Contains(target.SelectionKey))
                .Select(target => target.SelectionKey)
                .ToList();
            if (missingBenchmarkTargets.Count > 0)
                throw new InvalidOperationException(
                    $"The all-model benchmark coverage contract was violated: {missingBenchmarkTargets.Count} benchmark-capable selected subject(s) were not returned by the deterministic measurement engine. Missing identities: {string.Join(", ", missingBenchmarkTargets)}");

            var coverage = new ProviderModelBenchmarkCoverageSnapshot(report);
            if (coverage.AttemptedTargetCount != benchmarkTargets.Count || !coverage.IsArithmeticConsistent)
                throw new InvalidOperationException(
                    $"The deterministic benchmark coverage invariant failed: attempted={coverage.AttemptedTargetCount}, expected={benchmarkTargets.Count}, successful={coverage.SuccessfulTargetCount}, unresolved={coverage.UnresolvedTargetCount}.");

            var successfulTargets = coverage.SuccessfulTargetCount;
            var baseName = string.IsNullOrWhiteSpace(request.PresetBaseName)
                ? $"Initial calibration {DateTimeOffset.Now:yyyy-MM-dd HHmmss}"
                : request.PresetBaseName.Trim();
            IReadOnlyList<HardwarePerformancePreset> presets = [];
            if (successfulTargets > 0)
            {
                presets = await performancePresets.SaveBenchmarkProfileSetAsync(
                    report,
                    baseName,
                    userConfirmed: true,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                report.Warnings.Add("No benchmark subject produced successful provider-call evidence, so LocalGPT retained the failed coverage matrix without inventing or storing hardware profile routes.");
            }

            var result = new CouncilBenchmarkCalibrationResult
            {
                CouncilRunId = request.CouncilRunId,
                BenchmarkRunId = benchmarkRunId,
                RequestedTargetCount = requestedTargets.Count,
                BenchmarkTargetCount = benchmarkTargets.Count,
                SuccessfulTargetCount = successfulTargets,
                UnresolvedTargetSelectionKeys = coverage.UnresolvedSelectionKeys.ToList(),
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
                Name = "Consolidated four-section all-subject benchmark suite",
                Prompt =
                    "Execute the following authoritative Task Curator benchmark suite. This is your assigned Benchmark Subject job inside one all-model measurement phase. " +
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

    /// <summary>Builds stable user-facing names for the configured benchmark profile points without coupling token values to one machine.</summary>
    /// <param name="profileCount">Configured number of measured parameter points.</param>
    /// <returns>Ordered profile labels used by provider progress, live lanes and persisted calibration tiers.</returns>
    private IReadOnlyList<string> BuildProfileNames(int profileCount)
    {
        try
        {
            if (profileCount == 5)
                return ["Low", "Normal", "High", "Expert", "Max"];
            return Enumerable.Range(1, profileCount).Select(index => $"Profile {index}/{profileCount}").ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building benchmark profile labels failed.");
            throw;
        }
    }

    /// <summary>Builds the retained rich-lane result for one provider-qualified Benchmark Subject from actual call evidence.</summary>
    /// <param name="target">Completed or failed target measurement.</param>
    /// <param name="expectedProfileCount">Configured number of profile points.</param>
    /// <returns>Bounded Markdown for the live Council lane.</returns>
    private string BuildBenchmarkLaneResult(ProviderModelBenchmarkTargetResult target, int expectedProfileCount)
    {
        try
        {
            var providerCalls = target.Profiles.Sum(profile => profile.Tasks.Sum(task => task.AttemptCount));
            var successfulCalls = target.Profiles.Sum(profile => profile.Tasks.Count(task => task.Succeeded));
            var builder = new StringBuilder();
            builder.AppendLine($"### Benchmark Subject · {target.Model.DisplayName}");
            builder.AppendLine();
            builder.AppendLine($"- Provider endpoint: `{target.Model.Endpoint}`");
            builder.AppendLine($"- Provider model: `{target.Model.ModelName}`");
            builder.AppendLine($"- Actual provider calls observed: **{providerCalls}**");
            builder.AppendLine($"- Contract-compliant measured calls: **{successfulCalls}**");
            builder.AppendLine($"- Profile points returned: **{target.Profiles.Count}/{expectedProfileCount}**");
            foreach (var profile in target.Profiles)
            {
                var attemptedTasks = profile.Tasks.Count;
                var providerAttempts = profile.Tasks.Sum(task => task.AttemptCount);
                var succeeded = profile.Tasks.Count(task => task.Succeeded);
                builder.Append("- **").Append(profile.ProfileName).Append("** · ")
                    .Append(profile.ContextTokens.ToString("N0")).Append(" ctx / ")
                    .Append(profile.OutputTokens.ToString("N0")).Append(" out · provider calls ")
                    .Append(providerAttempts).Append(" · compliant tasks ").Append(succeeded).Append('/').Append(attemptedTasks)
                    .Append(" · quality ").Append(profile.AverageQualityScore.ToString("0.000"))
                    .Append(" · ").Append(profile.AverageTokensPerSecond.ToString("0.00")).Append(" tok/s")
                    .Append(" · ").Append(profile.AverageTotalMilliseconds.ToString("0")).AppendLine(" ms");
            }
            if (!string.IsNullOrWhiteSpace(target.Recommendation.ProfileName))
                builder.AppendLine($"- Recommendation from measured evidence: **{target.Recommendation.ProfileName}** · {target.Recommendation.ContextTokens:N0} ctx / {target.Recommendation.OutputTokens:N0} out");
            if (!string.IsNullOrWhiteSpace(target.Error))
                builder.AppendLine($"- Explicit failure evidence: {LimitEvidence(target.Error)}");
            if (providerCalls == 0)
                builder.AppendLine("- **No provider call evidence was returned.** This target remains a coverage failure; no advisory hardware value is treated as a benchmark result.");
            return builder.ToString().Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building live Benchmark Subject lane result failed for {SelectionKey}.", target.Model.SelectionKey);
            return $"Benchmark Subject `{target.Model.SelectionKey}` finished, but LocalGPT could not format its retained measurement lane.";
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
            var coverage = new ProviderModelBenchmarkCoverageSnapshot(result.Report);
            if (coverage.AttemptedTargetCount != result.BenchmarkTargetCount
                || coverage.SuccessfulTargetCount != result.SuccessfulTargetCount
                || !coverage.UnresolvedSelectionKeys.SequenceEqual(result.UnresolvedTargetSelectionKeys, StringComparer.OrdinalIgnoreCase)
                || !coverage.IsArithmeticConsistent)
            {
                throw new InvalidOperationException(
                    $"The persisted Council coverage state does not match deterministic benchmark evidence: attempted={coverage.AttemptedTargetCount}/{result.BenchmarkTargetCount}, successful={coverage.SuccessfulTargetCount}/{result.SuccessfulTargetCount}, unresolved={coverage.UnresolvedTargetCount}/{result.UnresolvedTargetSelectionKeys.Count}.");
            }

            builder.AppendLine($"- Members with at least one successful measured recommendation: **{result.SuccessfulTargetCount}**");
            builder.AppendLine($"- Stored measured profiles: **{result.Presets.Count}**");
            builder.AppendLine();
            builder.AppendLine("### Machine-derived coverage invariant");
            builder.AppendLine("This block is authoritative deterministic benchmark state. Later Council/model prose is commentary and must not replace, shrink, expand or reinterpret this identity set.");
            builder.AppendLine($"- Attempted benchmark-capable provider-qualified identities: **{coverage.AttemptedTargetCount}**");
            builder.AppendLine($"- Successful measured recommendation identities: **{coverage.SuccessfulTargetCount}**");
            builder.AppendLine($"- Unresolved attempted identities: **{coverage.UnresolvedTargetCount}**");
            builder.AppendLine($"- Arithmetic check: **{coverage.AttemptedTargetCount} - {coverage.SuccessfulTargetCount} = {coverage.UnresolvedTargetCount}**");
            if (coverage.UnresolvedTargetCount > 0)
            {
                builder.AppendLine();
                builder.AppendLine("#### Authoritative unresolved provider-qualified identities");
                foreach (var selectionKey in coverage.UnresolvedSelectionKeys)
                    builder.AppendLine($"- `{selectionKey.Replace("`", "'", StringComparison.Ordinal)}`");
                builder.AppendLine();
                builder.AppendLine("**Truth guard:** if any later AI reviewer reports a different unresolved count or a different identity set, that prose conflicts with deterministic measurement evidence and must be treated as incorrect. Copy this list exactly when restating coverage.");
            }
            else
            {
                builder.AppendLine("- Authoritative unresolved identity set: **empty**");
            }

            builder.AppendLine();
            builder.AppendLine("### Stored initial profiles");
            foreach (var preset in result.Presets)
                builder.AppendLine($"- `{preset.Name}`");

            builder.AppendLine();
            builder.AppendLine("### Provider-qualified subject measurement matrix");
            builder.AppendLine("Each row is actual provider execution evidence. Advisory hardware/web data is never substituted for these measurements. Failed/malformed answers remain evidence; they are not application JSON errors and are not silently retried forever.");
            builder.AppendLine();
            builder.AppendLine("| Subject | Provider calls | Contract-compliant calls | Successful points | Best quality | Best tok/s | Best time | Recommendation |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |");
            foreach (var target in result.Report.Targets)
            {
                var successfulProfiles = target.Profiles.Where(profile => profile.Tasks.Any(task => task.Succeeded)).ToList();
                var best = successfulProfiles.OrderByDescending(profile => profile.AverageQualityScore).ThenByDescending(profile => profile.AverageTokensPerSecond).FirstOrDefault();
                var recommendation = string.IsNullOrWhiteSpace(target.Recommendation.ProfileName)
                    ? "no successful recommendation"
                    : $"{target.Recommendation.ContextTokens:N0} ctx / {target.Recommendation.OutputTokens:N0} out";
                var providerCalls = target.Profiles.Sum(profile => profile.Tasks.Sum(task => task.AttemptCount));
                var successfulCalls = target.Profiles.Sum(profile => profile.Tasks.Count(task => task.Succeeded));
                builder.Append("| ").Append(target.Model.DisplayName.Replace("|", "\\|", StringComparison.Ordinal))
                    .Append(" | ").Append(providerCalls)
                    .Append(" | ").Append(successfulCalls)
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
            builder.AppendLine(coverage.UnresolvedTargetCount == 0
                ? "**Coverage gate: PASS.** Every benchmark-capable selected member produced a successful measured recommendation. Social review may compare and synthesize the measured initial profiles, but it may not replace deterministic evidence."
                : $"**Coverage gate: PARTIAL.** Exactly **{coverage.UnresolvedTargetCount}** attempted provider-qualified identity/identities remain unresolved. Later roles must preserve the authoritative unresolved list above and must not substitute a smaller example list, one-host subset or reviewer-generated count.");
            return builder.ToString().Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building Council benchmark calibration summary failed.");
            throw;
        }
    }
}
