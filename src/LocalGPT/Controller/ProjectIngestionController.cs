using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes quarantine-first project ingestion and bounded blob reconstruction to the regular LocalGPT UI/API surface.</summary>
[ApiController]
[Route("api/project-ingestion")]
public sealed class ProjectIngestionController(
    IProjectIngestionService ingestion,
    IProjectBlobReconstructionService blobs,
    IUploadProcessingRecommendationService recommendations,
    ILogger<ProjectIngestionController> logger) : ControllerBase
{
    [HttpGet("{workspaceName}")]
    public async Task<ActionResult<ProjectIngestionGateRecord>> Get(string workspaceName, CancellationToken cancellationToken)
    {
        var record = await ingestion.GetAsync(workspaceName, cancellationToken).ConfigureAwait(false);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{workspaceName}/inspect")]
    public async Task<ActionResult<ProjectIngestionGateRecord>> Inspect(string workspaceName, CancellationToken cancellationToken) =>
        Ok(await ingestion.InspectAsync(workspaceName, cancellationToken).ConfigureAwait(false));

    /// <summary>Returns the latest persisted advisory processing recommendation for one quarantine workspace.</summary>
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

    [HttpPost("review")]
    public async Task<ActionResult<ProjectIngestionGateRecord>> Review([FromBody] ProjectIngestionReviewRequest request, CancellationToken cancellationToken) =>
        Ok(await ingestion.ReviewAsync(request, cancellationToken).ConfigureAwait(false));

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

    [HttpPost("blob/start")]
    public async Task<ActionResult<ProjectBlobSessionSnapshot>> StartBlob([FromBody] ProjectBlobManifest manifest, CancellationToken cancellationToken) =>
        Ok(await blobs.StartAsync(manifest, cancellationToken).ConfigureAwait(false));

    [HttpPost("blob/{sessionId:guid}/chunk")]
    public async Task<ActionResult<ProjectBlobSessionSnapshot>> AddBlobChunk(
        Guid sessionId,
        [FromBody] ProjectBlobChunkRequest request,
        CancellationToken cancellationToken) =>
        Ok(await blobs.AddChunkAsync(sessionId, request.RelativePath, request.ChunkIndex, request.Base64Data, cancellationToken).ConfigureAwait(false));

    [HttpPost("blob/{sessionId:guid}/finalize")]
    public async Task<ActionResult<ProjectBlobSessionSnapshot>> FinalizeBlob(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await blobs.FinalizeAsync(sessionId, cancellationToken).ConfigureAwait(false));
}

/// <summary>HTTP payload for one ordered base64 file chunk.</summary>
public sealed class ProjectBlobChunkRequest
{
    public string RelativePath { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public string Base64Data { get; set; } = string.Empty;
}
