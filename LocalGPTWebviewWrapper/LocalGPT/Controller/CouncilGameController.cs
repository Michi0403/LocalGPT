using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/council/games")]
public sealed class CouncilGameController(
    ICouncilGameSessionService games,
    ILogger<CouncilGameController> logger) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Start(
        [FromBody] StartCouncilGameRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await games.StartAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Council game start was rejected.");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CouncilGameSessionSnapshot>>> List(
        [FromQuery] bool includeCompleted,
        CancellationToken cancellationToken) =>
        Ok(await games.ListAsync(includeCompleted, cancellationToken).ConfigureAwait(false));

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Get(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var snapshot = await games.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    [HttpPost("control")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Control(
        [FromBody] CouncilGameControlRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await games.ApplyControlAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Council game control was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("frame")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Frame(
        [FromBody] SubmitCouncilGameFrameRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await games.SubmitFrameAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Council game frame was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = ex.Message });
        }
    }
    [HttpPost("control-mode")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> ControlMode(
        [FromBody] SetCouncilGameControlModeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await games.SetControlModeAsync(
                request.SessionId,
                request.ControlMode,
                request.AutoplayEnabled,
                request.AutoplayDelayMilliseconds,
                cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Council game control mode was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("input-gate")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> InputGate(
        [FromBody] SetCouncilGameInputGateRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await games.SetInputGateAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Council game input gate was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = ex.Message });
        }
    }

}
