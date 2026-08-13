using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the code generation application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="workflow">Code generation workflow service dependency used by the code generation workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the code generation workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/code-generation/reviews")]
public sealed class CodeGenerationController(
    ICodeGenerationWorkflowService workflow,
    LocalGptCatalogService catalog,
    ILogger<CodeGenerationController> logger) : ControllerBase
{
    /// <summary>
    /// Gets the source-generation capability map used by DXAiChat and the AI Council.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("/api/code-generation/capabilities")]
    public IResult GetCapabilities()
    {
        try
        {
            return Results.Ok(new
            {
                ReviewWorkflow = new
                {
                    Create = "codegen.review.create",
                    Inspect = "codegen.review.get",
                    Execute = "codegen.review.execute",
                    Reject = "codegen.review.reject"
                },
                OutputKinds = new[]
                {
                    CodeGenerationOutputKinds.SourceFiles,
                    CodeGenerationOutputKinds.ClassLibrary,
                    CodeGenerationOutputKinds.ConsoleApplication,
                    CodeGenerationOutputKinds.Solution,
                    CodeGenerationOutputKinds.LocalGptAddon,
                    CodeGenerationOutputKinds.CSharpScript,
                    CodeGenerationOutputKinds.PowerShellScript,
                    CodeGenerationOutputKinds.JavaScriptModule
                },
                ExactFileGeneration = "Submit files[] with relativePath/content. CodeDOM is optional and has a plain C# fallback; .ps1 can be written directly.",
                WorkspaceWriteDxFunction = "council.artifact_workspace_file.write",
                catalog.MaxFiles,
                catalog.MaxSingleFileBytes,
                ArtifactTextExtensions = catalog.ArtifactTextExtensions.Order(StringComparer.OrdinalIgnoreCase).ToArray()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not return the code-generation capability map.");
            return Results.InternalServerError("Could not return code-generation capabilities. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Lists reviews for the code generation API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="take">Take value supplied to the code generation operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<IResult> ListReviews(
        [FromQuery] Guid? projectId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Results.Ok(await workflow.ListReviewsAsync(projectId, take, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not list code-generation reviews.");
            return Results.InternalServerError("Could not list code-generation reviews. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Retrieves review for the code generation API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{reviewId:guid}")]
    public async Task<IResult> GetReview(Guid reviewId, CancellationToken cancellationToken)
    {
        try
        {
            var review = await workflow.GetReviewAsync(reviewId, cancellationToken).ConfigureAwait(false);
            return review is null ? Results.NotFound() : Results.Ok(review);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not load code-generation review {ReviewId}.", reviewId);
            return Results.InternalServerError("Could not load the review. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Creates review for the code generation API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    [HumanApprovalRequired(
        "code-generation.review.create",
        "Store exact code-generation review",
        "Persist the submitted reviewed change plan so the human can inspect its files, outputs, hashes, and safety summary before execution.",
        "Medium",
        "Code review coordinator")]
    public async Task<IResult> CreateReview(
        [FromBody] CreateCodeGenerationReviewRequest request,
        [FromQuery] bool userConfirmed,
        CancellationToken cancellationToken)
    {
        if (!userConfirmed)
            return Results.Conflict(new { Error = "Fresh human confirmation is required to store this exact change review." });

        try
        {
            var review = await workflow.CreateReviewAsync(request, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/code-generation/reviews/{review.Id}", review);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Rejected invalid code-generation review request; payload content was omitted from logs.");
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not create a code-generation review; payload content was omitted from logs.");
            return Results.InternalServerError("Could not create the review. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Executes review for the code generation API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{reviewId:guid}/execute")]
    [HumanApprovalRequired(
        "code-generation.review.execute",
        "Execute exact code-generation review",
        "Generate the exact hash-bound reviewed files and, when requested by this same input, run the bounded build inside the isolated artifact workspace.",
        "High",
        "Code-generation security reviewer",
        requiredBeforeCompletion: true)]
    public async Task<IResult> ExecuteReview(
        Guid reviewId,
        [FromBody] ExecuteCodeGenerationReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await workflow.ExecuteReviewAsync(reviewId, request, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogInformation(ex, "Code-generation review {ReviewId} was not found.", reviewId);
            return Results.NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogInformation(ex, "Code-generation review {ReviewId} execution was denied by workflow policy.", reviewId);
            return Results.Conflict(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Code-generation review {ReviewId} execution failed; generated content was omitted from logs.", reviewId);
            return Results.InternalServerError("Generation failed. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Rejects review for the code generation API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{reviewId:guid}/reject")]
    public async Task<IResult> RejectReview(
        Guid reviewId,
        [FromBody] RejectCodeGenerationReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await workflow.RejectReviewAsync(reviewId, request, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogInformation(ex, "Code-generation review {ReviewId} was not found.", reviewId);
            return Results.NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogInformation(ex, "Code-generation review {ReviewId} rejection was denied by workflow policy.", reviewId);
            return Results.Conflict(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Code-generation review {ReviewId} rejection failed.", reviewId);
            return Results.InternalServerError("Review rejection failed. Review LocalGPT application logs.");
        }
    }
}
