using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Reads, resolves and user-confirmedly persists database-owned Council team and workflow definitions.
/// </summary>
[DocumentationUpdated("2.1.20")]
public interface ICouncilTeamConfigurationService
{
    /// <summary>Returns enabled or complete Council-team configuration records.</summary>
    /// <param name="includeDisabled">Whether disabled teams are included.</param>
    /// <param name="cancellationToken">Cancels the asynchronous database operation.</param>
    /// <returns>A task that completes with the ordered Council-team definitions.</returns>
    Task<IReadOnlyList<OrganicCouncilTeamDefinition>> GetTeamsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);

    /// <summary>Finds one enabled Council team by stable key, falling back to the general key for blank input.</summary>
    /// <param name="key">Stable Council-team key.</param>
    /// <param name="cancellationToken">Cancels the asynchronous database operation.</param>
    /// <returns>A task that completes with the team definition, or null when it is unavailable.</returns>
    Task<OrganicCouncilTeamDefinition?> FindTeamAsync(string? key, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates one Council team after explicit user confirmation and validation.</summary>
    /// <param name="request">Confirmed team save request.</param>
    /// <param name="cancellationToken">Cancels the asynchronous database operation.</param>
    /// <returns>A task that completes with the persisted normalized definition.</returns>
    Task<OrganicCouncilTeamDefinition> SaveAsync(SaveCouncilTeamConfigurationRequest request, CancellationToken cancellationToken = default);
}
