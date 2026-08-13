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
    /// Builds plans for <see cref="ICouncilHardwareRoadPlanner"/>, keeping the operation consistent with the state and invariants of the surrounding council hardware road planner workflow.
    /// </summary>
    /// <param name="configuredRoutes">One wire council model route dependency used by the council hardware road planner workflow to provide the corresponding application capability.</param>
    /// <param name="participants">String dependency used by the council hardware road planner workflow to provide the corresponding application capability.</param>
    /// <param name="requestedMaxOutputTokens">Requested max output tokens value supplied to the council hardware road planner operation and used when producing its result.</param>
    /// <param name="requestedMaxContextTokens">Requested max context tokens value supplied to the council hardware road planner operation and used when producing its result.</param>
    /// <param name="resourceLoadPercent">Resource load percent value supplied to the council hardware road planner operation and used when producing its result.</param>
    /// <param name="fallbackOllamaNumGpu">Fallback ollama num gpu value supplied to the council hardware road planner operation and used when producing its result.</param>
    /// <returns>The i read only dictionary string council hardware road plan produced by the operation.</returns>
    IReadOnlyDictionary<string, CouncilHardwareRoadPlan> BuildPlans(
        IReadOnlyCollection<OneWireCouncilModelRoute>? configuredRoutes,
        IReadOnlyCollection<string> participants,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int resourceLoadPercent,
        int? fallbackOllamaNumGpu);
}
