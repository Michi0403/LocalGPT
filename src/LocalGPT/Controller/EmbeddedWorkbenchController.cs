using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the embedded workbench application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="catalog">Embedded hardware catalog service dependency used by the embedded workbench workflow to provide the corresponding application capability.</param>
/// <param name="wiring">Embedded wiring service dependency used by the embedded workbench workflow to provide the corresponding application capability.</param>
/// <param name="planning">Embedded firmware planning service dependency used by the embedded workbench workflow to provide the corresponding application capability.</param>
/// <param name="telemetryBridge">Embedded telemetry bridge service dependency used by the embedded workbench workflow to provide the corresponding application capability.</param>
/// <param name="telemetryIngress">Embedded telemetry ingress service dependency used by the embedded workbench workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/embedded")]
public sealed class EmbeddedWorkbenchController(
    IEmbeddedHardwareCatalogService catalog,
    IEmbeddedWiringService wiring,
    IEmbeddedFirmwarePlanningService planning,
    IEmbeddedTelemetryBridgeService telemetryBridge,
    IEmbeddedTelemetryIngressService telemetryIngress,
    ILogger<EmbeddedWorkbenchController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves catalog for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("catalog")]
    public async Task<IResult> GetCatalog(CancellationToken cancellationToken) =>
        Results.Ok(await catalog.GetCatalogAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Retrieves boards for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("boards")]
    public async Task<IResult> GetBoards(CancellationToken cancellationToken) =>
        Results.Ok(await catalog.GetBoardProfilesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Retrieves board for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="boardProfileKey">Board profile key value supplied to the embedded workbench operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("boards/{boardProfileKey}")]
    public async Task<IResult> GetBoard(string boardProfileKey, CancellationToken cancellationToken)
    {
        var board = await catalog.GetBoardProfileAsync(boardProfileKey, cancellationToken).ConfigureAwait(false);
        return board is null ? Results.NotFound(new { error = "The embedded board profile was not found." }) : Results.Ok(board);
    }

    /// <summary>
    /// Retrieves protocols for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("protocols")]
    public async Task<IResult> GetProtocols(CancellationToken cancellationToken) =>
        Results.Ok(await catalog.GetProtocolDescriptorsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Retrieves publisher workbench contract for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The i result get publisher workbench contract results ok catalog produced by the operation.</returns>
    [HttpGet("publisher-workbench-contract")]
    public IResult GetPublisherWorkbenchContract() => Results.Ok(catalog.GetPublisherWorkbenchContract());

    /// <summary>
    /// Creates wiring draft for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("wiring/drafts")]
    public async Task<IResult> CreateWiringDraft([FromBody] EmbeddedWiringDraftCreateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => wiring.CreateDraftAsync(request.BoardProfileKey, request.Name, cancellationToken), "wiring draft creation").ConfigureAwait(false);

    /// <summary>
    /// Validates wiring for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("wiring/validate")]
    public async Task<IResult> ValidateWiring([FromBody] EmbeddedWiringValidationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => wiring.ValidateAsync(request, cancellationToken), "wiring validation").ConfigureAwait(false);

    /// <summary>
    /// Creates firmware plan for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("firmware/plan")]
    public async Task<IResult> CreateFirmwarePlan([FromBody] EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => planning.CreatePlanAsync(request, cancellationToken), "firmware planning").ConfigureAwait(false);

    /// <summary>
    /// Creates firmware artifacts for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("firmware/artifacts")]
    [HumanApprovalRequired("embedded.firmware.artifacts.create", "Create embedded firmware artifacts", "Write the reviewed sketch, PlatformIO configuration, wiring contract and plan archive. No compiler or flashing tool is executed.", "High", "Embedded firmware reviewer")]
    public async Task<IResult> CreateFirmwareArtifacts([FromBody] EmbeddedFirmwarePlanRequest request, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => planning.CreateArtifactsAsync(request, userConfirmed, cancellationToken), "firmware artifact creation").ConfigureAwait(false);

    /// <summary>
    /// Previews telemetry for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("telemetry/preview")]
    public async Task<IResult> PreviewTelemetry([FromBody] EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => telemetryBridge.PreviewAsync(request, cancellationToken), "telemetry preview").ConfigureAwait(false);

    /// <summary>
    /// Previews one wire envelope for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("telemetry/onewire-envelope")]
    public async Task<IResult> PreviewOneWireEnvelope([FromBody] EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => telemetryBridge.CreateOneWireEnvelopeAsync(request, cancellationToken), "logical 1-Wire envelope preview").ConfigureAwait(false);

    /// <summary>
    /// Publishes telemetry for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("telemetry/ingress")]
    public async Task<IResult> PublishTelemetry([FromBody] EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => telemetryIngress.PublishAsync(request, cancellationToken), "telemetry ingress").ConfigureAwait(false);

    /// <summary>
    /// Retrieves recent telemetry for the embedded workbench API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="deviceId">Identifier of the device to use for this operation.</param>
    /// <param name="maximum">Maximum value supplied to the embedded workbench operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("telemetry/recent")]
    public IResult GetRecentTelemetry([FromQuery] string? deviceId, [FromQuery] int maximum = 100) =>
        Results.Ok(telemetryIngress.GetRecent(deviceId, maximum));

    /// <summary>
    /// Returns the execute projection for the embedded workbench API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="EmbeddedWorkbenchController"/>.</typeparam>
    /// <param name="action">Action value supplied to the embedded workbench operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the embedded workbench operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    private async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action, string operation)
    {
        try
        {
            return Results.Ok(await action().ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or DirectoryNotFoundException or FileNotFoundException)
        {
            logger.LogWarning(ex, "Embedded workbench {Operation} was rejected.", operation);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (OperationCanceledException exception)
        {
            logger.LogInformation(exception, "Embedded workbench {Operation} was cancelled.", operation);
            return Results.Conflict(new { error = "The operation was cancelled." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Embedded workbench {Operation} failed; source, pin values and telemetry payloads were omitted from logs.", operation);
            return Results.InternalServerError(new { error = "The operation failed. Review LocalGPT application logs." });
        }
    }
}
