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
    /// Performs determine evaluation verdict as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="evaluation">Evaluation value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DetermineEvaluationVerdict(string evaluation)
    {
    try
    {
            if (evaluation.Contains("Human peer assessment: Supported", StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().VerdictSupported;
            if (evaluation.Contains("Human peer assessment: Needs correction", StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().VerdictNeedsCorrection;
            if (evaluation.Contains("Human peer assessment: Mixed", StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().VerdictMixed;
            return vocabulary.Get().VerdictNotReviewed;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(DetermineEvaluationVerdict)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(DetermineEvaluationVerdict)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs blocks boundary as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="upcomingRound">Upcoming round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="upcomingPhase">Upcoming phase value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="boundary">Boundary value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool BlocksBoundary(
        HumanCollaborationRequest request,
        int upcomingRound,
        string upcomingPhase,
        HumanCollaborationBoundary boundary)
    {
    try
    {
            var gateMode = NormalizeGateMode(request.GateMode, request.RequiredBeforeCompletion);
            if (gateMode == "None")
                return false;
            if (boundary == HumanCollaborationBoundary.Completion)
                return gateMode == "NextPhase" ||
                    gateMode == "NextRound" ||
                    gateMode == "Completion";

            var movedToLaterRound = upcomingRound > request.RequestedCouncilRound;
            var movedToLaterPhase = movedToLaterRound ||
                (upcomingRound == request.RequestedCouncilRound &&
                 !string.Equals(Normalize(upcomingPhase, 120), request.RequestedCouncilPhase, StringComparison.OrdinalIgnoreCase));

            return gateMode switch
            {
                "NextPhase" => movedToLaterPhase,
                "NextRound" => movedToLaterRound,
                "Completion" => false,
                _ => false
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BlocksBoundary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BlocksBoundary)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether reusable decision as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsReusableDecision(HumanCollaborationRequest request)
    {
    try
    {
            if (request.Status == vocabulary.Get().HumanStatusPending)
                return true;
            if (request.Status != vocabulary.Get().HumanStatusApproved &&
                request.Status != vocabulary.Get().HumanStatusAnswered &&
                request.Status != vocabulary.Get().HumanStatusDeclined)
                return false;

            return request.ApprovalReuseScope switch
            {
                HumanApprovalReuseScope.CurrentApplicationSession =>
                    request.ApprovalSessionId == approvalSessionId &&
                    (!request.ConsumeApproval || request.ConsumedAtUtc is null),
                HumanApprovalReuseScope.PersistentUntilChanged =>
                    !request.ConsumeApproval || request.ConsumedAtUtc is null,
                _ => request.ConsumedAtUtc is null
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(IsReusableDecision)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(IsReusableDecision)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves default reuse scope as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestKind">Request kind value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="riskLevel">Risk level value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The human approval reuse scope produced by the operation.</returns>
    private HumanApprovalReuseScope GetDefaultReuseScope(string requestKind, string? riskLevel)
    {
    try
    {
            if (requestKind != vocabulary.Get().HumanRequestApproval)
                return HumanApprovalReuseScope.ExactRequestOnce;
            return IsHighImpactRisk(riskLevel)
                ? HumanApprovalReuseScope.ExactRequestOnce
                : HumanApprovalReuseScope.CurrentApplicationSession;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetDefaultReuseScope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetDefaultReuseScope)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves default consume approval as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestKind">Request kind value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="riskLevel">Risk level value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetDefaultConsumeApproval(string requestKind, string? riskLevel) {
    try
    {
        return requestKind != vocabulary.Get().HumanRequestApproval || IsHighImpactRisk(riskLevel);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetDefaultConsumeApproval)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetDefaultConsumeApproval)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether high impact risk as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="riskLevel">Risk level value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsHighImpactRisk(string? riskLevel) {
    try
    {
        return string.Equals(riskLevel, "High", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(riskLevel, "Critical", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(IsHighImpactRisk)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(IsHighImpactRisk)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes question scope as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeQuestionScope(string? value)
    {
    try
    {
            if (string.Equals(value, "Consensus", StringComparison.OrdinalIgnoreCase))
                return "Consensus";
            if (string.Equals(value, "SelectedMembers", StringComparison.OrdinalIgnoreCase))
                return "SelectedMembers";
            return "Member";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeQuestionScope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeQuestionScope)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes gate mode as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="requiredBeforeCompletion">Value indicating whether required before completion should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeGateMode(string? value, bool requiredBeforeCompletion)
    {
    try
    {
            if (string.Equals(value, "NextPhase", StringComparison.OrdinalIgnoreCase))
                return "NextPhase";
            if (string.Equals(value, "NextRound", StringComparison.OrdinalIgnoreCase))
                return "NextRound";
            if (string.Equals(value, "Completion", StringComparison.OrdinalIgnoreCase) || requiredBeforeCompletion)
                return "Completion";
            return "None";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeGateMode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeGateMode)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes request kind as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeRequestKind(string? value)
    {
    try
    {
            if (string.Equals(value, vocabulary.Get().HumanRequestFeedback, StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().HumanRequestFeedback;
            if (string.Equals(value, vocabulary.Get().HumanRequestGuidance, StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().HumanRequestGuidance;
            return vocabulary.Get().HumanRequestApproval;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeRequestKind)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeRequestKind)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs normalize as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="maxLength">Max length value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Normalize(string? value, int maxLength, string fallback = "")
    {
    try
    {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized[..Math.Min(normalized.Length, maxLength)];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(Normalize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(Normalize)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes multiline as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="maxLength">Max length value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeMultiline(string? value, int maxLength)
    {
    try
    {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
            return normalized[..Math.Min(normalized.Length, maxLength)];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeMultiline)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeMultiline)} failed.");
        throw;
    }
}

    }
}
