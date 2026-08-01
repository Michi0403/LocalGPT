using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/text/structured")]
public sealed class StructuredTextController(
    IStructuredTextTranslationService translator,
    IRegexPatternService regexPatterns,
    ILogger<StructuredTextController> logger) : ControllerBase
{
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

    [HttpGet("json/regexes")]
    public async Task<IResult> ListJsonRegexes(CancellationToken cancellationToken)
    {
        try
        {
            var rows = await regexPatterns.ListAllAsync(5000).ConfigureAwait(false);
            var jsonPatterns = rows
                .Where(row => row.Name.StartsWith("builtin.json-", StringComparison.OrdinalIgnoreCase))
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
