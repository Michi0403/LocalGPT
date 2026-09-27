using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes bounded public web-search operations to human/operator clients.</summary>
/// <param name="search">Bounded web-search service.</param>
/// <param name="logger">Logger used for safe request diagnostics.</param>
[ApiController]
[Route("api/web-search")]
public sealed class WebSearchController(IWebSearchService search, ILogger<WebSearchController> logger) : ControllerBase
{
    /// <summary>Searches the configured public provider after explicit LocalGPT approval.</summary>
    /// <param name="request">Validated query and provider selection.</param>
    /// <param name="cancellationToken">Cancellation token for the outbound request.</param>
    /// <returns>Bounded attributed search evidence.</returns>
    [HttpPost("search")]
    [HumanApprovalRequired("web-search.public.query", "Search the public web", "Send the reviewed query to the selected public search provider and return bounded attributed evidence.", "Medium", "Local machine operator", true)]
    public async Task<IResult> SearchAsync([FromBody] WebSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return Results.Ok(await search.SearchAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Public web-search controller request failed; query and response content were omitted.");
            return Results.Problem("Public web search failed. Review LocalGPT application logs.");
        }
    }
}
