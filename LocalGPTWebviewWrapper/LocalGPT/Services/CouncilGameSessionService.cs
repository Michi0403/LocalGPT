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
public sealed class CouncilGameSessionService(
    ILogger<CouncilGameSessionService> logger) : ICouncilGameSessionService, IDisposable
{
    private const int DefaultFrameWidth = 80;
    private const int DefaultFrameHeight = 25;
    private const double FieldOfView = Math.PI / 3d;
    private readonly ConcurrentDictionary<Guid, CouncilGameSessionState> sessions = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> autoplayLoops = new();
    private int disposed;

    public event Action<Guid>? Changed;

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

    public Task<CouncilGameSessionSnapshot> ApplyControlAsync(
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

            lock (session.SyncRoot)
            {
                if (request.ExpectedTurn is long expected && expected != session.Turn)
                    throw new InvalidOperationException($"The game advanced from turn {expected} to {session.Turn}; refresh before sending another control.");
                if (session.Status != "Running")
                    throw new InvalidOperationException("The game session is not running.");

                var action = NormalizeAction(request.Action, request.AxisX, request.AxisY);
                if (!session.LegalActions.Contains(action, StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException($"Control action '{request.Action}' is not legal for {session.GameKey}.", nameof(request));

                session.HumanInputRequired = false;
                session.InputReason = "Resolving the selected control through the shared human/AI control service.";
                session.CurrentTurnOwner = "State resolver";
                ApplyAction(session, action, request.AimX, request.AimY);
                session.Turn++;
                session.LastAction = action;
                session.LastActionBy = string.IsNullOrWhiteSpace(request.ActorName) ? request.Source : request.ActorName.Trim();
                session.FrameText = Render(session);
                session.FrameCaption = BuildCaption(session);
                session.FrameRenderer = "LocalGPT deterministic preview renderer";
                session.FrameOwnerTurn = session.Turn;
                session.FrameOwner = session.FrameRenderer;
                session.UpdatedAtUtc = DateTime.UtcNow;

                if (session.ControlMode == CouncilGameControlMode.Ai)
                {
                    session.CurrentTurnOwner = "AI Player Controller";
                    session.InputReason = "AI player may submit the next action through localgpt.game.control.";
                }
                else
                {
                    session.CurrentTurnOwner = "Human Player";
                    session.HumanInputRequired = true;
                    session.InputReason = "Your turn: controls are visible again after the resolved frame update.";
                }
            }

            Notify(session.Id);
            logger.LogInformation(
                "Applied game control {Action} to session {GameSessionId} at turn {Turn} from {ControlSource}.",
                session.LastAction,
                session.Id,
                session.Turn,
                string.IsNullOrWhiteSpace(request.Source) ? "unknown" : request.Source);
            return Task.FromResult(ToSnapshot(session));
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

    private void EnsureAutoplayLoop(CouncilGameSessionState session)
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

    private string SelectAutoplayAction(CouncilGameSessionSnapshot snapshot)
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

    private void StopAutoplayLoop(Guid sessionId)
    {
        if (autoplayLoops.TryRemove(sessionId, out var cancellation))
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
    }

    private int NormalizeAutoplayDelay(int value) => Math.Clamp(value, 250, 10_000);

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref disposed) != 0)
            throw new ObjectDisposedException(nameof(CouncilGameSessionService));
    }

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

    private string NormalizeGameKey(string? gameKey)
    {
        var normalized = (gameKey ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
        return normalized switch
        {
            "doom" or "ascii-doom" or "ascii-doom-council-adventure" => "ascii-doom",
            "dragon" or "green-dragon" or "lotgd" or "green-dragon-runtime-story" => "green-dragon",
            _ => throw new ArgumentException($"Unsupported game key '{gameKey}'. Use ascii-doom or green-dragon.", nameof(gameKey))
        };
    }

    private string DefaultTeamFor(string gameKey) =>
        gameKey == "green-dragon" ? "green-dragon-runtime-story" : "ascii-doom-council-adventure";

    private List<string> BuildLegalActions(string gameKey) => gameKey == "green-dragon"
        ? ["move-forward", "move-backward", "turn-left", "turn-right", "use", "choice-1", "choice-2", "choice-3"]
        : ["move-forward", "move-backward", "strafe-left", "strafe-right", "turn-left", "turn-right", "shoot", "duck", "use"];

    private List<RuntimeInputBindingDefinition> BuildInputBindings(string gameKey) => gameKey == "green-dragon"
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

    private RuntimeInputBindingDefinition Binding(string action, string display, string keyboard, string gamepad) => new()
    {
        Action = action,
        DisplayName = display,
        KeyboardKey = keyboard,
        GamepadButton = gamepad,
        Description = "The human UI and AI Player Controller call the same localgpt.game.control action."
    };

    private string NormalizeAction(string? action, double? axisX, double? axisY)
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

    private void ApplyAction(CouncilGameSessionState session, string action, int? aimX, int? aimY)
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

    private void TryMove(CouncilGameSessionState session, double dx, double dy)
    {
        var map = doomMap;
        var nextX = session.PlayerX + (int)Math.Round(dx);
        var nextY = session.PlayerY + (int)Math.Round(dy);
        if (nextY < 0 || nextY >= map.Length || nextX < 0 || nextX >= map[nextY].Length) return;
        if (map[nextY][nextX] == '#') return;
        session.PlayerX = nextX;
        session.PlayerY = nextY;
    }

    private string Render(CouncilGameSessionState session) => session.GameKey == "green-dragon"
        ? RenderGreenDragon(session)
        : RenderDoomLike(session);

    private string RenderDoomLike(CouncilGameSessionState session)
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

    private string RenderGreenDragon(CouncilGameSessionState session)
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

    private double CastRay(double x, double y, double angle)
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

    private char WallGlyph(double distance, int x, int y) => distance switch
    {
        < 2.2d => (x + y) % 2 == 0 ? '█' : '▓',
        < 4.2d => (x + y) % 3 == 0 ? '▓' : '▒',
        < 7.5d => '▒',
        _ => '░'
    };

    private char FloorGlyph(int x, int y) => (x + y) % 5 == 0 ? ':' : (x + y) % 2 == 0 ? '.' : ' ';

    private void Put(char[][] lines, int row, int column, string text)
    {
        if (row < 0 || row >= lines.Length) return;
        for (var index = 0; index < text.Length && column + index < lines[row].Length; index++)
            lines[row][column + index] = text[index];
    }

    private string Fit(string text, int width)
    {
        var normalized = (text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        if (normalized.Length > width) return normalized[..width];
        return normalized.PadRight(width);
    }

    private string NormalizeFrame(string frame, int width, int height)
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

    private string BuildCaption(CouncilGameSessionState session) =>
        $"{session.DisplayName} · turn {session.Turn} · {session.CurrentTurnOwner} · renderer {session.FrameRenderer}";

    private string Compass(double radians)
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

    private double NormalizeRadians(double value)
    {
        while (value < 0) value += Math.PI * 2d;
        while (value >= Math.PI * 2d) value -= Math.PI * 2d;
        return value;
    }

    private CouncilGameSessionSnapshot ToSnapshot(CouncilGameSessionState session)
    {
        lock (session.SyncRoot)
        {
            return new CouncilGameSessionSnapshot
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
    }

    private RuntimeInputBindingDefinition CloneBinding(RuntimeInputBindingDefinition binding) => new()
    {
        Action = binding.Action,
        DisplayName = binding.DisplayName,
        KeyboardKey = binding.KeyboardKey,
        GamepadButton = binding.GamepadButton,
        Description = binding.Description
    };

    private void Notify(Guid sessionId)
    {
        var listeners = Changed?.GetInvocationList().Cast<Action<Guid>>().ToArray() ?? [];
        foreach (var listener in listeners)
        {
            try { listener(sessionId); }
            catch (Exception ex) { logger.LogWarning(ex, "A Council game UI listener failed for session {GameSessionId}.", sessionId); }
        }
    }

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
