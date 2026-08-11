using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Provides embedded workbench controller operations.
/// </summary>
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
    /// Gets catalog.
    /// </summary>
    [HttpGet("catalog")]
    public async Task<IResult> GetCatalog(CancellationToken cancellationToken) =>
        Results.Ok(await catalog.GetCatalogAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Gets boards.
    /// </summary>
    [HttpGet("boards")]
    public async Task<IResult> GetBoards(CancellationToken cancellationToken) =>
        Results.Ok(await catalog.GetBoardProfilesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Gets board.
    /// </summary>
    [HttpGet("boards/{boardProfileKey}")]
    public async Task<IResult> GetBoard(string boardProfileKey, CancellationToken cancellationToken)
    {
        var board = await catalog.GetBoardProfileAsync(boardProfileKey, cancellationToken).ConfigureAwait(false);
        return board is null ? Results.NotFound(new { error = "The embedded board profile was not found." }) : Results.Ok(board);
    }

    /// <summary>
    /// Gets protocols.
    /// </summary>
    [HttpGet("protocols")]
    public async Task<IResult> GetProtocols(CancellationToken cancellationToken) =>
        Results.Ok(await catalog.GetProtocolDescriptorsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Gets publisher workbench contract.
    /// </summary>
    [HttpGet("publisher-workbench-contract")]
    public IResult GetPublisherWorkbenchContract() => Results.Ok(catalog.GetPublisherWorkbenchContract());

    /// <summary>
    /// Creates wiring draft.
    /// </summary>
    [HttpPost("wiring/drafts")]
    public async Task<IResult> CreateWiringDraft([FromBody] EmbeddedWiringDraftCreateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => wiring.CreateDraftAsync(request.BoardProfileKey, request.Name, cancellationToken), "wiring draft creation").ConfigureAwait(false);

    /// <summary>
    /// Validates wiring.
    /// </summary>
    [HttpPost("wiring/validate")]
    public async Task<IResult> ValidateWiring([FromBody] EmbeddedWiringValidationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => wiring.ValidateAsync(request, cancellationToken), "wiring validation").ConfigureAwait(false);

    /// <summary>
    /// Creates firmware plan.
    /// </summary>
    [HttpPost("firmware/plan")]
    public async Task<IResult> CreateFirmwarePlan([FromBody] EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => planning.CreatePlanAsync(request, cancellationToken), "firmware planning").ConfigureAwait(false);

    /// <summary>
    /// Creates firmware artifacts.
    /// </summary>
    [HttpPost("firmware/artifacts")]
    [HumanApprovalRequired("embedded.firmware.artifacts.create", "Create embedded firmware artifacts", "Write the reviewed sketch, PlatformIO configuration, wiring contract and plan archive. No compiler or flashing tool is executed.", "High", "Embedded firmware reviewer")]
    public async Task<IResult> CreateFirmwareArtifacts([FromBody] EmbeddedFirmwarePlanRequest request, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => planning.CreateArtifactsAsync(request, userConfirmed, cancellationToken), "firmware artifact creation").ConfigureAwait(false);

    /// <summary>
    /// Runs the preview telemetry operation.
    /// </summary>
    [HttpPost("telemetry/preview")]
    public async Task<IResult> PreviewTelemetry([FromBody] EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => telemetryBridge.PreviewAsync(request, cancellationToken), "telemetry preview").ConfigureAwait(false);

    /// <summary>
    /// Runs the preview one wire envelope operation.
    /// </summary>
    [HttpPost("telemetry/onewire-envelope")]
    public async Task<IResult> PreviewOneWireEnvelope([FromBody] EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => telemetryBridge.CreateOneWireEnvelopeAsync(request, cancellationToken), "logical 1-Wire envelope preview").ConfigureAwait(false);

    /// <summary>
    /// Publishes telemetry.
    /// </summary>
    [HttpPost("telemetry/ingress")]
    public async Task<IResult> PublishTelemetry([FromBody] EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => telemetryIngress.PublishAsync(request, cancellationToken), "telemetry ingress").ConfigureAwait(false);

    /// <summary>
    /// Gets recent telemetry.
    /// </summary>
    [HttpGet("telemetry/recent")]
    public IResult GetRecentTelemetry([FromQuery] string? deviceId, [FromQuery] int maximum = 100) =>
        Results.Ok(telemetryIngress.GetRecent(deviceId, maximum));

    /// <summary>
    /// Runs the execute async operation.
    /// </summary>
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
