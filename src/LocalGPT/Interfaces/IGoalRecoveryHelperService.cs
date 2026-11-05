using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Goal execution's "let me do it for you" helper for running Councils: keeps the original user goal moving when a member fails or
/// an AI host goes down. Every choice stays inside the members and AI hosts that the current run already uses; decisions that
/// change who speaks for a paired role or remove a host from the run are offered to the local user through Human Collaboration.
/// </summary>
public interface IGoalRecoveryHelperService
{
    /// <summary>Decides whether a failed member slot is retried with the same member, continued by another member of the run, or left empty.</summary>
    /// <param name="context">Failed slot, run members, configuration constraints and current-run evidence.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The decision, including its readable reason and the evidence used.</returns>
    Task<GoalMemberRecoveryDecision> DecideMemberRecoveryAsync(GoalMemberRecoveryContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a provider failure that may come from an unavailable AI host: the first outage of a host in a run gets one bounded wait,
    /// then a single reachability check; a host that stays silent is marked down for the run and an exclusion for this run is offered.
    /// </summary>
    /// <param name="report">Failed member, its exact provider model and its host identity.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>Whether the member's own bounded retry may run now and whether the failure is attributed to the host.</returns>
    Task<GoalHostFailureDecision> HandleHostFailureAsync(GoalHostFailureReport report, CancellationToken cancellationToken = default);

    /// <summary>Checks before a member starts whether its AI host is usable in this run; a host marked down is checked once, an excluded host is refused.</summary>
    /// <param name="runId">Council run identifier.</param>
    /// <param name="hostKey">AI host identity of the member.</param>
    /// <param name="modelName">Provider-qualified member.</param>
    /// <param name="model">Resolved provider model of the member.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The admission result with a user-visible message when the start is refused.</returns>
    Task<GoalHostAdmission> AdmitHostAsync(Guid runId, string hostKey, string modelName, ProviderModelReference model, CancellationToken cancellationToken = default);

    /// <summary>Returns the run-scoped availability state of an AI host without probing it.</summary>
    /// <param name="runId">Council run identifier.</param>
    /// <param name="hostKey">AI host identity.</param>
    /// <returns>The host state; a host without observed outage is reported as available.</returns>
    GoalHostRunStatus GetHostStatus(Guid runId, string hostKey);

    /// <summary>Renders the run warning for an unreachable AI host from the configurable helper texts.</summary>
    /// <param name="hostKey">AI host identity.</param>
    /// <returns>The warning text.</returns>
    string BuildHostDownWarning(string hostKey);
}
