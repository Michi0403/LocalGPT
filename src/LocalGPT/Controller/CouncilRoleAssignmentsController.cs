using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;
namespace LocalGPT.Controller;

[ApiController]
[Route("api/council/role-assignments")]
/// <summary>
/// Handles council role assignments for model eligibility configuration.
/// Provides endpoints to list and assign roles within the Council team architecture.
/// </summary>
public sealed class CouncilRoleAssignmentsController(ICouncilRoleAssignmentService assignments,
    ILogger<CouncilRoleAssignmentsController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves role assignment choices for a specified model key.
    /// Enumerates available OrganicCouncilTeamDefinition instances that can be assigned roles.
    /// </summary>
    /// <param name="modelKey">The model identifier to look up role assignments for.</param>
    /// <param name="cancellationToken">Token to allow operation cancellation during enumeration.</param>
    /// <returns>A result containing the list of available role assignment choices.</returns>
    [HttpGet]
    public async Task<IResult> ListAsync([FromQuery] string modelKey, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await assignments.ListForModelAsync(modelKey, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError(ex, "Council role lookup failed."); return Results.InternalServerError("Council roles unavailable."); }
    }

    /// <summary>
    /// Persists a council role assignment request after human approval.
    /// Requires the <c>council.role-model.save</c> approval for high-impact eligibility changes.
    /// </summary>
    /// <param name="request">The assignment request containing model and role definitions.</param>
    /// <param name="cancellationToken">Token to allow operation cancellation during persistence.</param>
    /// <returns>A result containing the persisted assignment confirmation.</returns>
    [HttpPut]
    [HumanApprovalRequired("council.role-model.save", "Assign model to Council roles",
        "Change exact host/model eligibility of saved Council team roles.", "High", "Council team reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> SaveAsync([FromBody] CouncilRoleAssignmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null) return Results.BadRequest("Assignment request required.");
            request.UserConfirmed = true;
            return Results.Ok(await assignments.AssignAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }
        catch (Exception ex) { logger.LogError(ex, "Council role save failed."); return Results.InternalServerError("Council role save failed."); }
    }
}