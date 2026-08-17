using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>CRUD API for user-owned DXFunctions implemented by persisted Remote Control pipelines.</summary>
/// <param name="userFunctions">User devexpress ai function service dependency used by the user DevExpress AI function workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Devexpress ai function catalog service dependency used by the user DevExpress AI function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/dxai/user-functions")]
public sealed class UserDxAiFunctionController(
    IUserDxAiFunctionService userFunctions,
    IDxAiFunctionCatalogService catalog,
    ILogger<UserDxAiFunctionController> logger) : ControllerBase
{
    /// <summary>Lists all user-owned DXFunction definitions.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await userFunctions.ListAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Could not list user DXFunctions."); return Results.InternalServerError("User DXFunctions could not be listed. Review LocalGPT logs."); }
    }

    /// <summary>
    /// Returns the save projection for the user DevExpress AI function API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    [HumanApprovalRequired("dxai.user-function.save", "Save user DXFunction", "Create or update one named DXFunction backed by a user-owned Remote Control pipeline.", "High", "DXFunction configuration reviewer")]
    public async Task<IResult> SaveAsync([FromBody] SaveUserDxAiFunctionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            request.UserConfirmed = true;
            var result = await userFunctions.SaveAsync(request, cancellationToken).ConfigureAwait(false);
            await catalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }
        catch (ArgumentException exception) { return Results.BadRequest(exception.Message); }
        catch (System.Text.Json.JsonException exception) { return Results.BadRequest(exception.Message); }
        catch (KeyNotFoundException exception) { return Results.NotFound(exception.Message); }
        catch (Exception exception) { logger.LogError(exception, "Could not save user DXFunction {FunctionName}.", request.FunctionName); return Results.InternalServerError("User DXFunction was not saved. Review LocalGPT logs."); }
    }

    /// <summary>Deletes one user-owned DXFunction. System/DI-backed DXFunctions cannot be deleted through this API.</summary>
    /// <param name="functionName">Function name value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("{functionName}")]
    [HumanApprovalRequired("dxai.user-function.delete", "Delete user DXFunction", "Delete one user-owned DXFunction definition without deleting its referenced Remote Control pipeline.", "High", "DXFunction configuration reviewer")]
    public async Task<IResult> DeleteAsync(string functionName, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await userFunctions.DeleteAsync(functionName, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            await catalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            return removed ? Results.Ok(new { removed = true }) : Results.NotFound();
        }
        catch (ArgumentException exception) { return Results.BadRequest(exception.Message); }
        catch (Exception exception) { logger.LogError(exception, "Could not delete user DXFunction {FunctionName}.", functionName); return Results.InternalServerError("User DXFunction was not deleted. Review LocalGPT logs."); }
    }
}
