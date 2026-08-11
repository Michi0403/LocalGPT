using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;

namespace LocalGPT.Services;

public sealed class CouncilRunConfigurationService(
    ICouncilHardwareRoadPlanner hardwareRoadPlanner,
    ILogger<CouncilRunConfigurationService> logger) : ICouncilRunConfigurationService
{
    private readonly ConcurrentDictionary<Guid, CouncilRunState> runs = new();
    private readonly object preparationSyncRoot = new();
    private CouncilPreparationConfiguration? preparationConfiguration;

    public event Action<Guid>? Changed;

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
                    request.AllowParallelHardwareRoads));

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

    public bool Update(
        Guid runId,
        IReadOnlyCollection<OneWireCouncilModelRoute> routes,
        int resourceLoadPercent,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int? fallbackOllamaNumGpu,
        bool allowParallelHardwareRoads)
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
            state.CurrentRound,
            state.CurrentPhase,
            state.IsRoundSkipRequested,
            state.IsRunning);
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
                Math.Max(0, configuration.CritiqueRounds),
                configuration.IncludeMemory,
                configuration.CreateProjectPerRun,
                string.IsNullOrWhiteSpace(configuration.CouncilTeamKey) ? "general" : configuration.CouncilTeamKey.Trim());
    
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
            configuration.CritiqueRounds,
            configuration.IncludeMemory,
            configuration.CreateProjectPerRun,
            configuration.CouncilTeamKey);
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
