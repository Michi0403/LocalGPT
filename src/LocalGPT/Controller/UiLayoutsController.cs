using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes the same user-owned layout catalog used by the in-application Layout Studio.</summary>
[ApiController]
[Route("api/ui-layouts")]
public sealed class UiLayoutsController(ILocalGptUiLayoutService layouts, ILogger<UiLayoutsController> logger) : ControllerBase
{
    /// <summary>Returns every declared page/section layout merged with persisted user overrides.</summary>
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

    /// <summary>Saves one layout override.</summary>
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
