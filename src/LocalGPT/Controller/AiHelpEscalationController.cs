using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes bounded AI help decisions through an explicit, non-executing controller surface.</summary>
/// <param name="help">Owned durable recovery service.</param>
/// <param name="logger">Redacted controller diagnostics.</param>
[ApiController]
[Route("api/ai/help")]
public sealed class AiHelpEscalationController(IAiHelpEscalatorService help, ILogger<AiHelpEscalationController> logger) : ControllerBase
{
    /// <summary>Report a blocked task without granting permission to execute a tool.</summary>
    /// <param name="report">Attempt evidence.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>One deterministic next-step decision.</returns>
    [HttpPost("attempt")]
    public async Task<ActionResult<AiHelpRecoveryDecision>> ReportAsync([FromBody] AiHelpAttemptReport report, CancellationToken cancellationToken)
    {
        try { return Ok(await help.ReportAttemptAsync(report, cancellationToken).ConfigureAwait(false)); }
        catch (ArgumentException error) { return BadRequest(error.Message); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error)
        {
            logger.LogError("AI help report failed with {ErrorType}; task/evidence omitted.", error.GetType().Name);
            return Problem("AI help state unavailable.");
        }
    }

    /// <summary>Read durable decision without performing a new attempt.</summary>
    /// <param name="taskId">Stable task key.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Persisted decision or not found.</returns>
    [HttpGet("status")]
    public async Task<ActionResult<AiHelpRecoveryDecision>> InspectAsync([FromQuery] string taskId, CancellationToken cancellationToken)
    {
        try
        {
            var decision = await help.InspectAsync(taskId, cancellationToken).ConfigureAwait(false);
            return decision is null ? NotFound() : Ok(decision);
        }
        catch (ArgumentException error) { return BadRequest(error.Message); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error)
        {
            logger.LogError("AI help inspection failed with {ErrorType}.", error.GetType().Name);
            return Problem("AI help state unavailable.");
        }
    }

    /// <summary>Check only a previously issued human question; never resume an action automatically.</summary>
    /// <param name="taskId">Stable task key.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Waiting status or new human-provided guidance.</returns>
    [HttpPost("human-response")]
    public async Task<ActionResult<AiHelpRecoveryDecision>> CheckHumanAsync([FromQuery] string taskId, CancellationToken cancellationToken)
    {
        try
        {
            var decision = await help.CheckHumanResponseAsync(taskId, cancellationToken).ConfigureAwait(false);
            return decision is null ? NotFound() : Ok(decision);
        }
        catch (ArgumentException error) { return BadRequest(error.Message); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error)
        {
            logger.LogError("AI help human response check failed with {ErrorType}.", error.GetType().Name);
            return Problem("AI help human response unavailable.");
        }
    }
}
