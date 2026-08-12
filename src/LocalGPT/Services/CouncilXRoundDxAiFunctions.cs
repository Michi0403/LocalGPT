using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Reports the X-Round actions granted to the currently executing configured Council step.</summary>
public sealed class CouncilXRoundStatusFunction(
    ICouncilXRoundService xRounds,
    IAmbientLocalGptContext ambientContext,
    ILogger<CouncilXRoundStatusFunction> logger) : IDxAiFunctionHandler
{
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.x.status", "POST", "/api/dxai/functions/council.x.status/invoke",
        "Returns the first-class X-Round control policy active for the current configured Council step.",
        "No parameters.", "Read-only. It cannot change Council control flow.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <inheritdoc />
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ambient = ambientContext.Current;
            if (ambient.CouncilRunId is not Guid runId)
                return Task.FromResult(Failed("No active Council run owns this invocation."));
            var context = xRounds.GetActive(runId, ambient.CouncilRound, ambient.Phase);
            return Task.FromResult(context is null
                ? Failed("The current Council step has no active X-Round policy.")
                : Completed(context));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reading Council X-Round status failed.");
            return Task.FromResult(Failed("Council X-Round status could not be read. Review LocalGPT logs."));
        }
    }

    /// <summary>Builds a successful DXFunction result.</summary>
    private DxAiFunctionInvocationResult Completed(object value)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built a completed Council X-Round status result.");
            return new() { Succeeded = true, Status = "Completed", Value = value };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building a completed Council X-Round status result failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a failed DXFunction result.</summary>
    private DxAiFunctionInvocationResult Failed(string error)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built a rejected Council X-Round DXFunction result.");
            return new() { Status = "Failed", Error = error };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building a rejected Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
}

