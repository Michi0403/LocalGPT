using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Coordinates bounded X-Round control requests between Council DXFunctions and the configured workflow runtime.</summary>
public interface ICouncilXRoundService
{
    /// <summary>Activates X-Round policy for one currently executing configured workflow step.</summary>
    void Activate(CouncilXRoundStepContext context);
    /// <summary>Removes the active X-Round policy for the exact step execution.</summary>
    void Deactivate(Guid runId, int round, string phase);
    /// <summary>Gets the active X-Round policy visible to the current Council participant.</summary>
    CouncilXRoundStepContext? GetActive(Guid runId, int round, string phase);
    /// <summary>Validates and queues one X-Round request from a Council participant.</summary>
    CouncilXRoundDirective Request(AmbientLocalGptContextSnapshot ambient, CouncilXRoundAction action,
        string targetStepKey = "", string reason = "", string text = "", string prompt = "",
        string teamKey = "", string modelName = "");
    /// <summary>Drains X-Round requests emitted by the just-completed workflow step.</summary>
    IReadOnlyList<CouncilXRoundDirective> Drain(Guid runId, int round, string phase);
    /// <summary>Consumes one configured transition-budget unit for the source workflow step.</summary>
    bool TryConsumeTransitionBudget(Guid runId, string sourceStepKey, int maximumTransitions, out int usedTransitions);
    /// <summary>Clears all process-local X-Round state for a completed Council run.</summary>
    void EndRun(Guid runId);
}
