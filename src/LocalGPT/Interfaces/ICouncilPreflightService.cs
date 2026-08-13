using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council preflight behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilPreflightService
{
    /// <summary>
    /// Performs prepare as part of the council preflight service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="participants">String dependency used by the council preflight workflow to provide the corresponding application capability.</param>
    /// <param name="modelRoutes">Council hardware road plan dependency used by the council preflight workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council preflight report produced by the operation.</returns>
    Task<CouncilPreflightReport> PrepareAsync(
        MultiModelCouncilRequest request,
        IReadOnlyList<string> participants,
        IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds member readiness prompt as part of the council preflight service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the council preflight operation and used when producing its result.</param>
    /// <param name="participants">String dependency used by the council preflight workflow to provide the corresponding application capability.</param>
    /// <param name="report">Report value supplied to the council preflight operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildMemberReadinessPrompt(
        string modelName,
        IReadOnlyList<string> participants,
        CouncilPreflightReport report);
}
