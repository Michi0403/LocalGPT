using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Exposes guest runtime readiness and bounded execution receipts to Chat and Council without invoking code.</summary>
/// <param name="sandboxes">The session-owned Hyperlight backend.</param>
/// <param name="json">DXFunction JSON envelope service.</param>
/// <param name="logger">Diagnostic logger.</param>
public sealed class ListSessionSandboxRunsFunction(
    ISessionSandboxExecutionService sandboxes,
    IDxAiFunctionJsonService json,
    ILogger<ListSessionSandboxRunsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Read-only Hyperlight guest status and receipt discovery.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "session.sandbox.runs", "POST", "/api/dxai/functions/session.sandbox.runs/invoke",
        "Lists actual provisioned Hyperlight guests and recent persisted run receipts, including stdout, stderr and exit code.",
        "Optional sessionId and take (1-50); omit sessionId to see recent runs across sessions.",
        "Read-only. Neither reads repository files nor launches a guest.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"sessionId":{"type":["string","null"],"format":"uuid"},"take":{"type":"integer","minimum":1,"maximum":50}},"additionalProperties":false}""");

    /// <summary>Returns run evidence without falsely treating a queued result as completed testing.</summary>
    /// <param name="request">Optional run filter.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The persisted run summaries and current runtime readiness.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var binding = json.Bind<SessionSandboxQueryParameters>(request.Parameters);
            if (!binding.Succeeded) return Task.FromResult(json.InvalidParameters(binding.Error));
            var take = Math.Clamp(binding.Value.Take ?? 20, 1, 50);
            return Task.FromResult(json.Success(new { sandboxes.RuntimeReadiness, sandboxes.Languages,
                Runs = sandboxes.ListRuns(binding.Value.SessionId).Take(take).ToArray() }));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Sandbox receipt lookup cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Sandbox receipt lookup failed."); return Task.FromResult(new DxAiFunctionInvocationResult { Status = "Failed", Error = "Sandbox receipts could not be read." }); }
    }
}

/// <summary>Submits one human-approved C#, Python or PowerShell snippet to the real Hyperlight backend.</summary>
/// <param name="sandboxes">The isolated guest execution service.</param>
/// <param name="json">DXFunction JSON envelope service.</param>
/// <param name="logger">Diagnostic logger.</param>
public sealed class RunSessionSandboxFunction(
    ISessionSandboxExecutionService sandboxes,
    IDxAiFunctionJsonService json,
    ILogger<RunSessionSandboxFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Approval-gated source execution, not an unrestricted host command.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "session.sandbox.run", "POST", "/api/dxai/functions/session.sandbox.run/invoke",
        "Runs submitted Python, C# or PowerShell SOURCE inside the configured Hyperlight micro-VM, not on the host. Returns a queued runId to poll with session.sandbox.runs.",
        "Required languageKey and source; optional sessionId, projectId and conversationId. Source is a single guest snippet, not a mounted repository tree.",
        "Requires fresh human confirmation. No host interpreter, repository mount, network or automatic execution. Does not prove compilation until the completed receipt succeeds.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["languageKey","source"],"properties":{"languageKey":{"type":"string","minLength":1,"maxLength":80},"source":{"type":"string","minLength":1,"maxLength":160000},"sessionId":{"type":["string","null"],"format":"uuid"},"conversationId":{"type":["string","null"],"format":"uuid"},"projectId":{"type":["string","null"],"format":"uuid"}},"additionalProperties":false}""");

    /// <summary>Queues execution only after approval; never claims that a queued run has passed.</summary>
    /// <param name="request">Reviewed invocation including its human-approval state.</param>
    /// <param name="cancellationToken">Cancels submission before queueing.</param>
    /// <returns>A durable run identifier to poll for completion.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!request.UserConfirmed)
                return Task.FromResult(new DxAiFunctionInvocationResult { Status = "HumanApprovalRequired", Error = "Fresh approval is required for Hyperlight execution." });
            var binding = json.Bind<SessionSandboxDxRunParameters>(request.Parameters);
            if (!binding.Succeeded) return Task.FromResult(json.InvalidParameters(binding.Error));
            var p = binding.Value;
            var sessionId = p.SessionId ?? p.ConversationId ?? request.ConversationId ?? Guid.NewGuid();
            var run = sandboxes.Start(new SessionSandboxRunRequest(sessionId, p.ConversationId ?? request.ConversationId,
                p.ProjectId ?? request.ProjectId, p.LanguageKey, p.Source, "Manual", UserConfirmed: true));
            return Task.FromResult(json.Success(new { run.RunId, run.SessionId, run.Runtime, run.Status,
                run.SourceSha256, Message = "Queued in Hyperlight; use session.sandbox.runs to retrieve the final result before reporting tests as passed." }));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Sandbox guest submission cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Approved Hyperlight submission failed; source omitted from logs."); return Task.FromResult(new DxAiFunctionInvocationResult { Status = "Failed", Error = exception.Message }); }
    }
}

/// <summary>Optional read-only run query.</summary>
public sealed class SessionSandboxQueryParameters
{
    /// <summary>Limit results to a session.</summary>
    public Guid? SessionId { get; set; }
    /// <summary>Maximum number of returned receipts.</summary>
    public int? Take { get; set; }
}

/// <summary>Approved source-only Hyperlight invocation, with context assigned by the registry or the caller.</summary>
public sealed class SessionSandboxDxRunParameters
{
    /// <summary>Selected installed guest language key.</summary>
    public string LanguageKey { get; set; } = string.Empty;
    /// <summary>Guest source snippet.</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>Optional stable session.</summary>
    public Guid? SessionId { get; set; }
    /// <summary>Optional conversation identifier.</summary>
    public Guid? ConversationId { get; set; }
    /// <summary>Optional linked project identifier.</summary>
    public Guid? ProjectId { get; set; }
}
