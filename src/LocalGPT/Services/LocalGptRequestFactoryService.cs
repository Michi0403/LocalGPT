using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates LocalGPT request factory behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class LocalGptRequestFactoryService(ILogger<LocalGptRequestFactoryService> logger) : ILocalGptRequestFactoryService
{
    /// <summary>
    /// Creates project request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save LocalGPT project request produced by the operation.</returns>
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

    /// <summary>
    /// Creates topic request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The add LocalGPT project topic request produced by the operation.</returns>
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

    /// <summary>
    /// Creates version request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the LocalGPT request factory operation and used when producing its result.</param>
    /// <returns>The add LocalGPT project version request produced by the operation.</returns>
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

    /// <summary>
    /// Creates revision request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project revision request produced by the operation.</returns>
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

    /// <summary>
    /// Creates requirement request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project requirement request produced by the operation.</returns>
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

    /// <summary>
    /// Creates requirement link request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project requirement link request produced by the operation.</returns>
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

    /// <summary>
    /// Creates artifact request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project artifact request produced by the operation.</returns>
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

    /// <summary>
    /// Creates workspace root request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project workspace root request produced by the operation.</returns>
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

    /// <summary>
    /// Creates compiler installation request as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The save project compiler installation request produced by the operation.</returns>
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

    /// <summary>
    /// Creates d as part of the LocalGPT request factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="LocalGptRequestFactoryService"/>.</typeparam>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="operation">Operation value supplied to the LocalGPT request factory operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
    private T Created<T>(T request, string operation)
    {
        logger.LogTrace("Created default request model for {Operation}.", operation);
        return request;
    }
}
