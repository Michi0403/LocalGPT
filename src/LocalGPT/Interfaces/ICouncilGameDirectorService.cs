using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Acts as the authoritative game engine boundary. Controllers and Council actors may propose actions,
/// but only an approved director decision may advance a session.
/// </summary>
public interface ICouncilGameDirectorService
{
    /// <summary>Validates one proposal and gathers bounded creature/object predictions without mutating session state.</summary>
    /// <param name="context">Context value supplied to the council game director operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game director decision produced by the operation.</returns>
    Task<CouncilGameDirectorDecision> EvaluateAsync(
        CouncilGameDirectorContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Predicts consequences for one family of runtime actors without owning the authoritative state.</summary>
public interface ICouncilGameSubdirector
{
    /// <summary>
    /// Gets the stable key used to identify or correlate this council game subdirector instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="ICouncilGameSubdirector"/>.</value>
    string Key { get; }
    /// <summary>
    /// Gets the actor kind value that forms part of the council game subdirector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="ICouncilGameSubdirector"/>.</value>
    CouncilGameActorKind ActorKind { get; }

    /// <summary>Produces a bounded prediction for the proposed action.</summary>
    /// <param name="context">Context value supplied to the council game subdirector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game subdirector prediction produced by the operation.</returns>
    Task<CouncilGameSubdirectorPrediction> PredictAsync(
        CouncilGameDirectorContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Creates bounded runtime actor instances and maps them to Council role slots.</summary>
public interface ICouncilGameActorRuntimeFactory
{
    /// <summary>Creates the actor instances owned by one subdirector for the current immutable turn snapshot.</summary>
    /// <param name="context">Context value supplied to the council game actor runtime operation and used when producing its result.</param>
    /// <param name="actorKind">Actor kind value supplied to the council game actor runtime operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<CouncilGameActorRuntimeDescriptor> CreateActors(
        CouncilGameDirectorContext context,
        CouncilGameActorKind actorKind);
}
