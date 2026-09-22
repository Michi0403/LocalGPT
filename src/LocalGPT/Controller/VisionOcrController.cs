using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes bounded local OCR capability and workspace-image recognition without accepting arbitrary server file paths.</summary>
[ApiController]
[Route("api/vision-ocr")]
public sealed class VisionOcrController(
    IWorkspaceVisionOcrService ocr,
    ILogger<VisionOcrController> logger) : ControllerBase
{
    /// <summary>Reports whether the requested local OCR model is installed and runtime-compatible on a configured Ollama host.</summary>
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
