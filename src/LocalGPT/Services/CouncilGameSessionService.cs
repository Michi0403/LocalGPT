using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;

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
        /// <summary>Creates bounded scopes for database-backed team/runtime-class configuration lookups from this singleton game service.</summary>
        private readonly IServiceScopeFactory scopeFactory;
        /// <summary>
        /// Stores the logger used by <see cref="CouncilGameSessionService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<CouncilGameSessionService> logger;

        /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
        /// <param name="gameDirector">Injected dependency used by the CouncilGameSessionService.</param>
        /// <param name="scopeFactory">Scope factory used to resolve database-backed team/runtime-class configuration safely from this singleton service.</param>
        /// <param name="logger">Injected dependency used by the CouncilGameSessionService.</param>
        public CouncilGameSessionService(
            ICouncilGameDirectorService gameDirector,
            IServiceScopeFactory scopeFactory,
            ILogger<CouncilGameSessionService> logger)
        {
            this.gameDirector = gameDirector;
            this.scopeFactory = scopeFactory;
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
    public async Task<CouncilGameSessionSnapshot> StartAsync(
        StartCouncilGameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var definition = request.Definition;
            string gameKey;
            CouncilGameRuntimeProfile runtimeProfile;
            Guid? projectId;
            string projectVersion;
            string displayName;
            string defaultTeamKey;
            if (definition is null)
            {
                gameKey = NormalizeGameKey(request.GameKey);
                runtimeProfile = gameKey == "green-dragon" ? CouncilGameRuntimeProfile.Story : CouncilGameRuntimeProfile.Corridor;
                projectId = request.ProjectId;
                projectVersion = string.Empty;
                displayName = runtimeProfile == CouncilGameRuntimeProfile.Story ? "Green Dragon Runtime Story" : "ASCII corridor action game";
                defaultTeamKey = DefaultTeamFor(gameKey);
            }
            else
            {
                if (definition.ProjectId == Guid.Empty)
                    throw new ArgumentException("A project-built game definition must carry its owning project identifier.", nameof(request));
                if (request.ProjectId is Guid requestedProjectId && requestedProjectId != definition.ProjectId)
                    throw new ArgumentException("The requested project identifier does not match the project-built game definition.", nameof(request));
                if (!Enum.IsDefined(typeof(CouncilGameRuntimeProfile), definition.RuntimeProfile))
                    throw new ArgumentException("The project-built game definition targets an unsupported runtime profile.", nameof(request));

                var normalizedDefinitionKey = (definition.GameKey ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
                normalizedDefinitionKey = new string(normalizedDefinitionKey.Where(character => char.IsLetterOrDigit(character) || character == '-').ToArray()).Trim('-');
                if (string.IsNullOrWhiteSpace(normalizedDefinitionKey))
                    throw new ArgumentException("The project-built game definition contains an invalid game key.", nameof(request));
                if (normalizedDefinitionKey.Length > 160)
                    normalizedDefinitionKey = normalizedDefinitionKey[..160];

                gameKey = normalizedDefinitionKey;
                runtimeProfile = definition.RuntimeProfile;
                projectId = definition.ProjectId;
                projectVersion = (definition.ProjectVersion ?? string.Empty).Trim();
                displayName = string.IsNullOrWhiteSpace(definition.DisplayName) ? gameKey : definition.DisplayName.Trim();
                defaultTeamKey = string.IsNullOrWhiteSpace(definition.DefaultTeamKey) ? $"{gameKey}-council" : definition.DefaultTeamKey.Trim();
            }

            var requestedScenario = string.IsNullOrWhiteSpace(request.ScenarioPrompt) && definition is not null
                ? definition.ScenarioPrompt
                : request.ScenarioPrompt;
            var scenarioPrompt = string.Join(' ', (requestedScenario ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (scenarioPrompt.Length > 240)
                scenarioPrompt = scenarioPrompt[..240];
            var requestedMapSeed = request.MapSeed is > 0
                ? request.MapSeed.Value
                : definition?.MapSeed is > 0 ? definition.MapSeed : 0;
            var campaign = runtimeProfile == CouncilGameRuntimeProfile.Corridor
                && string.Equals(gameKey, "ascii-doom", StringComparison.OrdinalIgnoreCase)
                ? await ResolveDoomCampaignAsync(request, cancellationToken).ConfigureAwait(false)
                : null;
            var session = new CouncilGameSessionState
            {
                Id = Guid.NewGuid(),
                GameKey = gameKey,
                RuntimeProfile = runtimeProfile,
                ProjectId = projectId,
                ProjectVersion = projectVersion,
                TeamKey = string.IsNullOrWhiteSpace(request.TeamKey) ? defaultTeamKey : request.TeamKey.Trim(),
                ConversationId = request.ConversationId,
                CouncilRunId = request.CouncilRunId,
                DisplayName = displayName,
                ControlMode = request.ControlMode,
                AutoplayEnabled = request.ControlMode == CouncilGameControlMode.Ai,
                AutoplayDelayMilliseconds = NormalizeAutoplayDelay(request.AutoplayDelayMilliseconds),
                HumanInputRequired = request.ControlMode != CouncilGameControlMode.Ai,
                InputReason = request.ControlMode == CouncilGameControlMode.Ai
                    ? "AI hunter owns the next map-aware control step."
                    : request.ControlMode == CouncilGameControlMode.Shared
                        ? "Human and AI players may submit direct bounded controls to the same authoritative turn; background autoplay is off."
                        : "Your turn: use the shared controls; AI-origin movement is locked out.",
                CurrentTurnOwner = request.ControlMode == CouncilGameControlMode.Ai ? "AI Hunter" : request.ControlMode == CouncilGameControlMode.Shared ? "Human / AI" : "Human Player",
                DirectorMode = request.DirectorMode,
                GameDirectorModelName = request.GameDirectorModelName?.Trim() ?? string.Empty,
                CreatureDirectorCount = Math.Clamp(request.CreatureDirectorCount, 1, 8),
                LastDirectorDecision = "The GameDirector owns all state transitions; controllers may only submit proposals.",
                FrameWidth = Math.Clamp(request.FrameWidth, 20, 240),
                FrameHeight = Math.Clamp(request.FrameHeight, 8, 100),
                MapSeed = requestedMapSeed,
                CampaignSeed = requestedMapSeed,
                ScenarioPrompt = scenarioPrompt,
                CampaignRuntimeClassKey = campaign?.RuntimeClassKey ?? string.Empty,
                LevelProfiles = campaign?.Levels.Select(CloneLevelProfile).ToList() ?? [],
                CurrentLevelIndex = campaign?.StartingLevelIndex ?? 0,
                AutoAdvanceLevels = campaign?.AutoAdvanceLevels ?? false,
                LastActionBy = string.IsNullOrWhiteSpace(request.StartedBy) ? "Human User" : request.StartedBy.Trim(),
                PlayerX = runtimeProfile == CouncilGameRuntimeProfile.Story ? 4 : 3,
                PlayerY = runtimeProfile == CouncilGameRuntimeProfile.Story ? 4 : 3,
                FacingRadians = 0d,
                LegalActions = BuildLegalActions(runtimeProfile),
                InputBindings = BuildInputBindings(runtimeProfile)
            };
            if (runtimeProfile == CouncilGameRuntimeProfile.Corridor)
                InitializeDoomWorld(session);
            session.FrameText = Render(session);
            session.FrameCaption = BuildCaption(session);
            session.FrameRenderer = "LocalGPT deterministic preview renderer";
            sessions[session.Id] = session;
            EnsureAutoplayLoop(session);
            Notify(session.Id);
            logger.LogInformation(
                "Started Council game session {GameSessionId} for {GameKey} in {ControlMode} mode from project {ProjectId}; prompt and frame content were omitted.",
                session.Id,
                session.GameKey,
                session.ControlMode,
                session.ProjectId);
            return ToSnapshot(session);
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

    /// <summary>Ends only the game runtime, preserving the surrounding chat, provider/Council sessions and ASCII terminal surface.</summary>
    /// <param name="sessionId">Identifier of the game session to end.</param>
    /// <param name="endedBy">Bounded actor label recorded for diagnostics and the final snapshot.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The final game snapshot, or <c>null</c> when the requested game no longer exists.</returns>
    public Task<CouncilGameSessionSnapshot?> EndAsync(Guid sessionId, string endedBy = "Current User", CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessions.TryGetValue(sessionId, out var session))
                return Task.FromResult<CouncilGameSessionSnapshot?>(null);
            lock (session.SyncRoot)
            {
                session.Status = "Completed";
                session.AutoplayEnabled = false;
                session.HumanInputRequired = false;
                session.CurrentTurnOwner = "Game ended";
                session.InputReason = "The ASCII game ended; chat, Council/provider sessions and the terminal remain available.";
                session.LastAction = "end-game";
                session.LastActionBy = string.IsNullOrWhiteSpace(endedBy) ? "Current User" : endedBy.Trim();
                session.UpdatedAtUtc = DateTime.UtcNow;
            }
            StopAutoplayLoop(sessionId);
            Notify(sessionId);
            logger.LogInformation("Ended Council game session {GameSessionId} without closing its surrounding chat/provider session.", sessionId);
            return Task.FromResult<CouncilGameSessionSnapshot?>(ToSnapshot(session));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Ending Council game session {GameSessionId} was cancelled.", sessionId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ending Council game session {GameSessionId} failed.", sessionId);
            throw;
        }
    }

    /// <summary>Ends every running ASCII game owned by one Council run without affecting standalone games.</summary>
    /// <param name="councilRunId">Identifier of the owning Council run.</param>
    /// <param name="endedBy">Bounded actor label recorded on the ended game sessions.</param>
    /// <returns>The number of game sessions transitioned out of Running.</returns>
    public int EndByCouncilRun(Guid councilRunId, string endedBy = "Council lifecycle")
    {
        try
        {
            ThrowIfDisposed();
            var ended = 0;
            foreach (var session in sessions.Values.Where(item =>
                         item.CouncilRunId == councilRunId
                         && string.Equals(item.Status, "Running", StringComparison.OrdinalIgnoreCase)))
            {
                lock (session.SyncRoot)
                {
                    if (!string.Equals(session.Status, "Running", StringComparison.OrdinalIgnoreCase)
                        || session.CouncilRunId != councilRunId)
                        continue;
                    session.Status = "Completed";
                    session.AutoplayEnabled = false;
                    session.HumanInputRequired = false;
                    session.CurrentTurnOwner = "Council run ended";
                    session.InputReason = "The owning Council run ended, so its ASCII game runtime was stopped.";
                    session.LastAction = "end-council-game";
                    session.LastActionBy = string.IsNullOrWhiteSpace(endedBy) ? "Council lifecycle" : endedBy.Trim();
                    session.UpdatedAtUtc = DateTime.UtcNow;
                    ended++;
                }
                StopAutoplayLoop(session.Id);
                Notify(session.Id);
            }

            if (ended > 0)
                logger.LogInformation("Ended {GameSessionCount} Council-owned game session(s) for run {CouncilRunId}.", ended, councilRunId);
            return ended;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ending Council-owned game sessions failed for run {CouncilRunId}.", councilRunId);
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
                if (session.ControlMode == CouncilGameControlMode.Human
                    && string.Equals(request.Source, "AI", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("AI controls are disabled while this game is owned by the human player. Switch control mode explicitly in the Game UI before enabling AI hunter autoplay.");
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
                AdvanceDoomWorld(session);
                session.Turn++;
                session.LastAction = decision.NormalizedAction;
                session.LastActionBy = string.IsNullOrWhiteSpace(request.ActorName) ? request.Source : request.ActorName.Trim();
                session.LastDirectorDecision = decision.Reason;
                session.LastDirectorPredictions = decision.Predictions.Select(ClonePrediction).ToList();
                session.FrameText = Render(session);
                session.FrameCaption = BuildCaption(session);
                session.FrameRenderer = "LocalGPT deterministic preview renderer";
                session.AnimationFrames.Clear();
                session.AnimationDelayMilliseconds = 650;
                session.FrameOwnerTurn = session.Turn;
                session.FrameOwner = session.FrameRenderer;
                session.UpdatedAtUtc = DateTime.UtcNow;

                if (session.Status == "Running" && session.ControlMode == CouncilGameControlMode.Ai)
                {
                    session.CurrentTurnOwner = "AI Hunter";
                    session.InputReason = "AI hunter reads the authoritative map and chooses one deterministic legal control step.";
                }
                else if (session.Status == "Running")
                {
                    session.CurrentTurnOwner = session.ControlMode == CouncilGameControlMode.Shared ? "Human / AI" : "Human Player";
                    session.HumanInputRequired = true;
                    session.InputReason = session.ControlMode == CouncilGameControlMode.Shared
                        ? "Human and AI players may submit the next direct proposal; there is no background autoplay in Shared mode."
                        : "Your turn: controls submit proposals to the GameDirector.";
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
                session.AnimationFrames.Clear();
                session.AnimationDelayMilliseconds = 650;
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
                session.AutoplayEnabled = mode == CouncilGameControlMode.Ai;
                session.AutoplayDelayMilliseconds = NormalizeAutoplayDelay(autoplayDelayMilliseconds);
                session.HumanInputRequired = mode != CouncilGameControlMode.Ai;
                session.CurrentTurnOwner = mode == CouncilGameControlMode.Ai ? "AI Hunter" : mode == CouncilGameControlMode.Shared ? "Human / AI" : "Human Player";
                session.InputReason = mode == CouncilGameControlMode.Ai
                    ? "AI hunter uses deterministic map-aware pathfinding through the same bounded control contract as the user."
                    : mode == CouncilGameControlMode.Shared
                        ? "Human and AI direct controls share the authoritative game turn; background autoplay remains off."
                        : "Your turn: keyboard, touch and gamepad actions use the shared control contract; AI-origin movement is rejected.";
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
