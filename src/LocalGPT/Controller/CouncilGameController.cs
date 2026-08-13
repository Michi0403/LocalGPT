using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the council game application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="games">Council game session service dependency used by the council game workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/council/games")]
public sealed class CouncilGameController(
    ICouncilGameSessionService games,
    ILogger<CouncilGameController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the start projection for the council game API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the list projection for the council game API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="includeCompleted">Value indicating whether include completed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the get projection for the council game API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Previews control for the council game API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("control/preview")]
    public async Task<ActionResult<CouncilGameDirectorDecision>> PreviewControl(
        [FromBody] CouncilGameControlRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await games.PreviewControlAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Council game control preview was rejected for session {GameSessionId}.", request.SessionId);
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Council game control preview was cancelled for session {GameSessionId}.", request.SessionId);
            return Conflict(new { error = "The Council game control preview was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council game control preview failed for session {GameSessionId}; control content was omitted.", request.SessionId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Council game control preview failed");
        }
    }

    /// <summary>
    /// Returns the control projection for the council game API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the frame projection for the council game API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the control mode projection for the council game API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the input gate projection for the council game API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
