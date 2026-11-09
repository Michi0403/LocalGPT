using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents configured workflow execution state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="Round">Round value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="ExpandedStepIndex">Expanded step index value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="PreviousStep">Previous step value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="FallbackAnswer">Fallback answer value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="FinalAnswer">Final answer value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="XDirective">X directive value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="AuthoritativeCurationEvidence">Persisted verified upload and project metadata carried to every subsequent workflow step.</param>
internal sealed record ConfiguredWorkflowExecutionState(
    int Round,
    int ExpandedStepIndex,
    string PreviousStep,
    string FallbackAnswer,
    string FinalAnswer,
    CouncilXRoundDirective? XDirective = null,
    string AuthoritativeCurationEvidence = "");
