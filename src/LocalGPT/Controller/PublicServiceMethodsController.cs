using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// HTTP-facing business-purpose inventory and exact-action invocation of public service methods.
/// The DX Function Catalog owns persisted discovery/policy, the public service invoker owns DI method binding,
/// and HumanApprovalRequired owns local execution authority. No arbitrary CLR type or name can be invoked.
/// </summary>
/// <param name="directory">Public service method directory service dependency used by the public service methods workflow to provide the corresponding application capability.</param>
/// <param name="invoker">Public service method invoker dependency used by the public service methods workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/dxai/public-service")]
public sealed class PublicServiceMethodsController(
    IPublicServiceMethodDirectoryService directory,
    IPublicServiceMethodInvoker invoker,
    ILogger<PublicServiceMethodsController> logger) : ControllerBase
{
    /// <summary>Search service methods by business-purpose service class, name, or purpose; returns persisted callable schemas.</summary>
    /// <param name="query">Query value supplied to the public service methods operation and used when producing its result.</param>
    /// <param name="service">Service value supplied to the public service methods operation and used when producing its result.</param>
    /// <param name="skip">Skip value supplied to the public service methods operation and used when producing its result.</param>
    /// <param name="take">Take value supplied to the public service methods operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<IResult> ListAsync([FromQuery] string? query, [FromQuery] string? service, [FromQuery] int skip, [FromQuery] int take, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await directory.SearchAsync(query, service, skip, take, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Public service method discovery failed; search text omitted.");
            return Results.InternalServerError("Service operation inventory unavailable. Review LocalGPT diagnostics.");
        }
    }

    /// <summary>Groups catalogued service operations by their implementation's business domain. Properties are metadata only.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("groups")]
    public async Task<IResult> GroupsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await directory.GroupsAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Service contract metadata enumeration failed.");
            return Results.InternalServerError("Service contract metadata unavailable.");
        }
    }

    /// <summary>Finds one stored service signature by stable catalog key, including its JSON parameter schema and user exposure policy.</summary>
    /// <param name="catalogKey">Catalog key value supplied to the public service methods operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{catalogKey}")]
    public async Task<IResult> GetAsync(string catalogKey, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await directory.FindAsync(catalogKey, cancellationToken).ConfigureAwait(false);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Service contract lookup failed.");
            return Results.InternalServerError("Service contract lookup unavailable.");
        }
    }

    /// <summary>
    /// Invokes one explicitly approved catalogued service method. Unlike AI/1-Wire access, local invocation does
    /// not require exposing the same method to AI chat or remote peers; the catalog's enabled state is still honored.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("invoke")]
    [HumanApprovalRequired("localgpt.public_service.invoke", "Invoke catalogued local service", "Execute exactly one user-selected public service method with bound JSON arguments. The action may modify local application state.", "High", "Local service operator", requiredBeforeCompletion: true)]
    public async Task<IResult> InvokeAsync([FromBody] PublicServiceMethodInvocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.CatalogKey))
                return Results.BadRequest("A valid service catalog key is required.");
            return Results.Ok(await invoker.InvokeLocallyApprovedAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (System.Text.Json.JsonException) { return Results.BadRequest("Service method parameters do not match the registered contract."); }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "A catalogued service method could not be invoked.");
            return Results.Conflict("Service method is unavailable, not configured, or has a failed precondition.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Approved service invocation failed; method argument payload omitted.");
            return Results.InternalServerError("Approved service invocation failed. Review LocalGPT diagnostics.");
        }
    }
    /// <summary>Updates one exact service family's persisted AI and peer exposure in one user-approved operation, preserving all other policy fields.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("exposure")]
    [HumanApprovalRequired("localgpt.public_service.exposure", "Change service family exposure", "Choose which service family methods may be invoked by LocalGPT AI or trusted 1-Wire peers; no method is executed by this change.", "High", "Service policy operator", requiredBeforeCompletion: true)]
    public async Task<IResult> SetExposureAsync([FromBody] PublicServiceExposureRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Service))
                return Results.BadRequest("An exact service name is required.");
            return Results.Ok(await directory.SetExposureAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Approved public service exposure update failed; policy values omitted.");
            return Results.InternalServerError("Service exposure could not be updated. Review LocalGPT diagnostics.");
        }
    }

}
