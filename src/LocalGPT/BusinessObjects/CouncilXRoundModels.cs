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
/// <param name="RunId">Identifier of the run to use for this operation.</param>
/// <param name="Round">Round value supplied to the council x round step context operation and used when producing its result.</param>
/// <param name="Phase">Phase value supplied to the council x round step context operation and used when producing its result.</param>
/// <param name="StepKey">Step key value supplied to the council x round step context operation and used when producing its result.</param>
/// <param name="StepDisplayName">Step display name value supplied to the council x round step context operation and used when producing its result.</param>
/// <param name="CanRevisit">Value indicating whether revisit should apply to this operation.</param>
/// <param name="CanReturnText">Value indicating whether return text should apply to this operation.</param>
/// <param name="CanStartSingleModel">Value indicating whether start single model should apply to this operation.</param>
/// <param name="CanStartCouncil">Value indicating whether start council should apply to this operation.</param>
/// <param name="MaximumTransitions">Maximum transitions value supplied to the council x round step context operation and used when producing its result.</param>
/// <param name="RequiresHumanApproval">Value indicating whether requires human approval should apply to this operation.</param>
/// <param name="DefaultTargetStepKey">Default target step key value supplied to the council x round step context operation and used when producing its result.</param>
/// <param name="ChildCouncilTeamKey">Child council team key value supplied to the council x round step context operation and used when producing its result.</param>
/// <param name="MaximumChildCouncilDepth">Maximum child council depth value supplied to the council x round step context operation and used when producing its result.</param>
/// <param name="ChildModelName">Child model name value supplied to the council x round step context operation and used when producing its result.</param>
public sealed record CouncilXRoundStepContext(
    Guid RunId, int Round, string Phase, string StepKey, string StepDisplayName,
    bool CanRevisit, bool CanReturnText, bool CanStartSingleModel, bool CanStartCouncil,
    int MaximumTransitions, bool RequiresHumanApproval, string DefaultTargetStepKey,
    string ChildCouncilTeamKey, int MaximumChildCouncilDepth, string ChildModelName);

/// <summary>Represents one requested X-Round transition or derived subtask.</summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="RunId">Identifier of the run to use for this operation.</param>
/// <param name="Round">Round value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="Phase">Phase value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="SourceStepKey">Source step key value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="Action">Action value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="TargetStepKey">Target step key value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="Reason">Reason value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="Text">Text value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="Prompt">Prompt value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="TeamKey">Team key value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="ModelName">Model name value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="RequestedBy">Requested by value supplied to the council x round directive operation and used when producing its result.</param>
/// <param name="RequestedAtUtc">Requested at utc value supplied to the council x round directive operation and used when producing its result.</param>
public sealed record CouncilXRoundDirective(
    Guid Id, Guid RunId, int Round, string Phase, string SourceStepKey, CouncilXRoundAction Action,
    string TargetStepKey, string Reason, string Text, string Prompt, string TeamKey, string ModelName,
    string RequestedBy, DateTime RequestedAtUtc);
