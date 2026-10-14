using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Knowledge-owned reusable regex relationships, with explicit human approval for relationship mutation.</summary>
/// <param name="links">Knowledge regex link service dependency used by the knowledge regex links workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/knowledge/{knowledgeEntryId:guid}/regex-links")]
public sealed class KnowledgeRegexLinksController(IKnowledgeRegexLinkService links, ILogger<KnowledgeRegexLinksController> logger) : ControllerBase
{
    /// <summary>Lists semantically curated regex relationships belonging to one Council knowledge entry.</summary>
    /// <param name="knowledgeEntryId">Identifier of the knowledge entry to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<IResult> ListAsync(Guid knowledgeEntryId, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await links.GetForKnowledgeAsync(knowledgeEntryId, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Knowledge regex relationship listing failed."); return Results.InternalServerError("Knowledge regex links unavailable."); }
    }

    /// <summary>Tests already-linked regex recognizers without storing the submitted text.</summary>
    /// <param name="knowledgeEntryId">Identifier of the knowledge entry to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("recognize")]
    public async Task<IResult> RecognizeAsync(Guid knowledgeEntryId, [FromBody] KnowledgeRegexRecognitionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || request.Text?.Length > 200_000) return Results.BadRequest("Recognition input exceeds the allowed limit.");
            return Results.Ok(await links.TestRecognitionAsync(knowledgeEntryId, request.Text ?? string.Empty, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Knowledge regex recognition failed; text omitted."); return Results.InternalServerError("Knowledge regex recognition failed."); }
    }

    /// <summary>Saves one reviewed semantic regex relationship under the shared controller approval contract.</summary>
    /// <param name="knowledgeEntryId">Identifier of the knowledge entry to use for this operation.</param>
    /// <param name="regexPatternId">Identifier of the regex pattern to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("{regexPatternId:int}")]
    [HumanApprovalRequired("knowledge.regex-link.save", "Save knowledge regex link", "Create or edit an enabled semantic relationship between reviewed knowledge and a reusable regex.", "High", "Knowledge curator", requiredBeforeCompletion: true)]
    public async Task<IResult> SaveAsync(Guid knowledgeEntryId, int regexPatternId, [FromBody] SaveKnowledgeRegexPatternLinkRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || (request.KnowledgeEntryId != Guid.Empty && request.KnowledgeEntryId != knowledgeEntryId) ||
                (request.RegexPatternId != 0 && request.RegexPatternId != regexPatternId))
                return Results.BadRequest("Knowledge and regex IDs must match the route.");
            request.KnowledgeEntryId = knowledgeEntryId;
            request.RegexPatternId = regexPatternId;
            request.UserConfirmed = true;
            return Results.Ok(await links.SaveAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (ArgumentException) { return Results.BadRequest("Regex relationship is invalid."); }
        catch (Exception exception) { logger.LogError(exception, "Saving knowledge regex relationship failed; content omitted."); return Results.InternalServerError("Knowledge regex relationship could not be saved."); }
    }

    /// <summary>Removes one relationship only, never the underlying knowledge or pattern.</summary>
    /// <param name="knowledgeEntryId">Identifier of the knowledge entry to use for this operation.</param>
    /// <param name="regexPatternId">Identifier of the regex pattern to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("{regexPatternId:int}")]
    [HumanApprovalRequired("knowledge.regex-link.delete", "Delete knowledge regex link", "Remove a selected semantic regex link without deleting knowledge or regex objects.", "Medium", "Knowledge curator", requiredBeforeCompletion: true)]
    public async Task<IResult> DeleteAsync(Guid knowledgeEntryId, int regexPatternId, CancellationToken cancellationToken)
    {
        try
        {
            await links.DeleteAsync(knowledgeEntryId, regexPatternId, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Deleting knowledge regex relationship failed."); return Results.InternalServerError("Knowledge regex relationship could not be deleted."); }
    }
}
