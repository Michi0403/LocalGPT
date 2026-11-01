using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Persistent, bounded and permission-preserving task recovery.</summary>
public interface IAiHelpEscalatorService
{
    /// <summary>Record an attempt and return one bounded next action.</summary>
    /// <param name="report">Actual attempted action and its reported outcome.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Stable action that must be honored before any new attempt.</returns>
    Task<AiHelpRecoveryDecision> ReportAttemptAsync(AiHelpAttemptReport report, CancellationToken cancellationToken = default);
    /// <summary>Inspect previously persisted task status without retrying.</summary>
    /// <param name="taskId">Stable task key.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Last known decision, if present.</returns>
    Task<AiHelpRecoveryDecision?> InspectAsync(string taskId, CancellationToken cancellationToken = default);
    /// <summary>Check whether a human question has been resolved; never automatically reexecute an operation.</summary>
    /// <param name="taskId">Stable task key.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Current decision, preserving waiting state until resolution.</returns>
    Task<AiHelpRecoveryDecision?> CheckHumanResponseAsync(string taskId, CancellationToken cancellationToken = default);
}

/// <summary>Read-only model reviewer for a task-specific, single-use recovery proposal.</summary>
public interface IAiHelpCuratorService
{
    /// <summary>Ask a configured provider model for one alternative, without tool access.</summary>
    /// <param name="report">Observed attempt and untrusted evidence.</param>
    /// <param name="failures">Current failed-attempt count.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>A proposed correction or no proposal; the model cannot grant permissions.</returns>
    Task<AiHelpCuratorAdvice> DiagnoseAsync(AiHelpAttemptReport report, int failures, CancellationToken cancellationToken = default);
}
