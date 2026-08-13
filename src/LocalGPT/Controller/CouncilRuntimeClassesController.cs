using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the council runtime classes application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="runtimeClasses">Council runtime class service dependency used by the council runtime classes workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/council/runtime-classes")]
public sealed class CouncilRuntimeClassesController(
    ICouncilRuntimeClassService runtimeClasses,
    ILogger<CouncilRuntimeClassesController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves all for the council runtime classes API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="includeDisabled">Value indicating whether include disabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CouncilRuntimeClassDefinition>>> GetAll(
        [FromQuery] bool includeDisabled,
        CancellationToken cancellationToken) =>
        Ok(await runtimeClasses.GetDefinitionsAsync(includeDisabled, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the get projection for the council runtime classes API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="key">Key value supplied to the council runtime classes operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{key}")]
    public async Task<ActionResult<CouncilRuntimeClassDefinition>> Get(
        string key,
        CancellationToken cancellationToken)
    {
        var definition = await runtimeClasses.FindAsync(key, cancellationToken).ConfigureAwait(false);
        return definition is null ? NotFound() : Ok(definition);
    }

    /// <summary>
    /// Returns the save projection for the council runtime classes API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    [HumanApprovalRequired(
        "council.runtime-class.save",
        "Save Council runtime class",
        "Persist the reviewed runtime class fields, input ownership, keyboard/gamepad bindings, source references and recommended DXFunctions.",
        "High",
        "Runtime class reviewer")]
    public async Task<ActionResult<CouncilRuntimeClassDefinition>> Save(
        [FromBody] SaveCouncilRuntimeClassRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await runtimeClasses.SaveAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Council runtime class update was rejected for {RuntimeClassKey}.", request.Definition?.Key);
            return BadRequest(new { error = ex.Message });
        }
    }
}
