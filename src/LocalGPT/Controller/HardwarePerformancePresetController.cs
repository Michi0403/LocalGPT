using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes durable hardware-spooler performance profiles without placing persistence or benchmark synthesis in the UI.
/// </summary>
/// <param name="presets">Service that owns performance-profile persistence and normalization.</param>
/// <param name="logger">Logger used for bounded controller diagnostics.</param>
[ApiController]
[Route("api/hardware-performance-presets")]
public sealed class HardwarePerformancePresetController(
    IHardwarePerformancePresetService presets,
    ILogger<HardwarePerformancePresetController> logger) : ControllerBase
{
    /// <summary>Lists selectable hardware performance profiles.</summary>
    /// <param name="includeArchived">Value indicating whether include archived should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<IResult> GetPresets([FromQuery] bool includeArchived, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await presets.GetPresetsAsync(includeArchived, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Hardware performance preset list failed.");
            return Results.InternalServerError("Hardware performance presets could not be loaded. Review local logs for details.");
        }
    }

    /// <summary>
    /// Retrieves preset for the hardware performance preset API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IResult> GetPreset(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var preset = await presets.GetPresetAsync(id, cancellationToken).ConfigureAwait(false);
            return preset is null ? Results.NotFound() : Results.Ok(preset);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Hardware performance preset read failed for {PresetId}.", id);
            return Results.InternalServerError("The hardware performance preset could not be loaded. Review local logs for details.");
        }
    }

    /// <summary>
    /// Persists preset for the hardware performance preset API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="preset">Preset value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut]
    [HumanApprovalRequired("hardware-performance-preset.save", "Save hardware performance preset", "Persist provider-qualified hardware roads and token ranges for later Council sessions.", "Medium", "AI performance configuration maintainer")]
    public async Task<IResult> SavePreset(
        [FromBody] HardwarePerformancePreset preset,
        [FromQuery] bool userConfirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await presets.SavePresetAsync(preset, userConfirmed, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { Error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { Error = exception.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Hardware performance preset save failed.");
            return Results.InternalServerError("The hardware performance preset could not be saved. Review local logs for details.");
        }
    }


    /// <summary>Applies one stored performance profile to the prepared Council hardware roads for the next run.</summary>
    /// <param name="id">Identifier of the stored performance preset.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved the configuration change.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP-facing apply result.</returns>
    [HttpPost("{id:guid}/apply-preparation")]
    [HumanApprovalRequired("hardware-performance-preset.apply-preparation", "Apply hardware performance preset", "Apply one stored profile to matching prepared Council hardware roads without changing membership.", "Medium", "AI performance configuration maintainer")]
    public async Task<IResult> ApplyToPreparation(
        Guid id,
        [FromQuery] bool userConfirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            var appliedRoutes = await presets.ApplyPresetToPreparationAsync(id, userConfirmed, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new { PresetId = id, AppliedRoutes = appliedRoutes, Target = "Preparation" });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { Error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { Error = exception.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Hardware performance preset {PresetId} preparation apply failed.", id);
            return Results.InternalServerError("The hardware performance preset could not be applied. Review local logs for details.");
        }
    }

    /// <summary>Applies one stored performance profile to matching routes in a running Council.</summary>
    /// <param name="id">Identifier of the stored performance preset.</param>
    /// <param name="runId">Identifier of the running Council.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved the live configuration change.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP-facing apply result.</returns>
    [HttpPost("{id:guid}/apply-run/{runId:guid}")]
    [HumanApprovalRequired("hardware-performance-preset.apply-run", "Apply hardware performance preset to running Council", "Apply one stored profile to matching routes in one running Council without changing participants.", "Medium", "AI performance configuration maintainer")]
    public async Task<IResult> ApplyToRun(
        Guid id,
        Guid runId,
        [FromQuery] bool userConfirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            var appliedRoutes = await presets.ApplyPresetToRunAsync(id, runId, userConfirmed, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new { PresetId = id, RunId = runId, AppliedRoutes = appliedRoutes, Target = "RunningCouncil" });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { Error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { Error = exception.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Hardware performance preset {PresetId} apply to running Council {RunId} failed.", id, runId);
            return Results.InternalServerError("The hardware performance preset could not be applied to the running Council. Review local logs for details.");
        }
    }

    /// <summary>
    /// Deletes preset for the hardware performance preset API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("{id:guid}")]
    [HumanApprovalRequired("hardware-performance-preset.delete", "Delete hardware performance preset", "Delete one stored hardware-spooler profile without changing current Council routes.", "Medium", "AI performance configuration maintainer")]
    public async Task<IResult> DeletePreset(
        Guid id,
        [FromQuery] bool userConfirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            await presets.DeletePresetAsync(id, userConfirmed, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { Error = exception.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Hardware performance preset delete failed for {PresetId}.", id);
            return Results.InternalServerError("The hardware performance preset could not be deleted. Review local logs for details.");
        }
    }
}
