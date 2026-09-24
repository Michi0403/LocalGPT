using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes bounded LocalGPT-managed Python runtime policy and readiness operations without exposing arbitrary Python execution.</summary>
/// <param name="runtime">Local AI runtime service.</param>
/// <param name="logger">Logger used for controller diagnostics.</param>
[ApiController]
[Route("api/local-ai/runtime")]
public sealed class LocalAiRuntimeController(
    ILocalAiRuntimeService runtime,
    ILogger<LocalAiRuntimeController> logger) : ControllerBase
{
    /// <summary>Returns the current runtime policy and restart state.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The managed Python/Whisper runtime policy.</returns>
    [HttpGet("configuration")]
    public async Task<IResult> ConfigurationAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await runtime.GetRuntimeConfigurationAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Local AI runtime configuration API failed."); return Results.InternalServerError("Local AI runtime configuration could not be loaded. Review LocalGPT logs."); }
    }

    /// <summary>Updates the persistent managed Python/Whisper runtime policy after human approval.</summary>
    /// <param name="request">Complete replacement runtime policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective saved policy.</returns>
    [HttpPost("configuration")]
    [HumanApprovalRequired("localai.runtime.configuration.update", "Update local-AI runtime policy", "Change LocalGPT's managed Python/Whisper device, queue, media-bound and caching policy.", "High", "Local AI runtime reviewer")]
    public async Task<IResult> UpdateConfigurationAsync([FromBody] LocalAiRuntimeConfigurationChangeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            request.UserConfirmed = true;
            return Results.Ok(await runtime.UpdateRuntimeConfigurationAsync(request, userConfirmed: true, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Local AI runtime configuration update API failed; prompt text and runtime values were omitted from logs.");
            return Results.InternalServerError("Local AI runtime configuration could not be updated. Review LocalGPT logs.");
        }
    }

    /// <summary>Returns bounded Python runtime readiness and queue state.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current runtime status.</returns>
    [HttpGet("status")]
    public async Task<IResult> StatusAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await runtime.GetStatusAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Local AI runtime status API failed."); return Results.InternalServerError("Local AI runtime status could not be loaded. Review LocalGPT logs."); }
    }

    /// <summary>Runs the fixed Python.NET readiness probe without accepting arbitrary Python code.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fixed probe result.</returns>
    [HttpPost("probe")]
    public async Task<IResult> ProbeAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await runtime.ProbeAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Local AI runtime probe API failed; runtime paths were omitted from logs."); return Results.InternalServerError("Local AI runtime probe failed. Review LocalGPT logs."); }
    }
}
