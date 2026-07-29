using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ILocalGptRequestFactoryService
{
    SaveLocalGptProjectRequest CreateProjectRequest();
    AddLocalGptProjectTopicRequest CreateTopicRequest();
    AddLocalGptProjectVersionRequest CreateVersionRequest(string path = "");
    SaveProjectRevisionRequest CreateRevisionRequest();
    SaveProjectRequirementRequest CreateRequirementRequest();
    SaveProjectRequirementLinkRequest CreateRequirementLinkRequest();
    SaveProjectArtifactRequest CreateArtifactRequest();
    SaveProjectWorkspaceRootRequest CreateWorkspaceRootRequest();
    SaveProjectCompilerInstallationRequest CreateCompilerInstallationRequest();
}
