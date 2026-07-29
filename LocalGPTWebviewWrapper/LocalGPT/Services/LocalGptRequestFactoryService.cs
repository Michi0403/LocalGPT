using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class LocalGptRequestFactoryService : ILocalGptRequestFactoryService
{
    public SaveLocalGptProjectRequest CreateProjectRequest() => new()
    {
        ProjectType = "DotNetSolution",
        SolutionSearchPattern = @"(?i)\.(sln|slnx)$",
        FileIncludePattern = @"(?s).*",
        FileExcludePattern = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$",
        CurrentVersion = "0.1.0",
        Status = "Active",
        RecommendGit = true
    };

    public AddLocalGptProjectTopicRequest CreateTopicRequest() => new()
    {
        Status = "Planned"
    };

    public AddLocalGptProjectVersionRequest CreateVersionRequest(string path = "") => new()
    {
        PathSnapshot = path,
        IsCurrent = true
    };

    public SaveProjectRevisionRequest CreateRevisionRequest() => new()
    {
        BranchName = "main",
        RevisionName = $"revision-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
        ProjectStructureJson = "{}",
        IsCurrent = true
    };

    public SaveProjectRequirementRequest CreateRequirementRequest() => new()
    {
        RequirementType = "Functional",
        Status = "Planned",
        Priority = "Normal"
    };

    public SaveProjectRequirementLinkRequest CreateRequirementLinkRequest() => new()
    {
        TargetKind = "DXFunction"
    };

    public SaveProjectArtifactRequest CreateArtifactRequest() => new()
    {
        ArtifactKind = "Configuration",
        DataType = "string"
    };

    public SaveProjectWorkspaceRootRequest CreateWorkspaceRootRequest() => new()
    {
        ScopeKind = "Global",
        SolutionPattern = @"(?i)\.(sln|slnx)$",
        Priority = 100,
        IsEnabled = true
    };

    public SaveProjectCompilerInstallationRequest CreateCompilerInstallationRequest() => new()
    {
        Language = "DotNet",
        ValidationArguments = "--version",
        IsEnabled = true
    };
}
