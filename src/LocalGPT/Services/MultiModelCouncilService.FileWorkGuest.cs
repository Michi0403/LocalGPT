using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Imports a single explicitly approved guest-generated file into its existing source draft, never into an arbitrary host path.</summary>
public sealed partial class MultiModelCouncilService
{
    /// <summary>Executes the submitted author script in the chosen guest and imports only its matching completed output receipt.</summary>
    /// <param name="result">Owning Council execution.</param>
    /// <param name="request">Owning Chat request and callbacks.</param>
    /// <param name="definition">Configured source generation step.</param>
    /// <param name="progress">Persisted project and single workspace decision.</param>
    /// <param name="packet">Exclusive file work assignment.</param>
    /// <param name="authored">Actual model result containing the proposed script.</param>
    /// <param name="round">Current logical round.</param>
    /// <param name="phase">Current source generation phase.</param>
    /// <param name="cancellationToken">Cancels waiting for guest completion and stops a running matching guest.</param>
    /// <returns>A task that completes after the file receipt or explicit failure is persisted.</returns>
    private async Task ImportFileWorkGuestOutputAsync(MultiModelCouncilResult result, MultiModelCouncilRequest request,
        CouncilWorkflowStepDefinition definition, CodeGenerationDraftProgress progress, CodeGenerationDraftWorkPacket packet,
        MultiModelCouncilStep? authored, int round, string phase, CancellationToken cancellationToken)
    {
        Guid? ownedRun = null;
        try
        {
            if (authored is null || !string.IsNullOrWhiteSpace(authored.Error))
            {
                await codeGenerationWorkflow.CompleteDraftFileAsync(result.RunId, packet.Assignment, packet.Assignment.ModelName,
                    string.Empty, authored?.Error ?? "No guest script response was received.", cancellationToken).ConfigureAwait(false);
                return;
            }
            var answer = ReadFileWorkAnswer(authored).Trim();
            var first = answer.IndexOf('\n');
            var last = answer.LastIndexOf('\n');
            if (!answer.StartsWith("```", StringComparison.Ordinal) || first < 0 || last <= first ||
                !string.Equals(answer[(last + 1)..].Trim(), "```", StringComparison.Ordinal))
                throw new ArgumentException("The isolated file author did not return a complete script fence.");
            var script = answer[(first + 1)..(last + 1)];
            var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(script)));
            var sessionId = result.ChatRunId ?? result.RunId;
            var prior = fileWorkSandboxes.ListRuns(sessionId).Select(run => run.RunId).ToHashSet();
            // The existing approval handler binds the precise script, language and project.
            // This invocation cannot supply UserConfirmed or fall back to a host interpreter.
            var submission = await InvokeFileWorkFunctionAsync(result, request, round, phase, "session.sandbox.run",
                new { sessionId, conversationId = sessionId, projectId = progress.ProjectId,
                    languageKey = progress.GuestLanguageKey, source = script }, cancellationToken).ConfigureAwait(false);
            if (!submission.Succeeded && submission.Status != "HumanApprovalPending")
                throw new InvalidOperationException(submission.Error ?? submission.Status);
            var approvalStep = new CouncilWorkflowStepDefinition { DisplayName = definition.DisplayName, WaitForDeferredApprovalsBeforeNextStep = true };
            await WaitForDeferredApprovalsBeforeNextStepAsync(result, request, approvalStep, round, phase, false,
                cancellationToken).ConfigureAwait(false);
            var policy = runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, policy.GuestCompletionWaitSeconds)));
            while (!deadline.IsCancellationRequested)
            {
                var matching = fileWorkSandboxes.ListRuns(sessionId).Where(run => !prior.Contains(run.RunId) &&
                    run.ProjectId == progress.ProjectId && run.LanguageKey == progress.GuestLanguageKey &&
                    run.SourceSha256 == sourceHash).ToList();
                if (matching.Count > 1) throw new InvalidOperationException("More than one isolated receipt matches this assignment; refusing ambiguous output.");
                var guest = matching.SingleOrDefault();
                if (guest is not null)
                {
                    ownedRun = guest.RunId;
                    if (guest.CompletedUtc.HasValue)
                    {
                        var diagnostic = guest.Status == "Completed" && guest.ExitCode == 0 && !guest.OutputTruncated
                            ? null : $"Guest execution did not produce a complete file: {guest.Status}; exit={guest.ExitCode}; truncated={guest.OutputTruncated}; {guest.Diagnostic}";
                        var receipt = await codeGenerationWorkflow.CompleteDraftFileAsync(result.RunId, packet.Assignment,
                            packet.Assignment.ModelName, string.Empty, diagnostic, cancellationToken, guest).ConfigureAwait(false);
                        request.ProgressMessage?.Invoke(JsonSerializer.Serialize(new { receipt.RelativePath, receipt.GuestRunId, receipt.Sha256, receipt.Diagnostic }));
                        return;
                    }
                }
                // If the approval was declined, no guest starts. Do not loop for an absent job.
                if (guest is null && !await deferredDxAiInvocations.HasPendingApprovalForRunAsync(result.RunId, cancellationToken, "session.sandbox.run").ConfigureAwait(false))
                {
                    var deferred = await deferredDxAiInvocations.GetForRunAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                    if (!deferred.Any(item => item.FunctionName == "session.sandbox.run" && item.Status == vocabulary.Get().DeferredExecuting))
                        throw new InvalidOperationException("No approved isolated execution receipt was created for this script.");
                }
                await Task.Delay(TimeSpan.FromSeconds(1), deadline.Token).ConfigureAwait(false);
            }
            throw new TimeoutException("The isolated file-generation receipt did not finish within the configured deadline.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (ownedRun.HasValue) fileWorkSandboxes.Stop(ownedRun.Value);
            throw;
        }
        catch (Exception exception)
        {
            if (ownedRun.HasValue) fileWorkSandboxes.Stop(ownedRun.Value);
            logger.LogWarning(exception, "Council {RunId} guest file generation did not complete for {AssignmentId}.", result.RunId, packet.Assignment.Id);
            await codeGenerationWorkflow.CompleteDraftFileAsync(result.RunId, packet.Assignment, packet.Assignment.ModelName,
                string.Empty, exception.Message, cancellationToken).ConfigureAwait(false);
        }
    }
}
