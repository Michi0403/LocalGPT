namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a dxaichat function info application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Name">Name value supplied to the dxaichat function info operation and used when producing its result.</param>
/// <param name="Method">Method value supplied to the dxaichat function info operation and used when producing its result.</param>
/// <param name="Route">Route value supplied to the dxaichat function info operation and used when producing its result.</param>
/// <param name="Purpose">Purpose value supplied to the dxaichat function info operation and used when producing its result.</param>
/// <param name="Parameters">Parameters value supplied to the dxaichat function info operation and used when producing its result.</param>
/// <param name="SafetyNotes">Safety notes value supplied to the dxaichat function info operation and used when producing its result.</param>
/// <param name="IsReadOnly">Value indicating whether read only should apply to this operation.</param>
/// <param name="AvailableToAi">Value indicating whether available to AI should apply to this operation.</param>
/// <param name="RequiresHumanConfirmation">Value indicating whether requires human confirmation should apply to this operation.</param>
/// <param name="SupportsDirectInvocation">Value indicating whether direct invocation should apply to this operation.</param>
/// <param name="SupportsAutomaticInvocation">Value indicating whether automatic invocation should apply to this operation.</param>
/// <param name="Source">Source value supplied to the dxaichat function info operation and used when producing its result.</param>
/// <param name="ParameterSchemaJson">Parameter schema json value supplied to the dxaichat function info operation and used when producing its result.</param>
/// <param name="IsCoordinationOnly">Value indicating whether coordination only should apply to this operation.</param>
/// <param name="SupportsDeferredApprovalRequest">Value indicating whether deferred approval request should apply to this operation.</param>
/// <param name="ApprovalRequiredBeforeCompletion">Value indicating whether approval required before completion should apply to this operation.</param>
public sealed record DxaichatFunctionInfo(
    string Name,
    string Method,
    string Route,
    string Purpose,
    string Parameters,
    string SafetyNotes,
    bool IsReadOnly = true,
    bool AvailableToAi = true,
    bool RequiresHumanConfirmation = false,
    bool SupportsDirectInvocation = false,
    bool SupportsAutomaticInvocation = false,
    string Source = "ControllerRoute",
    string ParameterSchemaJson = "{\"type\":\"object\",\"properties\":{}}",
    bool IsCoordinationOnly = false,
    bool SupportsDeferredApprovalRequest = false,
    bool ApprovalRequiredBeforeCompletion = false);