/// <summary>Requests reconsideration or deliberate re-execution of a configured Council workflow step without erasing prior revisions.</summary>
public sealed class CouncilXRoundRevisitFunction(
    ICouncilXRoundService xRounds,
    IAmbientLocalGptContext ambientContext,
    ILogger<CouncilXRoundRevisitFunction> logger) : IDxAiFunctionHandler
{
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.x.revisit", "POST", "/api/dxai/functions/council.x.revisit/invoke",
        "Requests an X-Round jump to another configured workflow step. Reconsider is reasoning-only; reexecute deliberately permits the target step's normal DX/organic policy.",
        "targetStepKey, mode=reconsider|reexecute, reason.", "The request is accepted only while the current step explicitly grants revisit authority; configured transition and human gates still apply.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["reason"],"properties":{"targetStepKey":{"type":"string"},"mode":{"type":"string","enum":["reconsider","reexecute"]},"reason":{"type":"string"}},"additionalProperties":false}
        """);

    /// <inheritdoc />
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = request.Parameters;
            var target = ReadString(parameters, "targetStepKey");
            var reason = ReadString(parameters, "reason");
            if (string.IsNullOrWhiteSpace(reason))
                return Task.FromResult(Failed("reason is required so the X-Round jump remains auditable."));
            var mode = ReadString(parameters, "mode");
            var action = mode.Equals("reexecute", StringComparison.OrdinalIgnoreCase)
                ? CouncilXRoundAction.ReexecuteStep
                : CouncilXRoundAction.ReconsiderStep;
            return Task.FromResult(Completed(xRounds.Request(ambientContext.Current, action, target, reason)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Council X-Round revisit request was rejected.");
            return Task.FromResult(Failed(ex.Message));
        }
    }

    /// <summary>Reads one optional string property from function JSON.</summary>
    private string ReadString(JsonElement parameters, string name)
    {
        try
        {
            var result = parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() ?? string.Empty : string.Empty;
            System.Diagnostics.Trace.TraceInformation($"Read X-Round function string parameter '{name}'.");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Reading X-Round function parameter '{name}' failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a successful DXFunction result.</summary>
    private DxAiFunctionInvocationResult Completed(object value)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built an accepted Council X-Round DXFunction result.");
            return new() { Succeeded = true, Status = "Accepted", Value = value };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building an accepted Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a failed DXFunction result.</summary>
    private DxAiFunctionInvocationResult Failed(string error)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built a rejected Council X-Round DXFunction result.");
            return new() { Status = "Failed", Error = error };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building a rejected Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
}

/// <summary>Returns an explicit text result from a configured Council X-Round and completes the parent workflow.</summary>
public sealed class CouncilXRoundReturnTextFunction(
    ICouncilXRoundService xRounds,
    IAmbientLocalGptContext ambientContext,
    ILogger<CouncilXRoundReturnTextFunction> logger) : IDxAiFunctionHandler
{
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.x.return_text", "POST", "/api/dxai/functions/council.x.return_text/invoke",
        "Returns explicit text from the current X-Round to the parent Council workflow and requests clean workflow completion.",
        "text and optional reason.", "Available only when the current configured step grants X-Round text-return authority.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["text"],"properties":{"text":{"type":"string"},"reason":{"type":"string"}},"additionalProperties":false}""");

    /// <inheritdoc />
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = ReadString(request.Parameters, "text");
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(Failed("text is required."));
            var directive = xRounds.Request(
                ambientContext.Current,
                CouncilXRoundAction.ReturnText,
                reason: ReadString(request.Parameters, "reason"),
                text: text);
            return Task.FromResult(Completed(directive));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Council X-Round text-return request was rejected.");
            return Task.FromResult(Failed(ex.Message));
        }
    }

    /// <summary>Reads one optional string property from function JSON.</summary>
    private string ReadString(JsonElement parameters, string name)
    {
        try
        {
            var result = parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() ?? string.Empty : string.Empty;
            System.Diagnostics.Trace.TraceInformation($"Read X-Round function string parameter '{name}'.");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Reading X-Round function parameter '{name}' failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a successful DXFunction result.</summary>
    private DxAiFunctionInvocationResult Completed(object value)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built an accepted Council X-Round DXFunction result.");
            return new() { Succeeded = true, Status = "Accepted", Value = value };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building an accepted Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a failed DXFunction result.</summary>
    private DxAiFunctionInvocationResult Failed(string error)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built a rejected Council X-Round DXFunction result.");
            return new() { Status = "Failed", Error = error };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building a rejected Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
}

/// <summary>Starts one bounded single-model X-Function subtask and returns its result into the parent workflow.</summary>
public sealed class CouncilXRoundStartSingleModelFunction(
    ICouncilXRoundService xRounds,
    IAmbientLocalGptContext ambientContext,
    ILogger<CouncilXRoundStartSingleModelFunction> logger) : IDxAiFunctionHandler
{
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.x.start_single_model", "POST", "/api/dxai/functions/council.x.start_single_model/invoke",
        "Requests one selected Council model as a bounded derived X-Function subtask; its visible result is fed back into the parent workflow.",
        "prompt and optional provider-qualified modelName.", "Available only when the current configured step grants single-model X authority. The requested model must already belong to the parent Council.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["prompt"],"properties":{"prompt":{"type":"string"},"modelName":{"type":"string"},"reason":{"type":"string"}},"additionalProperties":false}""");

    /// <inheritdoc />
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var prompt = ReadString(request.Parameters, "prompt");
            if (string.IsNullOrWhiteSpace(prompt))
                return Task.FromResult(Failed("prompt is required."));
            var directive = xRounds.Request(
                ambientContext.Current,
                CouncilXRoundAction.StartSingleModel,
                reason: ReadString(request.Parameters, "reason"),
                prompt: prompt,
                modelName: ReadString(request.Parameters, "modelName"));
            return Task.FromResult(Completed(directive));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Council X-Round single-model request was rejected.");
            return Task.FromResult(Failed(ex.Message));
        }
    }

    /// <summary>Reads one optional string property from function JSON.</summary>
    private string ReadString(JsonElement parameters, string name)
    {
        try
        {
            var result = parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() ?? string.Empty : string.Empty;
            System.Diagnostics.Trace.TraceInformation($"Read X-Round function string parameter '{name}'.");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Reading X-Round function parameter '{name}' failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a successful DXFunction result.</summary>
    private DxAiFunctionInvocationResult Completed(object value)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built an accepted Council X-Round DXFunction result.");
            return new() { Succeeded = true, Status = "Accepted", Value = value };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building an accepted Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a failed DXFunction result.</summary>
    private DxAiFunctionInvocationResult Failed(string error)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built a rejected Council X-Round DXFunction result.");
            return new() { Status = "Failed", Error = error };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building a rejected Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
}

/// <summary>Starts another configured Council team as a bounded X-Function subtask and returns its result into the parent workflow.</summary>
public sealed class CouncilXRoundStartCouncilFunction(
    ICouncilXRoundService xRounds,
    IAmbientLocalGptContext ambientContext,
    ILogger<CouncilXRoundStartCouncilFunction> logger) : IDxAiFunctionHandler
{
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.x.start_council", "POST", "/api/dxai/functions/council.x.start_council/invoke",
        "Requests another configured Council team as a derived X-Function subtask; the child final text returns to the parent workflow with its own immutable run identity.",
        "prompt and optional teamKey.", "Available only when the current configured step grants child-Council X authority. Nested X-Council depth is bounded by the runtime.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["prompt"],"properties":{"prompt":{"type":"string"},"teamKey":{"type":"string"},"reason":{"type":"string"}},"additionalProperties":false}""");

    /// <inheritdoc />
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var prompt = ReadString(request.Parameters, "prompt");
            if (string.IsNullOrWhiteSpace(prompt))
                return Task.FromResult(Failed("prompt is required."));
            var directive = xRounds.Request(
                ambientContext.Current,
                CouncilXRoundAction.StartCouncil,
                reason: ReadString(request.Parameters, "reason"),
                prompt: prompt,
                teamKey: ReadString(request.Parameters, "teamKey"));
            return Task.FromResult(Completed(directive));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Council X-Round child-Council request was rejected.");
            return Task.FromResult(Failed(ex.Message));
        }
    }

    /// <summary>Reads one optional string property from function JSON.</summary>
    private string ReadString(JsonElement parameters, string name)
    {
        try
        {
            var result = parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() ?? string.Empty : string.Empty;
            System.Diagnostics.Trace.TraceInformation($"Read X-Round function string parameter '{name}'.");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Reading X-Round function parameter '{name}' failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a successful DXFunction result.</summary>
    private DxAiFunctionInvocationResult Completed(object value)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built an accepted Council X-Round DXFunction result.");
            return new() { Succeeded = true, Status = "Accepted", Value = value };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building an accepted Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
    /// <summary>Builds a failed DXFunction result.</summary>
    private DxAiFunctionInvocationResult Failed(string error)
    {
        try
        {
            System.Diagnostics.Trace.TraceInformation("Built a rejected Council X-Round DXFunction result.");
            return new() { Status = "Failed", Error = error };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building a rejected Council X-Round result failed: {ex.Message}");
            throw;
        }
    }
}
