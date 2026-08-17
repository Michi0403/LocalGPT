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
    /// Performs queue contribution as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="content">Content value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council contribution produced by the operation.</returns>
    public Task<HumanCouncilContribution> QueueContributionAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default) {
    try
    {
        return QueueContributionCoreAsync(
            councilRunId,
            content,
            requireEnabledProfile: true,
            directUserMessage: false,
            cancellationToken: cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueContributionAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueContributionAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs queue user message as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="content">Content value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council contribution produced by the operation.</returns>
    public Task<HumanCouncilContribution> QueueUserMessageAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default) {
    try
    {
        return QueueContributionCoreAsync(
            councilRunId,
            content,
            requireEnabledProfile: false,
            directUserMessage: true,
            cancellationToken: cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueUserMessageAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueUserMessageAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Sets preferred direct user message consumer as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public void SetPreferredDirectUserMessageConsumer(Guid councilRunId, string consumerKey)
    {
        try
        {
            if (councilRunId == Guid.Empty || string.IsNullOrWhiteSpace(consumerKey))
                return;
            preferredDirectUserMessageConsumers[councilRunId] = consumerKey.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Setting preferred direct-message consumer failed for Council run {CouncilRunId}.", councilRunId);
        }
    }

    /// <summary>
    /// Performs clear preferred direct user message consumer as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public void ClearPreferredDirectUserMessageConsumer(Guid councilRunId, string consumerKey)
    {
        try
        {
            if (councilRunId == Guid.Empty || string.IsNullOrWhiteSpace(consumerKey))
                return;
            if (preferredDirectUserMessageConsumers.TryGetValue(councilRunId, out var current) &&
                string.Equals(current, consumerKey.Trim(), StringComparison.Ordinal))
            {
                preferredDirectUserMessageConsumers.TryRemove(councilRunId, out _);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Clearing preferred direct-message consumer failed for Council run {CouncilRunId}.", councilRunId);
        }
    }

    /// <summary>
    /// Attempts to claim direct user message as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public bool TryClaimDirectUserMessage(Guid contributionId, Guid councilRunId, string consumerKey)
    {
        try
        {
            if (contributionId == Guid.Empty || councilRunId == Guid.Empty || string.IsNullOrWhiteSpace(consumerKey))
                return false;
            var normalizedConsumer = consumerKey.Trim();
            if (preferredDirectUserMessageConsumers.TryGetValue(councilRunId, out var preferredConsumer) &&
                !string.Equals(preferredConsumer, normalizedConsumer, StringComparison.Ordinal))
            {
                return false;
            }
            directUserMessageRuns[contributionId] = councilRunId;
            return directUserMessageClaims.TryAdd(contributionId, normalizedConsumer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claiming direct Council user message {ContributionId} failed.", contributionId);
            return false;
        }
    }

    /// <summary>
    /// Performs queue contribution core as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="content">Content value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="requireEnabledProfile">Value indicating whether require enabled profile should apply to this operation.</param>
    /// <param name="directUserMessage">Value indicating whether direct user message should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council contribution produced by the operation.</returns>
    private async Task<HumanCouncilContribution> QueueContributionCoreAsync(
        Guid councilRunId,
        string content,
        bool requireEnabledProfile,
        bool directUserMessage,
        CancellationToken cancellationToken)
    {
    try
    {
            EnsureTrustedHumanInteraction(directUserMessage
                ? "add a direct user message to a running council"
                : "submit a human council contribution");
            var normalized = NormalizeMultiline(content, MaxTextLength);
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("A Council user message cannot be empty.");

            var profile = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
            if (requireEnabledProfile && !profile.IsEnabled)
                throw new InvalidOperationException("Enable the Human Council Participant profile before joining a run.");
            if (!activeRuns.TryGetValue(councilRunId, out var active))
                throw new InvalidOperationException("The selected council run is no longer active.");

            var contribution = new HumanCouncilContribution
            {
                CouncilRunId = councilRunId,
                HumanDisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? "Human User" : profile.DisplayName,
                HumanRole = directUserMessage
                    ? "Direct user message"
                    : profile.RoleName,
                Content = normalized,
                EarliestCouncilRound = directUserMessage
                    ? Math.Max(0, active.CurrentRound)
                    : Math.Max(1, active.CurrentRound + 1),
                Status = vocabulary.Get().ContributionQueued,
                EvaluationVerdict = vocabulary.Get().VerdictPending,
                SubmittedAtUtc = DateTime.UtcNow
            };

            using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.HumanCouncilContributions.Add(contribution);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (directUserMessage)
            {
                directUserMessageRuns[contribution.Id] = councilRunId;
                NotifyDirectUserMessageQueued(contribution);
            }
            NotifyChanged();
            componentActivity.RecordInformation(
                "HumanCollaboration",
                directUserMessage ? "CouncilUserMessageQueued" : "ContributionQueued",
                directUserMessage
                    ? "A direct user message was queued for immediate active-model interruption/resume and subsequent Council heartbeats."
                    : "A human contribution was queued for the next council heartbeat.");
            logger.LogInformation(
                directUserMessage
                    ? "Queued direct user message {ContributionId} for council run {CouncilRunId} at earliest round {EarliestRound}; content was omitted from logs."
                    : "Queued human contribution {ContributionId} for council run {CouncilRunId} at earliest round {EarliestRound}; content was omitted from logs.",
                contribution.Id,
                contribution.CouncilRunId,
                contribution.EarliestCouncilRound);
            return contribution;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueContributionCoreAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueContributionCoreAsync)} failed.");
        throw;
    }
}

    }
}
