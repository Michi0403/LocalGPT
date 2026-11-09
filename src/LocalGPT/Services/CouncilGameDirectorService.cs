using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services;

/// <summary>
/// Provides deterministic final authority for game controls. The advisory subdirector predictions it attaches are the
/// running game's <c>director.prediction</c> rule rows; a configured low-parameter Council model may review the proposal
/// later, but cannot bypass this service.
/// </summary>
/// <param name="predictions">Builds the game's declarative subdirector predictions.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilGameDirectorService(
    IGameDirectorPredictionService predictions,
    ILogger<CouncilGameDirectorService> logger) : ICouncilGameDirectorService
{
    /// <summary>
    /// Evaluates one proposal against the running session's turn and legal-action contract and gathers the game's
    /// advisory predictions without mutating session state.
    /// </summary>
    /// <param name="context">Context value supplied to the council game director operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game director decision produced by the operation.</returns>
    public Task<CouncilGameDirectorDecision> EvaluateAsync(
        CouncilGameDirectorContext context,
        CancellationToken cancellationToken = default)
    {
        var sessionId = context?.Session?.Id;
        try
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            var action = context.NormalizedAction?.Trim() ?? string.Empty;
            var legal = context.Session.Status == "Running" &&
                        context.Session.LegalActions.Contains(action, StringComparer.OrdinalIgnoreCase);
            // Authored Game Studio scenes carry their own artist-authored actors; the game's declarative predictions
            // describe engine-run worlds and are not injected into a project revision's scene.
            var subdirectorPredictions = context.Session.Scene is null
                ? predictions.Predict(context).ToList()
                : new List<CouncilGameSubdirectorPrediction>();

            var modelName = context.DirectorMode == CouncilGameDirectorMode.CouncilModelPreferred
                ? context.DirectorModelName?.Trim() ?? string.Empty
                : string.Empty;
            var reviewNote = context.DirectorMode == CouncilGameDirectorMode.CouncilModelPreferred
                ? string.IsNullOrWhiteSpace(modelName)
                    ? " A Council-model review was requested, but no director model is configured; deterministic authority was used."
                    : $" Council model '{modelName}' is the configured reviewer; deterministic authority remains final."
                : string.Empty;

            return Task.FromResult(new CouncilGameDirectorDecision
            {
                SessionId = context.Session.Id,
                ExpectedTurn = context.Session.Turn,
                Approved = legal,
                NormalizedAction = action,
                DirectorMode = context.DirectorMode,
                DirectorModelName = modelName,
                Reason = legal
                    ? "The proposal matches the current turn, running session and legal-action contract." + reviewNote
                    : "The proposal was rejected because the session is not running or the action is outside the current legal-action contract." + reviewNote,
                Predictions = subdirectorPredictions
            });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "GameDirector evaluation was cancelled for session {GameSessionId}.", sessionId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "GameDirector evaluation failed for session {GameSessionId}; proposal content was omitted.", sessionId);
            throw;
        }
    }
}
