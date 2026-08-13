using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the local path explorer application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="paths">Local path explorer service dependency used by the local path explorer workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/local-paths")]
public sealed class LocalPathExplorerController(ILocalPathExplorerService paths) : ControllerBase
{
    /// <summary>
    /// Returns the roots projection for the local path explorer API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("roots")]
    public IResult Roots() => Results.Ok(paths.GetSuggestedRoots());

    /// <summary>
    /// Returns the browse projection for the local path explorer API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The i result browse from body local path browse request request results ok paths produced by the operation.</returns>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    [HttpPost("browse")]
    public IResult Browse([FromBody] LocalPathBrowseRequest request) => Results.Ok(paths.Browse(request));
}
