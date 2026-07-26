using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/projects")]
public sealed class LocalGptProjectsController(
    ILocalGptProjectService projects,
    ILogger<LocalGptProjectsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocalGptProjectSummary>>> GetProjects(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await projects.GetProjectsAsync(includeArchived, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<LocalGptProjectDetails>> GetProject(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
        return project is null ? NotFound() : Ok(project);
    }

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
