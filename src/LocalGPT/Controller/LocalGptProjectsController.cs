using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the LocalGPT projects application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="projects">Local gpt project service dependency used by the LocalGPT projects workflow to provide the corresponding application capability.</param>
/// <param name="gameProjects">Game project service used to build, inspect, and launch project-owned game definitions.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/projects")]
public sealed class LocalGptProjectsController(
    ILocalGptProjectService projects,
    IGameProjectService gameProjects,
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

    /// <summary>Reads the editable project-owned authoring profile for one Game project.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted game authoring profile, or HTTP 404 when no profile exists yet.</returns>
    [HttpGet("{projectId:guid}/game/profile")]
    public async Task<ActionResult<LocalGptGameProjectProfile>> GetGameProfile(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await gameProjects.GetProfileAsync(projectId, cancellationToken).ConfigureAwait(false);
            return profile is null ? NotFound() : Ok(profile);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project game profile request was rejected for project {ProjectId}.", projectId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Creates or updates the editable project-owned authoring profile for one Game project.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="request">Approved game authoring settings.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted game authoring profile.</returns>
    [HttpPost("{projectId:guid}/game/profile")]
    public async Task<ActionResult<LocalGptGameProjectProfile>> SaveGameProfile(
        Guid projectId,
        [FromBody] SaveGameProjectProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await gameProjects.SaveProfileAsync(projectId, request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project game profile save request was rejected for project {ProjectId}.", projectId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Builds one Game project into its approved runtime-definition artifact.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="request">Approval to compile the already-saved game design and requirement baseline.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted game build.</returns>
    [HttpPost("{projectId:guid}/game/build")]
    public async Task<ActionResult<ProjectGameBuildResult>> BuildGame(
        Guid projectId,
        [FromBody] BuildProjectGameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await gameProjects.BuildAsync(projectId, request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project game build request was rejected for project {ProjectId}.", projectId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Reads the latest approved runtime definition produced by one Game project.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The latest build, or HTTP 404 when no build exists yet.</returns>
    [HttpGet("{projectId:guid}/game/build")]
    public async Task<ActionResult<ProjectGameBuildResult>> GetGameBuild(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var build = await gameProjects.GetBuildAsync(projectId, cancellationToken).ConfigureAwait(false);
        return build is null ? NotFound() : Ok(build);
    }

    /// <summary>Launches the latest approved Game-project build through the shared GameDirector runtime.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="request">Launch ownership context.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The authoritative game-session snapshot.</returns>
    [HttpPost("{projectId:guid}/game/start")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> StartGame(
        Guid projectId,
        [FromBody] LaunchProjectGameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await gameProjects.LaunchAsync(projectId, request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project game launch request was rejected for project {ProjectId}.", projectId);
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
