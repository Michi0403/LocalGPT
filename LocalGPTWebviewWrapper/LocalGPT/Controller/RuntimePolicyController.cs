using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/runtime-policy")]
public sealed class RuntimePolicyController(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILocalGptRuntimePolicyStoreService runtimePolicyStore,
    ILocalGptRuntimePolicySeedDataService runtimePolicySeed,
    ILogger<RuntimePolicyController> logger) : ControllerBase
{
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
