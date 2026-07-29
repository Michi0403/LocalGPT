using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/runtime-policy")]
public sealed class RuntimePolicyController(
    ILocalGptRuntimePolicyDataService runtimePolicy,
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
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the LocalGPT runtime policy controller snapshot.");
            return Problem(ex.Message);
        }
    }
}
