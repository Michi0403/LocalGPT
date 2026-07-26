using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilPreflightService
{
    Task<CouncilPreflightReport> PrepareAsync(
        MultiModelCouncilRequest request,
        IReadOnlyList<string> participants,
        IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
        CancellationToken cancellationToken = default);

    string BuildMemberReadinessPrompt(
        string modelName,
        IReadOnlyList<string> participants,
        CouncilPreflightReport report);
}
