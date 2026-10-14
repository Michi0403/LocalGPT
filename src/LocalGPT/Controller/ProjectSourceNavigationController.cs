using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Project-owned regex/knowledge navigation and approved runtime publication; never exposes a raw Python executor.</summary>
/// <param name="navigation">Project source navigation service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/projects/source")]
public sealed class ProjectSourceNavigationController(IProjectSourceNavigationService navigation, ILogger<ProjectSourceNavigationController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the navigate projection for the project source navigation API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("navigate")]
    public async Task<IResult> Navigate([FromBody] ProjectSourceNavigationRequest request, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await navigation.NavigateAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Project source navigation request failed; no source content logged.");
            return Results.BadRequest("Project/revision navigation failed. Check the configured project and curator state.");
        }
    }

    /// <summary>
    /// Returns the publish projection for the project source navigation API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("publish-plugin")]
    [HumanApprovalRequired("project.source.publish-plugin", "Publish reviewed project source as function", "Publish one hash-verified, curator-reviewed project file as a runtime function snapshot. No code execution occurs.", "High", "Project and function reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> Publish([FromBody] PublishTrackedProjectScriptRequest request, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await navigation.PublishApprovedScriptAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Publishing project source failed; source content omitted.");
            return Results.BadRequest("Project source publication failed. Review version/hash/approval and local diagnostics.");
        }
    }
}
