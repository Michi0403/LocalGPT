using LocalGPT.BusinessObjects;
using System.Globalization;

namespace LocalGPT.Services.GoalExecution;

/// <summary>
/// AI host outage handling of the goal recovery helper: one bounded wait for the first outage of a host in a run, then a single
/// reachability check, then a run-scoped "down" mark with an offer to exclude the host for the run.
/// </summary>
public sealed partial class GoalRecoveryHelperService
{
    /// <inheritdoc />
    public async Task<GoalHostFailureDecision> HandleHostFailureAsync(GoalHostFailureReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        var state = GetRunState(report.RunId);
        var host = state.Hosts.GetOrAdd(report.HostKey ?? string.Empty, _ => new GoalRecoveryHostState());
        await host.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var texts = Texts;
            await RefreshHostExclusionAsync(report.RunId, report.HostKey ?? string.Empty, host, cancellationToken).ConfigureAwait(false);
            if (host.IsExcluded)
            {
                return new GoalHostFailureDecision
                {
                    HostUnavailable = true,
                    Notice = Render(texts.HostExcludedStepText, ("Model", report.ModelName), ("Host", report.HostKey)),
                    ExclusionRequestId = host.Exclusion.RequestId
                };
            }

            // The single check decides whether this is a host outage at all. A host that answers with the model needs no wait.
            var reachability = await providerModels.ProbeHostOnceAsync(report.Model, cancellationToken).ConfigureAwait(false);
            if (reachability is ProviderHostReachability.ModelAvailable or ProviderHostReachability.NotSupported)
                return MarkHostAnswering(report.RunId, report.HostKey ?? string.Empty, host, texts);

            var waitSeconds = FirstOutageWaitSeconds();
            if (reachability == ProviderHostReachability.ModelMissing)
            {
                // The host answers; this is a model problem, not an outage. Keep the previous bounded same-model wait.
                var modelBack = await providerModels
                    .WaitForAvailabilityAsync(report.Model, TimeSpan.FromSeconds(waitSeconds), cancellationToken)
                    .ConfigureAwait(false);
                logger.LogInformation(
                    "Goal recovery for run {RunId}: host {Host} answers but {Model} was {Outcome} within {Seconds}s.",
                    report.RunId,
                    report.HostKey,
                    report.ModelName,
                    modelBack ? "listed again" : "not listed",
                    waitSeconds);
                return new GoalHostFailureDecision { RetryNow = modelBack };
            }

            if (host.FirstOutageAtUtc is null)
            {
                // First outage of this host in this run: one bounded wait, then one more check ("ping once").
                host.FirstOutageAtUtc = DateTime.UtcNow;
                report.Progress?.Invoke(Render(
                    texts.HostWaitingNotice,
                    ("Model", report.ModelName),
                    ("Host", report.HostKey),
                    ("Seconds", waitSeconds.ToString(CultureInfo.InvariantCulture))));
                logger.LogWarning(
                    "Goal recovery for run {RunId}: AI host {Host} did not answer after {Model} failed in {Phase}; waiting up to {Seconds}s once for this run.",
                    report.RunId,
                    report.HostKey,
                    report.ModelName,
                    report.Phase,
                    waitSeconds);
                var back = await providerModels
                    .WaitForAvailabilityAsync(report.Model, TimeSpan.FromSeconds(waitSeconds), cancellationToken)
                    .ConfigureAwait(false);
                if (back)
                    return MarkHostAnswering(report.RunId, report.HostKey ?? string.Empty, host, texts);

                reachability = await providerModels.ProbeHostOnceAsync(report.Model, cancellationToken).ConfigureAwait(false);
                if (reachability is ProviderHostReachability.ModelAvailable or ProviderHostReachability.NotSupported)
                    return MarkHostAnswering(report.RunId, report.HostKey ?? string.Empty, host, texts);
                if (reachability == ProviderHostReachability.ModelMissing)
                    return new GoalHostFailureDecision();
            }

            var wasDown = host.IsDown;
            await MarkHostDownAsync(report, host, waitSeconds, cancellationToken).ConfigureAwait(false);
            return new GoalHostFailureDecision
            {
                HostUnavailable = true,
                Notice = host.IsExcluded
                    ? Render(texts.HostExcludedStepText, ("Model", report.ModelName), ("Host", report.HostKey))
                    : wasDown
                        ? Render(texts.HostUnreachableStepText, ("Model", report.ModelName), ("Host", report.HostKey))
                        : Render(
                            texts.HostDownNotice,
                            ("Model", report.ModelName),
                            ("Host", report.HostKey),
                            ("Seconds", waitSeconds.ToString(CultureInfo.InvariantCulture))),
                ExclusionRequestId = host.Exclusion.RequestId
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Goal recovery host handling was canceled for run {RunId}, host {Host}.", report.RunId, report.HostKey);
            throw;
        }
        catch (Exception exception)
        {
            // Fall back to the member's ordinary failure; never stall the run because the helper failed.
            logger.LogError(exception, "Goal recovery host handling failed for run {RunId}, host {Host}, model {Model}.", report.RunId, report.HostKey, report.ModelName);
            return new GoalHostFailureDecision();
        }
        finally
        {
            host.Gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<GoalHostAdmission> AdmitHostAsync(Guid runId, string hostKey, string modelName, ProviderModelReference model, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(model);
            if (string.IsNullOrWhiteSpace(hostKey)
                || !runs.TryGetValue(runId, out var state)
                || !state.Hosts.TryGetValue(hostKey, out var host)
                || (!host.IsDown && !host.IsExcluded))
            {
                return new GoalHostAdmission { Kind = GoalHostAdmissionKind.Allowed };
            }

            var texts = Texts;
            await host.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await RefreshHostExclusionAsync(runId, hostKey, host, cancellationToken).ConfigureAwait(false);
                if (host.IsExcluded)
                {
                    logger.LogInformation("Goal recovery for run {RunId}: {Model} not started, AI host {Host} is excluded for this run.", runId, modelName, hostKey);
                    return new GoalHostAdmission
                    {
                        Kind = GoalHostAdmissionKind.ExcludedForRun,
                        Message = Render(texts.HostExcludedStepText, ("Model", modelName), ("Host", hostKey))
                    };
                }

                if (!host.IsDown)
                    return new GoalHostAdmission { Kind = GoalHostAdmissionKind.Allowed };

                var reachability = await providerModels.ProbeHostOnceAsync(model, cancellationToken).ConfigureAwait(false);
                if (reachability == ProviderHostReachability.HostUnreachable)
                {
                    logger.LogInformation("Goal recovery for run {RunId}: {Model} not started, AI host {Host} still does not answer.", runId, modelName, hostKey);
                    return new GoalHostAdmission
                    {
                        Kind = GoalHostAdmissionKind.HostUnreachable,
                        Message = Render(texts.HostUnreachableStepText, ("Model", modelName), ("Host", hostKey))
                    };
                }

                MarkHostAnswering(runId, hostKey, host, texts);
                return new GoalHostAdmission { Kind = GoalHostAdmissionKind.Allowed };
            }
            finally
            {
                host.Gate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Goal recovery host admission was canceled for run {RunId}, host {Host}.", runId, hostKey);
            throw;
        }
        catch (Exception exception)
        {
            // Admission is an optimization of the outage path; on its own failure the member starts normally.
            logger.LogError(exception, "Goal recovery host admission failed for run {RunId}, host {Host}; starting {Model} normally.", runId, hostKey, modelName);
            return new GoalHostAdmission { Kind = GoalHostAdmissionKind.Allowed };
        }
    }

    /// <inheritdoc />
    public GoalHostRunStatus GetHostStatus(Guid runId, string hostKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hostKey)
                || !runs.TryGetValue(runId, out var state)
                || !state.Hosts.TryGetValue(hostKey, out var host))
            {
                return new GoalHostRunStatus { HostKey = hostKey ?? string.Empty };
            }

