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
        try
        {
            return Ok(await games.StartAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Council game start was rejected.");
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Council game start was cancelled.");
            return Conflict(new { error = "The Council game start was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council game start failed; request and frame content were omitted.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Council game start failed");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CouncilGameSessionSnapshot>>> List(
        [FromQuery] bool includeCompleted,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await games.ListAsync(includeCompleted, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Council game list was cancelled.");
            return Conflict(new { error = "The Council game list was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council game list failed; frame content was omitted.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Council game list failed");
        }
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Get(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await games.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
            return snapshot is null ? NotFound() : Ok(snapshot);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Council game read was cancelled for session {GameSessionId}.", sessionId);
            return Conflict(new { error = "The Council game read was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council game read failed for session {GameSessionId}; frame content was omitted.", sessionId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Council game read failed");
        }
    }

    [HttpPost("control")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Control(
        [FromBody] CouncilGameControlRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await games.ApplyControlAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Council game control was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Council game control was cancelled for session {GameSessionId}.", request.SessionId);
            return Conflict(new { error = "The Council game control was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council game control failed for session {GameSessionId}; control and frame content were omitted.", request.SessionId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Council game control failed");
        }
    }

    [HttpPost("frame")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Frame(
        [FromBody] SubmitCouncilGameFrameRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await games.SubmitFrameAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Council game frame was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Council game frame submission was cancelled for session {GameSessionId}.", request.SessionId);
            return Conflict(new { error = "The Council game frame submission was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council game frame submission failed for session {GameSessionId}; frame content was omitted.", request.SessionId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Council game frame submission failed");
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
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Council game control mode was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Council game control-mode change was cancelled for session {GameSessionId}.", request.SessionId);
            return Conflict(new { error = "The Council game control-mode change was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council game control-mode change failed for session {GameSessionId}.", request.SessionId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Council game control-mode change failed");
        }
    }

    [HttpPost("input-gate")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> InputGate(
        [FromBody] SetCouncilGameInputGateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await games.SetInputGateAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Council game input gate was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Council game input-gate change was cancelled for session {GameSessionId}.", request.SessionId);
            return Conflict(new { error = "The Council game input-gate change was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council game input-gate change failed for session {GameSessionId}.", request.SessionId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Council game input-gate change failed");
        }
    }
}
