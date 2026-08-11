using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the ambient local gpt context contract.
/// </summary>
public interface IAmbientLocalGptContext
{
    AmbientLocalGptContextSnapshot Current { get; }

    /// <summary>
    /// Runs the push system operation.
    /// </summary>
    IDisposable PushSystem(string source, string? correlationId = null);

    /// <summary>
    /// Runs the push council operation.
    /// </summary>
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
    /// <summary>
    /// Runs the push human interaction operation.
    /// </summary>
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
    /// <summary>
    /// Runs the push human approval operation.
    /// </summary>
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
