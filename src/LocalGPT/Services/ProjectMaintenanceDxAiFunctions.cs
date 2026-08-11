using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Helpers;

namespace LocalGPT.Services;

/// <summary>
/// Represents a get project maintenance function.
/// </summary>
public sealed class GetProjectMaintenanceFunction(IDxAiFunctionJsonService json,
    ILocalGptProjectService projects,
    IProjectMaintenanceService maintenance,
    ILogger<GetProjectMaintenanceFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.maintenance.get", "POST", "/api/dxai/functions/project.maintenance.get/invoke",
        "Read one project's solution path, workspace resolution, tracked file paths and regex metadata, compiler installations, revisions, and build verification state before maintaining source.",
        "JSON parameters: projectId required; revisionId optional.",
        "Read-only metadata. Absolute paths are returned only for the user-selected local project and remain reference data, not permission to read or write files.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"projectId":{"type":"string","format":"uuid"},"revisionId":{"type":["string","null"],"format":"uuid"}},"required":["projectId"],"additionalProperties":false}""");

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectMaintenanceGetParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var details = await projects.GetProjectAsync(parameters.ProjectId, cancellationToken).ConfigureAwait(false);
            if (details is null) return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The project was not found." };
            var workspace = await maintenance.ResolveWorkspaceAsync(parameters.ProjectId, cancellationToken).ConfigureAwait(false);
            var workspaceRoots = await maintenance.GetWorkspaceRootsAsync(parameters.ProjectId, cancellationToken).ConfigureAwait(false);
            var compilers = await maintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false);
            var files = await maintenance.GetTrackedFilesAsync(parameters.ProjectId, parameters.RevisionId, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("DXAIFunction returned project maintenance metadata for project {ProjectId} with {FileCount} tracked file(s).", parameters.ProjectId, files.Count);
            return json.Success(new
            {
            Project = new { details.Project.Id, details.Project.Name, details.Project.ProjectType, details.Project.RootPath, details.Project.SolutionPath, details.Project.SolutionSearchPattern, details.Project.FileIncludePattern, details.Project.FileExcludePattern },
            Workspace = workspace,
            WorkspaceRoots = workspaceRoots.Select(item => new { item.Id, item.Name, item.RootPath, item.ScopeKind, item.ProjectTypePattern, item.SolutionPattern, item.EnvironmentKind, item.EnvironmentRootPath, item.PreferredCompilerInstallationId, item.BuildArguments, item.EnvironmentVariablesJson, item.DefaultSubdirectoriesJson, item.AccessPolicyJson, item.ExpectedStructureRegex, item.LastPermissionStatus, item.LastPermissionSummary, item.LastPermissionReadAccess, item.LastPermissionWriteAccess, item.LastPermissionCheckedAtUtc, item.IsDefault, item.IsEnabled }),
            Compilers = compilers.Where(item => item.IsEnabled).Select(item => new { item.Id, item.Name, item.Language, item.ExecutablePath, item.CompilerHomePath, item.Version, item.Architecture, item.LastValidationSucceeded, item.IsDefaultForLanguage }),
            Revisions = details.Revisions.Select(item => new { item.Id, item.BranchName, item.RevisionName, item.IsCurrent, item.CompileVerified, item.CouncilVerified, item.ReadyForTesting, item.SourceRootPath, item.SolutionPath, item.SourceSnapshotHash, item.SnapshotArchivePath }),
            Files = files.Select(item => new { item.Id, item.ProjectRelativePath, item.AbsolutePath, item.SolutionPath, item.ProjectFilePath, item.FileRole, item.StructureRegex, item.ContentFormatRegex, item.ContentHash, item.SizeBytes, item.Exists, item.IsGenerated }),
                BuildVerifications = details.BuildVerifications.Select(item => new { item.Id, item.RevisionId, item.CompilerInstallationId, item.StartedAtUtc, item.CompletedAtUtc, item.BuildSucceeded, item.TestsExecuted, item.TestsSucceeded, item.SourceChangedDuringVerification, item.CouncilReviewSucceeded, item.UserApprovedReadyForTest, item.OutputLogPath, item.EvidenceManifestPath, item.OutputHash, item.SourceSnapshotHash, item.SnapshotArchivePath, item.Summary })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not load project maintenance metadata; project paths were omitted from logs.");
            return new DxAiFunctionInvocationResult { Status = "Failed", Error = "Project maintenance metadata could not be loaded. Review LocalGPT logs." };
        }
    }
}

