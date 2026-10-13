using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the DevExpress AI functions application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="functionClient">Devexpress ai function service client dependency used by the DevExpress AI functions workflow to provide the corresponding application capability.</param>
/// <param name="recovery">Devexpress ai function call recovery service dependency used by the DevExpress AI functions workflow to provide the corresponding application capability.</param>
/// <param name="deferredInvocations">Deferred devexpress ai invocation service dependency used by the DevExpress AI functions workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/dxai/functions")]
public sealed class DxAiFunctionsController(
    IDxAiFunctionServiceClient functionClient,
    IDxAiFunctionCallRecoveryService recovery,
    IDeferredDxAiInvocationService deferredInvocations,
    IDxAiFunctionInvocationAuditService audit,
    ILogger<DxAiFunctionsController> logger) : ControllerBase
{
    /// <summary>
    /// Lists functions for the DevExpress AI functions API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public IResult ListFunctions()
    {
        try
        {
            var functions = functionClient.GetFunctions();
            logger.LogDebug("Returned {FunctionCount} DI-backed DXAIFunction descriptor(s).", functions.Count);
            return Results.Ok(functions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not list DI-backed DXAIFunction descriptors.");
            return Results.InternalServerError("DXAIFunction discovery failed. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Returns the recover function text projection for the DevExpress AI functions API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("recover")]
    public async Task<IResult> RecoverFunctionText(
        [FromBody] DxAiFunctionTextRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        var result = recovery.Recover(request.Content, request.AutomaticInvocation);
        if (!result.Recognized || !request.InvokeRecognizedCalls)
            return Results.Ok(result);

        foreach (var call in result.Calls)
        {
            var invocation = await functionClient.CallAsync(
                call.FunctionName,
                new DxAiFunctionInvocationRequest
                {
                    Parameters = call.Arguments,
                    AutomaticInvocation = request.AutomaticInvocation,
                    UserConfirmed = false,
                    RequestedBy = request.RequestedBy,
                    ConversationId = request.ConversationId,
                    ProjectId = request.ProjectId,
                    ProjectVersionId = request.ProjectVersionId,
                    ApplicationVersion = request.ApplicationVersion
                },
                cancellationToken).ConfigureAwait(false);
            result.Invocations.Add(invocation);
        }
        return Results.Ok(result);
    }

    /// <summary>
    /// Executes approved deferred for the DevExpress AI functions API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="approvalRequestId">Identifier of the approval request to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("deferred/{approvalRequestId:guid}/execute")]
    public async Task<IResult> ExecuteApprovedDeferred(Guid approvalRequestId, CancellationToken cancellationToken) =>
        Results.Ok(await deferredInvocations.ExecuteApprovedForApprovalRequestAsync(approvalRequestId, cancellationToken: cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Invokes function for the DevExpress AI functions API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI functions operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <summary>Validates exact function parameters and produces a non-executing, policy-bound fingerprint.</summary>
    [HttpPost("{functionName}/preview")]
    public async Task<IResult> PreviewFunction(string functionName,
        [FromBody] DxAiFunctionInvocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            request.PreviewOnly = true;
            request.UserConfirmed = false;
            var result = await functionClient.CallAsync(functionName, request, cancellationToken).ConfigureAwait(false);
            return Results.Json(result, statusCode: result.Succeeded ? 200 : 400);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "DX function preview failed for {FunctionName}.", functionName);
            return Results.InternalServerError("DX function preview failed. See application diagnostics.");
        }
    }

    /// <summary>Returns sanitized durable function execution history without arguments or results.</summary>
    [HttpGet("history")]
    public async Task<IResult> GetInvocationHistoryAsync([FromQuery] int take, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await audit.GetRecentAsync(take == 0 ? 30 : take, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading DX function invocation history failed.");
            return Results.InternalServerError("DX function history is currently unavailable.");
        }
    }

    [HttpPost("{functionName}/invoke")]
    public async Task<IResult> InvokeFunction(
        string functionName,
        [FromBody] DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await functionClient.CallAsync(functionName, request, cancellationToken).ConfigureAwait(false);
        var statusCode = result.Status switch
        {
            "NotFound" => StatusCodes.Status404NotFound,
            "HumanConfirmationRequired" => StatusCodes.Status409Conflict,
            "HumanApprovalPending" => StatusCodes.Status202Accepted,
            "HumanApprovalDeclined" => StatusCodes.Status403Forbidden,
            "AutomaticInvocationDenied" => StatusCodes.Status403Forbidden,
            "InvalidParameters" => StatusCodes.Status400BadRequest,
            "StalePreview" => StatusCodes.Status409Conflict,
            "DiscoveryOnly" => StatusCodes.Status405MethodNotAllowed,
            "Failed" => StatusCodes.Status500InternalServerError,
            "Cancelled" => StatusCodes.Status409Conflict,
            _ => result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
        return Results.Json(result, statusCode: statusCode);
    }
}
