using LocalGPT.BusinessObjects;
using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for organic skill registry behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicSkillRegistryService
{
    /// <summary>
    /// Retrieves skills as part of the organic skill registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeDisabled">Value indicating whether include disabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OrganicSkillDefinition>> GetSkillsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists skill as part of the organic skill registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic skill definition produced by the operation.</returns>
    Task<OrganicSkillDefinition> SaveSkillAsync(SaveOrganicSkillRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Links project as part of the organic skill registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project organic skill link produced by the operation.</returns>
    Task<ProjectOrganicSkillLink> LinkProjectAsync(LinkProjectOrganicSkillRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs report member skill as part of the organic skill registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council member organic skill link produced by the operation.</returns>
    Task<CouncilMemberOrganicSkillLink> ReportMemberSkillAsync(ReportCouncilMemberSkillRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves wire skills as part of the organic skill registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireSkillDescriptor>> GetWireSkillsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs record untrusted self assessment as part of the organic skill registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="assessment">Assessment value supplied to the organic skill registry operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RecordUntrustedSelfAssessmentAsync(LocalGPT.WireProtocol.OneWireModelSelfAssessment assessment, CancellationToken cancellationToken = default);
}
