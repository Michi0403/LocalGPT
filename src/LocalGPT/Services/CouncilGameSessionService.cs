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
public sealed partial class CouncilGameSessionService : ICouncilGameSessionService, IDisposable
    {
        /// <summary>
        /// Stores the council game director service dependency used by <see cref="CouncilGameSessionService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilGameDirectorService gameDirector;
        /// <summary>
        /// Stores the logger used by <see cref="CouncilGameSessionService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<CouncilGameSessionService> logger;

        /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
        /// <param name="gameDirector">Injected dependency used by the CouncilGameSessionService.</param>
        /// <param name="logger">Injected dependency used by the CouncilGameSessionService.</param>
        public CouncilGameSessionService(
            ICouncilGameDirectorService gameDirector,
            ILogger<CouncilGameSessionService> logger)
        {
            this.gameDirector = gameDirector;
            this.logger = logger;
        }

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
}
