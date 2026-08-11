using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Provides local path explorer controller operations.
/// </summary>
[ApiController]
[Route("api/local-paths")]
public sealed class LocalPathExplorerController(ILocalPathExplorerService paths) : ControllerBase
{
    /// <summary>
    /// Runs the roots operation.
    /// </summary>
    [HttpGet("roots")]
    public IResult Roots() => Results.Ok(paths.GetSuggestedRoots());

    /// <summary>
    /// Runs the browse operation.
    /// </summary>
    [HttpPost("browse")]
    public IResult Browse([FromBody] LocalPathBrowseRequest request) => Results.Ok(paths.Browse(request));
}