/// <summary>
/// Represents a save project workspace environment function.
/// </summary>
public sealed class SaveProjectWorkspaceEnvironmentFunction(IDxAiFunctionJsonService json, IProjectMaintenanceService maintenance, ILogger<SaveProjectWorkspaceEnvironmentFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.workspace.environment.save", "POST", "/api/dxai/functions/project.workspace.environment.save/invoke",
        "Save one project/global workspace local-environment definition, preferred compiler, expected subdirectories, structure regex, and Council-maintainable access-policy regex rules.",
        "JSON parameters: request containing SaveProjectWorkspaceRootRequest.",
        "Metadata-only change after one-use human approval. Paths and regex rules grant no execution authority; assess and validate before build use.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler");

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectWorkspaceEnvironmentSaveParameters>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.Request.UserConfirmed = true;
            var result = await maintenance.SaveWorkspaceRootAsync(binding.Value.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved workspace environment metadata saved for workspace {WorkspaceRootId}; paths and regex content omitted from logs.", result.Id);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SaveProjectWorkspaceEnvironmentFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SaveProjectWorkspaceEnvironmentFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an assess project workspace environment function.
/// </summary>
public sealed class AssessProjectWorkspaceEnvironmentFunction(IDxAiFunctionJsonService json, IProjectMaintenanceService maintenance, ILogger<AssessProjectWorkspaceEnvironmentFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.workspace.environment.assess", "POST", "/api/dxai/functions/project.workspace.environment.assess/invoke",
        "Assess whether the current LocalGPT process has too broad or insufficient rights for a configured workspace, verify expected directories/regex rules, and check the assigned compiler state.",
        "JSON parameters: workspaceRootId.",
        "Requires fresh human approval because it creates and immediately removes one bounded probe file in the selected workspace. It does not compile or edit project sources.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["workspaceRootId"],"properties":{"workspaceRootId":{"type":"string","format":"uuid"}},"additionalProperties":false}""");

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectWorkspaceEnvironmentAssessParameters>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var result = await maintenance.AssessWorkspacePermissionsAsync(binding.Value.WorkspaceRootId, userConfirmedWriteProbe: true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved workspace permission assessment completed for workspace {WorkspaceRootId} with status {Status}.", result.WorkspaceRootId, result.Status);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AssessProjectWorkspaceEnvironmentFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AssessProjectWorkspaceEnvironmentFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a register project revision workspace function.
/// </summary>
public sealed class RegisterProjectRevisionWorkspaceFunction(IDxAiFunctionJsonService json, IProjectMaintenanceService maintenance, ILogger<RegisterProjectRevisionWorkspaceFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.revision.workspace.register", "POST", "/api/dxai/functions/project.revision.workspace.register/invoke",
        "Associate one existing isolated source workspace and optional solution path with a selected project revision before scanning or compiling it.",
        "JSON parameters: projectId, revisionId, sourceRootPath, optional solutionPath.",
        "High-impact path registration after one-use human approval. The operation stores helper paths only and never copies, deletes, or edits project files.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler");

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectRevisionWorkspaceRegisterParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var revision = await maintenance.RegisterRevisionWorkspaceAsync(
                parameters.ProjectId,
                parameters.RevisionId,
                parameters.SourceRootPath,
                parameters.SolutionPath,
                userConfirmed: true,
                cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved workspace registration completed for project {ProjectId} revision {RevisionId}; paths omitted from logs.", parameters.ProjectId, parameters.RevisionId);
            return json.Success(new { revision.Id, revision.ProjectId, revision.SourceRootPath, revision.SolutionPath, revision.CompileVerified, revision.CouncilVerified, revision.ReadyForTesting });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RegisterProjectRevisionWorkspaceFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RegisterProjectRevisionWorkspaceFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a scan project files function.
/// </summary>
public sealed class ScanProjectFilesFunction(IDxAiFunctionJsonService json, IProjectMaintenanceService maintenance, ILogger<ScanProjectFilesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.files.scan", "POST", "/api/dxai/functions/project.files.scan/invoke",
        "Scan one explicitly selected project root, detect the solution, and store stable absolute/relative paths, hashes, roles, and per-file structure/content regex metadata.",
        "JSON parameters: projectId plus optional revisionId, maximumFiles, maximumFileBytes, and maximumTextFileBytes. Omit maximumFiles/maximumFileBytes (or use non-positive values) to use the database-backed MaxFiles/MaxSingleFileBytes runtime policies instead of hard-coded source limits.",
        "Reads project files only after one-use human approval. Does not modify source, Git, or build outputs.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler");
    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectFilesScanParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var p = binding.Value;
            p.Request.UserConfirmed = true;
            var result = await maintenance.ScanProjectFilesAsync(p.ProjectId, p.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved project scan completed for project {ProjectId} with {FileCount} stored files.", p.ProjectId, result.FilesStored);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ScanProjectFilesFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ScanProjectFilesFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a save project file patterns function.
/// </summary>
public sealed class SaveProjectFilePatternsFunction(IDxAiFunctionJsonService json, IProjectMaintenanceService maintenance, ILogger<SaveProjectFilePatternsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.file.patterns.save", "POST", "/api/dxai/functions/project.file.patterns.save/invoke",
        "Store approved structure and content-format regular expressions plus the file role for one tracked project file.",
        "JSON parameters: trackedFileId plus SaveTrackedFilePatternRequest.",
        "Metadata-only write after one-use human approval. It never edits the project file itself.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler");

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectFilePatternsSaveParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            parameters.Request.UserConfirmed = true;
            var result = await maintenance.SaveTrackedFilePatternAsync(parameters.TrackedFileId, parameters.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved regex metadata was saved for tracked file {TrackedFileId}; regex content omitted from logs.", parameters.TrackedFileId);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SaveProjectFilePatternsFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SaveProjectFilePatternsFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a verify project revision build function.
/// </summary>
public sealed class VerifyProjectRevisionBuildFunction(IDxAiFunctionJsonService json, IProjectMaintenanceService maintenance, ILogger<VerifyProjectRevisionBuildFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.revision.build.verify", "POST", "/api/dxai/functions/project.revision.build.verify/invoke",
        "Run the user-selected compiler against one project revision and store bounded build/test evidence for council review.",
        "JSON parameters: projectId plus RunProjectBuildVerificationRequest.",
        "Executes a local compiler only after one-use human approval. It does not approve the revision or write source files.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler");
    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectRevisionBuildVerifyParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var p = binding.Value;
            p.Request.UserConfirmed = true;
            var result = await maintenance.RunBuildVerificationAsync(p.ProjectId, p.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved build verification {VerificationId} completed for project {ProjectId}.", result.Id, p.ProjectId);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(VerifyProjectRevisionBuildFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(VerifyProjectRevisionBuildFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a record project council build review function.
/// </summary>
public sealed class RecordProjectCouncilBuildReviewFunction(IDxAiFunctionJsonService json, IProjectMaintenanceService maintenance, ILogger<RecordProjectCouncilBuildReviewFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.revision.council-review", "POST", "/api/dxai/functions/project.revision.council-review/invoke",
        "Record the council's review of an existing build verification after members inspected the bounded compile/test evidence.",
        "JSON parameters: verificationId plus summary and compileErrorsAbsent.",
        "Stores review metadata only and requires human approval. It cannot mark a revision ready for testing.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler");
    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectCouncilBuildReviewRecordParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var p = binding.Value;
            p.Request.UserConfirmed = true;
            var result = await maintenance.RecordCouncilBuildReviewAsync(p.VerificationId, p.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved council review recorded for verification {VerificationId}.", p.VerificationId);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RecordProjectCouncilBuildReviewFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RecordProjectCouncilBuildReviewFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an approve project revision ready function.
/// </summary>
public sealed class ApproveProjectRevisionReadyFunction(IDxAiFunctionJsonService json, IProjectMaintenanceService maintenance, ILogger<ApproveProjectRevisionReadyFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.revision.ready.approve", "POST", "/api/dxai/functions/project.revision.ready.approve/invoke",
        "After successful compile, requested tests, and council review, create a lossless source snapshot and mark the revision ready for human testing.",
        "JSON parameters: projectId, revisionId, verificationId, requireTests, createLosslessSnapshot.",
        "High-impact final gate. Requires one-use human approval and never overwrites the source project.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler");
    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectRevisionApproveParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var p = binding.Value;
            p.Request.UserConfirmed = true;
            var result = await maintenance.ApproveRevisionReadyForTestAsync(p.ProjectId, p.RevisionId, p.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved revision {RevisionId} for project {ProjectId} as ready for testing.", p.RevisionId, p.ProjectId);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ApproveProjectRevisionReadyFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ApproveProjectRevisionReadyFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}
