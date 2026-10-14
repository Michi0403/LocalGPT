using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Persistent Council team definitions and resettable templates exposed through their business service, not the database.</summary>
/// <param name="teams">Council team configuration service dependency used by the council teams workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/council/teams")]
public sealed class CouncilTeamsController(ICouncilTeamConfigurationService teams, ILogger<CouncilTeamsController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the list projection for the council teams API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="includeDisabled">Value indicating whether include disabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<IResult> ListAsync([FromQuery] bool includeDisabled, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await teams.GetTeamsAsync(includeDisabled, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council team listing failed."); return Results.InternalServerError("Council team inventory unavailable."); }
    }

    /// <summary>
    /// Returns the templates projection for the council teams API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("templates")]
    public async Task<IResult> TemplatesAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await teams.GetDefaultTemplatesAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council team template listing failed."); return Results.InternalServerError("Council templates unavailable."); }
    }

    /// <summary>
    /// Returns the find projection for the council teams API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="key">Key value supplied to the council teams operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{key}")]
    public async Task<IResult> FindAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var team = await teams.FindTeamAsync(key, cancellationToken).ConfigureAwait(false);
            return team is null ? Results.NotFound() : Results.Ok(team);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council team lookup failed."); return Results.InternalServerError("Council team lookup unavailable."); }
    }

    /// <summary>
    /// Returns the save projection for the council teams API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut]
    [HumanApprovalRequired("council.team.save", "Save Council team", "Create or update a configured Council team and its model/role/workflow bindings.", "High", "Council team reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> SaveAsync([FromBody] SaveCouncilTeamConfigurationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || request.Team is null) return Results.BadRequest("Council team definition required.");
            request.UserConfirmed = true;
            return Results.Ok(await teams.SaveAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council team save failed; team configuration omitted."); return Results.InternalServerError("Council team could not be saved."); }
    }

    /// <summary>
    /// Returns the delete projection for the council teams API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="key">Key value supplied to the council teams operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("{key}")]
    [HumanApprovalRequired("council.team.delete", "Delete Council team", "Mark one persisted Council team deleted without deleting maintained templates.", "High", "Council team reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await teams.DeleteAsync(key, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council team delete failed."); return Results.InternalServerError("Council team could not be deleted."); }
    }

    /// <summary>
    /// Returns the reset projection for the council teams API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="key">Key value supplied to the council teams operation and used when producing its result.</param>
    /// <param name="templateKey">Template key value supplied to the council teams operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{key}/reset/{templateKey}")]
    [HumanApprovalRequired("council.team.reset", "Reset Council team", "Replace one team's configuration with the chosen maintained template.", "High", "Council team reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> ResetAsync(string key, string templateKey, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await teams.ResetToTemplateAsync(key, templateKey, userConfirmed: true, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council team reset failed."); return Results.InternalServerError("Council team could not be reset."); }
    }
}
