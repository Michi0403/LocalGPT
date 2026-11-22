using LocalGPT.BusinessObjects;
namespace LocalGPT.Interfaces;
/// <summary>
/// Service contract for council role assignment operations within the Council team architecture.
/// Provides type-safe interfaces for listing and assigning OrganicCouncilTeamDefinition roles.
/// </summary>
public interface ICouncilRoleAssignmentService
{
    /// <summary>
    /// Enumerates available role assignment choices for a specified model key.
    /// Retrieves all OrganicCouncilTeamDefinition instances that can be assigned to the model.
    /// </summary>
    /// <param name="modelKey">The model identifier to look up role assignments for.</param>
    /// <param name="cancellationToken">Token to allow operation cancellation during enumeration.</param>
    /// <returns>A read-only list of available role assignment choices.</returns>
    Task<IReadOnlyList<CouncilRoleAssignmentChoice>> ListForModelAsync(string modelKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a council role assignment after human approval.
    /// Assigns an OrganicCouncilTeamDefinition to the specified model with role configuration.
    /// </summary>
    /// <param name="request">The assignment request containing model and team definitions.</param>
    /// <param name="cancellationToken">Token to allow operation cancellation during persistence.</param>
    /// <returns>The persisted OrganicCouncilTeamDefinition after assignment.</returns>
    Task<OrganicCouncilTeamDefinition> AssignAsync(CouncilRoleAssignmentRequest request, CancellationToken cancellationToken = default);
}