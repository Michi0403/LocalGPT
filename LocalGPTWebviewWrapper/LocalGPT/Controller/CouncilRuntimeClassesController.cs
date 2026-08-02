using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/council/runtime-classes")]
public sealed class CouncilRuntimeClassesController(
    ICouncilRuntimeClassService runtimeClasses,
    ILogger<CouncilRuntimeClassesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CouncilRuntimeClassDefinition>>> GetAll(
        [FromQuery] bool includeDisabled,
        CancellationToken cancellationToken) =>
        Ok(await runtimeClasses.GetDefinitionsAsync(includeDisabled, cancellationToken).ConfigureAwait(false));

    [HttpGet("{key}")]
    public async Task<ActionResult<CouncilRuntimeClassDefinition>> Get(
        string key,
        CancellationToken cancellationToken)
    {
        var definition = await runtimeClasses.FindAsync(key, cancellationToken).ConfigureAwait(false);
        return definition is null ? NotFound() : Ok(definition);
    }

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
