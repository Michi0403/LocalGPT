using LocalGPT.BusinessObjects;
namespace LocalGPT.Interfaces;
public interface ICouncilRoleAssignmentService
{
    Task<IReadOnlyList<CouncilRoleAssignmentChoice>> ListForModelAsync(string modelKey, CancellationToken cancellationToken = default);
    Task<OrganicCouncilTeamDefinition> AssignAsync(CouncilRoleAssignmentRequest request, CancellationToken cancellationToken = default);
}
