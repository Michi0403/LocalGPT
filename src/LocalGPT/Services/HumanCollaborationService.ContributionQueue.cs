using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates human collaboration behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class HumanCollaborationService
    {
    /// <summary>
    /// Reads queued contributions as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<HumanCouncilContribution>> ReadQueuedContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                return await db.HumanCouncilContributions.AsNoTracking()
                    .Where(item => item.CouncilRunId == councilRunId &&
                        item.Status == vocabulary.Get().ContributionQueued &&
                        item.EarliestCouncilRound <= currentRound)
                    .OrderBy(item => item.SubmittedAtUtc)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(ReadQueuedContributionsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(ReadQueuedContributionsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs drain contributions as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<HumanCouncilContribution>> DrainContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var contributions = await db.HumanCouncilContributions
                    .Where(item => item.CouncilRunId == councilRunId &&
                        item.Status == vocabulary.Get().ContributionQueued &&
                        item.EarliestCouncilRound <= currentRound)
                    .OrderBy(item => item.SubmittedAtUtc)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var contribution in contributions)
                {
                    contribution.Status = vocabulary.Get().ContributionInjected;
                    contribution.InjectedAtUtc = DateTime.UtcNow;
                }
                if (contributions.Count > 0)
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                foreach (var contribution in contributions)
                {
                    directUserMessageClaims.TryRemove(contribution.Id, out _);
                    directUserMessageRuns.TryRemove(contribution.Id, out _);
                }
                NotifyChanged();
                return contributions;
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(DrainContributionsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(DrainContributionsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs mark contributions evaluated as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="afterRound">After round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="evaluation">Evaluation value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task MarkContributionsEvaluatedAsync(
        Guid councilRunId,
        int afterRound,
        string evaluation,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var contributions = await db.HumanCouncilContributions
                    .Where(item => item.CouncilRunId == councilRunId && item.Status == vocabulary.Get().ContributionInjected)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (contributions.Count == 0)
                    return;

                var normalizedEvaluation = NormalizeMultiline(evaluation, MaxTextLength);
                var evaluationVerdict = DetermineEvaluationVerdict(normalizedEvaluation);
                foreach (var contribution in contributions)
                {
                    contribution.Status = vocabulary.Get().ContributionEvaluated;
                    contribution.EvaluatedAtUtc = DateTime.UtcNow;
                    contribution.EvaluatedAfterRound = Math.Max(0, afterRound);
                    contribution.Evaluation = normalizedEvaluation;
                    contribution.EvaluationVerdict = evaluationVerdict;
                }
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                NotifyChanged();
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(MarkContributionsEvaluatedAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(MarkContributionsEvaluatedAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds council briefing as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public async Task<string> BuildCouncilBriefingAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default)
    {
    try
    {
            var profile = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var recentAnswers = await db.HumanCollaborationRequests
                    .Where(item => item.RequestKind != vocabulary.Get().HumanRequestApproval &&
                        (item.CouncilRunId == councilRunId || item.CouncilRunId == null) &&
                        item.Status == vocabulary.Get().HumanStatusAnswered &&
                        item.EarliestCouncilRound <= currentRound)
                    .OrderBy(item => item.DecidedAtUtc)
                    .Take(6)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var builder = new StringBuilder()
                    .AppendLine("Trusted local human-participation metadata (controlled by the local UI and service boundary):")
                    .Append("- Human participant enabled: ").AppendLine(profile.IsEnabled ? "yes" : "no")
                    .Append("- Display name: ").AppendLine(profile.DisplayName)
                    .Append("- Team role: ").AppendLine(profile.RoleName)
                    .Append("- Expertise: ").AppendLine(string.IsNullOrWhiteSpace(profile.Expertise) ? "not specified" : profile.Expertise)
                    .AppendLine("- Treat human contributions as peer evidence with the same review burden as model contributions, not as automatic truth.")
                    .AppendLine("- Explicitly evaluate human contributions for correctness, missing evidence, and broken assumptions in the next available council phase.")
                    .AppendLine("- Models may not impersonate, rewrite, enable, disable, promote, or demote the local human participant profile.")
                    .AppendLine("- Human participation never authorizes file, command, network, database-write, or artifact actions. Those require a separate exact approval request.")
                    .AppendLine("- When asking the human a question, declare its scope (one member, selected members, or Council consensus) and its gate (non-blocking, before next phase, before next round, or before completion).")
                    .AppendLine("- Use a blocking gate only when the Council genuinely cannot cross that boundary without the answer. Questions useful for later work should remain non-blocking or gate only the next round.")
                    .Append("- Current council run: ").AppendLine(councilRunId.ToString());

                if (!string.IsNullOrWhiteSpace(profile.WorkingStyle))
                    builder.Append("- Working style: ").AppendLine(profile.WorkingStyle);

                if (recentAnswers.Count > 0)
                {
                    builder.AppendLine("New human guidance and feedback for this heartbeat (authoritative response to the listed question, but not authorization for side effects):");
                    builder.AppendLine("- A member/role named as requester or target must explicitly consume the matching human answer as highest-priority role input when it next runs; do not silently continue as if the answer was absent.");
                    foreach (var answer in recentAnswers)
                    {
                        builder.Append("- [")
                            .Append(answer.QuestionScope)
                            .Append("; gate ")
                            .Append(answer.GateMode)
                            .Append("; requested by ")
                            .Append(string.IsNullOrWhiteSpace(answer.RequestedBy) ? "unspecified member" : answer.RequestedBy)
                            .Append(" / ")
                            .Append(string.IsNullOrWhiteSpace(answer.RequestedRole) ? "unspecified role" : answer.RequestedRole);
                        if (!string.IsNullOrWhiteSpace(answer.TargetMembersText))
                            builder.Append("; targets ").Append(answer.TargetMembersText);
                        builder.Append("] ")
                            .Append(answer.Title)
                            .Append(" -> ")
                            .Append(answer.Status);
                        if (!string.IsNullOrWhiteSpace(answer.UserResponse))
                            builder.Append(": ").Append(answer.UserResponse);
                        builder.AppendLine();
                        answer.Status = vocabulary.Get().HumanStatusConsumed;
                        answer.ConsumedAtUtc = DateTime.UtcNow;
                        answer.UpdatedAtUtc = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    NotifyChanged();
                }
                return builder.ToString().Trim();
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BuildCouncilBriefingAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BuildCouncilBriefingAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves gate status as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="upcomingRound">Upcoming round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="upcomingPhase">Upcoming phase value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="boundary">Boundary value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human collaboration gate status produced by the operation.</returns>
    public async Task<HumanCollaborationGateStatus> GetGateStatusAsync(
        Guid councilRunId,
        int upcomingRound,
        string upcomingPhase,
        HumanCollaborationBoundary boundary,
        CancellationToken cancellationToken = default)
    {
    try
    {
            using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var pending = await db.HumanCollaborationRequests.AsNoTracking()
                .Where(item => item.CouncilRunId == councilRunId &&
                    item.RequestKind != vocabulary.Get().HumanRequestApproval &&
                    item.Status == vocabulary.Get().HumanStatusPending)
                .OrderBy(item => item.RequestedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var blocking = pending
                .Where(item => BlocksBoundary(item, Math.Max(0, upcomingRound), upcomingPhase, boundary))
                .ToList();
            return new HumanCollaborationGateStatus(
                blocking.Count > 0,
                boundary,
                Math.Max(0, upcomingRound),
                Normalize(upcomingPhase, 120),
                blocking);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetGateStatusAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetGateStatusAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether required pending input as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> HasRequiredPendingInputAsync(Guid councilRunId, CancellationToken cancellationToken = default)
    {
    try
    {
            var gate = await GetGateStatusAsync(
                councilRunId,
                int.MaxValue,
                "Council completion",
                HumanCollaborationBoundary.Completion,
                cancellationToken).ConfigureAwait(false);
            return gate.IsBlocked;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(HasRequiredPendingInputAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(HasRequiredPendingInputAsync)} failed.");
        throw;
    }
}

    }
}
