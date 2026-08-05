namespace LocalGPT.BusinessObjects;

public sealed record CommandPolicyDecision(
    bool Allowed,
    string Decision,
    string Reason,
    string Profile);
