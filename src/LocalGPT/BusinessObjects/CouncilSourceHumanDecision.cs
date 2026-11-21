namespace LocalGPT.BusinessObjects;

/// <summary>Resolved, run-bound human guidance that must survive heartbeat consumption and source planning.</summary>
public sealed record CouncilSourceHumanDecision(
    Guid RequestId, string Title, string Question, string Answer, DateTime DecidedAtUtc);
