using LocalGPT.BusinessObjects;
using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

public interface IOrganicSkillRegistryService
{
    Task<IReadOnlyList<OrganicSkillDefinition>> GetSkillsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);
    Task<OrganicSkillDefinition> SaveSkillAsync(SaveOrganicSkillRequest request, CancellationToken cancellationToken = default);
    Task<ProjectOrganicSkillLink> LinkProjectAsync(LinkProjectOrganicSkillRequest request, CancellationToken cancellationToken = default);
    Task<CouncilMemberOrganicSkillLink> ReportMemberSkillAsync(ReportCouncilMemberSkillRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneWireSkillDescriptor>> GetWireSkillsAsync(CancellationToken cancellationToken = default);
    Task RecordUntrustedSelfAssessmentAsync(LocalGPT.WireProtocol.OneWireModelSelfAssessment assessment, CancellationToken cancellationToken = default);
}
