namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a command policy decision.
/// </summary>
public sealed record CommandPolicyDecision(
    bool Allowed,
    string Decision,
    string Reason,
    string Profile);
