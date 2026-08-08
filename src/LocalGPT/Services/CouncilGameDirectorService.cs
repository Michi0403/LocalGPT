using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Provides deterministic final authority for game controls and coordinates replaceable creature/object subdirectors.
/// A configured low-parameter Council model may review the proposal later, but cannot bypass this service.
/// </summary>
public sealed class CouncilGameDirectorService(
    IEnumerable<ICouncilGameSubdirector> subdirectors,
    ILogger<CouncilGameDirectorService> logger) : ICouncilGameDirectorService
{
    private readonly IReadOnlyList<ICouncilGameSubdirector> actorDirectors = subdirectors
        .OrderBy(item => item.ActorKind)
        .ThenBy(item => item.Key, StringComparer.Ordinal)
        .ToArray();

    /// <inheritdoc />
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
public sealed class CreatureCouncilGameSubdirector(
    ICouncilGameActorRuntimeFactory actorFactory) : ICouncilGameSubdirector
{
    public string Key => "creature-council";
    public CouncilGameActorKind ActorKind => CouncilGameActorKind.Creature;

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
public sealed class ReactiveObjectCouncilGameSubdirector(
    ICouncilGameActorRuntimeFactory actorFactory) : ICouncilGameSubdirector
{
    public string Key => "reactive-object-council";
    public CouncilGameActorKind ActorKind => CouncilGameActorKind.ReactiveObject;

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
public sealed class CouncilGameActorRuntimeFactory(
    ILogger<CouncilGameActorRuntimeFactory> logger) : ICouncilGameActorRuntimeFactory
{
    /// <inheritdoc />
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
