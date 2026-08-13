using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council runtime class behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilRuntimeClassService
{
    /// <summary>
    /// Retrieves definitions as part of the council runtime class service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeDisabled">Value indicating whether include disabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CouncilRuntimeClassDefinition>> GetDefinitionsAsync(
        bool includeDisabled = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs find as part of the council runtime class service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the council runtime class operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council runtime class definition produced by the operation.</returns>
    Task<CouncilRuntimeClassDefinition?> FindAsync(
        string? key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs save as part of the council runtime class service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council runtime class definition produced by the operation.</returns>
    Task<CouncilRuntimeClassDefinition> SaveAsync(
        SaveCouncilRuntimeClassRequest request,
        CancellationToken cancellationToken = default);
}
