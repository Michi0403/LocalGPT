using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/local-paths")]
public sealed class LocalPathExplorerController(ILocalPathExplorerService paths) : ControllerBase
{
    [HttpGet("roots")]
    public IResult Roots() => Results.Ok(paths.GetSuggestedRoots());

    [HttpPost("browse")]
    public IResult Browse([FromBody] LocalPathBrowseRequest request) => Results.Ok(paths.Browse(request));
}
