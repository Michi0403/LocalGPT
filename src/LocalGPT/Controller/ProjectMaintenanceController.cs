using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the project maintenance application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="maintenance">Project maintenance service dependency used by the project maintenance workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/project-maintenance")]
public sealed class ProjectMaintenanceController(
    IProjectMaintenanceService maintenance,
    ILogger<ProjectMaintenanceController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves workspaces for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("workspaces")]
    public async Task<IResult> GetWorkspaces([FromQuery] Guid? projectId, CancellationToken cancellationToken)
        => Results.Ok(await maintenance.GetWorkspaceRootsAsync(projectId, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Persists workspace for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("workspaces")]
    [HumanApprovalRequired("project.workspace.save", "Save project workspace", "Store one project, project-type, or global workspace root.", "Medium", "Project workspace administrator")]
    public async Task<IResult> SaveWorkspace([FromBody] SaveProjectWorkspaceRootRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.SaveWorkspaceRootAsync(request, cancellationToken), "workspace save").ConfigureAwait(false);

    /// <summary>
    /// Resolves workspace for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("workspaces/{projectId:guid}/resolve")]
    public async Task<IResult> ResolveWorkspace(Guid projectId, CancellationToken cancellationToken)
        => Results.Ok(await maintenance.ResolveWorkspaceAsync(projectId, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the assess workspace permissions projection for the project maintenance API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="workspaceRootId">Identifier of the workspace root to use for this operation.</param>
    /// <param name="userConfirmedWriteProbe">Value indicating whether user confirmed write probe should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("workspaces/{workspaceRootId:guid}/assess")]
    [HumanApprovalRequired("project.workspace.permissions.assess", "Assess workspace permissions", "Inspect the configured workspace structure and optionally perform one bounded create/delete write probe.", "Medium", "Workspace security reviewer")]
    public async Task<IResult> AssessWorkspacePermissions(Guid workspaceRootId, [FromQuery] bool userConfirmedWriteProbe, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.AssessWorkspacePermissionsAsync(workspaceRootId, userConfirmedWriteProbe, cancellationToken), "workspace permission assessment").ConfigureAwait(false);

    /// <summary>
    /// Retrieves compilers for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("compilers")]
    public async Task<IResult> GetCompilers(CancellationToken cancellationToken)
        => Results.Ok(await maintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Persists compiler for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("compilers")]
    [HumanApprovalRequired("project.compiler.save", "Save compiler installation", "Store one compiler executable and validation profile.", "Medium", "Project toolchain administrator")]
    public async Task<IResult> SaveCompiler([FromBody] SaveProjectCompilerInstallationRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.SaveCompilerInstallationAsync(request, cancellationToken), "compiler save").ConfigureAwait(false);

    /// <summary>
    /// Discovers compilers for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("compilers/discover")]
    [HumanApprovalRequired("project.compiler.discover", "Discover compiler installations", "Scan common and user-selected directories for compiler executables and store the detected paths.", "Medium", "Project toolchain administrator")]
    public async Task<IResult> DiscoverCompilers([FromBody] DiscoverProjectCompilersRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.DiscoverCompilerInstallationsAsync(request, cancellationToken), "compiler discovery").ConfigureAwait(false);

    /// <summary>
    /// Validates compiler for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="compilerId">Identifier of the compiler to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("compilers/{compilerId:guid}/validate")]
    [HumanApprovalRequired("project.compiler.validate", "Validate compiler installation", "Execute the selected compiler's bounded version probe.", "Medium", "Project toolchain administrator")]
    public async Task<IResult> ValidateCompiler(Guid compilerId, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.ValidateCompilerInstallationAsync(compilerId, userConfirmed, cancellationToken), "compiler validation").ConfigureAwait(false);

    /// <summary>
    /// Deletes compiler for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="compilerId">Identifier of the compiler to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("compilers/{compilerId:guid}")]
    [HumanApprovalRequired("project.compiler.delete", "Delete compiler installation", "Remove one stored compiler profile that is not referenced by a workspace or verification record.", "Medium", "Project toolchain administrator")]
    public async Task<IResult> DeleteCompiler(Guid compilerId, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.DeleteCompilerInstallationAsync(compilerId, userConfirmed, cancellationToken), "compiler delete").ConfigureAwait(false);

    /// <summary>
    /// Retrieves files for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("projects/{projectId:guid}/files")]
    public async Task<IResult> GetFiles(Guid projectId, [FromQuery] Guid? revisionId, CancellationToken cancellationToken)
        => Results.Ok(await maintenance.GetTrackedFilesAsync(projectId, revisionId, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the scan project projection for the project maintenance API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("projects/{projectId:guid}/scan")]
    [HumanApprovalRequired("project.files.scan", "Scan project files", "Read the selected project tree, store stable path metadata and hashes, and detect its solution file.", "Medium", "Project structure maintainer")]
    public async Task<IResult> ScanProject(Guid projectId, [FromBody] ScanProjectFilesRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.ScanProjectFilesAsync(projectId, request, cancellationToken), "project scan").ConfigureAwait(false);

    /// <summary>
    /// Persists file patterns for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="trackedFileId">Identifier of the tracked file to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("files/{trackedFileId:guid}/patterns")]
    [HumanApprovalRequired("project.file.patterns.save", "Save file structure patterns", "Store the approved structure and content-format regular expressions for one tracked project file.", "Medium", "Project structure maintainer")]
    public async Task<IResult> SaveFilePatterns(Guid trackedFileId, [FromBody] SaveTrackedFilePatternRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.SaveTrackedFilePatternAsync(trackedFileId, request, cancellationToken), "file pattern save").ConfigureAwait(false);

    /// <summary>
    /// Registers revision workspace for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("projects/{projectId:guid}/revisions/{revisionId:guid}/workspace")]
    [HumanApprovalRequired("project.revision.workspace.save", "Save revision workspace", "Associate one existing local workspace and optional solution file with the selected project revision.", "High", "Project workspace administrator")]
    public async Task<IResult> RegisterRevisionWorkspace(Guid projectId, Guid revisionId, [FromBody] RegisterRevisionWorkspaceRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.RegisterRevisionWorkspaceAsync(projectId, revisionId, request.SourceRootPath, request.SolutionPath, request.UserConfirmed, cancellationToken), "revision workspace registration").ConfigureAwait(false);

    /// <summary>
    /// Verifies revision for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("projects/{projectId:guid}/verify")]
    [HumanApprovalRequired("project.revision.build.verify", "Build project revision", "Execute the selected compiler against the approved project revision and store the bounded build/test evidence.", "High", "Build verification reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> VerifyRevision(Guid projectId, [FromBody] RunProjectBuildVerificationRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.RunBuildVerificationAsync(projectId, request, cancellationToken), "build verification").ConfigureAwait(false);

    /// <summary>
    /// Returns the record council review projection for the project maintenance API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="verificationId">Identifier of the verification to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("verifications/{verificationId:guid}/council-review")]
    [HumanApprovalRequired("project.revision.council-review", "Record council build review", "Store the council's review of the compile and test evidence.", "Medium", "Council build reviewer")]
    public async Task<IResult> RecordCouncilReview(Guid verificationId, [FromBody] RecordCouncilBuildReviewRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.RecordCouncilBuildReviewAsync(verificationId, request, cancellationToken), "council build review").ConfigureAwait(false);

    /// <summary>
    /// Approves ready for the project maintenance API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("projects/{projectId:guid}/revisions/{revisionId:guid}/approve-ready")]
    [HumanApprovalRequired("project.revision.ready.approve", "Approve revision for testing", "After successful build, requested tests, and council review, create a lossless source snapshot and mark the revision ready for human testing.", "High", "Release approval reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> ApproveReady(Guid projectId, Guid revisionId, [FromBody] ApproveRevisionReadyForTestRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.ApproveRevisionReadyForTestAsync(projectId, revisionId, request, cancellationToken), "revision approval").ConfigureAwait(false);

    /// <summary>
    /// Returns the execute projection for the project maintenance API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ProjectMaintenanceController"/>.</typeparam>
    /// <param name="action">Action value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    private async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action, string operation)
    {
        try
        {
            return Results.Ok(await action().ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or DirectoryNotFoundException or FileNotFoundException)
        {
            logger.LogWarning(ex, "Project maintenance {Operation} was rejected.", operation);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (OperationCanceledException exception)
        {
            logger.LogInformation(exception, "Project maintenance {Operation} was cancelled.", operation);
            return Results.Conflict(new { error = "The operation was cancelled." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Project maintenance {Operation} failed; project paths and source content were omitted from logs.", operation);
            return Results.InternalServerError(new { error = "The operation failed. Review LocalGPT application logs." });
        }
    }
}
