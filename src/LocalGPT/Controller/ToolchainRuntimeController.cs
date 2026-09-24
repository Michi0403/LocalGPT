using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes toolchain environment inspection and reviewed direct-download acquisition through service-owned policy.</summary>
/// <param name="environment">Toolchain environment service.</param>
/// <param name="profiles">Database-backed reusable toolchain execution-profile service.</param>
/// <param name="acquisition">Reviewed toolchain acquisition service.</param>
/// <param name="logger">Logger used for controller diagnostics.</param>
[ApiController]
[Route("api/toolchains")]
public sealed class ToolchainRuntimeController(
    IToolchainEnvironmentService environment,
    IToolchainExecutionProfileService profiles,
    IToolchainAcquisitionService acquisition,
    ILogger<ToolchainRuntimeController> logger) : ControllerBase
{
    /// <summary>Returns the redacted effective/process/application and supported operating-system environment scopes.</summary>
    /// <param name="cancellationToken">Cancellation token that stops the request.</param>
    /// <returns>The current toolchain environment snapshot.</returns>
    [HttpGet("environment")]
    public async Task<IResult> EnvironmentAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await environment.GetSnapshotAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Toolchain environment API failed; names and values were omitted from logs."); return Results.InternalServerError("Toolchain environment could not be loaded. Review LocalGPT logs."); }
    }

    /// <summary>Changes one explicitly approved process/application/user/machine environment value.</summary>
    /// <param name="request">Environment mutation request.</param>
    /// <param name="cancellationToken">Cancellation token that stops the request.</param>
    /// <returns>The refreshed toolchain environment snapshot.</returns>
    [HttpPost("environment")]
    [HumanApprovalRequired("toolchain.environment.change", "Change environment variable", "Change one LocalGPT process/application or supported operating-system environment variable.", "High", "Environment configuration reviewer")]
    public async Task<IResult> ChangeEnvironmentAsync([FromBody] ToolchainEnvironmentChangeRequest request, CancellationToken cancellationToken)
    {
        try { request.UserConfirmed = true; return Results.Ok(await environment.ChangeAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Toolchain environment mutation API failed; name and value were omitted from logs."); return Results.InternalServerError("The environment-variable change failed. Review LocalGPT logs."); }
    }

    /// <summary>Lists reviewed direct toolchain downloads without performing a network lookup.</summary>
    /// <param name="cancellationToken">Cancellation token that stops the request.</param>
    /// <returns>The reviewed acquisition catalog.</returns>
    [HttpGet("acquisition/catalog")]
    public async Task<IResult> AcquisitionCatalogAsync(CancellationToken cancellationToken)
    {
        try { return Results.Ok(await acquisition.GetCatalogAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Toolchain acquisition catalog API failed."); return Results.InternalServerError("Toolchain acquisition catalog could not be loaded. Review LocalGPT logs."); }
    }

    /// <summary>Downloads one reviewed direct toolchain artifact and never executes it automatically.</summary>
    /// <param name="request">Reviewed acquisition key and confirmation state.</param>
    /// <param name="cancellationToken">Cancellation token that stops the transfer.</param>
    /// <returns>The managed download path and SHA-256 digest.</returns>
    [HttpPost("acquisition/download")]
    [HumanApprovalRequired("toolchain.acquisition.download", "Download reviewed toolchain", "Download one pre-seeded manufacturer or GitHub toolchain artifact over HTTPS without executing it.", "High", "Toolchain acquisition reviewer")]
    public async Task<IResult> DownloadToolchainAsync([FromBody] ToolchainAcquisitionDownloadRequest request, CancellationToken cancellationToken)
    {
        try { request.UserConfirmed = true; return Results.Ok(await acquisition.DownloadAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Toolchain acquisition download API failed; URL and path were omitted from logs."); return Results.InternalServerError("Toolchain download failed. Review LocalGPT logs."); }
    }
    /// <summary>Lists database-backed reusable toolchain process profiles.</summary>
    [HttpGet("profiles")]
    public async Task<IResult> ProfilesAsync([FromQuery] string? capability, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await profiles.GetProfilesAsync(capability, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Toolchain execution profile API failed."); return Results.InternalServerError("Toolchain profiles could not be loaded. Review LocalGPT logs."); }
    }

    /// <summary>Saves one reusable toolchain process profile after normal human approval.</summary>
    [HttpPost("profiles")]
    [HumanApprovalRequired("toolchain.profile.save", "Save toolchain process profile", "Save a reusable runtime/compiler process definition and capability binding in the LocalGPT database.", "High", "Toolchain profile reviewer")]
    public async Task<IResult> SaveProfileAsync([FromBody] SaveToolchainExecutionProfileRequest request, CancellationToken cancellationToken)
    {
        try { request.UserConfirmed = true; return Results.Ok(await profiles.SaveProfileAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Saving a toolchain execution profile failed; command details were omitted from logs."); return Results.InternalServerError("The toolchain profile could not be saved. Review LocalGPT logs."); }
    }

    /// <summary>Runs one persisted process profile without a command shell after normal human approval.</summary>
    [HttpPost("profiles/execute")]
    [HumanApprovalRequired("toolchain.profile.execute", "Run toolchain process profile", "Run one persisted toolchain process profile with bounded output and no command shell.", "High", "Toolchain execution reviewer")]
    public async Task<IResult> ExecuteProfileAsync([FromBody] ToolchainProcessExecutionRequest request, CancellationToken cancellationToken)
    {
        try { request.UserConfirmed = true; return Results.Ok(await profiles.ExecuteAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception exception) { logger.LogError(exception, "Running a toolchain execution profile failed; command details and output were omitted from logs."); return Results.InternalServerError("The toolchain process failed. Review LocalGPT logs."); }
    }

}
