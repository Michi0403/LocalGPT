using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the remote knowledge import application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="importer">Remote knowledge import service dependency used by the remote knowledge import workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/knowledge/remote-import")]
public sealed class RemoteKnowledgeImportController(
    IRemoteKnowledgeImportService importer,
    ILogger<RemoteKnowledgeImportController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the preview projection for the remote knowledge import API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the import projection for the remote knowledge import API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
