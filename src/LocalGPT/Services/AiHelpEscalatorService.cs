using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Durable task-scoped bounded recovery state machine. Decisions coordinate existing tools; they never
/// invoke the failed action, bypass approval, or convert an LLM claim into a verified requirement.
/// The task state survives Blazor circuit disconnection and process restart.
/// </summary>
/// <param name="paths">Canonical per-user application data layout.</param>
/// <param name="policy">Editable seeded runtime recovery limits and text.</param>
/// <param name="scopeFactory">Creates a scoped independent provider curator.</param>
/// <param name="human">Existing durable Human Collaboration inbox and decision authority.</param>
/// <param name="dbContextFactory">Scoped reads of exact persisted Human Collaboration answers.</param>
/// <param name="ambient">Actual Council execution context, never invented by a model.</param>
/// <param name="vocabulary">Persisted human status strings.</param>
/// <param name="logger">Redacted diagnostics.</param>
public sealed class AiHelpEscalatorService(
    ILocalGptApplicationPathService paths,
    ILocalGptRuntimePolicyDataService policy,
    IServiceScopeFactory scopeFactory,
    IHumanCollaborationService human,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IAmbientLocalGptContext ambient,
    ILocalGptVocabularyService vocabulary,
    ILogger<AiHelpEscalatorService> logger) : IAiHelpEscalatorService
{
    /// <summary>Serializes reads and atomic writes of the shared persistent state.</summary>
    private readonly SemaphoreSlim storageGate = new(1, 1);
    /// <summary>Prevents concurrent updates or duplicate escalations for the same logical task.</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> taskGates = new(StringComparer.Ordinal);
    /// <summary>In-memory copy is owned solely by this singleton service and serialized on disk.</summary>
    private Dictionary<string, AiHelpTaskState>? tasks;

    /// <summary>Canonical on-disk recovery ledger, never controlled by task IDs or model paths.</summary>
    private string StateFile => Path.Combine(paths.GetLayout().RuntimeDirectory, "ai-help-escalations.json");

    /// <summary>Returns the last persisted decision without scheduling any work.</summary>
    /// <param name="taskId">Stable task identifier.</param>
    /// <param name="cancellationToken">Read cancellation.</param>
    /// <returns>Persisted decision, or null.</returns>
    public async Task<AiHelpRecoveryDecision?> InspectAsync(string taskId, CancellationToken cancellationToken = default)
    {
        ValidateTaskId(taskId);
        await storageGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return tasks!.TryGetValue(taskId, out var task) ? task.LastDecision : null;
        }
        finally { storageGate.Release(); }
    }

    /// <summary>Records one observed outcome; never automatically executes a tool or retries a Council run.</summary>
    /// <param name="report">Attempt with bounded untrusted provenance.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Stable next action which the orchestrator/agent must honor.</returns>
    public async Task<AiHelpRecoveryDecision> ReportAttemptAsync(AiHelpAttemptReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ValidateTaskId(report.TaskId);
        if (string.IsNullOrWhiteSpace(report.Task) || report.Task.Length > 2000 ||
            string.IsNullOrWhiteSpace(report.Action) || report.Action.Length > 500 ||
            (report.FunctionName?.Length ?? 0) > 160 || (report.FailedModelSelectionKey?.Length ?? 0) > 160 ||
            (report.Error?.Message?.Length ?? 0) > 2000 || (report.Error?.Code?.Length ?? 0) > 120 ||
            (report.UserQuestion?.Length ?? 0) > 1000 ||
            report.Status is not ("success" or "failed" or "no_progress" or "needs_input"))
            throw new ArgumentException("Task/action and evidence must be nonempty and within configured boundary limits.", nameof(report));
        var taskGate = taskGates.GetOrAdd(report.TaskId, _ => new SemaphoreSlim(1, 1));
        await taskGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(report.TaskId, cancellationToken).ConfigureAwait(false)
                        ?? new AiHelpTaskState { TaskId = report.TaskId };
            // Awaiting a human response is authoritative even if a model claims completion.
            // A terminal halt also cannot be reset merely by reporting invented success.
            if (state.LastDecision.Type is "await_user" or "halt" or "completed")
                return state.LastDecision;
            state.RecentAttemptIds ??= [];
            // Replayed DXFunction/HTTP calls may arrive out of order. Remember a bounded
            // set of attempt IDs, not only the most recent ID, so older replays cannot
            // inflate failures or trigger a second curator/human escalation.
            if (report.AttemptId is Guid attemptId &&
                (state.LastAttemptId == attemptId || state.RecentAttemptIds.Contains(attemptId)))
                return state.LastDecision;
            state.LastAttemptId = report.AttemptId;
            if (report.AttemptId is Guid newAttemptId)
            {
                state.RecentAttemptIds.Add(newAttemptId);
                if (state.RecentAttemptIds.Count > 32)
                    state.RecentAttemptIds.RemoveRange(0, state.RecentAttemptIds.Count - 32);
            }
            if (report.Status == "success")
            {
                // A reported success only closes recovery coordination, not artifact verification.
                state.LastDecision = new AiHelpRecoveryDecision { TaskId = report.TaskId, Type = "completed" };
                await SaveStateAsync(state).ConfigureAwait(false);
                return state.LastDecision;
            }

            // Do not permit another failed attempt after a curator-guided retry has already failed.
            if (state.CuratorUsed && state.LastDecision.Type == "retry_with_guidance")
                return await RequestUserAsync(state, report, "A curator-guided attempt failed; an operator must select a new approach.", cancellationToken).ConfigureAwait(false);

            state.Failures++;
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{report.Action}|{report.Error?.Code}|{report.Status}")));
            state.Repeated = string.Equals(state.LastFingerprint, fingerprint, StringComparison.Ordinal) ? state.Repeated + 1 : 1;
            state.LastFingerprint = fingerprint;
            // No caller-supplied 'validator' or 'tool' string is by itself proof that a required
            // parameter is actually required. The curator has no authority to make it required.
            var threshold = Math.Clamp(policy.GetInt(LocalGptRuntimeValue.HelpEscalatorUnguidedFailureLimit), 1, 5);
            if (!state.CuratorUsed && report.Status != "no_progress" &&
                state.Failures < threshold && state.Repeated < 2)
            {
                state.LastDecision = new AiHelpRecoveryDecision
                {
                    TaskId = report.TaskId,
                    Type = "try_alternative",
                    Instruction = policy.GetString(LocalGptRuntimeValue.HelpEscalatorAlternativeInstruction)
                };
                await SaveStateAsync(state).ConfigureAwait(false);
                return state.LastDecision;
            }
            if (state.CuratorUsed)
                return await RequestUserAsync(state, report, "The single curator consultation was consumed; a human decision is required.", cancellationToken).ConfigureAwait(false);

            // Persist the curator-used bit BEFORE the external model call. A crash or timeout may
            // require a human, but must never replay unbounded curator calls after restart.
            state.CuratorUsed = true;
            state.LastDecision = new AiHelpRecoveryDecision { TaskId = report.TaskId, Type = "halt", Reason = "Curator consultation was interrupted; check state before resuming." };
            await SaveStateAsync(state).ConfigureAwait(false);
            AiHelpCuratorAdvice advice;
            using (var scope = scopeFactory.CreateScope())
                advice = await scope.ServiceProvider.GetRequiredService<IAiHelpCuratorService>()
                    .DiagnoseAsync(report, state.Failures, cancellationToken).ConfigureAwait(false);
            if (advice.Type == "retry" && !string.IsNullOrWhiteSpace(advice.Instruction) &&
                !string.Equals(advice.Instruction.Trim(), report.Action.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                state.LastDecision = new AiHelpRecoveryDecision
                {
                    TaskId = report.TaskId, Type = "retry_with_guidance", Instruction = advice.Instruction
                };
                await SaveStateAsync(state).ConfigureAwait(false);
                return state.LastDecision;
            }
            var question = advice.Type == "ask_user" && !string.IsNullOrWhiteSpace(advice.Question)
                ? advice.Question
                : "The assigned action remains blocked after one curator consultation. Which different approach should LocalGPT take?";
            return await RequestUserAsync(state, report, question, cancellationToken).ConfigureAwait(false);
        }
        finally { taskGate.Release(); }
    }

    /// <summary>Checks an exact durable inbox question; a pending question cannot trigger further work.</summary>
    /// <param name="taskId">Stable task identifier.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Await-user while pending; a bounded new alternative only after an actual response.</returns>
    public async Task<AiHelpRecoveryDecision?> CheckHumanResponseAsync(string taskId, CancellationToken cancellationToken = default)
    {
        ValidateTaskId(taskId);
        var taskGate = taskGates.GetOrAdd(taskId, _ => new SemaphoreSlim(1, 1));
        await taskGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(taskId, cancellationToken).ConfigureAwait(false);
            if (state is null || state.LastDecision.Type != "await_user")
                return state?.LastDecision;
            if (state.LastDecision.HumanRequestId is not Guid requestId)
            {
                // A crash between writing the awaiting state and enqueue confirmation is
                // not proof that the human received a question. Fail closed, never spin.
                state.LastDecision = new AiHelpRecoveryDecision { TaskId = taskId, Type = "halt", Reason = "Human escalation receipt was not persisted; inspect the inbox before restarting this task." };
                await SaveStateAsync(state).ConfigureAwait(false);
                return state.LastDecision;
            }
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var item = await db.HumanCollaborationRequests.AsNoTracking()
                .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken).ConfigureAwait(false);
            if (item is null)
            {
                state.LastDecision = new AiHelpRecoveryDecision { TaskId = taskId, Type = "halt", Reason = "The persisted Human Collaboration request is missing; inspect the inbox before resuming." };
                await SaveStateAsync(state).ConfigureAwait(false);
                return state.LastDecision;
            }
            if (item.Status == vocabulary.Get().HumanStatusPending)
                return state.LastDecision;
            // The human response is coordination guidance, NOT an authorization to execute an
            // unrelated consequential tool. That tool must still pass its ordinary approval gate.
            if (item.Status != vocabulary.Get().HumanStatusAnswered || string.IsNullOrWhiteSpace(item.UserResponse))
            {
                state.LastDecision = new AiHelpRecoveryDecision { TaskId = taskId, Type = "halt", Reason = "Human question resolved without actionable guidance." };
            }
            else
            {
                state.LastDecision = new AiHelpRecoveryDecision
                {
                    TaskId = taskId, Type = "try_alternative",
                    Instruction = item.UserResponse[..Math.Min(1600, item.UserResponse.Length)]
                };
                state.CuratorUsed = false;
                state.Failures = 0;
                state.Repeated = 0;
                state.LastFingerprint = string.Empty;
            }
            await SaveStateAsync(state).ConfigureAwait(false);
            return state.LastDecision;
        }
        finally { taskGate.Release(); }
    }

    /// <summary>Creates an idempotent persistent human request in the existing collaboration inbox.</summary>
    /// <param name="state">Owning task state.</param>
    /// <param name="report">The blocked attempt.</param>
    /// <param name="question">One specific question.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Pending human response or terminal delivery error.</returns>
    private async Task<AiHelpRecoveryDecision> RequestUserAsync(AiHelpTaskState state, AiHelpAttemptReport report, string question, CancellationToken cancellationToken)
    {
        question = question[..Math.Min(1000, question.Length)].Trim();
        var correlation = "ai-help:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(report.TaskId)));
        // Establish a stable awaiting state before external enqueue; duplicate requests are deduplicated
        // by the HumanCollaboration service's existing correlation/operation contract.
        state.LastDecision = new AiHelpRecoveryDecision { TaskId = report.TaskId, Type = "await_user", Question = question };
        await SaveStateAsync(state).ConfigureAwait(false);
        try
        {
            var context = ambient.Current;
            var receipt = await human.AuthorizeOrEnqueueAsync(new HumanApprovalRequestSpec(
                correlation, "ai.help.escalate", "AI help required", question, "Low", "AiHelpEscalatorService",
                context.ActorDisplayName, "Human collaborator", context.CouncilRunId,
                Math.Max(0, context.CouncilRound), RequiredBeforeCompletion: false,
                IsSensitive: false, RequestKind: vocabulary.Get().HumanRequestGuidance,
                QuestionScope: "Member", GateMode: "NextPhase",
                RequestedCouncilRound: Math.Max(0, context.CouncilRound), RequestedCouncilPhase: context.Phase),
                directHumanConfirmation: false, cancellationToken).ConfigureAwait(false);
            if (receipt.RequestId is not Guid id)
                state.LastDecision = new AiHelpRecoveryDecision { TaskId = report.TaskId, Type = "halt", Reason = "Human inbox could not persist the escalation." };
            else
                state.LastDecision.HumanRequestId = id;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            logger.LogError("AI help inbox enqueue failed for task digest {TaskDigest}, error type {ErrorType}.", correlation, error.GetType().Name);
            state.LastDecision = new AiHelpRecoveryDecision { TaskId = report.TaskId, Type = "halt", Reason = "Human escalation delivery failed; inspect application diagnostics." };
        }
        await SaveStateAsync(state).ConfigureAwait(false);
        return state.LastDecision;
    }

    /// <summary>Reads one task under the shared file serialization lock.</summary>
    /// <param name="taskId">Validated identifier.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Existing task, or null.</returns>
    private async Task<AiHelpTaskState?> ReadStateAsync(string taskId, CancellationToken cancellationToken)
    {
        await storageGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return tasks!.TryGetValue(taskId, out var state) ? state : null;
        }
        finally { storageGate.Release(); }
    }

    /// <summary>Atomically replaces a small, bounded snapshot in LocalGPT runtime storage.</summary>
    /// <param name="state">Latest state for one task.</param>
    /// <returns>Completion after durable replacement.</returns>
    private async Task SaveStateAsync(AiHelpTaskState state)
    {
        await storageGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(CancellationToken.None).ConfigureAwait(false);
            state.UpdatedAtUtc = DateTime.UtcNow;
            tasks![state.TaskId] = state;
            // Retention limits avoid indefinite growth; pending requests are never evicted.
            var maximum = Math.Clamp(policy.GetInt(LocalGptRuntimeValue.HelpEscalatorMaximumRetainedTasks), 20, 10000);
            foreach (var retired in tasks.Values.Where(item => item.LastDecision.Type != "await_user")
                .OrderByDescending(item => item.UpdatedAtUtc).Skip(maximum).Select(item => item.TaskId).ToArray())
                tasks.Remove(retired);
            var file = StateFile;
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(tasks), CancellationToken.None).ConfigureAwait(false);
                File.Move(temporary, file, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        finally { storageGate.Release(); }
    }

    /// <summary>Loads the only authoritative snapshot once; malformed persistence fails closed.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Completion after loading existing data.</returns>
    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (tasks is not null) return;
        var file = StateFile;
        if (!File.Exists(file)) { tasks = new(StringComparer.Ordinal); return; }
        var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
        tasks = JsonSerializer.Deserialize<Dictionary<string, AiHelpTaskState>>(json)
            ?? throw new InvalidDataException("Persisted AI help ledger is invalid; refusing to reset pending questions.");
    }

    /// <summary>Requires one stable, bounded task identity.</summary>
    /// <param name="taskId">Untrusted caller-provided key.</param>
    private void ValidateTaskId(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId) || taskId.Length > 160)
            throw new ArgumentException("taskId must contain 1..160 characters.", nameof(taskId));
    }
}
