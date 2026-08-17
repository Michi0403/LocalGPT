using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the structured text application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="translator">Structured text translation service dependency used by the structured text workflow to provide the corresponding application capability.</param>
/// <param name="regexPatterns">Regex pattern service dependency used by the structured text workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="councilText">Council text service dependency used by the structured text workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/text/structured")]
public sealed class StructuredTextController(
    IStructuredTextTranslationService translator,
    IRegexPatternService regexPatterns,
    CouncilTextService councilText,
    ILogger<StructuredTextController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the translate JSON projection for the structured text API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("json/translate")]
    public IResult TranslateJson([FromBody] StructuredJsonTranslationRequest request)
    {
        try
        {
            var result = translator.TranslateJson(request);
            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Structured JSON translation endpoint failed; request text was omitted from logs.");
            return Results.InternalServerError("Structured JSON translation failed. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Returns the inspect JSON projection for the structured text API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("json/inspect")]
    public IResult InspectJson([FromBody] StructuredJsonTranslationRequest request)
    {
        try
        {
            request.IncludeRawJson = false;
            var result = translator.TranslateJson(request);
            return result.Succeeded
                ? Results.Ok(new
                {
                    result.Status,
                    result.Documents,
                    result.Warnings
                })
                : Results.BadRequest(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Structured JSON inspection endpoint failed; request text was omitted from logs.");
            return Results.InternalServerError("Structured JSON inspection failed. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Lists JSON regexes for the structured text API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("json/regexes")]
    public async Task<IResult> ListJsonRegexes(CancellationToken cancellationToken)
    {
        try
        {
            var rows = await regexPatterns.ListAllAsync(5000).ConfigureAwait(false);
            var jsonPatterns = rows
                .Where(row => councilText.StartsWithText(row.Name, "builtin.json-"))
                .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .Select(row => new { row.Name, row.Pattern, row.Flags, row.UpdatedOn })
                .ToList();
            return Results.Ok(jsonPatterns);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not list JSON-oriented regex definitions.");
            return Results.InternalServerError("JSON regex discovery failed. Review LocalGPT application logs.");
        }
    }
}
