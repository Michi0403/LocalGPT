using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes knowledge-backed cross-platform compiler/runtime discovery and missing-version knowledge requests.</summary>
/// <param name="knowledge">Toolchain knowledge service dependency used by the toolchain knowledge workflow to provide the corresponding application capability.</param>
/// <param name="projectMaintenance">Project maintenance service dependency used by the toolchain knowledge workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/toolchains")]
public sealed class ToolchainKnowledgeController(
    IToolchainKnowledgeService knowledge,
    IProjectMaintenanceService projectMaintenance,
    ILogger<ToolchainKnowledgeController> logger) : ControllerBase
{
    /// <summary>Lists the current knowledge-backed discovery profiles.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("profiles")]
    public async Task<IResult> ProfilesAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await knowledge.GetProfilesAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Could not list toolchain knowledge profiles."); return Results.InternalServerError("Toolchain profiles could not be loaded. Review LocalGPT logs."); }
    }

    /// <summary>Runs approved local PATH/knowledge-root discovery and optionally saves results into project compiler installations.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("discover")]
    [HumanApprovalRequired("toolchain.discover", "Discover local toolchains", "Search local PATH and knowledge-defined platform roots for compiler/runtime executables and optionally save detected profiles.", "Medium", "Toolchain discovery reviewer")]
    public async Task<IResult> DiscoverAsync([FromBody] DiscoverProjectCompilersRequest request, CancellationToken cancellationToken)
    {
        try { request.UserConfirmed = true; return Results.Ok(await projectMaintenance.DiscoverCompilerInstallationsAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Toolchain discovery API failed; paths were omitted from logs."); return Results.InternalServerError("Toolchain discovery failed. Review LocalGPT logs."); }
    }

    /// <summary>Queues a request for exact toolchain-version knowledge without performing an online lookup.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("knowledge/request")]
    public async Task<IResult> RequestKnowledgeAsync([FromBody] ToolchainKnowledgeGapRequest request, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await knowledge.RequestMissingVersionKnowledgeAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Requesting toolchain knowledge failed."); return Results.InternalServerError("The toolchain knowledge request could not be created. Review LocalGPT logs."); }
    }
}
