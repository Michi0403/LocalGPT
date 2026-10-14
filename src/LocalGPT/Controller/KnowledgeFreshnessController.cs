using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes stale-knowledge reporting and reviewed exact-source refresh through the API boundary.</summary>
/// <param name="freshness">Knowledge freshness review service dependency used by the knowledge freshness workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/knowledge/freshness")]
public sealed class KnowledgeFreshnessController(
    IKnowledgeFreshnessReviewService freshness,
    ILogger<KnowledgeFreshnessController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the report projection for the knowledge freshness API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the refresh projection for the knowledge freshness API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
