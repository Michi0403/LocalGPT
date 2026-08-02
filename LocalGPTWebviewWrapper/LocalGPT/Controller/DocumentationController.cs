using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes availability metadata, searchable XML comments and the versioned PDF produced by the DocFX build pipeline.
/// </summary>
[ApiController]
[Route("api/documentation")]
[DocumentationUpdated("2.1.18")]
public sealed class DocumentationController(
    IDocumentationCatalogService documentation,
    ILogger<DocumentationController> logger) : ControllerBase
{
    /// <summary>Returns the current generated-documentation and XML-comment availability.</summary>
    [HttpGet("status")]
    public ActionResult<LocalGptDocumentationStatus> Status()
    {
        try
        {
            return Ok(documentation.GetStatus());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading LocalGPT documentation status failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation status failed");
        }
    }

    /// <summary>Searches localized compiler-generated XML comments by member id, summary or remarks.</summary>
    [HttpGet("comments")]
    public ActionResult<IReadOnlyList<LocalGptDocumentationComment>> Comments(
        [FromQuery] string? query = null,
        [FromQuery] int limit = 100,
        [FromQuery] string? culture = null)
    {
        try
        {
            return Ok(documentation.SearchComments(query, limit, culture));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Searching LocalGPT XML documentation comments failed; query content was omitted.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation search failed");
        }
    }

    /// <summary>Returns one localized XML documentation member by its compiler member id.</summary>
    [HttpGet("comment")]
    public ActionResult<LocalGptDocumentationComment> Comment(
        [FromQuery] string memberId,
        [FromQuery] string? culture = null)
    {
        try
        {
            var comment = documentation.GetComment(memberId, culture);
            return comment is null ? NotFound() : Ok(comment);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading LocalGPT XML documentation member failed; member id was omitted.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation member failed");
        }
    }

    /// <summary>Downloads the versioned LocalGPT PDF generated for the running build.</summary>
    [HttpGet("pdf")]
    public IActionResult Pdf()
    {
        try
        {
            var status = documentation.GetStatus();
            var path = documentation.GetPdfPath();
            return path is null
                ? NotFound(new { error = "The versioned documentation PDF has not been generated for this build." })
                : PhysicalFile(path, "application/pdf", status.PdfFileName, enableRangeProcessing: true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Downloading the generated LocalGPT PDF failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation download failed");
        }
    }
}
