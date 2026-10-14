using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes the semantic ASCII/game action contract shared by browser input and approved AI/team invocation.</summary>
/// <param name="actions">Ascii semantic action service dependency used by the ascii semantic action workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/ascii/actions")]
public sealed class AsciiSemanticActionController(IAsciiSemanticActionService actions) : ControllerBase
{
    /// <summary>
    /// Returns the list projection for the ascii semantic action API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<AsciiSemanticActionDescriptor>> List() => Ok(actions.ListActions());

    /// <summary>
    /// Returns the invoke projection for the ascii semantic action API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("invoke")]
    public async Task<ActionResult<CouncilGameSessionSnapshot>> Invoke([FromBody] AsciiSemanticActionRequest request, CancellationToken cancellationToken) =>
        Ok(await actions.InvokeAsync(request, cancellationToken).ConfigureAwait(false));
}
