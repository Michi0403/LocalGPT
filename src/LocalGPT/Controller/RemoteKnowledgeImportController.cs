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
            ArgumentNullException.ThrowIfNull(request);
            request.PreviewOnly = true;
            request.SaveToKnowledge = false;
            request.UserConfirmed = false;
            return Ok(await importer.ImportAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or HttpRequestException)
        {
            logger.LogWarning(exception, "Remote knowledge inspection was rejected; source path and content were omitted.");
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Remote knowledge inspection was cancelled.");
            return Conflict(new { error = "The remote knowledge inspection was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote knowledge inspection failed; source path and content were omitted.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Remote knowledge inspection failed");
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
            ArgumentNullException.ThrowIfNull(request);
            return Ok(await importer.ImportAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or HttpRequestException)
        {
            logger.LogWarning(exception, "Remote knowledge import was rejected; source path and content were omitted.");
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Remote knowledge import was cancelled.");
            return Conflict(new { error = "The remote knowledge import was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote knowledge import failed; source path and content were omitted.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Remote knowledge import failed");
        }
    }
}
