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

    /// <summary>Returns the maintained supplied team templates independently from user-owned configured rows.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The resettable default template catalog.</returns>
    Task<IReadOnlyList<OrganicCouncilTeamDefinition>> GetDefaultTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks one configured team deleted after explicit user confirmation; supplied templates remain available for later reset.</summary>
    /// <param name="key">Configured team key.</param>
    /// <param name="userConfirmed">Whether the user confirmed the destructive action.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that completes after the deletion tombstone is persisted.</returns>
    Task DeleteAsync(string key, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Replaces one configured team's behavior with any supplied default template while preserving the configured team key.</summary>
    /// <param name="targetKey">Configured team key to replace or restore.</param>
    /// <param name="templateKey">Supplied template key.</param>
    /// <param name="userConfirmed">Whether the user confirmed the reset.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>The reset persisted team.</returns>
    Task<OrganicCouncilTeamDefinition> ResetToTemplateAsync(string targetKey, string templateKey, bool userConfirmed, CancellationToken cancellationToken = default);
}
