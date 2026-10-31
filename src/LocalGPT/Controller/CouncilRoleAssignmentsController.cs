using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;
namespace LocalGPT.Controller;

[ApiController]
[Route("api/council/role-assignments")]
public sealed class CouncilRoleAssignmentsController(ICouncilRoleAssignmentService assignments,
    ILogger<CouncilRoleAssignmentsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> ListAsync([FromQuery] string modelKey, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await assignments.ListForModelAsync(modelKey, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError(ex, "Council role lookup failed."); return Results.InternalServerError("Council roles unavailable."); }
    }

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
