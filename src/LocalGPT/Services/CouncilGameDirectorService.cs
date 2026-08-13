using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Provides deterministic final authority for game controls and coordinates replaceable creature/object subdirectors.
/// A configured low-parameter Council model may review the proposal later, but cannot bypass this service.
/// </summary>
/// <param name="subdirectors">Council game subdirector dependency used by the council game director workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilGameDirectorService(
    IEnumerable<ICouncilGameSubdirector> subdirectors,
    ILogger<CouncilGameDirectorService> logger) : ICouncilGameDirectorService
{
    /// <summary>
    /// Gets the actor directors collection maintained or exposed by this council game director instance for downstream processing.
    /// </summary>
    /// <value>The actor directors value exposed by <see cref="CouncilGameDirectorService"/>.</value>
    private readonly IReadOnlyList<ICouncilGameSubdirector> actorDirectors = subdirectors
        .OrderBy(item => item.ActorKind)
        .ThenBy(item => item.Key, StringComparer.Ordinal)
        .ToArray();

    /// <summary>
    /// Performs evaluate as part of the council game director service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    /// <param name="context">Context value supplied to the council game director operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game director decision produced by the operation.</returns>
    public async Task<CouncilGameDirectorDecision> EvaluateAsync(
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
            var predictions = new List<CouncilGameSubdirectorPrediction>(actorDirectors.Count);
            foreach (var subdirector in actorDirectors)
            {
                predictions.Add(await subdirector.PredictAsync(context, cancellationToken).ConfigureAwait(false));
            }

            var modelName = context.DirectorMode == CouncilGameDirectorMode.CouncilModelPreferred
                ? context.DirectorModelName?.Trim() ?? string.Empty
                : string.Empty;
            var reviewNote = context.DirectorMode == CouncilGameDirectorMode.CouncilModelPreferred
                ? string.IsNullOrWhiteSpace(modelName)
                    ? " A Council-model review was requested, but no director model is configured; deterministic authority was used."
                    : $" Council model '{modelName}' is the configured reviewer; deterministic authority remains final."
                : string.Empty;

            return new CouncilGameDirectorDecision
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
                Predictions = predictions
            };
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

/// <summary>Predicts bounded creature reactions for the next authoritative world step.</summary>
/// <param name="actorFactory">Council game actor runtime factory dependency used by the creature council game subdirector workflow to provide the corresponding application capability.</param>
public sealed class CreatureCouncilGameSubdirector(
    ICouncilGameActorRuntimeFactory actorFactory) : ICouncilGameSubdirector
{
    /// <summary>
    /// Gets the stable key used to identify or correlate this creature council game subdirector instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="CreatureCouncilGameSubdirector"/>.</value>
    public string Key => "creature-council";
    /// <summary>
    /// Gets the actor kind value that forms part of the creature council game subdirector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="CreatureCouncilGameSubdirector"/>.</value>
    public CouncilGameActorKind ActorKind => CouncilGameActorKind.Creature;

    /// <summary>
    /// Performs predict for <see cref="CreatureCouncilGameSubdirector"/>, keeping the operation consistent with the state and invariants of the surrounding creature council game subdirector workflow.
    /// </summary>
    /// <param name="context">Context value supplied to the creature council game subdirector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game subdirector prediction produced by the operation.</returns>
    public Task<CouncilGameSubdirectorPrediction> PredictAsync(
        CouncilGameDirectorContext context,
        CancellationToken cancellationToken = default)
    {
    try
    {
            cancellationToken.ThrowIfCancellationRequested();
            var noisyAction = context.NormalizedAction is "shoot" or "use";
            var actors = actorFactory.CreateActors(context, ActorKind);
            return Task.FromResult(new CouncilGameSubdirectorPrediction
            {
                DirectorKey = Key,
                ActorKind = ActorKind,
                RuntimeClassKey = "games.ascii.doom.creature",
                Prediction = noisyAction
                    ? $"{Math.Clamp(context.Session.CreatureDirectorCount, 1, 8)} configured creature director(s) may investigate the sound during the resolved world step."
                    : $"{Math.Clamp(context.Session.CreatureDirectorCount, 1, 8)} configured creature director(s) may keep patrol state or approach only when line-of-sight rules permit.",
                ConfidencePercent = 70,
                ActorInstances = actors
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method CreatureCouncilGameSubdirector.PredictAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>Predicts bounded reactions from doors, switches, pickups and hazards.</summary>
/// <param name="actorFactory">Council game actor runtime factory dependency used by the reactive object council game subdirector workflow to provide the corresponding application capability.</param>
public sealed class ReactiveObjectCouncilGameSubdirector(
    ICouncilGameActorRuntimeFactory actorFactory) : ICouncilGameSubdirector
{
    /// <summary>
    /// Gets the stable key used to identify or correlate this reactive object council game subdirector instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="ReactiveObjectCouncilGameSubdirector"/>.</value>
    public string Key => "reactive-object-council";
    /// <summary>
    /// Gets the actor kind value that forms part of the reactive object council game subdirector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="ReactiveObjectCouncilGameSubdirector"/>.</value>
    public CouncilGameActorKind ActorKind => CouncilGameActorKind.ReactiveObject;

    /// <summary>
    /// Performs predict for <see cref="ReactiveObjectCouncilGameSubdirector"/>, keeping the operation consistent with the state and invariants of the surrounding reactive object council game subdirector workflow.
    /// </summary>
    /// <param name="context">Context value supplied to the reactive object council game subdirector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game subdirector prediction produced by the operation.</returns>
    public Task<CouncilGameSubdirectorPrediction> PredictAsync(
        CouncilGameDirectorContext context,
        CancellationToken cancellationToken = default)
    {
    try
    {
            cancellationToken.ThrowIfCancellationRequested();
            var interaction = context.NormalizedAction == "use";
            var actors = actorFactory.CreateActors(context, ActorKind);
            return Task.FromResult(new CouncilGameSubdirectorPrediction
            {
                DirectorKey = Key,
                ActorKind = ActorKind,
                RuntimeClassKey = "games.ascii.doom.reactive-object",
                Prediction = interaction
                    ? "The nearest eligible door, switch or pickup may react after range and state checks."
                    : "Reactive objects remain unchanged unless movement enters a trigger volume.",
                ConfidencePercent = 75,
                ActorInstances = actors
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method ReactiveObjectCouncilGameSubdirector.PredictAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>Creates stable per-turn actor descriptors without granting them state-mutation authority.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilGameActorRuntimeFactory(
    ILogger<CouncilGameActorRuntimeFactory> logger) : ICouncilGameActorRuntimeFactory
{
    /// <summary>
    /// Creates actors using the configuration and dependencies owned by <see cref="CouncilGameActorRuntimeFactory"/>.
    /// </summary>
    /// <inheritdoc />
    /// <param name="context">Context value supplied to the council game actor runtime operation and used when producing its result.</param>
    /// <param name="actorKind">Actor kind value supplied to the council game actor runtime operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<CouncilGameActorRuntimeDescriptor> CreateActors(
        CouncilGameDirectorContext context,
        CouncilGameActorKind actorKind)
    {
        var sessionId = context?.Session?.Id;
        try
        {
            ArgumentNullException.ThrowIfNull(context);
            if (actorKind == CouncilGameActorKind.Creature)
            {
                var count = Math.Clamp(context.Session.CreatureDirectorCount, 1, 8);
                return Enumerable.Range(1, count)
                    .Select(index => new CouncilGameActorRuntimeDescriptor
                    {
                        InstanceKey = $"creature-{index}",
                        ActorKind = actorKind,
                        RuntimeClassKey = "games.ascii.doom.creature",
                        Archetype = index % 2 == 0 ? "hunter" : "patrol",
                        CouncilRole = "World Actor",
                        CouncilAssignmentGroup = "ascii-doom-actors",
                        CouncilAssignmentSlot = $"world-actor-{index}"
                    })
                    .ToArray();
            }

            if (actorKind == CouncilGameActorKind.ReactiveObject)
            {
                return new CouncilGameActorRuntimeDescriptor[]
                {
                    new()
                    {
                        InstanceKey = "reactive-door-1",
                        ActorKind = actorKind,
                        RuntimeClassKey = "games.ascii.doom.reactive-object",
                        Archetype = "door",
                        CouncilRole = "World Actor",
                        CouncilAssignmentGroup = "ascii-doom-actors",
                        CouncilAssignmentSlot = "world-object-1"
                    },
                    new()
                    {
                        InstanceKey = "reactive-trigger-1",
                        ActorKind = actorKind,
                        RuntimeClassKey = "games.ascii.doom.reactive-object",
                        Archetype = "trigger",
                        CouncilRole = "World Actor",
                        CouncilAssignmentGroup = "ascii-doom-actors",
                        CouncilAssignmentSlot = "world-object-2"
                    }
                };
            }

            return Array.Empty<CouncilGameActorRuntimeDescriptor>();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating Council game actor runtime descriptors failed for session {GameSessionId} and actor kind {ActorKind}.", sessionId, actorKind);
            throw;
        }
    }
}
