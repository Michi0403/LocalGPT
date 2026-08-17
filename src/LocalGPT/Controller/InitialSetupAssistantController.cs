using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes the AI-guided hardware/provider/model initial setup workflow through normal LocalGPT service boundaries.</summary>
/// <param name="setup">Initial setup orchestration service.</param>
/// <param name="recommendations">Optional CanIRun.ai recommendation service.</param>
/// <param name="providers">Knowledge-backed provider bootstrap service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
[ApiController]
[Route("api/initial-setup")]
public sealed class InitialSetupAssistantController(
    IInitialSetupAssistantService setup,
    ICanIRunHardwareRecommendationService recommendations,
    IAiProviderBootstrapService providers,
    ILogger<InitialSetupAssistantController> logger) : ControllerBase
{
    /// <summary>Returns current local hardware, provider profiles, installed models and onboarding state.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("status")]
    public async Task<IResult> Status(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await setup.GetSnapshotAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Initial setup status failed."); return Results.InternalServerError("Initial setup status could not be loaded. Review local logs for details."); }
    }

    /// <summary>Runs local hardware discovery and persists the resulting host profile after user approval.</summary>
    /// <param name="endpoint">Endpoint value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("hardware/detect")]
    [HumanApprovalRequired("initial-setup.hardware.detect", "Detect local hardware", "Run local read-only hardware probes and persist the resulting host profile for setup and benchmarking.", "Low", "Local machine operator")]
    public async Task<IResult> DetectHardware([FromQuery] string endpoint, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await setup.DetectHardwareAsync(endpoint, userConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Initial setup local hardware detection failed; hardware values omitted."); return Results.InternalServerError("Hardware detection failed. Review local logs for details."); }
    }

    /// <summary>Imports a user-provided HWiNFO text report through the setup hardware pipeline.</summary>
    /// <param name="endpoint">Endpoint value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("hardware/hwinfo/import")]
    [HumanApprovalRequired("initial-setup.hardware.hwinfo.import", "Import HWiNFO hardware report", "Parse the locally supplied HWiNFO text report and persist its reviewed hardware facts.", "Low", "Local machine operator")]
    public async Task<IResult> ImportHwInfo([FromQuery] string endpoint, [FromQuery] bool userConfirmed, [FromBody] InitialSetupHwInfoImportRequest request, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await setup.ImportHwInfoAsync(endpoint, request.ReportText, userConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Initial setup HWiNFO import failed; report/hardware values omitted."); return Results.InternalServerError("HWiNFO import failed. Review local logs for details."); }
    }

    /// <summary>Persists the user-reviewed multi-GPU list through configured-host hardware persistence.</summary>
    /// <param name="endpoint">Endpoint value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="devices">Devices value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("hardware")]
    [HumanApprovalRequired("initial-setup.hardware.save", "Save hardware list", "Persist the reviewed local accelerator list used for setup recommendations and Council benchmarking.", "Low", "Local machine operator")]
    public async Task<IResult> SaveHardware([FromQuery] string endpoint, [FromQuery] bool userConfirmed, [FromBody] List<InitialSetupHardwareDevice> devices, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await setup.SaveHardwareListAsync(devices, endpoint, userConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Saving initial setup hardware failed; hardware values omitted."); return Results.InternalServerError("Hardware could not be saved. Review local logs for details."); }
    }

    /// <summary>Performs one explicit CanIRun.ai lookup with source attribution.</summary>
    /// <param name="deviceSlug">Device slug value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("canirun/{deviceSlug}")]
    [HumanApprovalRequired("initial-setup.canirun.lookup", "Look up CanIRun.ai recommendations", "Contact canirun.ai for the selected GPU and parse its public model compatibility cards with source attribution.", "Low", "Local machine operator")]
    public async Task<IResult> CanIRun(string deviceSlug, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await recommendations.GetRecommendationsAsync(deviceSlug, userConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or HttpRequestException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "CanIRun.ai setup lookup failed; response omitted."); return Results.InternalServerError("CanIRun.ai recommendations could not be loaded. Review local logs for details."); }
    }

    /// <summary>Lists knowledge-backed provider bootstrap profiles for this operating system.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("providers")]
    public async Task<IResult> Providers(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await providers.GetProfilesAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider bootstrap profile list failed."); return Results.InternalServerError("Provider profiles could not be loaded. Review local logs for details."); }
    }

    /// <summary>Runs the selected provider profile's read-only detection command.</summary>
    /// <param name="profileKey">Profile key value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("providers/{profileKey}/detect")]
    [HumanApprovalRequired("initial-setup.provider.detect", "Detect AI provider", "Run the exact knowledge-backed provider detection command after user review.", "Low", "Local machine operator")]
    public async Task<IResult> DetectProvider(string profileKey, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await providers.DetectAsync(profileKey, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Provider detection failed for profile {ProfileKey}; command details omitted.", profileKey); return Results.InternalServerError("Provider detection failed. Review local logs for details."); }
    }

    /// <summary>Lists the selected provider's local model store through its knowledge-backed read-only command.</summary>
    /// <param name="profileKey">Profile key value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("providers/{profileKey}/models/list")]
    [HumanApprovalRequired("initial-setup.provider.models.list", "List AI provider models", "Run the exact knowledge-backed read-only provider model-list command after user review.", "Low", "Local machine operator")]
    public async Task<IResult> ListProviderModels(string profileKey, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await providers.ListModelsAsync(profileKey, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Provider model listing failed for profile {ProfileKey}; command/output details omitted.", profileKey); return Results.InternalServerError("Provider model listing failed. Review local logs for details."); }
    }

    /// <summary>Installs the selected provider through its knowledge-backed command.</summary>
    /// <param name="profileKey">Profile key value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("providers/{profileKey}/install")]
    [HumanApprovalRequired("initial-setup.provider.install", "Install AI provider", "Run the exact knowledge-backed provider installation command after user review.", "High", "Local machine operator")]
    public async Task<IResult> InstallProvider(string profileKey, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await providers.InstallAsync(profileKey, userConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Provider installation failed for profile {ProfileKey}; command details omitted.", profileKey); return Results.InternalServerError("Provider installation failed. Review local logs for details."); }
    }

    /// <summary>Starts the selected provider through its knowledge-backed command.</summary>
    /// <param name="profileKey">Profile key value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("providers/{profileKey}/start")]
    [HumanApprovalRequired("initial-setup.provider.start", "Start AI provider", "Run the exact knowledge-backed provider start command after user review.", "High", "Local machine operator")]
    public async Task<IResult> StartProvider(string profileKey, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await providers.StartAsync(profileKey, userConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Provider startup failed for profile {ProfileKey}; command details omitted.", profileKey); return Results.InternalServerError("Provider startup failed. Review local logs for details."); }
    }

    /// <summary>Registers the selected loopback provider endpoint in LocalGPT's existing AI configuration.</summary>
    /// <param name="profileKey">Profile key value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("providers/{profileKey}/configure")]
    [HumanApprovalRequired("initial-setup.provider.configure", "Register AI provider endpoint", "Persist the selected loopback provider endpoint in LocalGPT's existing provider configuration.", "Medium", "Local machine operator")]
    public async Task<IResult> ConfigureProvider(string profileKey, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(new { Endpoint = await providers.ConfigureEndpointAsync(profileKey, userConfirmed, cancellationToken).ConfigureAwait(false) }); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Provider endpoint registration failed for profile {ProfileKey}; endpoint details omitted.", profileKey); return Results.InternalServerError("Provider endpoint registration failed. Review local logs for details."); }
    }

    /// <summary>Installs one selected provider model through the knowledge-backed model command template.</summary>
    /// <param name="profileKey">Profile key value supplied to the initial setup assistant operation and used when producing its result.</param>
    /// <param name="modelId">Identifier of the model to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("providers/{profileKey}/models/{modelId}/install")]
    [HumanApprovalRequired("initial-setup.provider.model.install", "Install AI model", "Download/install the selected provider model through the exact knowledge-backed provider command template.", "High", "Local machine operator")]
    public async Task<IResult> InstallProviderModel(string profileKey, string modelId, [FromQuery] bool userConfirmed, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await providers.InstallModelAsync(profileKey, modelId, userConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Provider model installation failed for profile {ProfileKey}; model/command details omitted.", profileKey); return Results.InternalServerError("Provider model installation failed. Review local logs for details."); }
    }

    /// <summary>
    /// Creates benchmark team for the initial setup assistant API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("benchmark-team")]
    [HumanApprovalRequired("initial-setup.benchmark-team.save", "Create hardware-curated benchmark team", "Create or update the user-owned initial benchmark Council team using the selected installed models and preferred curator pool.", "Medium", "Council team maintainer")]
    public async Task<IResult> CreateBenchmarkTeam([FromBody] CreateInitialBenchmarkTeamRequest request, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await setup.CreateBenchmarkTeamAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Creating initial benchmark team failed; model values omitted."); return Results.InternalServerError("The benchmark team could not be created. Review local logs for details."); }
    }
}

/// <summary>Represents one bounded HWiNFO text import request for the initial setup controller.</summary>
public sealed class InitialSetupHwInfoImportRequest
{
    /// <summary>
    /// Gets or sets the report text value that forms part of the initial setup hw info import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The report text value exposed by <see cref="InitialSetupHwInfoImportRequest"/>.</value>
    public string ReportText { get; set; } = string.Empty;
}
