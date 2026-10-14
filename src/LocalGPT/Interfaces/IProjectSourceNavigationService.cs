using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Project and tracked-file owned source discovery/publication; regex curators supply independently reviewed pattern evidence.</summary>
public interface IProjectSourceNavigationService
{
    /// <summary>
    /// Performs navigate as part of the project source navigation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project source navigation result produced by the operation.</returns>
    Task<ProjectSourceNavigationResult> NavigateAsync(ProjectSourceNavigationRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Publishes approved script as part of the project source navigation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The publish tracked project script result produced by the operation.</returns>
    Task<PublishTrackedProjectScriptResult> PublishApprovedScriptAsync(PublishTrackedProjectScriptRequest request, CancellationToken cancellationToken = default);
}
