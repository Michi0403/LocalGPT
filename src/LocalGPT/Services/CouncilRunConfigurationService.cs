using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates council run configuration behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="hardwareRoadPlanner">Council hardware road planner dependency used by the council run configuration workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilRunConfigurationService(
    ICouncilHardwareRoadPlanner hardwareRoadPlanner,
    ILogger<CouncilRunConfigurationService> logger) : ICouncilRunConfigurationService
{
    /// <summary>
    /// Stores the in-memory runs collection maintained internally by <see cref="CouncilRunConfigurationService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CouncilRunState> runs = new();
    /// <summary>
    /// Stores the internal preparation sync root state used by <see cref="CouncilRunConfigurationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object preparationSyncRoot = new();
    /// <summary>
    /// Stores the internal preparation configuration state used by <see cref="CouncilRunConfigurationService"/> while executing its surrounding workflow.
    /// </summary>
    private CouncilPreparationConfiguration? preparationConfiguration;

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="CouncilRunConfigurationService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action<Guid>? Changed;

    /// <summary>
    /// Performs ensure as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="participants">String dependency used by the council run configuration workflow to provide the corresponding application capability.</param>
    /// <returns>The council run configuration snapshot produced by the operation.</returns>
    public CouncilRunConfigurationSnapshot Ensure(
        MultiModelCouncilRequest request,
        IReadOnlyCollection<string> participants)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(participants);

            var state = runs.GetOrAdd(
                request.RunId,
                _ => new CouncilRunState(
                    request.RunId,
                    participants,
                    request.ModelRoutes.Select(CloneRoute),
                    request.ResourceLoadPercent,
                    request.MaxOutputTokens,
                    request.MaxContextTokens,
                    request.OllamaNumGpu is < 0 ? 0 : request.OllamaNumGpu,
                    request.AllowParallelHardwareRoads,
                    request.MaxParallelModels,
                    request.ModelTimeoutSeconds)
                {
                    CouncilTeamKey = string.IsNullOrWhiteSpace(request.CouncilTeamKey) ? "general" : request.CouncilTeamKey.Trim(),
                    ModelPresetId = request.ModelPresetId,
                    HardwarePerformancePresetId = request.HardwarePerformancePresetId,
                    CritiqueRounds = Math.Max(0, request.MaxRounds),
                    IncludeMemory = request.IncludeMemory,
                    CreateProjectPerRun = request.CreateProjectForRun
                });

            lock (state.SyncRoot)
            {
                state.Participants = participants
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                // The first Ensure call captures the request as the run's configuration snapshot.
                // Later Ensure calls may refine the participant list, but must not overwrite
                // user edits that reached this running session before execution begins.
                state.IsRunning = true;
                return CreateSnapshotLocked(state);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(Ensure)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(Ensure)} failed.");
        throw;
    }
}


    /// <summary>
    /// Retrieves preparation as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The council preparation configuration produced by the operation.</returns>
    public CouncilPreparationConfiguration? GetPreparation()
    {
    try
    {
            lock (preparationSyncRoot)
                return preparationConfiguration is null ? null : ClonePreparation(preparationConfiguration);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(GetPreparation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(GetPreparation)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists preparation as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <returns>The council preparation configuration produced by the operation.</returns>
    public CouncilPreparationConfiguration SavePreparation(CouncilPreparationConfiguration configuration)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(configuration);
            var normalized = NormalizePreparation(configuration);
            lock (preparationSyncRoot)
                preparationConfiguration = normalized;

            logger.LogDebug(
                "Captured the process-local Council preparation configuration for {ModelCount} model(s); running Council snapshots remain isolated.",
                normalized.ModelNames.Count);
            return ClonePreparation(normalized);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(SavePreparation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(SavePreparation)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs get as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The council run configuration snapshot produced by the operation.</returns>
    public CouncilRunConfigurationSnapshot? Get(Guid runId)
    {
    try
    {
            if (!runs.TryGetValue(runId, out var state))
                return null;

            lock (state.SyncRoot)
                return CreateSnapshotLocked(state);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(Get)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(Get)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs update as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="routes">One wire council model route dependency used by the council run configuration workflow to provide the corresponding application capability.</param>
    /// <param name="resourceLoadPercent">Resource load percent value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="requestedMaxOutputTokens">Requested max output tokens value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="requestedMaxContextTokens">Requested max context tokens value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="fallbackOllamaNumGpu">Fallback ollama num gpu value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="allowParallelHardwareRoads">Value indicating whether allow parallel hardware roads should apply to this operation.</param>
    /// <param name="maxParallelModels">Max parallel models value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Update(
        Guid runId,
        IReadOnlyCollection<OneWireCouncilModelRoute> routes,
        int resourceLoadPercent,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int? fallbackOllamaNumGpu,
        bool allowParallelHardwareRoads,
        int maxParallelModels,
        int modelTimeoutSeconds)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(routes);
            if (!runs.TryGetValue(runId, out var state))
                return false;

            long revision;
            lock (state.SyncRoot)
            {
                if (!state.IsRunning)
                    return false;

                state.ModelRoutes = routes.Select(CloneRoute).ToList();
                state.ResourceLoadPercent = Math.Clamp((int)Math.Round(resourceLoadPercent / 5d) * 5, 0, 100);
                state.RequestedMaxOutputTokens = Math.Max(1, requestedMaxOutputTokens);
                state.RequestedMaxContextTokens = Math.Max(256, requestedMaxContextTokens);
                state.FallbackOllamaNumGpu = fallbackOllamaNumGpu is < 0 ? 0 : fallbackOllamaNumGpu;
                state.AllowParallelHardwareRoads = allowParallelHardwareRoads;
                state.MaxParallelModels = Math.Max(1, maxParallelModels);
                state.ModelTimeoutSeconds = Math.Clamp(modelTimeoutSeconds, 30, 1800);
                revision = ++state.Revision;
                PulseLocked(state);
            }

            logger.LogInformation(
                "Updated run-scoped Council model settings, token ceilings and fallback acceleration for run {RunId} to revision {Revision}; saved presets and other runs were not changed.",
                runId,
                revision);
            Changed?.Invoke(runId);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(Update)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(Update)} failed.");
        throw;
    }
}

    /// <summary>Updates the hardware-performance preset identity retained by a running Council configuration snapshot.</summary>
    /// <param name="runId">Identifier of the running Council to update.</param>
    /// <param name="hardwarePerformancePresetId">Saved performance-preset identifier, or <see langword="null"/> for custom running settings.</param>
    /// <returns><see langword="true"/> when the running snapshot was updated.</returns>
    public bool UpdateHardwarePerformancePresetIdentity(Guid runId, Guid? hardwarePerformancePresetId)
    {
        try
        {
            if (!runs.TryGetValue(runId, out var state))
                return false;

            long revision;
            lock (state.SyncRoot)
            {
                if (!state.IsRunning)
                    return false;

                state.HardwarePerformancePresetId = hardwarePerformancePresetId;
                revision = ++state.Revision;
                PulseLocked(state);
            }

            logger.LogInformation(
                "Updated running Council {RunId} hardware performance preset identity to {PresetId} at revision {Revision}.",
                runId,
                hardwarePerformancePresetId,
                revision);
            Changed?.Invoke(runId);
            return true;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Updating running Council {RunId} hardware performance preset identity was cancelled.", runId);
            else
                logger.LogError(exception, "Updating running Council {RunId} hardware performance preset identity failed.", runId);
            throw;
        }
    }

    /// <summary>
    /// Performs begin round as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council run configuration operation and used when producing its result.</param>
    public void BeginRound(Guid runId, int round, string phase)
    {
        if (!runs.TryGetValue(runId, out var state))
            return;

        CancellationTokenSource? previousRoundCancellation = null;
        lock (state.SyncRoot)
        {
            if (!state.IsRunning)
                return;

            if (state.CurrentRound == round &&
                string.Equals(state.CurrentPhase, phase, StringComparison.Ordinal) &&
                !state.RoundCancellation.IsCancellationRequested)
            {
                return;
            }

            previousRoundCancellation = state.RoundCancellation;
            state.RoundCancellation = new CancellationTokenSource();
            state.CurrentRound = round;
            state.CurrentPhase = phase;
            state.IsRoundSkipRequested = false;
            PulseLocked(state);
        }

        try
        {
            previousRoundCancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            previousRoundCancellation?.Dispose();
        }

        logger.LogInformation(
            "Council run {RunId} entered round {Round}, phase {Phase}; session-only model settings remain isolated to this run.",
            runId,
            round,
            phase);
        Changed?.Invoke(runId);
    }

    /// <summary>
    /// Retrieves round cancellation token as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>The cancellation token produced by the operation.</returns>
    public CancellationToken GetRoundCancellationToken(Guid runId, int round, string phase)
    {
    try
    {
            if (!runs.TryGetValue(runId, out var state))
                return CancellationToken.None;

            lock (state.SyncRoot)
            {
                return state.IsRunning &&
                    state.CurrentRound == round &&
                    string.Equals(state.CurrentPhase, phase, StringComparison.Ordinal)
                        ? state.RoundCancellation.Token
                        : CancellationToken.None;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(GetRoundCancellationToken)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(GetRoundCancellationToken)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether round skip requested as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsRoundSkipRequested(Guid runId, int round, string phase)
    {
    try
    {
            if (!runs.TryGetValue(runId, out var state))
                return false;

            lock (state.SyncRoot)
            {
                return state.IsRunning &&
                    state.IsRoundSkipRequested &&
                    state.CurrentRound == round &&
                    string.Equals(state.CurrentPhase, phase, StringComparison.Ordinal);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(IsRoundSkipRequested)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(IsRoundSkipRequested)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs request skip current round as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool RequestSkipCurrentRound(Guid runId)
    {
        if (!runs.TryGetValue(runId, out var state))
            return false;

        int round;
        string phase;
        CancellationTokenSource roundCancellation;
        lock (state.SyncRoot)
        {
            if (!state.IsRunning || state.CurrentRound < 0 || state.IsRoundSkipRequested)
                return false;

            state.IsRoundSkipRequested = true;
            round = state.CurrentRound;
            phase = state.CurrentPhase;
            roundCancellation = state.RoundCancellation;
            PulseLocked(state);
        }

        try
        {
            roundCancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }

        logger.LogInformation(
            "The user requested that Council run {RunId} skip round {Round}, phase {Phase}. Other runs are unaffected.",
            runId,
            round,
            phase);
        Changed?.Invoke(runId);
        return true;
    }

    /// <summary>
    /// Performs acquire model request as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="modelName">Model name value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="fallbackPlan">Fallback plan value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i council model request lease produced by the operation.</returns>
    public async ValueTask<ICouncilModelRequestLease> AcquireModelRequestAsync(
        Guid runId,
        string modelName,
        CouncilHardwareRoadPlan fallbackPlan,
        CancellationToken cancellationToken = default)
    {
    try
    {
            if (!runs.TryGetValue(runId, out var state))
                return new CouncilModelRequestLease(fallbackPlan, revision: 0, isEnabled: true, release: null);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CouncilRunPlanCandidate candidate;
                Task waitTask;
                lock (state.SyncRoot)
                {
                    if (!state.IsRunning)
                        return new CouncilModelRequestLease(fallbackPlan, state.Revision, isEnabled: true, release: null);

                    candidate = BuildCandidateLocked(state, modelName, fallbackPlan);
                    if (!candidate.IsEnabled)
                        return new CouncilModelRequestLease(candidate.Plan, candidate.Revision, isEnabled: false, release: null);

                    var aiHostKey = GetCouncilExecutionHostKey(modelName);
                    var laneKey = state.AllowParallelHardwareRoads
                        ? $"{aiHostKey}|{candidate.Plan.LaneKey}"
                        : $"{aiHostKey}|council:single-lane";
                    var laneCapacity = state.AllowParallelHardwareRoads
                        ? Math.Max(1, candidate.Plan.MaxConcurrentModelsOnLane)
                        : 1;
                    var activeCount = state.ActiveLaneCounts.GetValueOrDefault(laneKey);
                    if (activeCount < laneCapacity)
                    {
                        state.ActiveLaneCounts[laneKey] = activeCount + 1;
                        return new CouncilModelRequestLease(
                            candidate.Plan,
                            candidate.Revision,
                            isEnabled: true,
                            release: () => Release(state, laneKey));
                    }

                    waitTask = state.ChangeSignal.Task;
                }

                await waitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(AcquireModelRequestAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(AcquireModelRequestAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves council execution host key as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GetCouncilExecutionHostKey(string modelName)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            if (identity.TryParseSelectionKey(modelName, out var reference) &&
                Uri.TryCreate(reference.Endpoint, UriKind.Absolute, out var endpoint))
            {
                var host = string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    ? "127.0.0.1"
                    : endpoint.Host;
                return string.IsNullOrWhiteSpace(host)
                    ? "provider:unknown-host"
                    : host.Trim().ToLowerInvariant();
            }

            return "legacy-or-unqualified-host";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not resolve AI host identity for Council member {ModelName}; using the legacy host lane.", modelName);
            return "legacy-or-unqualified-host";
        }
    }

    /// <summary>
    /// Performs complete as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    public void Complete(Guid runId)
    {
        if (!runs.TryRemove(runId, out var state))
            return;

        lock (state.SyncRoot)
        {
            state.IsRunning = false;
            try
            {
                state.RoundCancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            PulseLocked(state);
        }

        state.RoundCancellation.Dispose();

        logger.LogInformation("Released run-scoped Council model settings for completed run {RunId}.", runId);
        Changed?.Invoke(runId);
    }

    /// <summary>
    /// Builds candidate locked as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="state">State value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="modelName">Model name value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="fallbackPlan">Fallback plan value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>The council run plan candidate produced by the operation.</returns>
    private CouncilRunPlanCandidate BuildCandidateLocked(
        CouncilRunState state,
        string modelName,
        CouncilHardwareRoadPlan fallbackPlan)
    {
    try
    {
            var route = state.ModelRoutes.LastOrDefault(item =>
                string.Equals(item.ModelName, modelName, StringComparison.OrdinalIgnoreCase));
            var isEnabled = route?.IsEnabled ?? true;
            var plans = hardwareRoadPlanner.BuildPlans(
                state.ModelRoutes,
                [modelName],
                state.RequestedMaxOutputTokens,
                state.RequestedMaxContextTokens,
                state.ResourceLoadPercent,
                state.FallbackOllamaNumGpu);
            var plan = plans.TryGetValue(modelName, out var configuredPlan)
                ? configuredPlan
                : fallbackPlan;
            return new CouncilRunPlanCandidate(plan, state.Revision, isEnabled);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(BuildCandidateLocked)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(BuildCandidateLocked)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs release as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="state">State value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="laneKey">Lane key value supplied to the council run configuration operation and used when producing its result.</param>
    private void Release(CouncilRunState state, string laneKey)
    {
    try
    {
            lock (state.SyncRoot)
            {
                var activeCount = state.ActiveLaneCounts.GetValueOrDefault(laneKey);
                if (activeCount <= 1)
                    state.ActiveLaneCounts.Remove(laneKey);
                else
                    state.ActiveLaneCounts[laneKey] = activeCount - 1;
                PulseLocked(state);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(Release)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(Release)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs pulse locked as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="state">State value supplied to the council run configuration operation and used when producing its result.</param>
    private void PulseLocked(CouncilRunState state)
    {
    try
    {
            var previous = state.ChangeSignal;
            state.ChangeSignal = CreateSignal();
            previous.TrySetResult(true);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(PulseLocked)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(PulseLocked)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates signal as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The task completion source bool produced by the operation.</returns>
    private TaskCompletionSource<bool> CreateSignal() {
    try
    {
        return new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(CreateSignal)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(CreateSignal)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates snapshot locked as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="state">State value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>The council run configuration snapshot produced by the operation.</returns>
    private CouncilRunConfigurationSnapshot CreateSnapshotLocked(CouncilRunState state) {
    try
    {
        return new(
            state.RunId,
            state.Revision,
            state.Participants.ToList(),
            state.ModelRoutes.Select(CloneRoute).ToList(),
            state.ResourceLoadPercent,
            state.RequestedMaxOutputTokens,
            state.RequestedMaxContextTokens,
            state.FallbackOllamaNumGpu,
            state.AllowParallelHardwareRoads,
            state.MaxParallelModels,
            state.ModelTimeoutSeconds,
            state.CurrentRound,
            state.CurrentPhase,
            state.IsRoundSkipRequested,
            state.IsRunning)
        {
            CouncilTeamKey = state.CouncilTeamKey,
            ModelPresetId = state.ModelPresetId,
            HardwarePerformancePresetId = state.HardwarePerformancePresetId,
            CritiqueRounds = state.CritiqueRounds,
            IncludeMemory = state.IncludeMemory,
            CreateProjectPerRun = state.CreateProjectPerRun
        };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(CreateSnapshotLocked)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(CreateSnapshotLocked)} failed.");
        throw;
    }
}


    /// <summary>
    /// Normalizes preparation as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <returns>The council preparation configuration produced by the operation.</returns>
    private CouncilPreparationConfiguration NormalizePreparation(CouncilPreparationConfiguration configuration)
    {
    try
    {
            var modelNames = configuration.ModelNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var modelRoutes = configuration.ModelRoutes
                .Where(route => route is not null && !string.IsNullOrWhiteSpace(route.ModelName))
                .Select(CloneRoute)
                .ToList();
            return new CouncilPreparationConfiguration(
                modelNames,
                modelRoutes,
                Math.Clamp((int)Math.Round(configuration.ResourceLoadPercent / 5d) * 5, 0, 100),
                Math.Max(1, configuration.MaxOutputTokens),
                Math.Max(256, configuration.MaxContextTokens),
                configuration.OllamaNumGpu is < 0 ? 0 : configuration.OllamaNumGpu,
                configuration.AllowParallelHardwareRoads,
                Math.Max(1, configuration.MaxParallelModels),
                Math.Clamp(configuration.ModelTimeoutSeconds, 30, 1800),
                Math.Max(0, configuration.CritiqueRounds),
                configuration.IncludeMemory,
                configuration.CreateProjectPerRun,
                string.IsNullOrWhiteSpace(configuration.CouncilTeamKey) ? "general" : configuration.CouncilTeamKey.Trim())
            {
                ModelPresetId = configuration.ModelPresetId,
                HardwarePerformancePresetId = configuration.HardwarePerformancePresetId
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(NormalizePreparation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(NormalizePreparation)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone preparation as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <returns>The council preparation configuration produced by the operation.</returns>
    private CouncilPreparationConfiguration ClonePreparation(CouncilPreparationConfiguration configuration) {
    try
    {
        return new(
            configuration.ModelNames.ToList(),
            configuration.ModelRoutes.Select(CloneRoute).ToList(),
            configuration.ResourceLoadPercent,
            configuration.MaxOutputTokens,
            configuration.MaxContextTokens,
            configuration.OllamaNumGpu,
            configuration.AllowParallelHardwareRoads,
            configuration.MaxParallelModels,
            configuration.ModelTimeoutSeconds,
            configuration.CritiqueRounds,
            configuration.IncludeMemory,
            configuration.CreateProjectPerRun,
            configuration.CouncilTeamKey)
        {
            ModelPresetId = configuration.ModelPresetId,
            HardwarePerformancePresetId = configuration.HardwarePerformancePresetId
        };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(ClonePreparation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(ClonePreparation)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone route as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="route">Route value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>The one wire council model route produced by the operation.</returns>
    private OneWireCouncilModelRoute CloneRoute(OneWireCouncilModelRoute route) {
    try
    {
        return new()
    {
        ModelName = route.ModelName,
        ProviderKind = route.ProviderKind,
        ProviderName = route.ProviderName,
        ProviderEndpoint = route.ProviderEndpoint,
        ProviderModelName = route.ProviderModelName,
        HardwareKind = route.HardwareKind,
        HardwareIndex = route.HardwareIndex,
        HardwareName = route.HardwareName,
        MinOutputTokens = route.MinOutputTokens,
        MaxOutputTokens = route.MaxOutputTokens,
        MinContextTokens = route.MinContextTokens,
        MaxContextTokens = route.MaxContextTokens,
        OllamaNumGpu = route.OllamaNumGpu,
        LoadPercentOverride = route.LoadPercentOverride,
        SelfReportedDxFunctions = [.. route.SelfReportedDxFunctions],
        SelfReportedControllerMethods = [.. route.SelfReportedControllerMethods],
        SelfReportedOrganicCapabilities = [.. route.SelfReportedOrganicCapabilities],
        SelfReportedSkills = [.. route.SelfReportedSkills],
        IsEnabled = route.IsEnabled,
        MaxConcurrentModelsOnLane = route.MaxConcurrentModelsOnLane
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(CloneRoute)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRunConfigurationService)}.{nameof(CloneRoute)} failed.");
        throw;
    }
}
}
