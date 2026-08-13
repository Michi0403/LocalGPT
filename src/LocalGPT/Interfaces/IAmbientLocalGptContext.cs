using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for ambient LocalGPT context behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IAmbientLocalGptContext
{
    /// <summary>
    /// Gets the current value that forms part of the ambient LocalGPT context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current value exposed by <see cref="IAmbientLocalGptContext"/>.</value>
    AmbientLocalGptContextSnapshot Current { get; }

    /// <summary>
    /// Performs push system for <see cref="IAmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="source">Source value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <returns>The i disposable produced by the operation.</returns>
    IDisposable PushSystem(string source, string? correlationId = null);

    /// <summary>
    /// Performs push council for <see cref="IAmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="councilRound">Council round value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <returns>The i disposable produced by the operation.</returns>
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
    /// Performs push human interaction for <see cref="ILocalHumanInteractionContext"/>, keeping the operation consistent with the state and invariants of the surrounding local human interaction context workflow.
    /// </summary>
    /// <param name="humanProfileId">Identifier of the human profile to use for this operation.</param>
    /// <param name="displayName">Display name value supplied to the local human interaction context operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the local human interaction context operation and used when producing its result.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="councilRound">Council round value supplied to the local human interaction context operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the local human interaction context operation and used when producing its result.</param>
    /// <returns>The i disposable produced by the operation.</returns>
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
    /// Performs push human approval for <see cref="IHumanApprovalExecutionContext"/>, keeping the operation consistent with the state and invariants of the surrounding human approval execution context workflow.
    /// </summary>
    /// <param name="humanProfileId">Identifier of the human profile to use for this operation.</param>
    /// <param name="displayName">Display name value supplied to the human approval execution context operation and used when producing its result.</param>
    /// <param name="approvalRequestId">Identifier of the approval request to use for this operation.</param>
    /// <param name="source">Source value supplied to the human approval execution context operation and used when producing its result.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="councilRound">Council round value supplied to the human approval execution context operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the human approval execution context operation and used when producing its result.</param>
    /// <returns>The i disposable produced by the operation.</returns>
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
