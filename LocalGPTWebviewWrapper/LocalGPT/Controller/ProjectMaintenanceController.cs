using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/project-maintenance")]
public sealed class ProjectMaintenanceController(
    IProjectMaintenanceService maintenance,
    ILogger<ProjectMaintenanceController> logger) : ControllerBase
{
    [HttpGet("workspaces")]
    public async Task<IResult> GetWorkspaces([FromQuery] Guid? projectId, CancellationToken cancellationToken)
        => Results.Ok(await maintenance.GetWorkspaceRootsAsync(projectId, cancellationToken).ConfigureAwait(false));

    [HttpPost("workspaces")]
    [HumanApprovalRequired("project.workspace.save", "Save project workspace", "Store one project, project-type, or global workspace root.", "Medium", "Project workspace administrator")]
    public async Task<IResult> SaveWorkspace([FromBody] SaveProjectWorkspaceRootRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.SaveWorkspaceRootAsync(request, cancellationToken), "workspace save").ConfigureAwait(false);

    [HttpGet("workspaces/{projectId:guid}/resolve")]
    public async Task<IResult> ResolveWorkspace(Guid projectId, CancellationToken cancellationToken)
        => Results.Ok(await maintenance.ResolveWorkspaceAsync(projectId, cancellationToken).ConfigureAwait(false));

    [HttpGet("compilers")]
    public async Task<IResult> GetCompilers(CancellationToken cancellationToken)
        => Results.Ok(await maintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false));

    [HttpPost("compilers")]
    [HumanApprovalRequired("project.compiler.save", "Save compiler installation", "Store one compiler executable and validation profile.", "Medium", "Project toolchain administrator")]
    public async Task<IResult> SaveCompiler([FromBody] SaveProjectCompilerInstallationRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.SaveCompilerInstallationAsync(request, cancellationToken), "compiler save").ConfigureAwait(false);

    [HttpPost("compilers/discover")]
    [HumanApprovalRequired("project.compiler.discover", "Discover compiler installations", "Scan common and user-selected directories for compiler executables and store the detected paths.", "Medium", "Project toolchain administrator")]
    public async Task<IResult> DiscoverCompilers([FromBody] DiscoverProjectCompilersRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.DiscoverCompilerInstallationsAsync(request, cancellationToken), "compiler discovery").ConfigureAwait(false);

    [HttpPost("compilers/{compilerId:guid}/validate")]
    [HumanApprovalRequired("project.compiler.validate", "Validate compiler installation", "Execute the selected compiler's bounded version probe.", "Medium", "Project toolchain administrator")]
    public async Task<IResult> ValidateCompiler(Guid compilerId, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.ValidateCompilerInstallationAsync(compilerId, userConfirmed, cancellationToken), "compiler validation").ConfigureAwait(false);

    [HttpGet("projects/{projectId:guid}/files")]
    public async Task<IResult> GetFiles(Guid projectId, [FromQuery] Guid? revisionId, CancellationToken cancellationToken)
        => Results.Ok(await maintenance.GetTrackedFilesAsync(projectId, revisionId, cancellationToken).ConfigureAwait(false));

    [HttpPost("projects/{projectId:guid}/scan")]
    [HumanApprovalRequired("project.files.scan", "Scan project files", "Read the selected project tree, store stable path metadata and hashes, and detect its solution file.", "Medium", "Project structure maintainer")]
    public async Task<IResult> ScanProject(Guid projectId, [FromBody] ScanProjectFilesRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.ScanProjectFilesAsync(projectId, request, cancellationToken), "project scan").ConfigureAwait(false);

    [HttpPut("files/{trackedFileId:guid}/patterns")]
    [HumanApprovalRequired("project.file.patterns.save", "Save file structure patterns", "Store the approved structure and content-format regular expressions for one tracked project file.", "Medium", "Project structure maintainer")]
    public async Task<IResult> SaveFilePatterns(Guid trackedFileId, [FromBody] SaveTrackedFilePatternRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.SaveTrackedFilePatternAsync(trackedFileId, request, cancellationToken), "file pattern save").ConfigureAwait(false);

    [HttpPost("projects/{projectId:guid}/revisions/{revisionId:guid}/workspace")]
    [HumanApprovalRequired("project.revision.workspace.save", "Save revision workspace", "Associate one existing local workspace and optional solution file with the selected project revision.", "High", "Project workspace administrator")]
    public async Task<IResult> RegisterRevisionWorkspace(Guid projectId, Guid revisionId, [FromBody] RegisterRevisionWorkspaceRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.RegisterRevisionWorkspaceAsync(projectId, revisionId, request.SourceRootPath, request.SolutionPath, request.UserConfirmed, cancellationToken), "revision workspace registration").ConfigureAwait(false);

    [HttpPost("projects/{projectId:guid}/verify")]
    [HumanApprovalRequired("project.revision.build.verify", "Build project revision", "Execute the selected compiler against the approved project revision and store the bounded build/test evidence.", "High", "Build verification reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> VerifyRevision(Guid projectId, [FromBody] RunProjectBuildVerificationRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.RunBuildVerificationAsync(projectId, request, cancellationToken), "build verification").ConfigureAwait(false);

    [HttpPost("verifications/{verificationId:guid}/council-review")]
    [HumanApprovalRequired("project.revision.council-review", "Record council build review", "Store the council's review of the compile and test evidence.", "Medium", "Council build reviewer")]
    public async Task<IResult> RecordCouncilReview(Guid verificationId, [FromBody] RecordCouncilBuildReviewRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.RecordCouncilBuildReviewAsync(verificationId, request, cancellationToken), "council build review").ConfigureAwait(false);

    [HttpPost("projects/{projectId:guid}/revisions/{revisionId:guid}/approve-ready")]
    [HumanApprovalRequired("project.revision.ready.approve", "Approve revision for testing", "After successful build, requested tests, and council review, create a lossless source snapshot and mark the revision ready for human testing.", "High", "Release approval reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> ApproveReady(Guid projectId, Guid revisionId, [FromBody] ApproveRevisionReadyForTestRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => maintenance.ApproveRevisionReadyForTestAsync(projectId, revisionId, request, cancellationToken), "revision approval").ConfigureAwait(false);

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
