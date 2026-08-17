using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using LocalGPT.BusinessObjects;
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
        GameKey = session.GameKey,
        TeamKey = session.TeamKey,
        ConversationId = session.ConversationId,
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
        LegalActions = session.LegalActions.ToArray(),
        InputBindings = session.InputBindings.Select(CloneBinding).ToArray(),
        LastAction = session.LastAction,
        LastActionBy = session.LastActionBy,
        PlayerX = session.PlayerX,
        PlayerY = session.PlayerY,
        FacingDegrees = Math.Round(NormalizeRadians(session.FacingRadians) * 180d / Math.PI, 1),
        IsDucking = session.IsDucking,
        Health = session.Health,
        Ammo = session.Ammo,
        CreatedAtUtc = session.CreatedAtUtc,
        UpdatedAtUtc = session.UpdatedAtUtc
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

    /// <summary>
    /// Stores the internal doom map state used by <see cref="CouncilGameSessionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string[] doomMap =
    [
        "################################",
        "#........#.....................#",
        "#........#....######...........#",
        "#........#....#....#...........#",
        "#.............#....#...........#",
        "#..######.....#....#######.....#",
        "#..#....#.....#..........#.....#",
        "#..#....#.....######.....#.....#",
        "#..#....#..........#.....#.....#",
        "#..#....######.....#.....#.....#",
        "#..#.........#.....#...........#",
        "#..######....#.....##########..#",
        "#.......#....#.................#",
        "#####...#....###########.......#",
        "#.......#..............#.......#",
        "#.......########.......#.......#",
        "#......................#.......#",
        "#..#####################.......#",
        "#..............................#",
        "################################"
    ];

    }
}
