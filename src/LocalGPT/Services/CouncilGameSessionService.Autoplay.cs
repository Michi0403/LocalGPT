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

    }
}
