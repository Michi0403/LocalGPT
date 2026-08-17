using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Returns the bounded shared local console feed without executing a command.</summary>
/// <param name="console">Shared console command service.</param>
/// <param name="json">DXFunction JSON result service.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
public sealed class ConsoleHistoryFunction(IConsoleCommandService console, IDxAiFunctionJsonService json, ILogger<ConsoleHistoryFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the read-only console history capability.</summary>
    /// <value>The descriptor value exposed by <see cref="ConsoleHistoryFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.console.history", "POST", "/api/dxai/functions/localgpt.console.history/invoke",
        "Returns recent bounded stdout/stderr/system events from LocalGPT's shared ASCII command console.",
        "Optional take limits returned events.",
        "Read-only. Command arguments are not reconstructed from logs.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"take":{"type":"integer","minimum":1,"maximum":400}},"additionalProperties":false}""");

    /// <summary>Returns recent console output through the standard DXFunction envelope.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var take = 120;
            if (request.Parameters.ValueKind == System.Text.Json.JsonValueKind.Object
                && request.Parameters.TryGetProperty("take", out var takeValue))
                take = takeValue.TryGetInt32(out var parsed) ? parsed : 120;
            return Task.FromResult(json.Success(console.GetRecentOutput(take)));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Reading console history DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Reading console history DXFunction failed."); return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Console history could not be read. Review LocalGPT logs." }); }
    }
}

/// <summary>Runs one exact local command only after the normal DXFunction human-confirmation gate approves it.</summary>
/// <param name="console">Shared cross-platform console service.</param>
/// <param name="json">DXFunction parameter/result service.</param>
/// <param name="logger">Logger used for diagnostics without command content.</param>
public sealed class ExecuteConsoleCommandFunction(IConsoleCommandService console, IDxAiFunctionJsonService json, ILogger<ExecuteConsoleCommandFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the consequential generic console execution capability.</summary>
    /// <value>The descriptor value exposed by <see cref="ExecuteConsoleCommandFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.console.execute", "POST", "/api/dxai/functions/localgpt.console.execute/invoke",
        "Executes one reviewed local Direct, PowerShell, Bash, or cmd command and streams bounded output to LocalGPT's shared ASCII command console.",
        "Provide displayName, shell and either commandText or executable plus arguments. workingDirectory/environment/timeoutSeconds are optional.",
        "Consequential local command execution. Requires fresh human confirmation; never eligible for automatic invocation and never elevates privileges.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["displayName","shell"],"properties":{"displayName":{"type":"string","maxLength":120},"shell":{"type":"string","enum":["Auto","Direct","PowerShell","Bash","Cmd"]},"commandText":{"type":"string","maxLength":16000},"executable":{"type":"string","maxLength":1024},"arguments":{"type":"array","maxItems":128,"items":{"type":"string","maxLength":4096}},"workingDirectory":{"type":"string","maxLength":2048},"environment":{"type":"array","maxItems":64,"items":{"type":"object","required":["name","value"],"properties":{"name":{"type":"string","maxLength":128},"value":{"type":"string","maxLength":8192},"source":{"type":"string","maxLength":120},"isEnabled":{"type":"boolean"}},"additionalProperties":false}},"timeoutSeconds":{"type":"integer","minimum":1,"maximum":600}},"additionalProperties":false}""");

    /// <summary>Executes the bound request after registry approval.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalConsoleCommandRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.IsReadOnly = false;
            binding.Value.UserConfirmed = true;
            return json.Success(await console.ExecuteAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Console execution DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Console execution DXFunction failed; command content was omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "The local console command failed. Review LocalGPT logs." }; }
    }
}
