using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/dxai/functions")]
public sealed class DxAiFunctionsController(
    IDxAiFunctionServiceClient functionClient,
    ILogger<DxAiFunctionsController> logger) : ControllerBase
{
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
