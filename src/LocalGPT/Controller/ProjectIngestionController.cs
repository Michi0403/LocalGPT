using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes quarantine-first project ingestion and bounded blob reconstruction to the regular LocalGPT UI/API surface.</summary>
/// <param name="ingestion">Project ingestion service dependency used by the project ingestion workflow to provide the corresponding application capability.</param>
/// <param name="blobs">Project blob reconstruction service dependency used by the project ingestion workflow to provide the corresponding application capability.</param>
/// <param name="recommendations">Upload processing recommendation service dependency used by the project ingestion workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/project-ingestion")]
public sealed class ProjectIngestionController(
    IProjectIngestionService ingestion,
    IProjectBlobReconstructionService blobs,
    IUploadProcessingRecommendationService recommendations,
    ILogger<ProjectIngestionController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the get projection for the project ingestion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="workspaceName">Workspace name value supplied to the project ingestion operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{workspaceName}")]
    public async Task<ActionResult<ProjectIngestionGateRecord>> Get(string workspaceName, CancellationToken cancellationToken)
    {
        var record = await ingestion.GetAsync(workspaceName, cancellationToken).ConfigureAwait(false);
        /// <summary>
        /// Represents an is helper type nested within <see cref="ProjectIngestionController"/>, grouping the state or behavior used only by that containing workflow.
        /// </summary>
        return record is null ? NotFound() : Ok(record);
    }

    /// <summary>
    /// Returns the inspect projection for the project ingestion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="workspaceName">Workspace name value supplied to the project ingestion operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{workspaceName}/inspect")]
    public async Task<ActionResult<ProjectIngestionGateRecord>> Inspect(string workspaceName, CancellationToken cancellationToken) =>
        Ok(await ingestion.InspectAsync(workspaceName, cancellationToken).ConfigureAwait(false));

    /// <summary>Returns the latest persisted advisory processing recommendation for one quarantine workspace.</summary>
    /// <param name="workspaceName">Workspace name value supplied to the project ingestion operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{workspaceName}/recommendation")]
    public async Task<ActionResult<UploadProcessingRecommendation>> GetRecommendation(string workspaceName, CancellationToken cancellationToken)
    {
        try
        {
            var recommendation = await recommendations.GetAsync(workspaceName, cancellationToken).ConfigureAwait(false);
            return recommendation is null ? NotFound() : Ok(recommendation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Loading upload-processing recommendation failed for {WorkspaceName}.", workspaceName);
            throw;
        }
    }

    /// <summary>Builds a user-reviewable AI recommendation from curator-approved evidence, approved knowledge and existing Council teams.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("recommend")]
    public async Task<ActionResult<UploadProcessingRecommendation>> Recommend([FromBody] UploadProcessingRecommendationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await recommendations.RecommendAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generating upload-processing recommendation failed for {WorkspaceName}.", request.WorkspaceName);
            throw;
        }
    }

    /// <summary>
    /// Returns the review projection for the project ingestion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("review")]
    public async Task<ActionResult<ProjectIngestionGateRecord>> Review([FromBody] ProjectIngestionReviewRequest request, CancellationToken cancellationToken) =>
        Ok(await ingestion.ReviewAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the promote projection for the project ingestion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="workspaceName">Workspace name value supplied to the project ingestion operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{workspaceName}/promote")]
    [HumanApprovalRequired(
        "project.ingestion.promote",
        "Promote quarantined project",
        "Copy or extract only the reviewed, bounded project material from quarantine into the promoted workspace after deterministic checks and independent review.",
        "High",
        "Project ingestion reviewer",
        requiredBeforeCompletion: true)]
    public async Task<ActionResult<ProjectIngestionGateRecord>> Promote(string workspaceName, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Ok(await ingestion.PromoteAsync(workspaceName, userConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Project ingestion promotion was rejected for {WorkspaceName}.", workspaceName);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Starts blob for the project ingestion API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="manifest">Manifest value supplied to the project ingestion operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("blob/start")]
    public async Task<ActionResult<ProjectBlobSessionSnapshot>> StartBlob([FromBody] ProjectBlobManifest manifest, CancellationToken cancellationToken) =>
        Ok(await blobs.StartAsync(manifest, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Adds blob chunk for the project ingestion API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("blob/{sessionId:guid}/chunk")]
    public async Task<ActionResult<ProjectBlobSessionSnapshot>> AddBlobChunk(
        Guid sessionId,
        [FromBody] ProjectBlobChunkRequest request,
        CancellationToken cancellationToken) =>
        Ok(await blobs.AddChunkAsync(sessionId, request.RelativePath, request.ChunkIndex, request.Base64Data, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the finalize blob projection for the project ingestion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("blob/{sessionId:guid}/finalize")]
    public async Task<ActionResult<ProjectBlobSessionSnapshot>> FinalizeBlob(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await blobs.FinalizeAsync(sessionId, cancellationToken).ConfigureAwait(false));
}

/// <summary>HTTP payload for one ordered base64 file chunk.</summary>
public sealed class ProjectBlobChunkRequest
{
    /// <summary>
    /// Gets or sets the relative path used by this project blob chunk instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="ProjectBlobChunkRequest"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the chunk index value that forms part of the project blob chunk state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chunk index value exposed by <see cref="ProjectBlobChunkRequest"/>.</value>
    public int ChunkIndex { get; set; }
    /// <summary>
    /// Gets or sets the base64 data value that forms part of the project blob chunk state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The base64 data value exposed by <see cref="ProjectBlobChunkRequest"/>.</value>
    public string Base64Data { get; set; } = string.Empty;
}
