using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council game session behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilGameSessionService
    {
    /// <summary>
    /// Performs to snapshot as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    private CouncilGameSessionSnapshot ToSnapshot(CouncilGameSessionState session)
    {
    try
    {
            lock (session.SyncRoot)
                return ToSnapshotUnsafe(session);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ToSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ToSnapshot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs to snapshot unsafe as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    private CouncilGameSessionSnapshot ToSnapshotUnsafe(CouncilGameSessionState session) {
    try
    {
        return new()
    {
        Id = session.Id,
        Scene = session.Scene,
        SceneClock = session.SceneClock is null ? null : new GameSceneClock
        {
            Tick = session.SceneClock.Tick,
            FractionalRealSeconds = session.SceneClock.FractionalRealSeconds,
            WorldSeconds = session.SceneClock.WorldSeconds,
            CurrentTurn = session.SceneClock.CurrentTurn,
            CameraX = session.SceneClock.CameraX,
            CameraY = session.SceneClock.CameraY,
            ActorPositions = session.SceneClock.ActorPositions.ToDictionary(item => item.Key, item => item.Value),
            SceneVariables = new Dictionary<string, string>(session.SceneClock.SceneVariables, StringComparer.Ordinal)
        },
        SourceRevisionId = session.SourceRevisionId,
        GameKey = session.GameKey,
        TeamKey = session.TeamKey,
        ConversationId = session.ConversationId,
        CouncilRunId = session.CouncilRunId,
        ProjectId = session.ProjectId,
        ProjectVersion = session.ProjectVersion,
        DisplayName = session.DisplayName,
        Status = session.Status,
        ControlMode = session.ControlMode,
        AutoplayEnabled = session.AutoplayEnabled,
        AutoplayDelayMilliseconds = session.AutoplayDelayMilliseconds,
        HumanInputRequired = session.HumanInputRequired,
        InputReason = session.InputReason,
        CurrentTurnOwner = session.CurrentTurnOwner,
        DirectorMode = session.DirectorMode,
        GameDirectorName = session.GameDirectorName,
        GameDirectorModelName = session.GameDirectorModelName,
        CreatureDirectorCount = session.CreatureDirectorCount,
        LastDirectorDecision = session.LastDirectorDecision,
        LastDirectorPredictions = session.LastDirectorPredictions.Select(ClonePrediction).ToArray(),
        Turn = session.Turn,
        FrameWidth = session.FrameWidth,
        FrameHeight = session.FrameHeight,
        FrameText = session.FrameText,
        FrameCaption = session.FrameCaption,
        FrameRenderer = session.FrameRenderer,
        AsciiColorMode = session.AsciiColorMode,
        DefaultForegroundColor = session.DefaultForegroundColor,
        DefaultBackgroundColor = session.DefaultBackgroundColor,
        FrameStyleRuns = session.Scene is null ? ResolveFrameStyleRuns(session, session.FrameText).ToArray() : [],
        AnimationFrames = session.AnimationFrames.ToArray(),
        AnimationDelayMilliseconds = session.AnimationDelayMilliseconds,
        AnimationSubtitle = session.AnimationSubtitle,
        AnimationSubtitleHoldMilliseconds = session.AnimationSubtitleHoldMilliseconds,
        AnimationFrameStyleRuns = session.AnimationFrames.Select((frame, index) =>
            (IReadOnlyList<CouncilAsciiStyleRun>)(index < session.AnimationFrameStyleRuns.Count
                ? CloneAsciiStyleRuns(session.AnimationFrameStyleRuns[index])
                : BuildSemanticAsciiStyleRuns(session, frame))).ToArray(),
        AnimationSubtitleStyle = NormalizeAsciiStyle(session.AnimationSubtitleStyle, session.AsciiColorMode),
        LegalActions = session.LegalActions.ToArray(),
        InputBindings = session.InputBindings.Select(CloneBinding).ToArray(),
        LastAction = session.LastAction,
        LastActionBy = session.LastActionBy,
        CurrentLevel = session.Ruleset.Levels.Count == 0 ? 1 : CurrentLevelRow(session)!.LevelNumber,
        TotalLevels = Math.Max(1, session.Ruleset.Levels.Count),
        LevelName = CurrentLevelRow(session)?.Name ?? string.Empty,
        CampaignSeed = session.CampaignSeed,
        WorldMap = session.WorldMap.ToArray(),
        MapSeed = session.MapSeed,
        ScenarioPrompt = session.ScenarioPrompt,
        BlockedMoveStreak = session.BlockedMoveStreak,
        LastMoveBlocked = session.LastMoveBlocked,
        CreatedAtUtc = session.CreatedAtUtc,
        UpdatedAtUtc = session.UpdatedAtUtc,
        Ruleset = session.Ruleset,
        Actors = actorQuery.Clone(session.Actors),
        Markers = session.Markers.Select(marker => new GameMarkerState { Key = marker.Key, Glyph = marker.Glyph, X = marker.X, Y = marker.Y }).ToArray(),
        Variables = new Dictionary<string, string>(session.Variables, StringComparer.OrdinalIgnoreCase),
        Progression = CloneProgression(session.Progression),
        Timeline = session.Timeline.Select(CloneTimelineEntry).ToArray(),
        Message = session.Message,
        ViewKey = session.Ruleset.ViewKey,
        TurnModelKey = session.Ruleset.TurnModelKey,
        ActiveEntrantKeys = session.Progression.Match >= 0 && session.Progression.Match + 1 < session.Progression.RoundEntrantKeys.Count
            ? [session.Progression.RoundEntrantKeys[session.Progression.Match], session.Progression.RoundEntrantKeys[session.Progression.Match + 1]]
            : [],
        SupportsSeedReplay = !string.IsNullOrWhiteSpace(session.Ruleset.MapGeneratorKey)
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ToSnapshotUnsafe)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ToSnapshotUnsafe)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone binding as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="binding">Binding value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The runtime input binding definition produced by the operation.</returns>
    private RuntimeInputBindingDefinition CloneBinding(RuntimeInputBindingDefinition binding) {
    try
    {
        return new()
    {
        Action = binding.Action,
        DisplayName = binding.DisplayName,
        KeyboardKey = binding.KeyboardKey,
        GamepadButton = binding.GamepadButton,
        Description = binding.Description
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(CloneBinding)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(CloneBinding)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone prediction as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="prediction">Prediction value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The council game subdirector prediction produced by the operation.</returns>
    private CouncilGameSubdirectorPrediction ClonePrediction(CouncilGameSubdirectorPrediction prediction) {
    try
    {
        return new()
    {
        DirectorKey = prediction.DirectorKey,
        ActorKind = prediction.ActorKind,
        RuntimeClassKey = prediction.RuntimeClassKey,
        Prediction = prediction.Prediction,
        ConfidencePercent = prediction.ConfidencePercent,
        ActorInstances = prediction.ActorInstances.Select(CloneActorRuntime).ToArray()
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ClonePrediction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ClonePrediction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone actor runtime as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="actor">Actor value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The council game actor runtime descriptor produced by the operation.</returns>
    private CouncilGameActorRuntimeDescriptor CloneActorRuntime(CouncilGameActorRuntimeDescriptor actor) {
    try
    {
        return new()
    {
        InstanceKey = actor.InstanceKey,
        ActorKind = actor.ActorKind,
        RuntimeClassKey = actor.RuntimeClassKey,
        Archetype = actor.Archetype,
        CouncilRole = actor.CouncilRole,
        CouncilAssignmentGroup = actor.CouncilAssignmentGroup,
        CouncilAssignmentSlot = actor.CouncilAssignmentSlot
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(CloneActorRuntime)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(CloneActorRuntime)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs notify as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    private void Notify(Guid sessionId)
    {
        var listeners = Changed?.GetInvocationList().Cast<Action<Guid>>().ToArray() ?? [];
        foreach (var listener in listeners)
        {
            try { listener(sessionId); }
            catch (Exception ex) { logger.LogWarning(ex, "A Council game UI listener failed for session {GameSessionId}.", sessionId); }
        }
    }

    /// <summary>Returns the level row the session currently plays, or <c>null</c> when the game defines no levels.</summary>
    /// <param name="session">Session whose progression and ruleset are read.</param>
    /// <returns>The current level row.</returns>
    private GameProjectLevel? CurrentLevelRow(CouncilGameSessionState session)
    {
        try
        {
            var levels = session.Ruleset.Levels.OrderBy(level => level.LevelIndex).ToList();
            return levels.Count == 0 ? null : levels[Math.Clamp(session.Progression.LevelIndex, 0, levels.Count - 1)];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the current level of game session {GameSessionId} failed.", session.Id);
            throw;
        }
    }

    /// <summary>Copies generic progression state for an immutable snapshot.</summary>
    /// <param name="source">Live progression state.</param>
    /// <returns>An independent copy.</returns>
    private GameProgressionState CloneProgression(GameProgressionState source)
    {
        try
        {
            return new GameProgressionState
            {
                LevelIndex = source.LevelIndex,
                AutoAdvanceLevels = source.AutoAdvanceLevels,
                Round = source.Round,
                Match = source.Match,
                Exchange = source.Exchange,
                Initialized = source.Initialized,
                ChampionKey = source.ChampionKey,
                RoundEntrantKeys = [.. source.RoundEntrantKeys],
                NextRoundEntrantKeys = [.. source.NextRoundEntrantKeys],
                TimelineSequence = source.TimelineSequence
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Copying game progression state failed.");
            throw;
        }
    }

    /// <summary>Copies one generic timeline entry for an immutable snapshot.</summary>
    /// <param name="source">Live timeline entry.</param>
    /// <returns>An independent copy.</returns>
    private GameTimelineEntry CloneTimelineEntry(GameTimelineEntry source)
    {
        try
        {
            return new GameTimelineEntry
            {
                Sequence = source.Sequence,
                Round = source.Round,
                Match = source.Match,
                Exchange = source.Exchange,
                EventKind = source.EventKind,
                LeftState = source.LeftState,
                RightState = source.RightState,
                Detail = source.Detail
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Copying a game timeline entry failed.");
            throw;
        }
    }

    }
}
