using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes the same user-owned layout catalog used by the in-application Layout Studio.</summary>
/// <param name="layouts">Local gpt ui layout service dependency used by the UI layouts workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/ui-layouts")]
public sealed class UiLayoutsController(ILocalGptUiLayoutService layouts, ILogger<UiLayoutsController> logger) : ControllerBase
{
    /// <summary>Returns every declared page/section layout merged with persisted user overrides.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocalGptUiLayoutSurface>>> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await layouts.GetAllAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "UI layout catalog request failed.");
            return Problem("The UI layout catalog could not be loaded.");
        }
    }

    /// <summary>
    /// Returns the put projection for the UI layouts API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="surface">Surface value supplied to the UI layouts operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut]
    public async Task<ActionResult<LocalGptUiLayoutSurface>> PutAsync([FromBody] LocalGptUiLayoutSurface surface, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await layouts.SaveAsync(surface, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "UI layout save request failed for {LayoutKey}.", surface?.Key);
            return Problem("The UI layout override could not be saved.");
        }
    }

    /// <summary>Resets one layout override to its source-declared defaults.</summary>
    /// <param name="key">Key value supplied to the UI layouts operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete]
    public async Task<IActionResult> DeleteAsync([FromQuery] string key, CancellationToken cancellationToken)
    {
        try
        {
            await layouts.ResetAsync(key, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "UI layout reset request failed for {LayoutKey}.", key);
            return Problem("The UI layout override could not be reset.");
        }
    }
}
