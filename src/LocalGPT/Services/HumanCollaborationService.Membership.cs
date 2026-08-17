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
    /// Performs synchronize active human membership as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the human collaboration operation and used when producing its result.</param>
    private void SynchronizeActiveHumanMembership(HumanCouncilParticipantProfile profile)
    {
    try
    {
            var humanMember = $"Human: {Normalize(profile.DisplayName, 120, "Human User")}";
            foreach (var pair in activeRuns.ToArray())
            {
                var current = pair.Value;
                var members = current.CouncilMembers
                    .Where(member => !member.StartsWith("Human:", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (profile.IsEnabled)
                    members.Add(humanMember);

                activeRuns[pair.Key] = current with
                {
                    CouncilMembers = members
                };
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(SynchronizeActiveHumanMembership)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(SynchronizeActiveHumanMembership)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs begin council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="members">String dependency used by the human collaboration workflow to provide the corresponding application capability.</param>
    public void BeginCouncilRun(Guid runId, IReadOnlyList<string> members)
    {
    try
    {
            activeRuns[runId] = new HumanCouncilRunSnapshot(runId, DateTime.UtcNow, 0, "Starting", members.ToList(), false);
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BeginCouncilRun)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BeginCouncilRun)} failed.");
        throw;
    }
}

    /// <summary>
    /// Updates council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="isWaitingForFinalHumanInput">Value indicating whether is waiting for final human input should apply to this operation.</param>
    public void UpdateCouncilRun(Guid runId, int currentRound, string phase, bool isWaitingForFinalHumanInput = false)
    {
    try
    {
            if (!activeRuns.TryGetValue(runId, out var current))
                return;
            activeRuns[runId] = current with
            {
                CurrentRound = Math.Max(0, currentRound),
                Phase = Normalize(phase, 120),
                IsWaitingForFinalHumanInput = isWaitingForFinalHumanInput
            };
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(UpdateCouncilRun)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(UpdateCouncilRun)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs end council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    public void EndCouncilRun(Guid runId)
    {
    try
    {
            activeRuns.TryRemove(runId, out _);
            preferredDirectUserMessageConsumers.TryRemove(runId, out _);
            foreach (var entry in directUserMessageRuns.Where(entry => entry.Value == runId).ToArray())
            {
                directUserMessageRuns.TryRemove(entry.Key, out _);
                directUserMessageClaims.TryRemove(entry.Key, out _);
            }
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(EndCouncilRun)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(EndCouncilRun)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures trusted human interaction as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="operation">Operation value supplied to the human collaboration operation and used when producing its result.</param>
    private void EnsureTrustedHumanInteraction(string operation)
    {
    try
    {
            if (!ambientContext.Current.IsTrustedHumanInteraction(vocabulary.Get()))
                throw new InvalidOperationException($"Only the trusted local human UI may {operation}.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(EnsureTrustedHumanInteraction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(EnsureTrustedHumanInteraction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs notify direct user message queued as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="contribution">Contribution value supplied to the human collaboration operation and used when producing its result.</param>
    private void NotifyDirectUserMessageQueued(HumanCouncilContribution contribution)
    {
        var listeners = DirectUserMessageQueued?.GetInvocationList()
            .Cast<Action<HumanCouncilContribution>>()
            .ToArray() ?? [];
        foreach (var listener in listeners)
        {
            try
            {
                listener(contribution);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "A direct Council user-message listener failed; remaining listeners will still be notified.");
            }
        }
    }

    /// <summary>
    /// Performs notify changed as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void NotifyChanged()
    {
        var listeners = Changed?.GetInvocationList().Cast<Action>().ToArray() ?? [];
        foreach (var listener in listeners)
        {
            try
            {
                listener();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "A Human Collaboration Inbox change listener failed; remaining listeners will still be notified.");
            }
        }
    }

    }
}