            return new GoalHostRunStatus
            {
                HostKey = hostKey,
                IsDown = host.IsDown,
                IsExcluded = host.IsExcluded,
                FirstOutageAtUtc = host.FirstOutageAtUtc,
                ExclusionRequestId = host.Exclusion.RequestId
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery host status failed for run {RunId}, host {Host}.", runId, hostKey);
            throw;
        }
    }

    /// <inheritdoc />
    public string BuildHostDownWarning(string hostKey)
    {
        try
        {
            return Render(Texts.HostDownWarning, ("Host", hostKey));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the goal recovery host warning failed for host {Host}.", hostKey);
            throw;
        }
    }

    /// <summary>Returns whether a host may receive members in a run (not marked down and not excluded).</summary>
    /// <param name="state">Recovery state of the run.</param>
    /// <param name="hostKey">AI host identity.</param>
    /// <returns><see langword="true"/> when the host is usable.</returns>
    private bool IsHostUsable(GoalRecoveryRunState state, string hostKey)
    {
        try
        {
            return string.IsNullOrWhiteSpace(hostKey)
                || !state.Hosts.TryGetValue(hostKey, out var host)
                || (!host.IsDown && !host.IsExcluded);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery host usability check failed for host {Host}.", hostKey);
            throw;
        }
    }

    /// <summary>Returns the configured first-outage wait, falling back to the minimum availability wait.</summary>
    /// <returns>Seconds, at least 1.</returns>
    private int FirstOutageWaitSeconds()
    {
        try
        {
            var parameters = ExecutionParameters;
            var seconds = parameters.FirstHostOutageWaitSeconds > 0
                ? parameters.FirstHostOutageWaitSeconds
                : parameters.MinimumAvailabilityWaitSeconds;
            return Math.Max(1, seconds);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the goal recovery first-outage wait failed.");
            throw;
        }
    }

    /// <summary>Clears a run-scoped down mark after the host answered and returns a retry decision.</summary>
    /// <param name="runId">Council run identifier.</param>
    /// <param name="hostKey">AI host identity.</param>
    /// <param name="host">Host state.</param>
    /// <param name="texts">Helper texts.</param>
    /// <returns>A decision that allows the member's own retry now.</returns>
    private GoalHostFailureDecision MarkHostAnswering(Guid runId, string hostKey, GoalRecoveryHostState host, GoalRecoveryHelperTexts texts)
    {
        try
        {
            if (!host.IsDown)
                return new GoalHostFailureDecision { RetryNow = true };

            host.IsDown = false;
            logger.LogInformation("Goal recovery for run {RunId}: AI host {Host} answers again; its members start normally.", runId, hostKey);
            return new GoalHostFailureDecision
            {
                RetryNow = true,
                Notice = Render(texts.HostBackNotice, ("Host", hostKey))
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery could not clear the down mark of host {Host} in run {RunId}.", hostKey, runId);
            throw;
        }
    }

    /// <summary>Marks a host down for the run and offers excluding it for the run once.</summary>
    /// <param name="report">Failure that led to the mark.</param>
    /// <param name="host">Host state.</param>
    /// <param name="waitSeconds">Configured first-outage wait, shown in the offer.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the mark and the offer are recorded.</returns>
    private async Task MarkHostDownAsync(GoalHostFailureReport report, GoalRecoveryHostState host, int waitSeconds, CancellationToken cancellationToken)
    {
        try
        {
            var firstMark = !host.IsDown;
            host.IsDown = true;
            if (host.Exclusion.Spec is null)
            {
                var texts = Texts;
                var hostDigest = Digest(report.HostKey);
                host.Exclusion.Spec = new HumanApprovalRequestSpec(
                    CorrelationId: $"council:{report.RunId:N}:host-exclusion:{hostDigest}",
                    OperationKey: $"council.host.exclude-run.{report.RunId:N}.{hostDigest}",
                    Title: Render(texts.HostExclusionTitle, ("Host", report.HostKey)),
                    Description: Render(
                        texts.HostExclusionDescription,
                        ("Host", report.HostKey),
                        ("Model", report.ModelName),
                        ("Seconds", waitSeconds.ToString(CultureInfo.InvariantCulture))),
                    RiskLevel: "Low",
                    Source: nameof(GoalRecoveryHelperService),
                    RequestedBy: texts.ApprovalRequestedBy,
                    RequestedRole: texts.ApprovalRequestedRole,
                    CouncilRunId: report.RunId,
                    EarliestCouncilRound: 0,
                    RequiredBeforeCompletion: false,
                    IsSensitive: false,
                    SuggestedResponsesText: texts.HostExclusionSuggestedResponses,
                    ResponsePrompt: texts.HostExclusionResponsePrompt,
                    AllowFreeText: true);
            }

            await RefreshHostExclusionAsync(report.RunId, report.HostKey ?? string.Empty, host, cancellationToken).ConfigureAwait(false);
            if (firstMark)
            {
                logger.LogWarning(
                    "Goal recovery for run {RunId}: AI host {Host} marked down after the outage wait and one reachability check ({Model}, {Phase}); exclusion for this run offered as request {RequestId}.",
                    report.RunId,
                    report.HostKey,
                    report.ModelName,
                    report.Phase,
                    host.Exclusion.RequestId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery could not mark AI host {Host} down for run {RunId}.", report.HostKey, report.RunId);
            throw;
        }
    }

    /// <summary>Reads the user's decision on the host exclusion offer until it is decided; the decision is remembered for the run.</summary>
    /// <param name="runId">Council run identifier.</param>
    /// <param name="hostKey">AI host identity.</param>
    /// <param name="host">Host state.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the decision has been refreshed.</returns>
    private async Task RefreshHostExclusionAsync(Guid runId, string hostKey, GoalRecoveryHostState host, CancellationToken cancellationToken)
    {
        if (host.Exclusion.Spec is null || host.Exclusion.IsDecided)
            return;

        try
        {
            var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(host.Exclusion.Spec, cancellationToken: cancellationToken).ConfigureAwait(false);
            ApplyApprovalGate(host.Exclusion, gate);
            if (host.Exclusion.IsDecided)
            {
                host.IsExcluded = host.Exclusion.IsAuthorized;
                logger.LogInformation(
                    "Goal recovery for run {RunId}: the local user {Decision} AI host {Host} for this run (request {RequestId}).",
                    runId,
                    host.IsExcluded ? "excluded" : "kept",
                    hostKey,
                    host.Exclusion.RequestId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The inbox being unavailable keeps the host in the "check once before each member" state.
            logger.LogWarning(exception, "Goal recovery could not read the exclusion decision for AI host {Host} in run {RunId}.", hostKey, runId);
        }
    }

    /// <summary>Run-scoped availability state of one AI host.</summary>
    private sealed class GoalRecoveryHostState
    {
        /// <summary>Gets the gate that serializes outage handling for the host.</summary>
        /// <value>A single-entry semaphore.</value>
        public SemaphoreSlim Gate { get; } = new(1, 1);
        /// <summary>Gets or sets the time of the first observed outage in the run.</summary>
        /// <value>UTC time, or <see langword="null"/> before an outage.</value>
        public DateTime? FirstOutageAtUtc { get; set; }
        /// <summary>Gets or sets whether the host is marked down for the run.</summary>
        /// <value><see langword="true"/> while the host does not answer.</value>
        public bool IsDown { get; set; }
        /// <summary>Gets or sets whether the local user excluded the host for the run.</summary>
        /// <value><see langword="true"/> after an approved exclusion.</value>
        public bool IsExcluded { get; set; }
        /// <summary>Gets the exclusion offer and its remembered decision.</summary>
        /// <value>The decision memo.</value>
        public GoalRecoveryApprovalMemo Exclusion { get; } = new();
    }
}
