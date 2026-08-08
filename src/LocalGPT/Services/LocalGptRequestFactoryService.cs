using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class LocalGptRequestFactoryService(ILogger<LocalGptRequestFactoryService> logger) : ILocalGptRequestFactoryService
{
    public SaveLocalGptProjectRequest CreateProjectRequest() {
    try
    {
        return Created(new SaveLocalGptProjectRequest
    {
        ProjectType = "DotNetSolution",
        SolutionSearchPattern = @"(?i)\.(sln|slnx)$",
        FileIncludePattern = @"(?s).*",
        FileExcludePattern = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$",
        CurrentVersion = "0.1.0",
        Status = "Active",
        RecommendGit = true
    }, nameof(CreateProjectRequest));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateProjectRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateProjectRequest)} failed.");
        throw;
    }
}

    public AddLocalGptProjectTopicRequest CreateTopicRequest() {
    try
    {
        return Created(new AddLocalGptProjectTopicRequest
    {
        Status = "Planned"
    }, nameof(CreateTopicRequest));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateTopicRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateTopicRequest)} failed.");
        throw;
    }
}

    public AddLocalGptProjectVersionRequest CreateVersionRequest(string path = "") {
    try
    {
        return Created(new AddLocalGptProjectVersionRequest
    {
        PathSnapshot = path,
        IsCurrent = true
    }, nameof(CreateVersionRequest));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateVersionRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateVersionRequest)} failed.");
        throw;
    }
}

    public SaveProjectRevisionRequest CreateRevisionRequest() {
    try
    {
        return Created(new SaveProjectRevisionRequest
    {
        BranchName = "main",
        RevisionName = $"revision-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
        ProjectStructureJson = "{}",
        IsCurrent = true
    }, nameof(CreateRevisionRequest));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateRevisionRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateRevisionRequest)} failed.");
        throw;
    }
}

    public SaveProjectRequirementRequest CreateRequirementRequest() {
    try
    {
        return Created(new SaveProjectRequirementRequest
    {
        RequirementType = "Functional",
        Status = "Planned",
        Priority = "Normal"
    }, nameof(CreateRequirementRequest));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateRequirementRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateRequirementRequest)} failed.");
        throw;
    }
}

    public SaveProjectRequirementLinkRequest CreateRequirementLinkRequest() {
    try
    {
        return Created(new SaveProjectRequirementLinkRequest
    {
        TargetKind = "DXFunction"
    }, nameof(CreateRequirementLinkRequest));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateRequirementLinkRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateRequirementLinkRequest)} failed.");
        throw;
    }
}

    public SaveProjectArtifactRequest CreateArtifactRequest() {
    try
    {
        return Created(new SaveProjectArtifactRequest
    {
        ArtifactKind = "Configuration",
        DataType = "string"
    }, nameof(CreateArtifactRequest));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateArtifactRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateArtifactRequest)} failed.");
        throw;
    }
}

    public SaveProjectWorkspaceRootRequest CreateWorkspaceRootRequest() {
    try
    {
        return Created(new SaveProjectWorkspaceRootRequest
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateWorkspaceRootRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateWorkspaceRootRequest)} failed.");
        throw;
    }
}

    public SaveProjectCompilerInstallationRequest CreateCompilerInstallationRequest() {
    try
    {
        return Created(new SaveProjectCompilerInstallationRequest
    {
        Language = "DotNet",
        ValidationArguments = "--version",
        IsEnabled = true
    }, nameof(CreateCompilerInstallationRequest));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateCompilerInstallationRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptRequestFactoryService)}.{nameof(CreateCompilerInstallationRequest)} failed.");
        throw;
    }
}

    private T Created<T>(T request, string operation)
    {
        logger.LogTrace("Created default request model for {Operation}.", operation);
        return request;
    }
}
