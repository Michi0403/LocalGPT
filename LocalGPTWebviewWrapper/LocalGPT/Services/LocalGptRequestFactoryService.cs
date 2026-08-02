using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class LocalGptRequestFactoryService(ILogger<LocalGptRequestFactoryService> logger) : ILocalGptRequestFactoryService
{
    public SaveLocalGptProjectRequest CreateProjectRequest() => Created(new SaveLocalGptProjectRequest
    {
        ProjectType = "DotNetSolution",
        SolutionSearchPattern = @"(?i)\.(sln|slnx)$",
        FileIncludePattern = @"(?s).*",
        FileExcludePattern = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$",
        CurrentVersion = "0.1.0",
        Status = "Active",
        RecommendGit = true
    }, nameof(CreateProjectRequest));

    public AddLocalGptProjectTopicRequest CreateTopicRequest() => Created(new AddLocalGptProjectTopicRequest
    {
        Status = "Planned"
    }, nameof(CreateTopicRequest));

    public AddLocalGptProjectVersionRequest CreateVersionRequest(string path = "") => Created(new AddLocalGptProjectVersionRequest
    {
        PathSnapshot = path,
        IsCurrent = true
    }, nameof(CreateVersionRequest));

    public SaveProjectRevisionRequest CreateRevisionRequest() => Created(new SaveProjectRevisionRequest
    {
        BranchName = "main",
        RevisionName = $"revision-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
        ProjectStructureJson = "{}",
        IsCurrent = true
    }, nameof(CreateRevisionRequest));

    public SaveProjectRequirementRequest CreateRequirementRequest() => Created(new SaveProjectRequirementRequest
    {
        RequirementType = "Functional",
        Status = "Planned",
        Priority = "Normal"
    }, nameof(CreateRequirementRequest));

    public SaveProjectRequirementLinkRequest CreateRequirementLinkRequest() => Created(new SaveProjectRequirementLinkRequest
    {
        TargetKind = "DXFunction"
    }, nameof(CreateRequirementLinkRequest));

    public SaveProjectArtifactRequest CreateArtifactRequest() => Created(new SaveProjectArtifactRequest
    {
        ArtifactKind = "Configuration",
        DataType = "string"
    }, nameof(CreateArtifactRequest));

    public SaveProjectWorkspaceRootRequest CreateWorkspaceRootRequest() => Created(new SaveProjectWorkspaceRootRequest
    {
        ScopeKind = "Global",
        SolutionPattern = @"(?i)\.(sln|slnx)$",
        EnvironmentKind = "LocalHost",
        EnvironmentVariablesJson = "{}",
        DefaultSubdirectoriesJson = "[\"src\",\"docs\",\"tests\",\"artifacts\"]",
        AccessPolicyJson = "[{\"name\":\"Project sources\",\"relativePathRegex\":\"(?i)^(src|source)(/|$)\",\"expectedEntryKind\":\"Either\",\"requiredAccess\":\"ReadWrite\",\"severity\":\"Warning\",\"required\":false,\"councilMaintained\":true}]",
        Priority = 100,
        IsEnabled = true
    }, nameof(CreateWorkspaceRootRequest));

    public SaveProjectCompilerInstallationRequest CreateCompilerInstallationRequest() => Created(new SaveProjectCompilerInstallationRequest
    {
        Language = "DotNet",
        ValidationArguments = "--version",
        IsEnabled = true
    }, nameof(CreateCompilerInstallationRequest));

    private T Created<T>(T request, string operation)
    {
        logger.LogTrace("Created default request model for {Operation}.", operation);
        return request;
    }
}
