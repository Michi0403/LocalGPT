using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the local gpt request factory service contract.
/// </summary>
public interface ILocalGptRequestFactoryService
{
    /// <summary>
    /// Creates project request.
    /// </summary>
    SaveLocalGptProjectRequest CreateProjectRequest();
    /// <summary>
    /// Creates topic request.
    /// </summary>
    AddLocalGptProjectTopicRequest CreateTopicRequest();
    /// <summary>
    /// Creates version request.
    /// </summary>
    AddLocalGptProjectVersionRequest CreateVersionRequest(string path = "");
    /// <summary>
    /// Creates revision request.
    /// </summary>
    SaveProjectRevisionRequest CreateRevisionRequest();
    /// <summary>
    /// Creates requirement request.
    /// </summary>
    SaveProjectRequirementRequest CreateRequirementRequest();
    /// <summary>
    /// Creates requirement link request.
    /// </summary>
    SaveProjectRequirementLinkRequest CreateRequirementLinkRequest();
    /// <summary>
    /// Creates artifact request.
    /// </summary>
    SaveProjectArtifactRequest CreateArtifactRequest();
    /// <summary>
    /// Creates workspace root request.
    /// </summary>
    SaveProjectWorkspaceRootRequest CreateWorkspaceRootRequest();
    /// <summary>
    /// Creates compiler installation request.
    /// </summary>
    SaveProjectCompilerInstallationRequest CreateCompilerInstallationRequest();
}
