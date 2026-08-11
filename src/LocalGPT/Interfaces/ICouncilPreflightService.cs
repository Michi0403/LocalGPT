using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council preflight service contract.
/// </summary>
public interface ICouncilPreflightService
{
    /// <summary>
    /// Runs the prepare async operation.
    /// </summary>
    Task<CouncilPreflightReport> PrepareAsync(
        MultiModelCouncilRequest request,
        IReadOnlyList<string> participants,
        IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds member readiness prompt.
    /// </summary>
    string BuildMemberReadinessPrompt(
        string modelName,
        IReadOnlyList<string> participants,
        CouncilPreflightReport report);
}
