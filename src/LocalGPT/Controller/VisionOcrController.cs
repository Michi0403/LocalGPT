using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes bounded local OCR capability and workspace-image recognition without accepting arbitrary server file paths.</summary>
/// <param name="ocr">Workspace vision ocr service dependency used by the vision OCR workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/vision-ocr")]
public sealed class VisionOcrController(
    IWorkspaceVisionOcrService ocr,
    ILogger<VisionOcrController> logger) : ControllerBase
{
    /// <summary>Reports whether the requested local OCR model is installed and runtime-compatible on a configured Ollama host.</summary>
    /// <param name="modelName">Model name value supplied to the vision OCR operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("capability")]
    public async Task<ActionResult<LocalVisionOcrCapability>> Capability([FromQuery] string? modelName, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await ocr.GetCapabilityAsync(modelName, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading local OCR capability failed.");
            throw;
        }
    }

    /// <summary>Recognizes text in one image already contained by a chat-upload workspace.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("workspace")]
    public async Task<ActionResult<WorkspaceImageOcrResult>> Workspace([FromBody] WorkspaceImageOcrRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await ocr.RecognizeWorkspaceImageAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or FileNotFoundException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Workspace OCR request was rejected by its bounded input policy.");
            return BadRequest(new { error = exception.Message });
        }
    }
}
