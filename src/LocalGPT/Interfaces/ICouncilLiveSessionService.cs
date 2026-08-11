using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council live session service contract.
/// </summary>
public interface ICouncilLiveSessionService
{
    event Action<Guid>? Changed;

    /// <summary>
    /// Runs the begin operation.
    /// </summary>
    CancellationToken Begin(
        Guid runId,
        IReadOnlyList<string> councilMembers,
        string userMessage,
        string initialTranscript);
    /// <summary>
    /// Runs the append operation.
    /// </summary>
    void Append(Guid runId, string text);
    /// <summary>
    /// Sets status.
    /// </summary>
    void SetStatus(Guid runId, string statusMessage);
    /// <summary>
    /// Runs the touch operation.
    /// </summary>
    void Touch(Guid runId);
    /// <summary>
    /// Runs the append user message operation.
    /// </summary>
    void AppendUserMessage(Guid runId, string text);
    /// <summary>
    /// Runs the complete operation.
    /// </summary>
    void Complete(Guid runId);
    /// <summary>
    /// Determines whether cel.
    /// </summary>
    bool Cancel(Guid runId);
    /// <summary>
    /// Runs the get operation.
    /// </summary>
    CouncilLiveSessionSnapshot? Get(Guid runId);
    /// <summary>
    /// Gets summary.
    /// </summary>
    CouncilLiveSessionSummary? GetSummary(Guid runId);
    /// <summary>
    /// Gets active.
    /// </summary>
    IReadOnlyList<CouncilLiveSessionSnapshot> GetActive();
    /// <summary>
    /// Gets active summaries.
    /// </summary>
    IReadOnlyList<CouncilLiveSessionSummary> GetActiveSummaries();
}
