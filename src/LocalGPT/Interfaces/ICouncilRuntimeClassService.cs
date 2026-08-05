using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilRuntimeClassService
{
    Task<IReadOnlyList<CouncilRuntimeClassDefinition>> GetDefinitionsAsync(
        bool includeDisabled = false,
        CancellationToken cancellationToken = default);

    Task<CouncilRuntimeClassDefinition?> FindAsync(
        string? key,
        CancellationToken cancellationToken = default);

    Task<CouncilRuntimeClassDefinition> SaveAsync(
        SaveCouncilRuntimeClassRequest request,
        CancellationToken cancellationToken = default);
}
