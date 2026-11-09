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
