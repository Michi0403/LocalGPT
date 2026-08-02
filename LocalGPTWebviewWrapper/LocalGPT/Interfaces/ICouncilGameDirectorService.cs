using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Acts as the authoritative game engine boundary. Controllers and Council actors may propose actions,
/// but only an approved director decision may advance a session.
/// </summary>
public interface ICouncilGameDirectorService
{
    /// <summary>Validates one proposal and gathers bounded creature/object predictions without mutating session state.</summary>
    Task<CouncilGameDirectorDecision> EvaluateAsync(
        CouncilGameDirectorContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Predicts consequences for one family of runtime actors without owning the authoritative state.</summary>
public interface ICouncilGameSubdirector
{
    string Key { get; }
    CouncilGameActorKind ActorKind { get; }

    /// <summary>Produces a bounded prediction for the proposed action.</summary>
    Task<CouncilGameSubdirectorPrediction> PredictAsync(
        CouncilGameDirectorContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Creates bounded runtime actor instances and maps them to Council role slots.</summary>
public interface ICouncilGameActorRuntimeFactory
{
    /// <summary>Creates the actor instances owned by one subdirector for the current immutable turn snapshot.</summary>
    IReadOnlyList<CouncilGameActorRuntimeDescriptor> CreateActors(
        CouncilGameDirectorContext context,
        CouncilGameActorKind actorKind);
}
