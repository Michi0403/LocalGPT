using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Provides DevExpress ai functions controller operations.
/// </summary>
[ApiController]
[Route("api/dxai/functions")]
public sealed class DxAiFunctionsController(
    IDxAiFunctionServiceClient functionClient,
    IDxAiFunctionCallRecoveryService recovery,
    IDeferredDxAiInvocationService deferredInvocations,
    ILogger<DxAiFunctionsController> logger) : ControllerBase
{
    /// <summary>
    /// Runs the list functions operation.
    /// </summary>
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
    /// Runs the recover function text operation.
    /// </summary>
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
    /// Runs the execute approved deferred operation.
    /// </summary>
    [HttpPost("deferred/{approvalRequestId:guid}/execute")]
    public async Task<IResult> ExecuteApprovedDeferred(Guid approvalRequestId, CancellationToken cancellationToken) =>
        Results.Ok(await deferredInvocations.ExecuteApprovedForApprovalRequestAsync(approvalRequestId, cancellationToken: cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Runs the invoke function operation.
    /// </summary>
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
            "DiscoveryOnly" => StatusCodes.Status405MethodNotAllowed,
            "Failed" => StatusCodes.Status500InternalServerError,
            "Cancelled" => StatusCodes.Status409Conflict,
            _ => result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
        return Results.Json(result, statusCode: statusCode);
    }
}
