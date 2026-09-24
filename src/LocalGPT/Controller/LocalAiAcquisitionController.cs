using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes LocalGPT's reviewed direct/GitHub-first local-AI catalog independently from optional model-hub integrations.</summary>
/// <param name="acquisition">Direct local-AI acquisition service.</param>
/// <param name="logger">Logger used for controller diagnostics.</param>
[ApiController]
[Route("api/local-ai/acquisition")]
public sealed class LocalAiAcquisitionController(
    ILocalAiAcquisitionService acquisition,
    ILogger<LocalAiAcquisitionController> logger) : ControllerBase
{
    /// <summary>Lists reviewed direct local-AI sources.</summary>
    /// <returns>The direct local-AI catalog.</returns>
    [HttpGet("catalog")]
    public IResult Catalog()
    {
        try { return Results.Ok(acquisition.GetKnownModels()); }
        catch (Exception exception) { logger.LogError(exception, "Direct local-AI acquisition catalog API failed."); return Results.InternalServerError("Local-AI source catalog could not be loaded. Review LocalGPT logs."); }
    }

    /// <summary>Downloads one reviewed upstream project source archive without Git, GitHub CLI, or a model-hub client.</summary>
    /// <param name="request">Source key and confirmation state.</param>
    /// <param name="cancellationToken">Cancellation token that stops the transfer.</param>
    /// <returns>The managed source path and SHA-256 digest.</returns>
    [HttpPost("source/download")]
    [HumanApprovalRequired("localai.source.download", "Download local-AI source", "Download one reviewed upstream AI project source archive directly over HTTPS.", "High", "Local AI source reviewer")]
    public async Task<IResult> DownloadSourceAsync([FromBody] LocalAiKnownModelInstallRequest request, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await acquisition.DownloadSourceAsync(request.SourceKey, userConfirmed: true, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Direct local-AI source download API failed; source identity, URL and path were omitted."); return Results.InternalServerError("Local-AI source download failed. Review LocalGPT logs."); }
    }

    /// <summary>Downloads and installs one executable reviewed local-AI model integration.</summary>
    /// <param name="request">Reviewed source key, variant, and confirmation state.</param>
    /// <param name="cancellationToken">Cancellation token that stops download or installation.</param>
    /// <returns>The registered model installation.</returns>
    [HttpPost("install")]
    [HumanApprovalRequired("localai.known.install", "Install reviewed local-AI model", "Download reviewed source and model weights, then install the bounded Python adapter into LocalGPT's managed environment.", "High", "Local AI installation reviewer")]
    public async Task<IResult> InstallAsync([FromBody] LocalAiKnownModelInstallRequest request, CancellationToken cancellationToken)
    {
        try { request.UserConfirmed = true; return Results.Ok(await acquisition.InstallKnownModelAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Direct local-AI model installation API failed; source, model identity and paths were omitted."); return Results.InternalServerError("Local-AI model installation failed. Review LocalGPT logs."); }
    }
}
