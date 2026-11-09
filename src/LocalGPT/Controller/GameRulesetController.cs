using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the game catalog, the engine primitives and the declarative rule rows of game projects, so users and tools can
/// author their own games (stats, archetypes, actions, rules, levels, texts, aliases) without engine code.
/// </summary>
/// <param name="rulesets">Game project catalog and rule-row store.</param>
/// <param name="primitives">Registry of the engine primitives rule rows may name.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/games")]
public sealed class GameRulesetController(
    IGameRulesetStoreService rulesets,
    IGamePrimitiveRegistry primitives,
    ILogger<GameRulesetController> logger) : ControllerBase
{
    /// <summary>Lists every game project (system seeds first) with its launcher data.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game catalog.</returns>
    [HttpGet("catalog")]
    public async Task<ActionResult<IReadOnlyList<GameCatalogEntry>>> Catalog(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await rulesets.ListCatalogAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Listing the game catalog was cancelled.");
            return Conflict(new { error = "Listing the game catalog was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing the game catalog failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Listing the game catalog failed");
        }
    }

    /// <summary>Lists the engine primitive keys by kind (maps, actions, rules, views, autoplay, styles, council progressions).</summary>
    /// <returns>The primitive keys rule rows and profiles may name.</returns>
    [HttpGet("primitives")]
    public ActionResult<IReadOnlyDictionary<string, IReadOnlyList<string>>> Primitives()
    {
        try
        {
            return Ok(primitives.ListKeys());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing the game engine primitives failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Listing the game engine primitives failed");
        }
    }

    /// <summary>Returns the own rule rows of one game project (tables left empty inherit from its rules template).</summary>
    /// <param name="projectId">Identifier of the game project.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game project's own rule rows.</returns>
    [HttpGet("{projectId:guid}/rules")]
    public async Task<ActionResult<SaveGameRulesetRequest>> GetRules(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await rulesets.GetOwnRowsAsync(projectId, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException exception)
        {
            logger.LogWarning(exception, "Game project {ProjectId} has no game profile.", projectId);
            return NotFound(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Reading the rules of game project {ProjectId} was cancelled.", projectId);
            return Conflict(new { error = "Reading the game rules was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the rules of game project {ProjectId} failed.", projectId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Reading the game rules failed");
        }
    }

    /// <summary>Replaces the own rule rows of one game project; requires explicit user confirmation in the request.</summary>
    /// <param name="projectId">Identifier of the game project.</param>
    /// <param name="request">The complete own rule rows of the game.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The composed ruleset after the change.</returns>
    [HttpPut("{projectId:guid}/rules")]
    public async Task<ActionResult<GameRuleset>> SaveRules(Guid projectId, [FromBody] SaveGameRulesetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await rulesets.SaveRulesetAsync(projectId, request, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException exception)
        {
            logger.LogWarning(exception, "Game project {ProjectId} has no game profile.", projectId);
            return NotFound(new { error = exception.Message });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Saving the rules of game project {ProjectId} was rejected.", projectId);
            return BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Saving the rules of game project {ProjectId} was cancelled.", projectId);
            return Conflict(new { error = "Saving the game rules was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving the rules of game project {ProjectId} failed.", projectId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Saving the game rules failed");
        }
    }
}
