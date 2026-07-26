using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IAmbientLocalGptContext
{
    AmbientLocalGptContextSnapshot Current { get; }

    IDisposable PushSystem(string source, string? correlationId = null);

    IDisposable PushCouncil(
        Guid councilRunId,
        int councilRound,
        string phase,
        string? correlationId = null);
}

/// <summary>
/// Capability-bearing boundary for trusted local-human UI interaction only.
/// It can create peer participation context, never operation approval.
/// </summary>
public interface ILocalHumanInteractionContext
{
    IDisposable PushHumanInteraction(
        Guid humanProfileId,
        string displayName,
        string source,
        string? correlationId = null,
        Guid? councilRunId = null,
        int councilRound = 0,
        string phase = "");
}

/// <summary>
/// Capability-bearing boundary for execution of one exact persisted approval.
/// It must never be injected into ordinary UI, model tools, or general services.
/// </summary>
public interface IHumanApprovalExecutionContext
{
    IDisposable PushHumanApproval(
        Guid humanProfileId,
        string displayName,
        Guid approvalRequestId,
        string source,
        string correlationId,
        Guid? councilRunId = null,
        int councilRound = 0,
        string phase = "");
}
