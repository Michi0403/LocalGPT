using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Coordinates bounded X-Round control requests between Council DXFunctions and the configured workflow runtime.</summary>
public interface ICouncilXRoundService
{
    /// <summary>Activates X-Round policy for one currently executing configured workflow step.</summary>
    /// <param name="context">Context value supplied to the council x round operation and used when producing its result.</param>
    void Activate(CouncilXRoundStepContext context);
    /// <summary>Removes the active X-Round policy for the exact step execution.</summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council x round operation and used when producing its result.</param>
    void Deactivate(Guid runId, int round, string phase);
    /// <summary>Gets the active X-Round policy visible to the current Council participant.</summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council x round operation and used when producing its result.</param>
    /// <returns>The council x round step context produced by the operation.</returns>
    CouncilXRoundStepContext? GetActive(Guid runId, int round, string phase);
    /// <summary>Validates and queues one X-Round request from a Council participant.</summary>
    /// <param name="ambient">Ambient value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="action">Action value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="targetStepKey">Target step key value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="reason">Reason value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="prompt">Prompt value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="teamKey">Team key value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="modelName">Model name value supplied to the council x round operation and used when producing its result.</param>
    /// <returns>The council x round directive produced by the operation.</returns>
    CouncilXRoundDirective Request(AmbientLocalGptContextSnapshot ambient, CouncilXRoundAction action,
        string targetStepKey = "", string reason = "", string text = "", string prompt = "",
        string teamKey = "", string modelName = "");
    /// <summary>Drains X-Round requests emitted by the just-completed workflow step.</summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council x round operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<CouncilXRoundDirective> Drain(Guid runId, int round, string phase);
    /// <summary>Consumes one configured transition-budget unit for the source workflow step.</summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="sourceStepKey">Source step key value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="maximumTransitions">Maximum transitions value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="usedTransitions">Used transitions value supplied to the council x round operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryConsumeTransitionBudget(Guid runId, string sourceStepKey, int maximumTransitions, out int usedTransitions);
    /// <summary>Clears all process-local X-Round state for a completed Council run.</summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    void EndRun(Guid runId);
}
