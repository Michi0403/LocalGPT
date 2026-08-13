using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Provides one shared control surface for humans and AI players. The deterministic renderer keeps
/// /Chat reactive; a single Council renderer may replace a turn's complete frame through SubmitFrameAsync.
/// </summary>
/// <param name="gameDirector">Council game director service dependency used by the council game session workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilGameSessionService(
    ICouncilGameDirectorService gameDirector,
    ILogger<CouncilGameSessionService> logger) : ICouncilGameSessionService, IDisposable
{
    /// <summary>
    /// Defines the default frame width constant used by <see cref="CouncilGameSessionService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int DefaultFrameWidth = 80;
    /// <summary>
    /// Defines the default frame height constant used by <see cref="CouncilGameSessionService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int DefaultFrameHeight = 25;
    /// <summary>
    /// Defines the field of view constant used by <see cref="CouncilGameSessionService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const double FieldOfView = Math.PI / 3d;
    /// <summary>
    /// Stores the in-memory sessions collection maintained internally by <see cref="CouncilGameSessionService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CouncilGameSessionState> sessions = new();
    /// <summary>
    /// Stores the cancellation source used by <see cref="CouncilGameSessionService"/> to stop its current background or asynchronous operation.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> autoplayLoops = new();
    /// <summary>
    /// Stores the internal disposed state used by <see cref="CouncilGameSessionService"/> while executing its surrounding workflow.
    /// </summary>
    private int disposed;

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="CouncilGameSessionService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action<Guid>? Changed;

    /// <summary>
    /// Performs start as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    public Task<CouncilGameSessionSnapshot> StartAsync(
        StartCouncilGameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            var gameKey = NormalizeGameKey(request.GameKey);
            var session = new CouncilGameSessionState
            {
                Id = Guid.NewGuid(),
                GameKey = gameKey,
                TeamKey = string.IsNullOrWhiteSpace(request.TeamKey) ? DefaultTeamFor(gameKey) : request.TeamKey.Trim(),
                ConversationId = request.ConversationId,
                DisplayName = gameKey == "green-dragon" ? "Green Dragon Runtime Story" : "ASCII corridor action game",
                ControlMode = request.ControlMode,
                AutoplayEnabled = request.AutoplayEnabled || request.ControlMode == CouncilGameControlMode.Ai,
                AutoplayDelayMilliseconds = NormalizeAutoplayDelay(request.AutoplayDelayMilliseconds),
                HumanInputRequired = request.ControlMode != CouncilGameControlMode.Ai,
                InputReason = request.ControlMode == CouncilGameControlMode.Ai
                    ? "AI player owns the next control step."
                    : "Your turn: use the same controls that an AI player receives.",
                CurrentTurnOwner = request.ControlMode == CouncilGameControlMode.Ai ? "AI Player Controller" : "Human Player",
                DirectorMode = request.DirectorMode,
                GameDirectorModelName = request.GameDirectorModelName?.Trim() ?? string.Empty,
                CreatureDirectorCount = Math.Clamp(request.CreatureDirectorCount, 1, 8),
                LastDirectorDecision = "The GameDirector owns all state transitions; controllers may only submit proposals.",
                FrameWidth = DefaultFrameWidth,
                FrameHeight = DefaultFrameHeight,
                LastActionBy = string.IsNullOrWhiteSpace(request.StartedBy) ? "Human User" : request.StartedBy.Trim(),
                PlayerX = gameKey == "green-dragon" ? 4 : 3,
                PlayerY = gameKey == "green-dragon" ? 4 : 3,
                FacingRadians = 0d,
                LegalActions = BuildLegalActions(gameKey),
                InputBindings = BuildInputBindings(gameKey)
            };
            session.FrameText = Render(session);
            session.FrameCaption = BuildCaption(session);
            session.FrameRenderer = "LocalGPT deterministic preview renderer";
            sessions[session.Id] = session;
            EnsureAutoplayLoop(session);
            Notify(session.Id);
            logger.LogInformation(
                "Started Council game session {GameSessionId} for {GameKey} in {ControlMode} mode; prompt and frame content were omitted.",
                session.Id,
                session.GameKey,
                session.ControlMode);
            return Task.FromResult(ToSnapshot(session));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Starting a Council game session was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Starting a Council game session failed; request content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs get as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    public Task<CouncilGameSessionSnapshot?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(sessions.TryGetValue(sessionId, out var session) ? ToSnapshot(session) : null);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Reading Council game session {GameSessionId} was cancelled.", sessionId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council game session {GameSessionId} failed.", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves active as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    public Task<CouncilGameSessionSnapshot?> GetActiveAsync(Guid? conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            var session = sessions.Values
                .Where(item => item.Status == "Running" && (conversationId is null || item.ConversationId == conversationId))
                .OrderByDescending(item => item.UpdatedAtUtc)
                .FirstOrDefault();
            return Task.FromResult(session is null ? null : ToSnapshot(session));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Reading the active Council game session was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the active Council game session failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs list as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeCompleted">Value indicating whether include completed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public Task<IReadOnlyList<CouncilGameSessionSnapshot>> ListAsync(bool includeCompleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<CouncilGameSessionSnapshot> result = sessions.Values
                .Where(item => includeCompleted || item.Status == "Running")
                .OrderByDescending(item => item.UpdatedAtUtc)
                .Select(ToSnapshot)
                .ToList();
            return Task.FromResult(result);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Listing Council game sessions was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing Council game sessions failed.");
            throw;
        }
    }

    /// <summary>
    /// Previews control as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game director decision produced by the operation.</returns>
    public async Task<CouncilGameDirectorDecision> PreviewControlAsync(
        CouncilGameControlRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessions.TryGetValue(request.SessionId, out var session))
                throw new KeyNotFoundException($"Council game session {request.SessionId} was not found.");

            CouncilGameSessionSnapshot snapshot;
            string normalizedAction;
            lock (session.SyncRoot)
            {
                if (request.ExpectedTurn is long expected && expected != session.Turn)
                    throw new InvalidOperationException($"The game advanced from turn {expected} to {session.Turn}; refresh before sending another control.");
                if (session.Status != "Running")
                    throw new InvalidOperationException("The game session is not running.");
                normalizedAction = NormalizeAction(request.Action, request.AxisX, request.AxisY);
                snapshot = ToSnapshotUnsafe(session);
            }

            return await gameDirector.EvaluateAsync(new CouncilGameDirectorContext
            {
                Session = snapshot,
                Proposal = request,
                NormalizedAction = normalizedAction,
                DirectorMode = snapshot.DirectorMode,
                DirectorModelName = snapshot.GameDirectorModelName
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Previewing a Council game control was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Previewing a Council game control failed; request content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Applies control as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    public async Task<CouncilGameSessionSnapshot> ApplyControlAsync(
        CouncilGameControlRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await PreviewControlAsync(request, cancellationToken).ConfigureAwait(false);
            if (!decision.Approved)
                throw new InvalidOperationException(decision.Reason);
            if (!sessions.TryGetValue(request.SessionId, out var session))
                throw new KeyNotFoundException($"Council game session {request.SessionId} was not found.");

            lock (session.SyncRoot)
            {
                if (session.Turn != decision.ExpectedTurn)
                    throw new InvalidOperationException($"The game advanced from turn {decision.ExpectedTurn} to {session.Turn} while the GameDirector reviewed the proposal.");
                if (request.ExpectedTurn is long expected && expected != session.Turn)
                    throw new InvalidOperationException($"The game advanced from turn {expected} to {session.Turn}; refresh before sending another control.");
                if (session.Status != "Running")
                    throw new InvalidOperationException("The game session is not running.");

                session.HumanInputRequired = false;
                session.InputReason = "The GameDirector approved the proposal and is resolving one authoritative world step.";
                session.CurrentTurnOwner = session.GameDirectorName;
                ApplyAction(session, decision.NormalizedAction, request.AimX, request.AimY);
                session.Turn++;
                session.LastAction = decision.NormalizedAction;
                session.LastActionBy = string.IsNullOrWhiteSpace(request.ActorName) ? request.Source : request.ActorName.Trim();
                session.LastDirectorDecision = decision.Reason;
                session.LastDirectorPredictions = decision.Predictions.Select(ClonePrediction).ToList();
                session.FrameText = Render(session);
                session.FrameCaption = BuildCaption(session);
                session.FrameRenderer = "LocalGPT deterministic preview renderer";
                session.FrameOwnerTurn = session.Turn;
                session.FrameOwner = session.FrameRenderer;
                session.UpdatedAtUtc = DateTime.UtcNow;

                if (session.ControlMode == CouncilGameControlMode.Ai)
                {
                    session.CurrentTurnOwner = "AI Player Controller";
                    session.InputReason = "AI player may submit the next proposal; the GameDirector validates it before state mutation.";
                }
                else
                {
                    session.CurrentTurnOwner = "Human Player";
                    session.HumanInputRequired = true;
                    session.InputReason = "Your turn: controls submit proposals to the GameDirector.";
                }
            }

            Notify(session.Id);
            logger.LogInformation(
                "GameDirector approved control {Action} for session {GameSessionId} at turn {Turn} from {ControlSource}.",
                session.LastAction,
                session.Id,
                session.Turn,
                string.IsNullOrWhiteSpace(request.Source) ? "unknown" : request.Source);
            return ToSnapshot(session);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Applying a Council game control was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying a Council game control failed; request content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs submit frame as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    public Task<CouncilGameSessionSnapshot> SubmitFrameAsync(
        SubmitCouncilGameFrameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessions.TryGetValue(request.SessionId, out var session))
                throw new KeyNotFoundException($"Council game session {request.SessionId} was not found.");
            if (string.IsNullOrWhiteSpace(request.RendererName))
                throw new ArgumentException("RendererName is required so one AI member can own the complete frame.", nameof(request));

            lock (session.SyncRoot)
            {
                if (request.Turn != session.Turn)
                    throw new InvalidOperationException($"Frame turn {request.Turn} does not match authoritative turn {session.Turn}.");
                if (session.FrameOwnerTurn == request.Turn &&
                    !string.IsNullOrWhiteSpace(session.FrameOwner) &&
                    !string.Equals(session.FrameOwner, request.RendererName, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(session.FrameOwner, "LocalGPT deterministic preview renderer", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Turn {request.Turn} already has one frame owner: {session.FrameOwner}.");
                }

                session.FrameText = NormalizeFrame(request.FrameText, session.FrameWidth, session.FrameHeight);
                session.FrameCaption = string.IsNullOrWhiteSpace(request.Caption) ? BuildCaption(session) : request.Caption.Trim();
                session.FrameRenderer = request.RendererName.Trim();
                session.FrameOwner = session.FrameRenderer;
                session.FrameOwnerTurn = request.Turn;
                session.UpdatedAtUtc = DateTime.UtcNow;
            }

            Notify(session.Id);
            logger.LogInformation(
                "Accepted one complete Council game frame for session {GameSessionId}, turn {Turn}, renderer {RendererName}; frame content was omitted.",
                session.Id,
                request.Turn,
                request.RendererName);
            return Task.FromResult(ToSnapshot(session));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Submitting a Council game frame was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Submitting a Council game frame failed; frame content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Sets input gate as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    public Task<CouncilGameSessionSnapshot> SetInputGateAsync(
        SetCouncilGameInputGateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessions.TryGetValue(request.SessionId, out var session))
                throw new KeyNotFoundException($"Council game session {request.SessionId} was not found.");

            lock (session.SyncRoot)
            {
                session.HumanInputRequired = request.HumanInputRequired && session.ControlMode != CouncilGameControlMode.Ai;
                session.InputReason = string.IsNullOrWhiteSpace(request.Reason)
                    ? (session.HumanInputRequired ? "Waiting for one player control." : "The Council owns the next step.")
                    : request.Reason.Trim();
                if (request.LegalActions.Count > 0)
                    session.LegalActions = request.LegalActions.Select(item => NormalizeAction(item, null, null)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                session.CurrentTurnOwner = session.HumanInputRequired ? "Human Player" : "AI Council";
                session.UpdatedAtUtc = DateTime.UtcNow;
            }
            Notify(session.Id);
            return Task.FromResult(ToSnapshot(session));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Updating the Council game input gate was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Updating the Council game input gate failed; request content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Sets control mode as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="mode">Mode value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="autoplayEnabled">Value indicating whether autoplay enabled should apply to this operation.</param>
    /// <param name="autoplayDelayMilliseconds">Autoplay delay milliseconds value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    public Task<CouncilGameSessionSnapshot> SetControlModeAsync(
        Guid sessionId,
        CouncilGameControlMode mode,
        bool autoplayEnabled,
        int autoplayDelayMilliseconds = 1200,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessions.TryGetValue(sessionId, out var session))
                throw new KeyNotFoundException($"Council game session {sessionId} was not found.");
            lock (session.SyncRoot)
            {
                session.ControlMode = mode;
                session.AutoplayEnabled = autoplayEnabled || mode == CouncilGameControlMode.Ai;
                session.AutoplayDelayMilliseconds = NormalizeAutoplayDelay(autoplayDelayMilliseconds);
                session.HumanInputRequired = mode != CouncilGameControlMode.Ai;
                session.CurrentTurnOwner = mode == CouncilGameControlMode.Ai ? "AI Player Controller" : "Human Player";
                session.InputReason = mode == CouncilGameControlMode.Ai
                    ? "AI player uses localgpt.game.control through the same action contract as the user."
                    : "Your turn: keyboard, touch and gamepad actions use the shared control contract.";
                session.UpdatedAtUtc = DateTime.UtcNow;
            }
            EnsureAutoplayLoop(session);
            Notify(session.Id);
            return Task.FromResult(ToSnapshot(session));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Updating the Council game control mode was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Updating the Council game control mode failed.");
            throw;
        }
    }

    /// <summary>
    /// Ensures autoplay loop as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    private void EnsureAutoplayLoop(CouncilGameSessionState session)
    {
    try
    {
            if (!session.AutoplayEnabled || session.ControlMode == CouncilGameControlMode.Human || session.Status != "Running")
            {
                StopAutoplayLoop(session.Id);
                return;
            }

            if (autoplayLoops.ContainsKey(session.Id))
                return;

            var cancellation = new CancellationTokenSource();
            if (!autoplayLoops.TryAdd(session.Id, cancellation))
            {
                cancellation.Dispose();
                return;
            }

            _ = Task.Run(() => RunAutoplayLoopAsync(session.Id, cancellation.Token), CancellationToken.None);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(EnsureAutoplayLoop)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(EnsureAutoplayLoop)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs run autoplay loop as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RunAutoplayLoopAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && sessions.TryGetValue(sessionId, out var session))
            {
                CouncilGameSessionSnapshot snapshot;
                lock (session.SyncRoot)
                {
                    if (!session.AutoplayEnabled || session.ControlMode == CouncilGameControlMode.Human || session.Status != "Running")
                        break;
                    snapshot = ToSnapshot(session);
                }

                await Task.Delay(snapshot.AutoplayDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                var action = SelectAutoplayAction(snapshot);
                try
                {
                    await ApplyControlAsync(new CouncilGameControlRequest
                    {
                        SessionId = snapshot.Id,
                        Action = action,
                        ExpectedTurn = snapshot.Turn,
                        Source = "AI",
                        ActorName = "LocalGPT AI Player Controller"
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (InvalidOperationException exception)
                {
                    // A human or another Council member won this turn. The next loop iteration refreshes state.
                    logger.LogTrace(exception, "AI game autoplay lost an optimistic turn race for session {GameSessionId}; state will refresh.", sessionId);
                }
            }
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "AI game autoplay stopped for session {GameSessionId}.", sessionId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AI game autoplay failed for session {GameSessionId}.", sessionId);
        }
        finally
        {
            if (autoplayLoops.TryRemove(sessionId, out var source))
                source.Dispose();
        }
    }

    /// <summary>
    /// Performs select autoplay action as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="snapshot">Snapshot value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SelectAutoplayAction(CouncilGameSessionSnapshot snapshot)
    {
    try
    {
            if (snapshot.GameKey == "green-dragon")
            {
                string[] storyActions = ["choice-1", "move-forward", "use", "choice-2", "turn-right", "choice-3"];
                return storyActions[(int)(snapshot.Turn % storyActions.Length)];
            }

            // Deterministic and bounded so an AI player proves the same controller contract without requiring
            // another expensive model turn for every key press. A Council AI can still override it through
            // localgpt.game.control using ExpectedTurn concurrency protection.
            string[] corridorActions =
            [
                "move-forward", "turn-right", "move-forward", "shoot", "strafe-left",
                "move-forward", "use", "turn-left", "move-forward", "duck", "shoot", "duck"
            ];
            return corridorActions[(int)(snapshot.Turn % corridorActions.Length)];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(SelectAutoplayAction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(SelectAutoplayAction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Stops autoplay loop as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    private void StopAutoplayLoop(Guid sessionId)
    {
    try
    {
            if (autoplayLoops.TryRemove(sessionId, out var cancellation))
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(StopAutoplayLoop)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(StopAutoplayLoop)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes autoplay delay as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int NormalizeAutoplayDelay(int value) {
    try
    {
        return Math.Clamp(value, 250, 10_000);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeAutoplayDelay)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeAutoplayDelay)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs throw if disposed as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void ThrowIfDisposed()
    {
    try
    {
            if (Volatile.Read(ref disposed) != 0)
                throw new ObjectDisposedException(nameof(CouncilGameSessionService));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ThrowIfDisposed)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ThrowIfDisposed)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="CouncilGameSessionService"/> and leaves the council game session workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;
            foreach (var sessionId in autoplayLoops.Keys.ToArray())
                StopAutoplayLoop(sessionId);
            logger.LogDebug("Disposed the Council game session service and stopped its autoplay loops.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Disposing the Council game session service failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes game key as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeGameKey(string? gameKey)
    {
    try
    {
            var normalized = (gameKey ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            return normalized switch
            {
                "doom" or "ascii-doom" or "ascii-doom-council-adventure" => "ascii-doom",
                "dragon" or "green-dragon" or "lotgd" or "green-dragon-runtime-story" => "green-dragon",
                _ => throw new ArgumentException($"Unsupported game key '{gameKey}'. Use ascii-doom or green-dragon.", nameof(gameKey))
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeGameKey)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeGameKey)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs default team for as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DefaultTeamFor(string gameKey) {
    try
    {
        return gameKey == "green-dragon" ? "green-dragon-runtime-story" : "ascii-doom-council-adventure";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(DefaultTeamFor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(DefaultTeamFor)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds legal actions as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> BuildLegalActions(string gameKey) {
    try
    {
        return gameKey == "green-dragon"
        ? ["move-forward", "move-backward", "turn-left", "turn-right", "use", "choice-1", "choice-2", "choice-3"]
        : ["move-forward", "move-backward", "strafe-left", "strafe-right", "turn-left", "turn-right", "shoot", "duck", "use"];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildLegalActions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildLegalActions)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds input bindings as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<RuntimeInputBindingDefinition> BuildInputBindings(string gameKey) {
    try
    {
        return gameKey == "green-dragon"
        ?
        [
            Binding("move-forward", "Move / choice up", "W / ArrowUp", "D-pad Up"),
            Binding("move-backward", "Move / choice down", "S / ArrowDown", "D-pad Down"),
            Binding("turn-left", "Previous choice", "A / ArrowLeft", "D-pad Left"),
            Binding("turn-right", "Next choice", "D / ArrowRight", "D-pad Right"),
            Binding("use", "Confirm / interact", "E / Enter", "A"),
            Binding("choice-1", "Choice 1", "1", "X"),
            Binding("choice-2", "Choice 2", "2", "Y"),
            Binding("choice-3", "Choice 3", "3", "B")
        ]
        :
        [
            Binding("move-forward", "Move forward", "W / ArrowUp", "Left stick up"),
            Binding("move-backward", "Move backward", "S / ArrowDown", "Left stick down"),
            Binding("strafe-left", "Strafe left", "A", "Left stick left"),
            Binding("strafe-right", "Strafe right", "D", "Left stick right"),
            Binding("turn-left", "Turn / aim left", "Q / ArrowLeft", "Right stick left"),
            Binding("turn-right", "Turn / aim right", "R / ArrowRight", "Right stick right"),
            Binding("shoot", "Shoot along the current x/y facing ray", "Space", "Right trigger"),
            Binding("duck", "Duck / stand", "Ctrl / C", "B"),
            Binding("use", "Use door / switch", "E / Enter", "A")
        ];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildInputBindings)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildInputBindings)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs binding as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="action">Action value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="display">Display value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="keyboard">Keyboard value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="gamepad">Gamepad value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The runtime input binding definition produced by the operation.</returns>
    private RuntimeInputBindingDefinition Binding(string action, string display, string keyboard, string gamepad) {
    try
    {
        return new()
    {
        Action = action,
        DisplayName = display,
        KeyboardKey = keyboard,
        GamepadButton = gamepad,
        Description = "The human UI and AI Player Controller call the same localgpt.game.control action."
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Binding)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Binding)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes action as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="action">Action value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="axisX">Axis x value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="axisY">Axis y value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeAction(string? action, double? axisX, double? axisY)
    {
    try
    {
            var value = (action ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            if (string.IsNullOrWhiteSpace(value) && (axisX.HasValue || axisY.HasValue))
            {
                var x = axisX ?? 0;
                var y = axisY ?? 0;
                if (Math.Abs(y) >= Math.Abs(x)) value = y < 0 ? "move-forward" : "move-backward";
                else value = x < 0 ? "strafe-left" : "strafe-right";
            }
            return value switch
            {
                "forward" or "up" or "w" => "move-forward",
                "back" or "backward" or "down" or "s" => "move-backward",
                "left" or "a" => "strafe-left",
                "right" or "d" => "strafe-right",
                "look-left" or "aim-left" or "rotate-left" or "q" => "turn-left",
                "look-right" or "aim-right" or "rotate-right" or "r" => "turn-right",
                "fire" or "attack" or "space" => "shoot",
                "crouch" or "ctrl" => "duck",
                "interact" or "enter" or "e" => "use",
                "1" => "choice-1",
                "2" => "choice-2",
                "3" => "choice-3",
                _ => value
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeAction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeAction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Applies action as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="action">Action value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="aimX">Aim x value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="aimY">Aim y value supplied to the council game session operation and used when producing its result.</param>
    private void ApplyAction(CouncilGameSessionState session, string action, int? aimX, int? aimY)
    {
    try
    {
            if (session.GameKey == "green-dragon")
            {
                switch (action)
                {
                    case "move-forward": session.PlayerY = Math.Max(1, session.PlayerY - 1); break;
                    case "move-backward": session.PlayerY = Math.Min(8, session.PlayerY + 1); break;
                    case "turn-left": session.FacingRadians -= Math.PI / 8d; break;
                    case "turn-right": session.FacingRadians += Math.PI / 8d; break;
                    case "use": session.StoryLine = "The innkeeper nods and points toward the moonlit forest path."; break;
                    case "choice-1": session.StoryLine = "You accept the lantern and prepare for the forest path."; break;
                    case "choice-2": session.StoryLine = "You ask the villagers what they heard beyond the old gate."; break;
                    case "choice-3": session.StoryLine = "You rest by the hearth while the Story Director advances the world."; break;
                }
                return;
            }

            switch (action)
            {
                case "turn-left":
                    session.FacingRadians -= Math.PI / 12d;
                    break;
                case "turn-right":
                    session.FacingRadians += Math.PI / 12d;
                    break;
                case "duck":
                    session.IsDucking = !session.IsDucking;
                    break;
                case "shoot":
                    if (session.Ammo > 0)
                    {
                        session.Ammo--;
                        session.MuzzleFlash = 2;
                        if (aimX.HasValue && aimY.HasValue)
                            session.FacingRadians = Math.Atan2(aimY.Value - session.PlayerY, aimX.Value - session.PlayerX);
                    }
                    break;
                case "use":
                    session.UsePulse = 2;
                    break;
                case "move-forward":
                    TryMove(session, Math.Cos(session.FacingRadians), Math.Sin(session.FacingRadians));
                    break;
                case "move-backward":
                    TryMove(session, -Math.Cos(session.FacingRadians), -Math.Sin(session.FacingRadians));
                    break;
                case "strafe-left":
                    TryMove(session, Math.Cos(session.FacingRadians - Math.PI / 2d), Math.Sin(session.FacingRadians - Math.PI / 2d));
                    break;
                case "strafe-right":
                    TryMove(session, Math.Cos(session.FacingRadians + Math.PI / 2d), Math.Sin(session.FacingRadians + Math.PI / 2d));
                    break;
            }
            session.FacingRadians = NormalizeRadians(session.FacingRadians);
            session.MuzzleFlash = Math.Max(0, session.MuzzleFlash - 1);
            session.UsePulse = Math.Max(0, session.UsePulse - 1);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ApplyAction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ApplyAction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to move as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="dx">Devexpress value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="dy">Dy value supplied to the council game session operation and used when producing its result.</param>
    private void TryMove(CouncilGameSessionState session, double dx, double dy)
    {
    try
    {
            var map = doomMap;
            var nextX = session.PlayerX + (int)Math.Round(dx);
            var nextY = session.PlayerY + (int)Math.Round(dy);
            if (nextY < 0 || nextY >= map.Length || nextX < 0 || nextX >= map[nextY].Length) return;
            if (map[nextY][nextX] == '#') return;
            session.PlayerX = nextX;
            session.PlayerY = nextY;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(TryMove)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(TryMove)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs render as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Render(CouncilGameSessionState session) {
    try
    {
        return session.GameKey == "green-dragon"
        ? RenderGreenDragon(session)
        : RenderDoomLike(session);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Render)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Render)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs render doom like as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderDoomLike(CouncilGameSessionState session)
    {
    try
    {
            var width = session.FrameWidth;
            var height = session.FrameHeight;
            var viewHeight = height - 6;
            var lines = Enumerable.Range(0, height).Select(_ => new string(' ', width).ToCharArray()).ToArray();
            for (var x = 0; x < width; x++)
            {
                var rayAngle = session.FacingRadians - FieldOfView / 2d + FieldOfView * x / Math.Max(1, width - 1);
                var distance = CastRay(session.PlayerX + .5d, session.PlayerY + .5d, rayAngle);
                var correctedDistance = Math.Max(.15d, distance * Math.Cos(rayAngle - session.FacingRadians));
                var wallHeight = Math.Clamp((int)Math.Round(viewHeight / correctedDistance * .82d), 1, viewHeight);
                var top = Math.Max(0, (viewHeight - wallHeight) / 2 - (session.IsDucking ? -2 : 0));
                var bottom = Math.Min(viewHeight - 1, top + wallHeight);
                for (var y = 0; y < viewHeight; y++)
                {
                    lines[y][x] = y < top ? '.' : y <= bottom ? WallGlyph(correctedDistance, x, y) : FloorGlyph(x, y);
                }
            }
            var centerY = viewHeight / 2;
            var centerX = width / 2;
            lines[centerY][centerX] = session.MuzzleFlash > 0 ? '*' : '+';
            if (centerX > 0) lines[centerY][centerX - 1] = '-';
            if (centerX + 1 < width) lines[centerY][centerX + 1] = '-';
            if (centerY > 0) lines[centerY - 1][centerX] = '|';
            if (centerY + 1 < viewHeight) lines[centerY + 1][centerX] = '|';

            Put(lines, viewHeight, 0, new string('═', width));
            Put(lines, viewHeight + 1, 0, Fit($" ASCII CORRIDOR // TURN {session.Turn:000} // {Compass(session.FacingRadians),3} // {(session.IsDucking ? "DUCK" : "STAND")}", width));
            Put(lines, viewHeight + 2, 0, Fit($" HP {session.Health:000}   AMMO {session.Ammo:000}   POS {session.PlayerX:00},{session.PlayerY:00}   ACTION {session.LastAction}", width));
            Put(lines, viewHeight + 3, 0, Fit(" W/S move  A/D strafe  Q/R turn  SPACE shoot  CTRL duck  E use  F fullscreen", width));
            Put(lines, viewHeight + 4, 0, Fit(" Fan-made open-source configuration study; no commercial assets, WADs, or original engine runtime included.", width));
            Put(lines, viewHeight + 5, 0, new string('═', width));
            return string.Join(Environment.NewLine, lines.Select(line => new string(line)));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderDoomLike)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderDoomLike)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs render green dragon as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderGreenDragon(CouncilGameSessionState session)
    {
    try
    {
            var width = session.FrameWidth;
            var lines = new List<string>
            {
                "┌" + new string('─', width - 2) + "┐",
                Fit("│                         THE LANTERN VILLAGE                                 │", width),
                Fit("│                                                                            │", width),
                Fit("│        /\\                  [OLD GATE]                    /\\              │", width),
                Fit("│       /  \\        * lanterns *                         /  \\             │", width),
                Fit("│      / INN\\        .-.-.-.-.-.                       /SHOP\\            │", width),
                Fit("│      |____|---------'         '-----------------------|____|             │", width),
                Fit("│           \\                    @ player                 /                 │", width),
                Fit("│            \\____________________.______________________/                  │", width),
                Fit("│                                 |                                          │", width),
                Fit("│                              FOREST PATH                                    │", width),
                Fit("│                                 |                                          │", width),
                Fit("│                            {green woods}                                    │", width),
                Fit("│                                                                            │", width),
                Fit($"│  {session.StoryLine}", width),
                Fit("│                                                                            │", width),
                Fit("│  1 Accept the lantern   2 Ask the villagers   3 Rest by the hearth       │", width),
                Fit("│  W/S move  A/D choose  E/Enter confirm  1/2/3 direct choice              │", width),
                Fit("│                                                                            │", width),
                Fit($"│  TURN {session.Turn:000}  PLAYER {session.PlayerX:00},{session.PlayerY:00}  LAST {session.LastAction}", width),
                Fit("│                                                                            │", width),
                Fit("│  Runtime story example inspired by open-source text-RPG architecture;      │", width),
                Fit("│  original scene and content, not affiliated with LOTGD.                    │", width),
                "└" + new string('─', width - 2) + "┘"
            };
            while (lines.Count < session.FrameHeight) lines.Insert(lines.Count - 1, Fit("│", width));
            return string.Join(Environment.NewLine, lines.Take(session.FrameHeight));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderGreenDragon)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(RenderGreenDragon)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs cast ray as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="x">X value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="angle">Angle value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double CastRay(double x, double y, double angle)
    {
    try
    {
            const double maxDistance = 20d;
            for (var distance = .05d; distance < maxDistance; distance += .05d)
            {
                var sampleX = (int)(x + Math.Cos(angle) * distance);
                var sampleY = (int)(y + Math.Sin(angle) * distance);
                if (sampleY < 0 || sampleY >= doomMap.Length || sampleX < 0 || sampleX >= doomMap[sampleY].Length) return distance;
                if (doomMap[sampleY][sampleX] == '#') return distance;
            }
            return maxDistance;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(CastRay)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(CastRay)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs wall glyph as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="distance">Distance value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="x">X value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The char produced by the operation.</returns>
    private char WallGlyph(double distance, int x, int y) {
    try
    {
        return distance switch
    {
        < 2.2d => (x + y) % 2 == 0 ? '█' : '▓',
        < 4.2d => (x + y) % 3 == 0 ? '▓' : '▒',
        < 7.5d => '▒',
        _ => '░'
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(WallGlyph)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(WallGlyph)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs floor glyph as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="x">X value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The char produced by the operation.</returns>
    private char FloorGlyph(int x, int y) {
    try
    {
        return (x + y) % 5 == 0 ? ':' : (x + y) % 2 == 0 ? '.' : ' ';
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(FloorGlyph)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(FloorGlyph)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs put as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="lines">Lines value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="row">Row value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="column">Column value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the council game session operation and used when producing its result.</param>
    private void Put(char[][] lines, int row, int column, string text)
    {
    try
    {
            if (row < 0 || row >= lines.Length) return;
            for (var index = 0; index < text.Length && column + index < lines[row].Length; index++)
                lines[row][column + index] = text[index];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Put)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Put)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs fit as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Fit(string text, int width)
    {
    try
    {
            var normalized = (text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
            if (normalized.Length > width) return normalized[..width];
            return normalized.PadRight(width);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Fit)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Fit)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes frame as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="frame">Frame value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeFrame(string frame, int width, int height)
    {
    try
    {
            var sourceLines = (frame ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n');
            var result = new List<string>(height);
            for (var row = 0; row < height; row++)
            {
                var line = row < sourceLines.Length ? sourceLines[row] : string.Empty;
                result.Add(Fit(line, width));
            }
            return string.Join(Environment.NewLine, result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeFrame)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeFrame)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds caption as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildCaption(CouncilGameSessionState session) {
    try
    {
        return $"{session.DisplayName} · turn {session.Turn} · {session.CurrentTurnOwner} · renderer {session.FrameRenderer}";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildCaption)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildCaption)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs compass as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="radians">Radians value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Compass(double radians)
    {
    try
    {
            var degrees = NormalizeRadians(radians) * 180d / Math.PI;
            return degrees switch
            {
                >= 337.5 or < 22.5 => "E",
                < 67.5 => "SE",
                < 112.5 => "S",
                < 157.5 => "SW",
                < 202.5 => "W",
                < 247.5 => "NW",
                < 292.5 => "N",
                _ => "NE"
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Compass)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Compass)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes radians as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double NormalizeRadians(double value)
    {
    try
    {
            while (value < 0) value += Math.PI * 2d;
            while (value >= Math.PI * 2d) value -= Math.PI * 2d;
            return value;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeRadians)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeRadians)} failed.");
        throw;
    }
}

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
