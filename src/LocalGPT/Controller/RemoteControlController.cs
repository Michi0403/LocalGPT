using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LocalGPT.Controller;

/// <summary>Provides the HTTP control plane for user-owned Remote Control connectors, pipelines, execution history, and token-authenticated webhooks.</summary>
/// <param name="connectors">Connector persistence and runtime service.</param>
/// <param name="pipelines">Action-pipeline persistence and execution service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
[ApiController]
[Route("api/remote-control")]
public sealed class RemoteControlController(
    IRemoteControlConnectorService connectors,
    IRemoteControlPipelineService pipelines,
    ILogger<RemoteControlController> logger) : ControllerBase
{
    /// <summary>Lists user-created connector definitions.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("connectors")]
    public async Task<IResult> ListConnectors(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await connectors.ListAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Remote Control connector list failed."); return Results.InternalServerError("Remote Control connectors could not be loaded. Review local logs for details."); }
    }

    /// <summary>
    /// Retrieves connector for the remote control API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("connectors/{key}")]
    public async Task<IResult> GetConnector(string key, CancellationToken cancellationToken)
    {
        try
        {
            var connector = await connectors.GetAsync(key, cancellationToken).ConfigureAwait(false);
            return connector is null ? Results.NotFound() : Results.Ok(connector);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Remote Control connector read failed for {ConnectorKey}.", key); return Results.InternalServerError("The Remote Control connector could not be loaded. Review local logs for details."); }
    }

    /// <summary>Saves one connector. External network access remains disabled unless the persisted connector explicitly enables it.</summary>
    /// <param name="definition">Definition value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("connectors")]
    [HumanApprovalRequired("remote-control.connector.save", "Save Remote Control connector", "Persist or change a connector that can later contact an explicitly allowlisted endpoint or accept a token-authenticated webhook.", "Medium", "Integration maintainer")]
    public async Task<IResult> SaveConnector([FromBody] RemoteControlConnectorDefinition definition, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await connectors.SaveAsync(definition, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Remote Control connector save failed; secret-bearing fields were omitted."); return Results.InternalServerError("The Remote Control connector could not be saved. Review local logs for details."); }
    }

    /// <summary>
    /// Deletes connector for the remote control API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("connectors/{key}")]
    [HumanApprovalRequired("remote-control.connector.delete", "Delete Remote Control connector", "Delete one user-owned connector and its dependent action pipelines.", "Medium", "Integration maintainer")]
    public async Task<IResult> DeleteConnector(string key, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return await connectors.DeleteAsync(key, cancellationToken).ConfigureAwait(false) ? Results.NoContent() : Results.NotFound(); }
        catch (OperationCanceledException) { throw; }
        catch (InvalidOperationException exception) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Remote Control connector delete failed for {ConnectorKey}.", key); return Results.InternalServerError("The Remote Control connector could not be deleted. Review local logs for details."); }
    }

    /// <summary>Rotates a webhook connector's secret token.</summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("connectors/{key}/rotate-webhook-token")]
    [HumanApprovalRequired("remote-control.connector.rotate-webhook-token", "Rotate Remote Control webhook token", "Invalidate the previous webhook token and issue a new random token for this connector.", "Medium", "Integration maintainer")]
    public async Task<IResult> RotateWebhookToken(string key, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(new { Token = await connectors.RotateWebhookTokenAsync(key, cancellationToken).ConfigureAwait(false) }); }
        catch (OperationCanceledException) { throw; }
        catch (KeyNotFoundException exception) { return Results.NotFound(new { Error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Remote Control webhook token rotation failed for {ConnectorKey}; token omitted.", key); return Results.InternalServerError("The webhook token could not be rotated. Review local logs for details."); }
    }

    /// <summary>Performs one explicit pull. Matching pipelines still enforce their target DXFunctions' own approval and automatic-invocation policy.</summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="runPipelines">Value indicating whether run pipelines should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("connectors/{key}/pull")]
    public async Task<IResult> PullConnector(string key, [FromQuery] bool runPipelines, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await connectors.PullAsync(key, runPipelines, automaticInvocation: false, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidDataException or InvalidOperationException or HttpRequestException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Remote Control pull failed for {ConnectorKey}; payload and endpoint omitted.", key); return Results.InternalServerError("The Remote Control pull failed. Review local logs for details."); }
    }

    /// <summary>Accepts one token-authenticated webhook payload. The token must be supplied in the X-LocalGPT-Webhook-Token request header.</summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("webhook/{key}")]
    [RequestSizeLimit(RemoteControlLimits.AbsoluteMaximumPayloadBytes)]
    public async Task<IResult> ReceiveWebhook(string key, CancellationToken cancellationToken)
    {
        try
        {
            var token = Request.Headers["X-LocalGPT-Webhook-Token"].ToString();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var payload = await connectors.ReceiveWebhookAsync(key, token, content, Request.ContentType ?? string.Empty, cancellationToken).ConfigureAwait(false);
            return Results.Accepted(value: new { payload.ConnectorKey, payload.Trigger, payload.PayloadBytes, Status = "Accepted" });
        }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
        catch (KeyNotFoundException exception) { return Results.NotFound(new { Error = exception.Message }); }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Remote Control webhook failed for {ConnectorKey}; payload and token omitted.", key); return Results.InternalServerError("The webhook could not be accepted. Review local logs for details."); }
    }

    /// <summary>Lists user-authored action pipelines.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("pipelines")]
    public async Task<IResult> ListPipelines(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await pipelines.ListAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Remote Control pipeline list failed."); return Results.InternalServerError("Remote Control pipelines could not be loaded. Review local logs for details."); }
    }

    /// <summary>Saves one pipeline of existing DXFunction or public-service catalog actions.</summary>
    /// <param name="definition">Definition value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("pipelines")]
    [HumanApprovalRequired("remote-control.pipeline.save", "Save Remote Control action pipeline", "Persist an action pipeline that can be triggered manually or by an enabled pull/webhook connector. Each target keeps its own DXFunction approval policy.", "Medium", "Integration maintainer")]
    public async Task<IResult> SavePipeline([FromBody] RemoteControlPipelineDefinition definition, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await pipelines.SaveAsync(definition, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Remote Control pipeline save failed; action templates omitted."); return Results.InternalServerError("The Remote Control pipeline could not be saved. Review local logs for details."); }
    }

    /// <summary>
    /// Deletes pipeline for the remote control API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("pipelines/{key}")]
    [HumanApprovalRequired("remote-control.pipeline.delete", "Delete Remote Control action pipeline", "Delete one user-authored action pipeline.", "Medium", "Integration maintainer")]
    public async Task<IResult> DeletePipeline(string key, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return await pipelines.DeleteAsync(key, cancellationToken).ConfigureAwait(false) ? Results.NoContent() : Results.NotFound(); }
        catch (OperationCanceledException) { throw; }
        catch (InvalidOperationException exception) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Remote Control pipeline delete failed for {PipelineKey}.", key); return Results.InternalServerError("The Remote Control pipeline could not be deleted. Review local logs for details."); }
    }

    /// <summary>Executes a pipeline against a caller-supplied payload. Nested write actions are not implicitly confirmed.</summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("pipelines/{key}/execute")]
    public async Task<IResult> ExecutePipeline(string key, [FromBody] RemoteControlManualExecutionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new RemoteControlPayload
            {
                ConnectorKey = request.ConnectorKey,
                Trigger = RemoteControlTriggerKind.Manual,
                ContentType = request.ContentType,
                RawText = request.Payload,
                PayloadBytes = Encoding.UTF8.GetByteCount(request.Payload ?? string.Empty)
            };
            return Results.Ok(await pipelines.ExecuteAsync(key, payload, automaticInvocation: false, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidDataException or InvalidOperationException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Remote Control pipeline execution failed for {PipelineKey}; payload omitted.", key); return Results.InternalServerError("The Remote Control pipeline failed. Review local logs for details."); }
    }

    /// <summary>Returns recent bounded pull, webhook, and pipeline audit rows.</summary>
    /// <param name="take">Take value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("history")]
    public async Task<IResult> History([FromQuery] int take, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await connectors.GetHistoryAsync(take <= 0 ? 100 : take, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Remote Control history read failed."); return Results.InternalServerError("Remote Control history could not be loaded. Review local logs for details."); }
    }

    /// <summary>Lists enabled DXFunction Catalog entries that can be selected as pipeline targets.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("targets")]
    public async Task<IResult> Targets(CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await pipelines.ListTargetsAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Remote Control target catalog read failed."); return Results.InternalServerError("DXFunction targets could not be loaded. Review local logs for details."); }
    }
}

/// <summary>Contains the manual payload supplied to a Remote Control action-pipeline execution.</summary>
public sealed class RemoteControlManualExecutionRequest
{
    /// <summary>Gets or sets the optional connector identity associated with the manual payload.</summary>
    /// <value>The connector key value exposed by <see cref="RemoteControlManualExecutionRequest"/>.</value>
    public string ConnectorKey { get; set; } = "manual";
    /// <summary>
    /// Gets or sets the content type value that forms part of the remote control manual execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content type value exposed by <see cref="RemoteControlManualExecutionRequest"/>.</value>
    public string ContentType { get; set; } = "application/json";
    /// <summary>
    /// Gets or sets the payload value that forms part of the remote control manual execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The payload value exposed by <see cref="RemoteControlManualExecutionRequest"/>.</value>
    public string Payload { get; set; } = "{}";
}
