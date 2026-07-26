namespace LocalGPT.BusinessObjects;

public sealed record CommandPolicyDecision(
    bool Allowed,
    string Decision,
    string Reason,
    string Profile)
{
    public static CommandPolicyDecision Allow(string profile, string reason) =>
        new(true, "Allowed", reason, profile);

    public static CommandPolicyDecision Deny(string reason) =>
        new(false, "Denied", reason, "Denied");
}
