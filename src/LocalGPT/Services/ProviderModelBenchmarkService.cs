using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services;

/// <summary>
/// Bounded benchmark and council review for provider-qualified models. The service does not download
/// models or alter provider-global settings. Applying a recommendation creates or updates a user-approved
/// LocalGPT model preset whose routes retain provider, endpoint and provider model name.
/// </summary>
public sealed partial class ProviderModelBenchmarkService : IProviderModelBenchmarkService
{
    /// <summary>
    /// Stores the provider model runtime service dependency used by <see cref="ProviderModelBenchmarkService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IProviderModelRuntimeService providerModels;
    /// <summary>
    /// Stores the model preset service dependency used by <see cref="ProviderModelBenchmarkService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IModelPresetService modelPresets;
    /// <summary>
    /// Stores the hardware performance preset service dependency used by <see cref="ProviderModelBenchmarkService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IHardwarePerformancePresetService performancePresets;
    /// <summary>
    /// Stores the council live session service dependency used by <see cref="ProviderModelBenchmarkService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ICouncilLiveSessionService liveSessions;
    /// <summary>
    /// Stores the provider model reviewer policy service dependency used by <see cref="ProviderModelBenchmarkService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IProviderModelReviewerPolicyService reviewerPolicy;
    /// <summary>
    /// Stores database-backed runtime token bounds so benchmark limits adapt to maintained configuration rather than one developer machine.
    /// </summary>
    private readonly LocalGptCatalogService catalog;
    /// <summary>
    /// Stores the logger used by <see cref="ProviderModelBenchmarkService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<ProviderModelBenchmarkService> logger;

    /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
    /// <param name="providerModels">Injected dependency used by the ProviderModelBenchmarkService.</param>
    /// <param name="modelPresets">Injected dependency used by the ProviderModelBenchmarkService.</param>
    /// <param name="performancePresets">Injected dependency used by the ProviderModelBenchmarkService.</param>
    /// <param name="liveSessions">Injected dependency used by the ProviderModelBenchmarkService.</param>
    /// <param name="reviewerPolicy">Injected dependency used by the ProviderModelBenchmarkService.</param>
    /// <param name="catalog">Database-backed LocalGPT runtime policy and token bounds.</param>
    /// <param name="logger">Injected dependency used by the ProviderModelBenchmarkService.</param>
    public ProviderModelBenchmarkService(
        IProviderModelRuntimeService providerModels,
        IModelPresetService modelPresets,
        IHardwarePerformancePresetService performancePresets,
        ICouncilLiveSessionService liveSessions,
        IProviderModelReviewerPolicyService reviewerPolicy,
        LocalGptCatalogService catalog,
        ILogger<ProviderModelBenchmarkService> logger)
    {
        this.providerModels = providerModels;
        this.modelPresets = modelPresets;
        this.performancePresets = performancePresets;
        this.liveSessions = liveSessions;
        this.reviewerPolicy = reviewerPolicy;
        this.catalog = catalog;
        this.logger = logger;
    }

