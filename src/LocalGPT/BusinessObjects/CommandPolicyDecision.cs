namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a command policy decision application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Allowed">Value indicating whether allowed should apply to this operation.</param>
/// <param name="Decision">Decision value supplied to the command policy decision operation and used when producing its result.</param>
/// <param name="Reason">Reason value supplied to the command policy decision operation and used when producing its result.</param>
/// <param name="Profile">Profile value supplied to the command policy decision operation and used when producing its result.</param>
public sealed record CommandPolicyDecision(
    bool Allowed,
    string Decision,
    string Reason,
    string Profile);
