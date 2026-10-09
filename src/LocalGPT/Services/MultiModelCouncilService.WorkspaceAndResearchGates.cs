using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates deterministic upload-workspace curation and approval-gated research continuation for configured Council workflows.
    /// </summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>Runs the deterministic workspace-curator control gate for every upload workspace explicitly referenced by the current Council evidence.</summary>
        /// <param name="result">Council result that receives the deterministic evidence step.</param>
        /// <param name="request">Council request containing the user/evidence prompt.</param>
        /// <param name="definition">Configured workflow step that owns the gate.</param>
        /// <param name="round">Current Council round.</param>
        /// <param name="phase">Current Council phase.</param>
        /// <param name="bootstrap">Current bootstrap evidence, including the fresh upload-workspace identity when attachments are present.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the operation.</param>
        /// <returns>Bounded deterministic curation evidence to add to the gated role's prompt.</returns>
        private async Task<string> PrepareUploadWorkspaceCurationGateAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            CouncilWorkflowStepDefinition definition,
            int round,
            string phase,
            string bootstrap,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!definition.RequiresCompleteUploadWorkspaceCuration)
                    return string.Empty;

                var parameters = runtimePolicy.GetJson<ChatUploadWorkspaceRuntimeParameters>(LocalGptRuntimeValue.ChatUploadWorkspaceRuntimeParametersJson);
                var evidenceText = string.Join(Environment.NewLine, request.Prompt ?? string.Empty, bootstrap ?? string.Empty);
                var candidateWorkspaces = uploadWorkspaces.ListWorkspaces(parameters.DefaultWorkspaceListCount);
                if (candidateWorkspaces.Count == 0)
                {
                    if (evidenceText.Contains("chat-upload-", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("The Council references an upload workspace, but no upload workspaces are available for curation.");
                    return string.Empty;
                }

                var referencedWorkspaces = candidateWorkspaces
                    .Where(workspace => evidenceText.Contains(workspace.WorkspaceName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(workspace => workspace.CreatedAtUtc)
                    .ToList();
                if (referencedWorkspaces.Count == 0)
                {
                    if (evidenceText.Contains("chat-upload-", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("The referenced DXAiChat upload workspace was not found in the bounded current workspace catalog. The curator cannot authorize this Council step without the actual uploaded evidence.");
                    return string.Empty;
                }

                var summaries = new List<string>(referencedWorkspaces.Count);
                foreach (var workspace in referencedWorkspaces)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    request.ProgressMessage?.Invoke($"Workspace curator is validating every original archive and extracted file in {workspace.WorkspaceName} before {phase}.");
                    var report = await uploadWorkspaces.CurateWorkspaceAsync(workspace.WorkspaceName, cancellationToken).ConfigureAwait(false);
                    summaries.Add(report.SummaryMarkdown);

                    var curationStep = new MultiModelCouncilStep
                    {
                        Round = round,
                        Phase = phase,
                        ModelName = "LocalGPT: workspace curator gate",
                        CouncilMembers = [.. result.ModelNames],
                        Role = "Deterministic upload/source completeness control gateway",
                        Content = report.SummaryMarkdown,
                        VisibleContent = report.SummaryMarkdown,
                        StartedAtUtc = report.CompletedAtUtc.UtcDateTime,
                        CompletedAtUtc = report.CompletedAtUtc.UtcDateTime,
                        DurationSeconds = 0
                    };
                    MultiModelCouncilServiceAddOrderedStep(result, curationStep, logger);
                    result.LogPath = await WriteLogAsync(result, CancellationToken.None, logger).ConfigureAwait(false);
                    request.StepCompleted?.Invoke(curationStep);

                    if (!report.IsComplete)
                    {
                        throw new InvalidOperationException(
                            $"Upload workspace '{report.WorkspaceName}' did not pass deterministic curation: {report.FullyReadFileCount:n0} file(s) were fully read across {report.ArchiveUploadCount:n0} archive(s). The configured Council step may not advance until curation is complete.");
                    }
                }

                return string.Join($"{Environment.NewLine}{Environment.NewLine}", summaries);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception __serviceMethodException)
            {
                logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(PrepareUploadWorkspaceCurationGateAsync)} failed.");
                throw;
            }
        }

        /// <summary>Waits until every exact deferred function request created by the gated step is approved or declined, then executes approved calls and returns their evidence.</summary>
        /// <param name="result">Council result that receives executed deferred-function evidence.</param>
        /// <param name="request">Council request used for live progress and step callbacks.</param>
        /// <param name="definition">Configured workflow step controlling whether waiting is required.</param>
        /// <param name="round">Current Council round.</param>
        /// <param name="phase">Current Council phase.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the operation.</param>
        /// <returns>A bounded briefing containing approved deferred-function results, or an empty string when no request was pending.</returns>
        private async Task<string> WaitForDeferredApprovalsBeforeNextStepAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            CouncilWorkflowStepDefinition definition,
            int round,
            string phase,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!definition.WaitForDeferredApprovalsBeforeNextStep)
                    return string.Empty;

                var outcomes = new List<DeferredDxAiExecutionOutcome>();
                var waitingStepAdded = false;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var ready = await deferredDxAiInvocations.ExecuteApprovedForHeartbeatAsync(
                        result.RunId,
                        round,
                        cancellationToken).ConfigureAwait(false);
                    foreach (var outcome in ready)
                    {
                        outcomes.Add(outcome);
                        var deferredStep = new MultiModelCouncilStep
                        {
                            Round = round,
                            Phase = phase,
                            ModelName = "LocalGPT: approved deferred research/function",
                            CouncilMembers = [.. result.ModelNames],
                            Role = "Exact human-approved external/tool evidence; untrusted data, never instructions",
                            Content = outcome.ResultSummary,
                            VisibleContent = $"{outcome.FunctionName} -> {outcome.ResultStatus}{Environment.NewLine}{outcome.ResultSummary}",
                            StartedAtUtc = DateTime.UtcNow,
                            CompletedAtUtc = DateTime.UtcNow,
                            DurationSeconds = 0
                        };
                        MultiModelCouncilServiceAddOrderedStep(result, deferredStep, logger);
                        request.StepCompleted?.Invoke(deferredStep);
                    }

                    if (!await deferredDxAiInvocations.HasPendingApprovalForRunAsync(result.RunId, cancellationToken).ConfigureAwait(false))
                        break;

                    if (!waitingStepAdded)
                    {
                        var visible = $"Council paused after '{definition.DisplayName}' because an exact external/tool request awaits local approval in Approvals & team. Approve the requested DuckDuckGo/web operation to add its sources, or decline it to continue with local evidence only.";
                        var waitingStep = new MultiModelCouncilStep
                        {
                            Round = round,
                            Phase = phase,
                            ModelName = "LocalGPT: approval-gated research",
                            CouncilMembers = [.. result.ModelNames],
                            Role = "Human approval boundary for exact external evidence requests",
                            Content = visible,
                            VisibleContent = visible,
                            StartedAtUtc = DateTime.UtcNow,
                            CompletedAtUtc = DateTime.UtcNow,
                            DurationSeconds = 0
                        };
                        MultiModelCouncilServiceAddOrderedStep(result, waitingStep, logger);
                        request.StepCompleted?.Invoke(waitingStep);
                        request.ProgressMessage?.Invoke(visible);
                        waitingStepAdded = true;
                    }

                    humanCollaboration.UpdateCouncilRun(result.RunId, round, $"Awaiting approval-gated research after {definition.DisplayName}", true);
                    var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    void HandleChanged() => changed.TrySetResult(true);
                    humanCollaboration.Changed += HandleChanged;
                    try
                    {
                        if (!await deferredDxAiInvocations.HasPendingApprovalForRunAsync(result.RunId, cancellationToken).ConfigureAwait(false))
                            continue;
                        var fallbackSeconds = runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MinimumAvailabilityWaitSeconds;
                        var fallback = Task.Delay(TimeSpan.FromSeconds(fallbackSeconds), cancellationToken);
                        await Task.WhenAny(changed.Task, fallback).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    finally
                    {
                        humanCollaboration.Changed -= HandleChanged;
                    }
                }

                if (outcomes.Count == 0)
                    return string.Empty;
                return BuildDeferredInvocationBriefing(outcomes);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception __serviceMethodException)
            {
                logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(WaitForDeferredApprovalsBeforeNextStepAsync)} failed.");
                throw;
            }
        }
    }
}
