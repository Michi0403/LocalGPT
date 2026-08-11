using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council runtime class service contract.
/// </summary>
public interface ICouncilRuntimeClassService
{
    /// <summary>
    /// Gets definitions async.
    /// </summary>
    Task<IReadOnlyList<CouncilRuntimeClassDefinition>> GetDefinitionsAsync(
        bool includeDisabled = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds async.
    /// </summary>
    Task<CouncilRuntimeClassDefinition?> FindAsync(
        string? key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves async.
    /// </summary>
    Task<CouncilRuntimeClassDefinition> SaveAsync(
        SaveCouncilRuntimeClassRequest request,
        CancellationToken cancellationToken = default);
}
