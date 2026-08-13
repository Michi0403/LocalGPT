using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the runtime policy application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the runtime policy workflow to provide the corresponding application capability.</param>
/// <param name="runtimePolicyStore">Local gpt runtime policy store service dependency used by the runtime policy workflow to provide the corresponding application capability.</param>
/// <param name="runtimePolicySeed">Local gpt runtime policy seed data service dependency used by the runtime policy workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/runtime-policy")]
public sealed class RuntimePolicyController(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILocalGptRuntimePolicyStoreService runtimePolicyStore,
    ILocalGptRuntimePolicySeedDataService runtimePolicySeed,
    ILogger<RuntimePolicyController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the get projection for the runtime policy API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public ActionResult<LocalGptRuntimePolicySnapshot> Get()
    {
        try
        {
            var snapshot = runtimePolicy.GetSnapshot();
            logger.LogDebug($"Returned the LocalGPT runtime policy controller snapshot.");
            return Ok(snapshot);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return the LocalGPT runtime policy controller snapshot: {exception.Message}");
            return Problem(exception.Message);
        }
    }

    /// <summary>
    /// Retrieves definition for the runtime policy API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("definition")]
    public ActionResult<LocalGptRuntimePolicyDefinition> GetDefinition()
    {
        try
        {
            var definition = runtimePolicyStore.GetDefinition();
            logger.LogDebug($"Returned the database-backed LocalGPT runtime policy definition.");
            return Ok(definition);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return the database-backed LocalGPT runtime policy definition: {exception.Message}");
            return Problem(exception.Message);
        }
    }

    /// <summary>
    /// Retrieves seed for the runtime policy API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("seed")]
    public ActionResult<LocalGptRuntimePolicySeedModel> GetSeed()
    {
        try
        {
            var seed = runtimePolicySeed.GetSeed();
            logger.LogDebug($"Returned the LocalGPT runtime policy first-run seed model.");
            return Ok(seed);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return the LocalGPT runtime policy first-run seed model: {exception.Message}");
            return Problem(exception.Message);
        }
    }

    /// <summary>
    /// Returns the reload projection for the runtime policy API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("reload")]
    public ActionResult<LocalGptRuntimePolicySnapshot> Reload()
    {
        try
        {
            var snapshot = runtimePolicy.Reload();
            logger.LogInformation($"Reloaded the LocalGPT runtime policy through the controller boundary.");
            return Ok(snapshot);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not reload the LocalGPT runtime policy through the controller boundary: {exception.Message}");
            return Problem(exception.Message);
        }
    }
}
