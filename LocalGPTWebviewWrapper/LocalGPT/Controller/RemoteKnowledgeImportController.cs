using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/knowledge/remote-import")]
public sealed class RemoteKnowledgeImportController(
    IRemoteKnowledgeImportService importer,
    ILogger<RemoteKnowledgeImportController> logger) : ControllerBase
{
    [HttpPost("preview")]
    public async Task<ActionResult<RemoteKnowledgeImportResult>> Preview(
        [FromBody] RemoteKnowledgeImportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            request.PreviewOnly = true;
            request.SaveToKnowledge = false;
            request.UserConfirmed = false;
            return Ok(await importer.ImportAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or InvalidDataException or HttpRequestException)
        {
            logger.LogWarning(ex, "Remote knowledge inspection was rejected.");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    [HumanApprovalRequired(
        "knowledge.remote.import",
        "Import GitHub or webpage knowledge",
        "Download the selected public source, safely extract matching files, and save compact source maps to LocalGPT knowledge.",
        "High",
        "Knowledge curator")]
    public async Task<ActionResult<RemoteKnowledgeImportResult>> Import(
        [FromBody] RemoteKnowledgeImportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await importer.ImportAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or InvalidDataException or HttpRequestException)
        {
            logger.LogWarning(ex, "Remote knowledge import was rejected.");
            return BadRequest(new { error = ex.Message });
        }
    }
}
