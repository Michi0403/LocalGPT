using LocalGPT.BusinessObjects;
using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the organic skill registry service contract.
/// </summary>
public interface IOrganicSkillRegistryService
{
    /// <summary>
    /// Gets skills async.
    /// </summary>
    Task<IReadOnlyList<OrganicSkillDefinition>> GetSkillsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves skill async.
    /// </summary>
    Task<OrganicSkillDefinition> SaveSkillAsync(SaveOrganicSkillRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the link project async operation.
    /// </summary>
    Task<ProjectOrganicSkillLink> LinkProjectAsync(LinkProjectOrganicSkillRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the report member skill async operation.
    /// </summary>
    Task<CouncilMemberOrganicSkillLink> ReportMemberSkillAsync(ReportCouncilMemberSkillRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets wire skills async.
    /// </summary>
    Task<IReadOnlyList<OneWireSkillDescriptor>> GetWireSkillsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the record untrusted self assessment async operation.
    /// </summary>
    Task RecordUntrustedSelfAssessmentAsync(LocalGPT.WireProtocol.OneWireModelSelfAssessment assessment, CancellationToken cancellationToken = default);
}
