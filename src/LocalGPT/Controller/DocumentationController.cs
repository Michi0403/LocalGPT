using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes availability metadata, searchable XML comments and the versioned PDF produced by the DocFX build pipeline.
/// </summary>
[ApiController]
[Route("api/documentation")]
[DocumentationUpdated("2.2.8")]
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


    /// <summary>Serves generated DocFX HTML and supporting assets from the recursively resolved installed documentation root.</summary>
    [HttpGet("/help-docs")]
    [HttpGet("/help-docs/{**relativePath}")]
    public IActionResult Html([FromRoute] string? relativePath = null)
    {
        try
        {
            var path = documentation.GetHtmlFilePath(relativePath);
            if (path is null)
                return NotFound(new { error = "Generated documentation was not found below the application directory." });

            var contentTypes = new FileExtensionContentTypeProvider();
            if (!contentTypes.TryGetContentType(path, out var contentType))
                contentType = "application/octet-stream";
            return new PhysicalFileResult(path, contentType) { EnableRangeProcessing = true };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Serving an installed LocalGPT documentation asset failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation asset failed");
        }
    }

    /// <summary>Downloads the versioned LocalGPT PDF generated for the running build.</summary>
    [HttpGet("pdf")]
    public IActionResult Pdf()
    {
        try
        {
            var path = documentation.GetPdfPath();
            if (path is null)
                return NotFound(new { error = "The versioned documentation PDF has not been generated for this build." });

            var fileName = Path.GetFileName(path);
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return new PhysicalFileResult(path, "application/pdf") { EnableRangeProcessing = true };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Downloading the generated LocalGPT PDF failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation download failed");
        }
    }
}
