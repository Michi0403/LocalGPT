using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Services;

/// <summary>
/// Advances games whose turn model is driven by Council workflow steps. The game's council progression rule decides
/// the outcome; this partial only commits the produced frames and texts to the authoritative session.
/// </summary>
public sealed partial class CouncilGameSessionService
{
    /// <inheritdoc />
    public Task<GameCouncilResolution> AdvanceCouncilTurnAsync(
        GameCouncilAdvanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessions.TryGetValue(request.SessionId, out var session))
                throw new KeyNotFoundException($"Council game session {request.SessionId} was not found.");

            GameCouncilStepOutcome outcome;
            lock (session.SyncRoot)
            {
                if (!engine.IsCouncilDriven(session.Ruleset))
                    throw new InvalidOperationException($"Game session {session.Id} ({session.GameKey}) is not driven by Council turns.");
                if (session.Status != "Running")
                    throw new InvalidOperationException("The game session is not running.");
                outcome = engine.AdvanceCouncil(session, request);
                ApplyCouncilOutcome(session, outcome);
            }

            Notify(session.Id);
            logger.LogInformation(
                "Advanced Council-driven game session {GameSessionId} ({GameKey}) to turn {Turn} with {FrameCount} frame(s); initializeOnly={InitializeOnly}, completed={Completed}. AI content was omitted.",
                session.Id,
                session.GameKey,
                session.Turn,
                outcome.Frames.Count,
                request.InitializeOnly,
                outcome.Completed);
            return Task.FromResult(new GameCouncilResolution
            {
                Session = ToSnapshot(session),
                SummaryMarkdown = outcome.SummaryMarkdown,
                Completed = outcome.Completed
            });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Advancing a Council-driven game session was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Advancing Council-driven game session {GameSessionId} failed; AI content was omitted.", request?.SessionId);
            throw;
        }
    }

    /// <summary>
    /// Commits one council progression outcome: the animation frames with their style runs (view-provided runs, otherwise
    /// the ruleset's semantic style rules), the resting frame, subtitle, caption and turn ownership.
    /// </summary>
    /// <param name="session">Locked session that receives the outcome.</param>
    /// <param name="outcome">Outcome produced by the game's council progression rule.</param>
    private void ApplyCouncilOutcome(CouncilGameSessionState session, GameCouncilStepOutcome outcome)
    {
        try
        {
            var frames = outcome.Frames.Where(frame => frame is not null).ToList();
            session.AnimationFrames = frames
                .Select(frame => NormalizeFrame(frame.Text, session.FrameWidth, session.FrameHeight))
                .ToList();
            session.AnimationFrameStyleRuns = frames
                .Select((frame, index) => frame.StyleRuns is not null
                    ? NormalizeAsciiStyleRuns(frame.StyleRuns, session.FrameWidth, session.FrameHeight, CouncilAsciiColorMode.TerminalDefault)
                    : BuildSemanticAsciiStyleRuns(session, session.AnimationFrames[index]))
                .ToList();
            session.AnimationSubtitle = outcome.Subtitle;
            session.AnimationSubtitleStyle = outcome.SubtitleStyle;
            session.AnimationDelayMilliseconds = outcome.FrameDelayMilliseconds;
            session.AnimationSubtitleHoldMilliseconds = outcome.SubtitleHoldMilliseconds;

            if (session.AnimationFrames.Count > 0)
            {
                session.FrameText = session.AnimationFrames[^1];
                session.FrameStyleRuns = CloneAsciiStyleRuns(session.AnimationFrameStyleRuns[^1]);
            }
            else if (!string.IsNullOrEmpty(outcome.RestingFrame.Text))
            {
                session.FrameText = NormalizeFrame(outcome.RestingFrame.Text, session.FrameWidth, session.FrameHeight);
                session.FrameStyleRuns = outcome.RestingFrame.StyleRuns is not null
                    ? NormalizeAsciiStyleRuns(outcome.RestingFrame.StyleRuns, session.FrameWidth, session.FrameHeight, CouncilAsciiColorMode.TerminalDefault)
                    : BuildSemanticAsciiStyleRuns(session, session.FrameText);
            }
            else
            {
                RenderAuthoritativeFrame(session);
                if (session.FrameStyleRuns.Count == 0)
                    session.FrameStyleRuns = BuildSemanticAsciiStyleRuns(session, session.FrameText);
            }

            session.FrameCaption = outcome.Caption;
            session.FrameRenderer = outcome.Renderer;
            session.CurrentTurnOwner = outcome.TurnOwner;
            session.LastAction = outcome.LastAction;
            session.LastActionBy = outcome.TurnOwner;
            session.Turn++;
            session.UpdatedAtUtc = DateTime.UtcNow;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Committing a council progression outcome to game session {GameSessionId} failed.", session.Id);
            throw;
        }
    }
}
