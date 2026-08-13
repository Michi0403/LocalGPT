using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LocalGPT.Controller;

/// <summary>
/// User-owned catalog for deciding which LocalGPT DX functions and public service methods are visible to AI chat or securely linked 1-Wire peers.
/// </summary>
/// <param name="catalog">Devexpress ai function catalog service dependency used by the DevExpress AI function catalog workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/dxai/catalog")]
public sealed class DxAiFunctionCatalogController(
    IDxAiFunctionCatalogService catalog,
    ILogger<DxAiFunctionCatalogController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the get projection for the DevExpress AI function catalog API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.GetEntriesAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read the DX function catalog.");
            return Results.InternalServerError("DX function catalog discovery failed. Review LocalGPT application logs.");
        }
    }

    /// <summary>
    /// Returns the synchronize projection for the DevExpress AI function catalog API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("synchronize")]
    public async Task<IResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not synchronize the DX function catalog.");
            return Results.InternalServerError("DX function catalog synchronization failed. Existing user policy was not intentionally replaced.");
        }
    }

    /// <summary>
    /// Persists policy for the DevExpress AI function catalog API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("policy")]
    public async Task<IResult> SavePolicyAsync([FromBody] DxAiFunctionCatalogSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.SavePolicyAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Rejected malformed peer policy JSON for catalog key {CatalogKey}.", request.CatalogKey);
            return Results.BadRequest("Allowed peer IDs must be a JSON string array.");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not save DX function catalog policy {CatalogKey}.", request.CatalogKey);
            return Results.InternalServerError("DX function policy was not saved. Review LocalGPT application logs.");
        }
    }
}
