using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Defines a first-class cross-round control action that a configured Council workflow may request.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilXRoundAction
{
    /// <summary>Re-enters a workflow step for reasoning only; DX/organic execution is suppressed on that revision.</summary>
    ReconsiderStep,
    /// <summary>Re-enters a workflow step and permits its normal configured execution policy to run again.</summary>
    ReexecuteStep,
    /// <summary>Returns an explicit text result and ends the parent workflow cleanly.</summary>
    ReturnText,
    /// <summary>Runs one selected model as a bounded derived subtask and feeds its result back into the parent workflow.</summary>
    StartSingleModel,
    /// <summary>Runs another configured Council team as a bounded derived subtask and feeds its result back into the parent workflow.</summary>
    StartCouncil
}

/// <summary>Describes the X-Round policy currently active for one configured Council workflow step.</summary>
public sealed record CouncilXRoundStepContext(
    Guid RunId, int Round, string Phase, string StepKey, string StepDisplayName,
    bool CanRevisit, bool CanReturnText, bool CanStartSingleModel, bool CanStartCouncil,
    int MaximumTransitions, bool RequiresHumanApproval, string DefaultTargetStepKey,
    string ChildCouncilTeamKey, int MaximumChildCouncilDepth, string ChildModelName);

/// <summary>Represents one requested X-Round transition or derived subtask.</summary>
public sealed record CouncilXRoundDirective(
    Guid Id, Guid RunId, int Round, string Phase, string SourceStepKey, CouncilXRoundAction Action,
    string TargetStepKey, string Reason, string Text, string Prompt, string TeamKey, string ModelName,
    string RequestedBy, DateTime RequestedAtUtc);
