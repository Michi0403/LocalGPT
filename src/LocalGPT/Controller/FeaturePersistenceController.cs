using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>CRUD API for durable records owned by newer LocalGPT feature modules.</summary>
/// <param name="persistence">Feature persistence service.</param>
/// <param name="logger">Writes bounded controller diagnostics.</param>
[ApiController]
[Route("api/feature-persistence")]
[DocumentationUpdated("2.1.23")]
public sealed class FeaturePersistenceController(
    IFeaturePersistenceService persistence,
    ILogger<FeaturePersistenceController> logger) : ControllerBase
{
    /// <summary>Lists persistent Council prompt starters.</summary>
    /// <param name="includeDisabled">Value indicating whether include disabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("council-starters")]
    public Task<IResult> GetCouncilStarters([FromQuery] bool includeDisabled, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.GetCouncilPromptStartersAsync(includeDisabled, cancellationToken), "Council starter list");

    /// <summary>
    /// Retrieves council starter for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("council-starters/{id:guid}")]
    public Task<IResult> GetCouncilStarter(Guid id, CancellationToken cancellationToken) =>
        ExecuteNullableAsync(() => persistence.GetCouncilPromptStarterAsync(id, cancellationToken), "Council starter read");

    /// <summary>
    /// Persists council starter for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("council-starters")]
    [HumanApprovalRequired("feature.council-starter.save", "Save Council starter", "Store one chat or direct-Council prompt configuration.", "Medium", "Council configuration maintainer")]
    public Task<IResult> SaveCouncilStarter([FromBody] SaveFeatureRecordRequest<CouncilPromptStarterConfiguration> request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.SaveCouncilPromptStarterAsync(request, cancellationToken), "Council starter save");

    /// <summary>
    /// Deletes council starter for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("council-starters/{id:guid}")]
    [HumanApprovalRequired("feature.council-starter.delete", "Delete Council starter", "Delete one persistent prompt starter.", "Medium", "Council configuration maintainer")]
    public Task<IResult> DeleteCouncilStarter(Guid id, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.DeleteCouncilPromptStarterAsync(id, userConfirmed, cancellationToken), "Council starter delete");

    /// <summary>Lists localization catalog registrations.</summary>
    /// <param name="includeDisabled">Value indicating whether include disabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("localization-catalogs")]
    public Task<IResult> GetLocalizationCatalogs([FromQuery] bool includeDisabled, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.GetLocalizationCatalogsAsync(includeDisabled, cancellationToken), "localization catalog list");

    /// <summary>
    /// Retrieves localization catalog for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("localization-catalogs/{id:guid}")]
    public Task<IResult> GetLocalizationCatalog(Guid id, CancellationToken cancellationToken) =>
        ExecuteNullableAsync(() => persistence.GetLocalizationCatalogAsync(id, cancellationToken), "localization catalog read");

    /// <summary>
    /// Persists localization catalog for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("localization-catalogs")]
    [HumanApprovalRequired("feature.localization-catalog.save", "Save localization catalog", "Store one validated language-catalog registration.", "Medium", "Localization maintainer")]
    public Task<IResult> SaveLocalizationCatalog([FromBody] SaveFeatureRecordRequest<LocalizationCatalogRegistration> request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.SaveLocalizationCatalogAsync(request, cancellationToken), "localization catalog save");

    /// <summary>
    /// Deletes localization catalog for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("localization-catalogs/{id:guid}")]
    [HumanApprovalRequired("feature.localization-catalog.delete", "Delete localization catalog", "Delete one language-catalog registration without deleting unrelated files.", "Medium", "Localization maintainer")]
    public Task<IResult> DeleteLocalizationCatalog(Guid id, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.DeleteLocalizationCatalogAsync(id, userConfirmed, cancellationToken), "localization catalog delete");

    /// <summary>Lists documentation build evidence.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("documentation-builds")]
    public Task<IResult> GetDocumentationBuilds(CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.GetDocumentationBuildsAsync(cancellationToken), "documentation build list");

    /// <summary>
    /// Retrieves documentation build for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("documentation-builds/{id:guid}")]
    public Task<IResult> GetDocumentationBuild(Guid id, CancellationToken cancellationToken) =>
        ExecuteNullableAsync(() => persistence.GetDocumentationBuildAsync(id, cancellationToken), "documentation build read");

    /// <summary>
    /// Persists documentation build for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("documentation-builds")]
    [HumanApprovalRequired("feature.documentation-build.save", "Save documentation build", "Store one documentation generation evidence record.", "Low", "Documentation maintainer")]
    public Task<IResult> SaveDocumentationBuild([FromBody] SaveFeatureRecordRequest<DocumentationBuildRecord> request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.SaveDocumentationBuildAsync(request, cancellationToken), "documentation build save");

    /// <summary>
    /// Deletes documentation build for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("documentation-builds/{id:guid}")]
    [HumanApprovalRequired("feature.documentation-build.delete", "Delete documentation build", "Delete one stored documentation evidence record.", "Low", "Documentation maintainer")]
    public Task<IResult> DeleteDocumentationBuild(Guid id, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.DeleteDocumentationBuildAsync(id, userConfirmed, cancellationToken), "documentation build delete");

    /// <summary>Lists embedded firmware plan records.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("firmware-plans")]
    public Task<IResult> GetFirmwarePlans(CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.GetEmbeddedFirmwarePlansAsync(cancellationToken), "firmware plan list");

    /// <summary>
    /// Retrieves firmware plan for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("firmware-plans/{id:guid}")]
    public Task<IResult> GetFirmwarePlan(Guid id, CancellationToken cancellationToken) =>
        ExecuteNullableAsync(() => persistence.GetEmbeddedFirmwarePlanAsync(id, cancellationToken), "firmware plan read");

    /// <summary>
    /// Persists firmware plan for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("firmware-plans")]
    [HumanApprovalRequired("feature.firmware-plan.save", "Save firmware plan", "Store one versioned embedded-hardware planning envelope.", "Medium", "Embedded project maintainer")]
    public Task<IResult> SaveFirmwarePlan([FromBody] SaveFeatureRecordRequest<EmbeddedFirmwarePlanRecord> request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.SaveEmbeddedFirmwarePlanAsync(request, cancellationToken), "firmware plan save");

    /// <summary>
    /// Deletes firmware plan for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("firmware-plans/{id:guid}")]
    [HumanApprovalRequired("feature.firmware-plan.delete", "Delete firmware plan", "Delete one stored firmware planning envelope.", "Medium", "Embedded project maintainer")]
    public Task<IResult> DeleteFirmwarePlan(Guid id, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.DeleteEmbeddedFirmwarePlanAsync(id, userConfirmed, cancellationToken), "firmware plan delete");

    /// <summary>Lists authoritative GameDirector session records.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("game-sessions")]
    public Task<IResult> GetGameSessions(CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.GetCouncilGameSessionsAsync(cancellationToken), "GameDirector session list");

    /// <summary>
    /// Retrieves game session for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("game-sessions/{id:guid}")]
    public Task<IResult> GetGameSession(Guid id, CancellationToken cancellationToken) =>
        ExecuteNullableAsync(() => persistence.GetCouncilGameSessionAsync(id, cancellationToken), "GameDirector session read");

    /// <summary>
    /// Persists game session for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("game-sessions")]
    [HumanApprovalRequired("feature.game-session.save", "Save GameDirector session", "Store one authoritative game-runtime snapshot envelope.", "Medium", "Game runtime maintainer")]
    public Task<IResult> SaveGameSession([FromBody] SaveFeatureRecordRequest<CouncilGameSessionRecord> request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.SaveCouncilGameSessionAsync(request, cancellationToken), "GameDirector session save");

    /// <summary>
    /// Deletes game session for the feature persistence API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("game-sessions/{id:guid}")]
    [HumanApprovalRequired("feature.game-session.delete", "Delete GameDirector session", "Delete one stored game-runtime snapshot envelope.", "Medium", "Game runtime maintainer")]
    public Task<IResult> DeleteGameSession(Guid id, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        ExecuteAsync(() => persistence.DeleteCouncilGameSessionAsync(id, userConfirmed, cancellationToken), "GameDirector session delete");

    /// <summary>Executes one nullable read action with consistent bounded logging.</summary>
    /// <typeparam name="T">Reference result type.</typeparam>
    /// <param name="action">Asynchronous feature read.</param>
    /// <param name="operation">Bounded operation name.</param>
    /// <returns>A task that returns 404 for a missing record or a normal CRUD result.</returns>
    private async Task<IResult> ExecuteNullableAsync<T>(Func<Task<T?>> action, string operation) where T : class
    {
        try
        {
            var result = await action().ConfigureAwait(false);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogInformation(exception, "Feature persistence {Operation} was cancelled.", operation);
            return Results.Conflict(new { error = "The operation was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Feature persistence {Operation} failed.", operation);
            return Results.InternalServerError(new { error = "The operation failed. Review LocalGPT application logs." });
        }
    }

    /// <summary>Executes one CRUD action with consistent bounded logging and HTTP errors.</summary>
    /// <typeparam name="T">Operation result type.</typeparam>
    /// <param name="action">Asynchronous feature operation.</param>
    /// <param name="operation">Bounded operation name.</param>
    /// <returns>A task that returns an HTTP result.</returns>
    private async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action, string operation)
    {
        try
        {
            return Results.Ok(await action().ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Feature persistence {Operation} was rejected.", operation);
            return Results.BadRequest(new { error = exception.Message });
        }
        catch (OperationCanceledException exception)
        {
            logger.LogInformation(exception, "Feature persistence {Operation} was cancelled.", operation);
            return Results.Conflict(new { error = "The operation was cancelled." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Feature persistence {Operation} failed; payload content was omitted from logs.", operation);
            return Results.InternalServerError(new { error = "The operation failed. Review LocalGPT application logs." });
        }
    }
}