    /// <summary>
    /// Performs run as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider model benchmark report produced by the operation.</returns>
    public async Task<ProviderModelBenchmarkReport> RunAsync(
        ProviderModelBenchmarkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var report = new ProviderModelBenchmarkReport
        {
            RunId = request.RunId == Guid.Empty ? Guid.NewGuid() : request.RunId
        };
        var targets = request.Targets
            .Where(model => model is not null && model.SupportsBenchmark && !string.IsNullOrWhiteSpace(model.ModelName))
            .GroupBy(model => model.SelectionKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var reviewers = request.CouncilReviewers
            .Where(model => model is not null && model.SupportsBenchmark && !string.IsNullOrWhiteSpace(model.ModelName))
            .GroupBy(model => model.SelectionKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(reviewerPolicy.GetPriority)
            .ThenBy(model => model.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(model => model.Endpoint, StringComparer.OrdinalIgnoreCase)
            .ThenBy(model => model.ModelName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, request.MaxCouncilReviewers))
            .ToList();
        report.CouncilMembers = reviewers.Select(model => model.SelectionKey).ToList();

        if (targets.Count == 0)
        {
            report.Warnings.Add("No benchmark-capable provider-qualified model was selected.");
            report.CompletedAtUtc = DateTimeOffset.UtcNow;
            return report;
        }

        var maxProfiles = Math.Max(1, request.MaxProfilesPerModel);
        var maxTasks = Math.Clamp(request.MaxTasks, 1, 4);
        var maxSeconds = Math.Clamp(request.MaxSecondsPerCall, 10, 900);
        var maximumContext = Math.Clamp(request.MaximumContextTokens, catalog.MinContextTokens, catalog.MaxContextTokens);
        var maximumOutput = Math.Clamp(request.MaximumOutputTokens, catalog.MinOutputTokens, catalog.MaxOutputTokens);
        var threshold = Math.Clamp(request.ImprovementThresholdPercent, 0d, 50d);
        var stopAfterConsecutiveProfileFailures = Math.Clamp(request.StopAfterConsecutiveProfileFailures, 0, maxProfiles);
        var tasks = BuildTasks(request).Take(maxTasks).ToList();
        var maximumMeasurementCalls = (long)targets.Count * maxProfiles * tasks.Count;
        var maximumReviewCalls = request.IncludeCouncilReview
            ? (long)targets.Count * Math.Max(0, request.MaxCouncilReviewers)
            : 0L;
        var maximumCallCount = maximumMeasurementCalls + maximumReviewCalls;
        var maximumBoundedDuration = TimeSpan.FromSeconds((long)maximumCallCount * maxSeconds);
        var maximumBoundedDurationText = maximumBoundedDuration.TotalHours >= 1d
            ? $"{maximumBoundedDuration.TotalHours:0.#} hours"
            : $"{Math.Max(1d, maximumBoundedDuration.TotalMinutes):0} minutes";

        var sessionMembers = new List<string> { "Benchmark Council" };
        sessionMembers.AddRange(targets
            .Concat(reviewers)
            .GroupBy(model => model.SelectionKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First().DisplayName));

        var initialTranscript = $"""
            ## Provider Benchmark Council

            _Run `{report.RunId:N}` started with {targets.Count} target model(s), up to {maxProfiles} profile(s) per model and {tasks.Count} deterministic task(s) per profile._

            The benchmark is provider-qualified: every request stays bound to its selected provider endpoint and model identity. Automatic DXFunctions are disabled during measurements so tool negotiation cannot distort latency or quality results.

            Configured upper bound: up to {maximumCallCount} provider calls and {maximumBoundedDurationText} if every call reaches its timeout. Normal responses shorten the run{(request.StopWhenImprovementStalls ? ", and the enabled improvement stop rule may stop a target before its final profile" : string.Empty)}.

            """;
        var liveCancellation = request.OwnLiveSession
            ? liveSessions.Begin(
                report.RunId,
                sessionMembers,
                $"Run bounded provider benchmark for {targets.Count} selected model(s).",
                initialTranscript)
            : CancellationToken.None;
        logger.LogInformation(
            "Started Provider Benchmark Council run {RunId} for {TargetCount} target(s), {ProfileLimit} profile(s) and {TaskCount} task(s); standalone live session ownership is {OwnLiveSession}.",
            report.RunId,
            targets.Count,
            maxProfiles,
            tasks.Count,
            request.OwnLiveSession);
        using var runCts = request.OwnLiveSession
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, liveCancellation)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var runToken = runCts.Token;

        void Publish(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            var normalized = text.EndsWith('\n') ? text : text + Environment.NewLine;
            request.ProgressMessage?.Invoke(normalized);
            if (request.OwnLiveSession)
                liveSessions.Append(report.RunId, normalized);
        }

        try
        {
            Publish($"_Limits: {maxSeconds}s per call · context ≤ {maximumContext:N0} · output ≤ {maximumOutput:N0} · stop threshold {threshold:0.#}%._\n");

            for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                runToken.ThrowIfCancellationRequested();
                var target = targets[targetIndex];
                var targetResult = new ProviderModelBenchmarkTargetResult { Model = target };
                report.Targets.Add(targetResult);
                Publish($"\n### Target {targetIndex + 1}/{targets.Count}: {target.DisplayName}\n");

                try
                {
                    var profiles = BuildProfiles(target, request, maxProfiles, maximumContext, maximumOutput).ToList();
                    var bestScore = double.MinValue;
                    var consecutiveNonImprovingProfiles = 0;
                    var consecutiveFailedProfiles = 0;
                    for (var profileIndex = 0; profileIndex < profiles.Count; profileIndex++)
                    {
                        runToken.ThrowIfCancellationRequested();
                        var profile = profiles[profileIndex];
                        Publish($"- Profile {profileIndex + 1}/{profiles.Count}: **{profile.Name}** · context {profile.ContextTokens:N0} · output {profile.OutputTokens:N0}" +
                            (profile.OllamaNumGpu is int numGpu ? $" · Ollama num_gpu {numGpu}" : string.Empty));

                        var profileResult = await RunProfileAsync(
                            target,
                            profile,
                            tasks,
                            maxSeconds,
                            message => Publish($"  {message}"),
                            runToken).ConfigureAwait(false);
                        targetResult.Profiles.Add(profileResult);

                        if (profileResult.Tasks.Any(task => task.Succeeded))
                        {
                            consecutiveFailedProfiles = 0;
                            Publish($"  - Profile score: {profileResult.Score:0.00} · quality {profileResult.AverageQualityScore:0.000} · {profileResult.AverageTokensPerSecond:0.00} token/s · {profileResult.AverageTotalMilliseconds:0} ms average.");
                            if (bestScore == double.MinValue)
                            {
                                bestScore = profileResult.Score;
                            }
                            else
                            {
                                var denominator = Math.Max(0.001d, Math.Abs(bestScore));
                                var improvement = (profileResult.Score - bestScore) / denominator * 100d;
                                if (improvement >= threshold)
                                {
                                    bestScore = Math.Max(bestScore, profileResult.Score);
                                    consecutiveNonImprovingProfiles = 0;
                                }
                                else if (!profile.Name.Contains("CPU-safe control", StringComparison.OrdinalIgnoreCase))
                                {
                                    consecutiveNonImprovingProfiles++;
                                }
                            }
                        }
                        else
                        {
                            consecutiveFailedProfiles++;
                            Publish("  - No benchmark task completed successfully for this profile.");
                        }

                        if (stopAfterConsecutiveProfileFailures > 0 &&
                            consecutiveFailedProfiles >= stopAfterConsecutiveProfileFailures &&
                            profileIndex + 1 < profiles.Count)
                        {
                            Publish($"  - Remaining profile escalation skipped after {consecutiveFailedProfiles} consecutive profile failure(s). Failed measurements remain explicit evidence; LocalGPT does not invent higher token limits.");
                            break;
                        }

                        // Profiles are bounded provider measurements. Improvement-based early stop remains opt-in;
                        // the maintained initial calibration disables it so all configured profile points are attempted.
                        if (request.StopWhenImprovementStalls &&
                            profileIndex >= 3 &&
                            consecutiveNonImprovingProfiles >= 2)
                        {
                            targetResult.StoppedBecauseImprovementWasBelowThreshold = true;
                            Publish("  - Remaining profiles skipped because two consecutive profiles stayed below the configured improvement threshold.");
                            break;
                        }
                    }

                    var best = targetResult.Profiles
                        .Where(profile => profile.Tasks.Any(task => task.Succeeded))
                        .OrderByDescending(profile => profile.Score)
                        .FirstOrDefault();
                    if (best is null)
                    {
                        targetResult.Error = "No profile completed a benchmark task successfully.";
                        Publish("- Target failed: no profile produced a successful benchmark response.");
                        continue;
                    }

                    if (request.IncludeCouncilReview)
                    {
                        var effectiveReviewers = reviewers
                            .Where(reviewer => !reviewer.SelectionKey.Equals(target.SelectionKey, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        if (effectiveReviewers.Count == 0)
                            effectiveReviewers.Add(target);

                        var boundedReviewers = effectiveReviewers
                            .Take(Math.Max(0, request.MaxCouncilReviewers))
                            .ToList();
                        Publish($"- Council review: {boundedReviewers.Count} reviewer(s) will inspect the best profile **{best.ProfileName}**.");
                        for (var reviewerIndex = 0; reviewerIndex < boundedReviewers.Count; reviewerIndex++)
                        {
                            var reviewer = boundedReviewers[reviewerIndex];
                            var review = await ReviewRecommendationAsync(
                                reviewer,
                                target,
                                best,
                                maxSeconds,
                                maximumContext,
                                maximumOutput,
                                message => Publish($"  {message}"),
                                runToken).ConfigureAwait(false);
                            targetResult.CouncilReviews.Add(review);
                        }
                    }

                    var successfulReviews = targetResult.CouncilReviews.Where(review => string.IsNullOrWhiteSpace(review.Error)).ToList();
                    var councilScore = successfulReviews.Count == 0
                        ? best.Score
                        : successfulReviews.Average(review => (review.QualityScore + review.ReliabilityScore) / 2d);
                    var context = successfulReviews.Count == 0
                        ? best.ContextTokens
                        : ClampToSupportedStep((int)Math.Round(successfulReviews.Average(review => review.RecommendedContextTokens)), 2048, maximumContext);
                    var output = successfulReviews.Count == 0
                        ? best.OutputTokens
                        : ClampToSupportedStep((int)Math.Round(successfulReviews.Average(review => review.RecommendedOutputTokens)), 128, maximumOutput);
                    targetResult.Recommendation = new ProviderModelBenchmarkRecommendation
                    {
                        ProfileName = best.ProfileName,
                        ContextTokens = context,
                        OutputTokens = output,
                        OllamaNumGpu = best.OllamaNumGpu,
                        EmpiricalScore = best.Score,
                        CouncilScore = councilScore,
                        Rationale = successfulReviews.Count == 0
                            ? "The recommendation is based on bounded deterministic quality and latency measurements. No independent reviewer completed a valid review."
                            : $"{successfulReviews.Count} council reviewer(s) agreed on the bounded provider-specific route. Their context/output recommendations were averaged and clamped to the user-selected limits."
                    };
                    Publish($"- Recommendation ready: **{best.ProfileName}** · context {context:N0} · output {output:N0} · empirical {best.Score:0.00} · council {councilScore:0.00}.");
                }
                catch (OperationCanceledException) when (runToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Provider benchmark failed for model identity {ModelIdentity}; prompts and responses were omitted.", target.StableId);
                    targetResult.Error = "The provider model benchmark failed. Review LocalGPT logs for the provider-qualified model identity.";
                    Publish("- Target failed because the provider-qualified benchmark raised an exception. Prompt and model output content were omitted from the live transcript.");
                }
            }

            report.CompletedAtUtc = DateTimeOffset.UtcNow;
            var succeeded = report.Targets.Count(target => string.IsNullOrWhiteSpace(target.Error)
                && !string.IsNullOrWhiteSpace(target.Recommendation.ProfileName));
            Publish($"\n## Benchmark Council completed\n\n{succeeded}/{report.Targets.Count} target model(s) produced a recommendation. No provider or preset settings were changed automatically.");
            logger.LogInformation(
                "Completed Provider Benchmark Council run {RunId}; {SucceededCount}/{TargetCount} target(s) produced recommendations.",
                report.RunId,
                succeeded,
                report.Targets.Count);
            return report;
        }
        catch (OperationCanceledException) when (runToken.IsCancellationRequested)
        {
            report.CompletedAtUtc = DateTimeOffset.UtcNow;
            Publish("\n## Benchmark Council stopped\n\nThe run was cancelled from its initiating control or from the live Council session controls. No recommendations were applied.");
            logger.LogInformation("Provider Benchmark Council run {RunId} was cancelled.", report.RunId);
            throw;
        }
        catch (Exception exception)
        {
            report.CompletedAtUtc = DateTimeOffset.UtcNow;
            logger.LogError(exception, "Provider Benchmark Council run {RunId} failed; prompts and model output were omitted.", report.RunId);
            Publish("\n## Benchmark Council failed\n\nThe run ended unexpectedly. Existing provider and preset settings were left unchanged; review the LocalGPT application log for technical details.");
            throw;
        }
        finally
        {
            if (request.OwnLiveSession)
                liveSessions.Complete(report.RunId);
        }
    }

    /// <summary>
    /// Persists the measured benchmark result as a hardware-spooler performance profile. This path intentionally
    /// does not change selected Council members or provider-global settings.
    /// </summary>
    /// <param name="report">Report value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="presetName">Preset name value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The hardware performance preset produced by the operation.</returns>
    public async Task<HardwarePerformancePreset> SavePerformancePresetAsync(
        ProviderModelBenchmarkReport report,
        string presetName,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await performancePresets.SaveBenchmarkResultAsync(
                report, presetName, userConfirmed, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Saving benchmark {BenchmarkRunId} as a performance preset was cancelled.", report?.RunId);
            else
                logger.LogError(exception, "Saving benchmark {BenchmarkRunId} as a performance preset failed.", report?.RunId);
            throw;
        }
    }

    /// <summary>
    /// Applies recommendations as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="report">Report value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="presetName">Preset name value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="makeDefault">Value indicating whether make default should apply to this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<CouncilModelPreset>> ApplyRecommendationsAsync(
        ProviderModelBenchmarkReport report,
        string presetName,
        bool makeDefault,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
    try
    {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before benchmark recommendations are applied.");
            ArgumentNullException.ThrowIfNull(report);
            var recommended = report.Targets
                .Where(target => string.IsNullOrWhiteSpace(target.Error) && !string.IsNullOrWhiteSpace(target.Recommendation.ProfileName))
                .ToList();
            if (recommended.Count == 0)
                throw new InvalidOperationException("The benchmark report contains no successful recommendation to apply.");

            if (!report.AppliedPerformancePresetId.HasValue)
                await SavePerformancePresetAsync(report, presetName, userConfirmed: true, cancellationToken).ConfigureAwait(false);

            var routes = recommended.Select(target => new OneWireCouncilModelRoute
            {
                ModelName = target.Model.SelectionKey,
                ProviderKind = target.Model.ProviderKind,
                ProviderName = target.Model.ProviderName,
                ProviderEndpoint = target.Model.Endpoint,
                ProviderModelName = target.Model.ModelName,
                HardwareKind = OneWireHardwareKind.Auto,
                HardwareIndex = -1,
                HardwareName = "Benchmark recommendation",
                MinOutputTokens = Math.Min(256, target.Recommendation.OutputTokens),
                MaxOutputTokens = target.Recommendation.OutputTokens,
                MinContextTokens = Math.Min(2048, target.Recommendation.ContextTokens),
                MaxContextTokens = target.Recommendation.ContextTokens,
                OllamaNumGpu = target.Model.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                    ? target.Recommendation.OllamaNumGpu
                    : null,
                IsEnabled = true,
                MaxConcurrentModelsOnLane = 1
            }).ToList();

            var normalizedName = string.IsNullOrWhiteSpace(presetName)
                ? $"Provider council benchmark {DateTimeOffset.Now:yyyy-MM-dd HHmm}"
                : presetName.Trim();
            normalizedName = normalizedName[..Math.Min(normalizedName.Length, 160)];
            var preset = new CouncilModelPreset
            {
                Name = normalizedName,
                Description = $"User-approved provider-qualified benchmark {report.RunId}. Same-named models remain separated by provider and endpoint.",
                ModelNamesJson = JsonSerializer.Serialize(routes.Select(route => route.ModelName).ToList()),
                ModelRoutesJson = JsonSerializer.Serialize(routes),
                AllowParallelHardwareRoads = false,
                MaxOutputTokens = routes.Max(route => route.MaxOutputTokens),
                MaxContextTokens = routes.Max(route => route.MaxContextTokens),
                MaxParallelModels = 1,
                OllamaNumGpu = routes.Count == 1 ? routes[0].OllamaNumGpu : null,
                IncludeMemory = false,
                GenerateArtifacts = false,
                CreateProjectPerRun = false,
                IsDefault = makeDefault,
                IsUserApproved = true
            };
            var saved = await modelPresets.SavePresetAsync(preset, userConfirmed, cancellationToken).ConfigureAwait(false);
            report.AppliedPresetId = saved.Id;
            report.AppliedPresetName = saved.Name;
            return [saved];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(ApplyRecommendationsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(ApplyRecommendationsAsync)} failed.");
        throw;
    }
}
}
