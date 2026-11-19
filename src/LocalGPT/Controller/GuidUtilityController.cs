using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes the same narrow read-only computation contract to local clients.</summary>
/// <param name="utility">Identifier computation service.</param>
/// <param name="logger">Controller diagnostics.</param>
[ApiController]
[Route("api/identity/guid")]
public sealed class GuidUtilityController(IGuidUtilityService utility, ILogger<GuidUtilityController> logger) : ControllerBase
{
    /// <summary>Computes a new unbound GUID or validates a supplied value without modifying persistent state.</summary>
    /// <param name="request">Validated operation and optional input value.</param>
    /// <returns>Canonical result or invalid-operation response.</returns>
    [HttpPost]
    public IResult Execute(GuidUtilityRequest request)
    {
        try { return Results.Ok(utility.Execute(request)); }
        catch (ArgumentException exception)
        {
            logger.LogDebug(exception, "Invalid GUID computation request.");
            return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Operation)] = [exception.Message] });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "GUID computation API failed.");
            return Results.InternalServerError();
        }
    }
}
