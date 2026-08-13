using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the LocalGPT projects application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="projects">Local gpt project service dependency used by the LocalGPT projects workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/projects")]
public sealed class LocalGptProjectsController(
    ILocalGptProjectService projects,
    ILogger<LocalGptProjectsController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves projects for the LocalGPT projects API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="includeArchived">Value indicating whether include archived should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocalGptProjectSummary>>> GetProjects(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await projects.GetProjectsAsync(includeArchived, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Retrieves project for the LocalGPT projects API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<LocalGptProjectDetails>> GetProject(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
        return project is null ? NotFound() : Ok(project);
    }

    /// <summary>
    /// Persists project for the LocalGPT projects API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    public async Task<ActionResult<LocalGptProject>> SaveProject(
        [FromBody] SaveLocalGptProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var project = await projects.SaveProjectAsync(request, cancellationToken).ConfigureAwait(false);
            return Ok(project);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project save request was rejected.");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Adds topic for the LocalGPT projects API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{projectId:guid}/topics")]
    public async Task<ActionResult<LocalGptProjectTopic>> AddTopic(
        Guid projectId,
        [FromBody] AddLocalGptProjectTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await projects.AddTopicAsync(projectId, request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project topic request was rejected.");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Adds version for the LocalGPT projects API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{projectId:guid}/versions")]
    public async Task<ActionResult<LocalGptProjectVersion>> AddVersion(
        Guid projectId,
        [FromBody] AddLocalGptProjectVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await projects.AddVersionAsync(projectId, request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project version request was rejected.");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Links knowledge for the LocalGPT projects API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectTopicId">Identifier of the project topic to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("topics/{projectTopicId:guid}/knowledge")]
    public async Task<IActionResult> LinkKnowledge(
        Guid projectTopicId,
        [FromBody] LinkProjectTopicKnowledgeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await projects.LinkKnowledgeAsync(projectTopicId, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project knowledge-link request was rejected.");
            return BadRequest(new { error = ex.Message });
        }
    }
}
