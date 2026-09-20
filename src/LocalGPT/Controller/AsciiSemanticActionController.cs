using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes the semantic ASCII/game action contract shared by browser input and approved AI/team invocation.</summary>
[ApiController]
[Route("api/ascii/actions")]
public sealed class AsciiSemanticActionController(IAsciiSemanticActionService actions) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<AsciiSemanticActionDescriptor>> List() => Ok(actions.ListActions());

    [HttpPost("invoke")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Invoke([FromBody] AsciiSemanticActionRequest request, CancellationToken cancellationToken) =>
        Ok(await actions.InvokeAsync(request, cancellationToken).ConfigureAwait(false));
}
