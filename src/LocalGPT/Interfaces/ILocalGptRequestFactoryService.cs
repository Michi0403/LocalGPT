using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for LocalGPT request factory behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalGptRequestFactoryService
{
    /// <summary>
    /// Creates project request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save LocalGPT project request produced by the operation.</returns>
    SaveLocalGptProjectRequest CreateProjectRequest();
    /// <summary>
    /// Creates topic request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The add LocalGPT project topic request produced by the operation.</returns>
    AddLocalGptProjectTopicRequest CreateTopicRequest();
    /// <summary>
    /// Creates version request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the LocalGPT request factory operation and used when producing its result.</param>
    /// <returns>The add LocalGPT project version request produced by the operation.</returns>
    AddLocalGptProjectVersionRequest CreateVersionRequest(string path = "");
    /// <summary>
    /// Creates revision request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project revision request produced by the operation.</returns>
    SaveProjectRevisionRequest CreateRevisionRequest();
    /// <summary>
    /// Creates requirement request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project requirement request produced by the operation.</returns>
    SaveProjectRequirementRequest CreateRequirementRequest();
    /// <summary>
    /// Creates requirement link request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project requirement link request produced by the operation.</returns>
    SaveProjectRequirementLinkRequest CreateRequirementLinkRequest();
    /// <summary>
    /// Creates artifact request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project artifact request produced by the operation.</returns>
    SaveProjectArtifactRequest CreateArtifactRequest();
    /// <summary>
    /// Creates workspace root request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project workspace root request produced by the operation.</returns>
    SaveProjectWorkspaceRootRequest CreateWorkspaceRootRequest();
    /// <summary>
    /// Creates compiler installation request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project compiler installation request produced by the operation.</returns>
    SaveProjectCompilerInstallationRequest CreateCompilerInstallationRequest();
}
