using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Resolves a stable, bounded hardware road for every council member. The planner is deliberately
/// separate from the council orchestration so hardware growth does not turn the heartbeat service
/// into another monolith.
/// </summary>
public interface ICouncilHardwareRoadPlanner
{
    /// <summary>
    /// Builds plans.
    /// </summary>
    IReadOnlyDictionary<string, CouncilHardwareRoadPlan> BuildPlans(
        IReadOnlyCollection<OneWireCouncilModelRoute>? configuredRoutes,
        IReadOnlyCollection<string> participants,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int resourceLoadPercent,
        int? fallbackOllamaNumGpu);
}
