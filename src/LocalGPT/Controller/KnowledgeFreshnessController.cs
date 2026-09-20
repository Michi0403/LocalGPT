using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes stale-knowledge reporting and reviewed exact-source refresh through the API boundary.</summary>
[ApiController]
[Route("api/knowledge/freshness")]
public sealed class KnowledgeFreshnessController(
    IKnowledgeFreshnessReviewService freshness,
    ILogger<KnowledgeFreshnessController> logger) : ControllerBase
{
    [HttpPost("report")]
    public async Task<ActionResult<KnowledgeFreshnessReviewResult>> Report([FromBody] KnowledgeFreshnessReportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await freshness.ReportAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Knowledge freshness report was rejected; content was omitted.");
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("refresh")]
    [HumanApprovalRequired(
        "knowledge.freshness.refresh",
        "Refresh one exact knowledge source",
        "Fetch one reviewed external URL and replace stale Council knowledge through the bounded remote importer.",
        "High",
        "Knowledge curator")]
    public async Task<ActionResult<KnowledgeFreshnessReviewResult>> Refresh([FromBody] KnowledgeSourceRefreshRequest request, CancellationToken cancellationToken)
    {
        try
        {
            request.UserConfirmed = true;
            return Ok(await freshness.RefreshApprovedSourceAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or InvalidDataException or HttpRequestException)
        {
            logger.LogWarning(exception, "Exact-source knowledge refresh was rejected; URL content was omitted.");
            return BadRequest(new { error = exception.Message });
        }
    }
}
