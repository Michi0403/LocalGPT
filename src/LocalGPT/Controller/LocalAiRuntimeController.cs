using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes bounded LocalGPT-managed Python runtime policy and readiness operations without exposing arbitrary Python execution.</summary>
/// <param name="runtime">Local AI runtime service.</param>
/// <param name="logger">Logger used for controller diagnostics.</param>
/// <param name="pythonJobs">Python job orchestration service dependency used by the local AI runtime workflow to provide the corresponding application capability.</param>
/// <param name="modelCatalog">The optional Hugging Face metadata catalog.</param>
[ApiController]
[Route("api/local-ai/runtime")]
public sealed class LocalAiRuntimeController(
    ILocalAiRuntimeService runtime,
    IPythonJobOrchestrationService pythonJobs,
    IHuggingFaceModelCatalogService modelCatalog,
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

    /// <summary>Returns the bounded, payload-free queue and worker history.</summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("jobs")]
    public IResult Jobs()
    {
        try { return Results.Ok(pythonJobs.ListJobs(100)); }
        catch (Exception exception) { logger.LogError(exception, "Python task listing failed."); return Results.InternalServerError("Python task listing unavailable."); }
    }

    /// <summary>Requests cancellation of one specific Python task; does not run any source code.</summary>
    /// <param name="jobId">Identifier of the job to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("jobs/{jobId:guid}/cancel")]
    [HumanApprovalRequired("localai.python.jobs.cancel", "Cancel Python task", "Stop one already-running local Python or media job.", "Medium", "Local AI task reviewer")]
    public IResult CancelJob(Guid jobId)
    {
        try { return pythonJobs.CancelJob(jobId) ? Results.Ok(new { CancelRequested = true, JobId = jobId }) : Results.NotFound(); }
        catch (Exception exception) { logger.LogError(exception, "Cancelling Python task failed."); return Results.InternalServerError("Python task cancellation failed."); }
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

    /// <summary>Lists platform-supported Python package profiles.</summary>
    [HttpGet("package-profiles")]
    public IResult PackageProfiles() => Results.Ok(runtime.GetPackageProfiles());

    /// <summary>Lists verified managed installations without returning machine-local paths.</summary>
    [HttpGet("models")]
    public IResult Models() => Results.Ok(runtime.GetInstalledModels().Select(model => new LocalAiInstalledModelInfo(model)).ToArray());

    /// <summary>Returns the public metadata for one managed installation.</summary>
    [HttpGet("models/{installationId}")]
    public IResult Model(string installationId)
    {
        var model = runtime.GetInstalledModels().FirstOrDefault(item => item.InstallationId == installationId);
        return model is null ? Results.NotFound() : Results.Ok(new LocalAiInstalledModelInfo(model));
    }

    /// <summary>Searches the optional Hugging Face catalog for model metadata.</summary>
    [HttpPost("model-catalog/search")]
    public async Task<IResult> SearchModelsAsync([FromBody] HuggingFaceModelSearchRequest request, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await modelCatalog.SearchAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning("Hugging Face metadata search failed with {ExceptionType}.", exception.GetType().Name);
            return Results.Problem("Check Hugging Face availability and the selected filters in /install → Local AI runtimes.", statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Downloads and verifies one approved model revision before registering it.</summary>
    [HttpPost("models")]
    [HumanApprovalRequired("localai.model.install", "Install specialized AI model", "Download and validate the selected model revision in LocalGPT's managed Python environment.", "High", "Local AI installation reviewer")]
    public async Task<IResult> InstallModelAsync([FromBody] LocalAiModelInstallRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var installed = await runtime.InstallModelAsync(request, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/local-ai/runtime/models/{Uri.EscapeDataString(installed.InstallationId)}", new LocalAiInstalledModelInfo(installed));
        }
        catch (OperationCanceledException) { throw; }
        catch (LocalAiModelVerificationException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
        }
        catch (Exception exception)
        {
            logger.LogWarning("Managed model installation failed with {ExceptionType}.", exception.GetType().Name);
            return Results.Problem("Check the managed Python environment, package profiles, and model compatibility in /install → Local AI runtimes.", statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }

    /// <summary>Removes one approved managed model installation.</summary>
    [HttpDelete("models/{installationId}")]
    [HumanApprovalRequired("localai.model.remove", "Remove specialized AI model", "Remove the selected model snapshot and its managed registration.", "High", "Local AI installation reviewer")]
    public async Task<IResult> RemoveModelAsync(string installationId, CancellationToken cancellationToken)
    {
        if (!runtime.GetInstalledModels().Any(item => item.InstallationId == installationId))
            return Results.NotFound();
        try
        {
            await runtime.RemoveModelAsync(installationId, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning("Managed model removal failed with {ExceptionType}.", exception.GetType().Name);
            return Results.Problem("The model could not be removed. Review the Local AI runtime status and setup log.");
        }
    }
}
