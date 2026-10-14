using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Business-owned API for persisted and human-approved runtime extensions; callable DXFunctions still use the central registry.</summary>
/// <param name="plugins">Runtime plugin service dependency used by the runtime plugins workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Devexpress ai function catalog service dependency used by the runtime plugins workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/runtime-plugins")]
public sealed class RuntimePluginsController(
    IRuntimePluginService plugins,
    IDxAiFunctionCatalogService catalog,
    ILogger<RuntimePluginsController> logger) : ControllerBase
{
    /// <summary>Lists persisted extension definitions including source and compilation state for the local operator.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await plugins.ListAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Runtime extension inventory failed; source omitted."); return Results.InternalServerError("Runtime extension inventory unavailable."); }
    }

    /// <summary>Returns a maintained template for a particular supported extension language.</summary>
    /// <param name="kind">Kind value supplied to the runtime plugins operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("templates/{kind}")]
    public IResult GetTemplate(string kind)
    {
        try
        {
            if (!Enum.TryParse<RuntimePluginKind>(kind, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                return Results.BadRequest("Unsupported plugin language.");
            return Results.Ok(new { Kind = parsed.ToString(), Source = plugins.GetTemplate(parsed) });
        }
        catch (Exception exception) { logger.LogError(exception, "Runtime extension template retrieval failed."); return Results.InternalServerError("Plugin template unavailable."); }
    }

    /// <summary>Publishes a reviewed runtime extension definition, keeping execution as a separate approved action.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    [HumanApprovalRequired("runtime.plugins.save", "Save runtime plugin", "Persist a reviewed runtime plugin definition; source may contain executable code.", "High", "Runtime plugin reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> SaveAsync([FromBody] SaveRuntimePluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || request.Plugin is null) return Results.BadRequest("Plugin definition is required.");
            request.UserConfirmed = true;
            var saved = await plugins.SaveAsync(request, cancellationToken).ConfigureAwait(false);
            await catalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(saved);
        }
        catch (OperationCanceledException) { throw; }
        catch (ArgumentException) { return Results.BadRequest("The plugin definition is invalid."); }
        catch (InvalidOperationException) { return Results.Conflict("Plugin source or function identity conflicts with an existing definition."); }
        catch (Exception exception) { logger.LogError(exception, "Runtime plugin save failed; executable source omitted."); return Results.InternalServerError("Runtime plugin could not be saved."); }
    }

    /// <summary>Builds or validates one persisted runtime plugin using its selected compiler; does not execute the function.</summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{id:guid}/build-load")]
    [HumanApprovalRequired("runtime.plugins.build", "Build runtime plugin", "Compile or validate a reviewed runtime plugin, then make its descriptor available to LocalGPT.", "High", "Runtime plugin reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> BuildAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var built = await plugins.BuildAndLoadAsync(id, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            await catalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(built);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Runtime plugin build failed; executable source omitted."); return Results.InternalServerError("Runtime plugin build failed."); }
    }

    /// <summary>Deletes exactly one persisted runtime plugin definition after the existing approval gate.</summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("{id:guid}")]
    [HumanApprovalRequired("runtime.plugins.delete", "Delete runtime plugin", "Remove a persisted runtime extension definition and unload its executable content.", "High", "Runtime plugin reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await plugins.DeleteAsync(id, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            await catalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            return deleted ? Results.Ok(new { Id = id, Deleted = true }) : Results.NotFound();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Runtime plugin deletion failed."); return Results.InternalServerError("Runtime plugin could not be deleted."); }
    }
}
