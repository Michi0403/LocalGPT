using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Read-only inspection of Python.NET, CPU/GPU and plugin processes by the Council.</summary>
/// <param name="jobs">Python job orchestration service dependency used by the list python jobs function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the list python jobs function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListPythonJobsFunction(IPythonJobOrchestrationService jobs, IDxAiFunctionJsonService json, ILogger<ListPythonJobsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list python jobs function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListPythonJobsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.python.jobs.list", "POST", "/api/dxai/functions/localai.python.jobs.list/invoke",
        "Inspect the bounded Python task history: embedded GIL lane, isolated plugin processes, state, time and cancellation.",
        "No parameters.", "Read-only and payload-free job state; no code, prompts, device paths or media returned.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", IsCoordinationOnly: true,
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ListPythonJobsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list python jobs function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(json.Success(jobs.ListJobs(100)));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Python job dashboard through DXFunction failed.");
            return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Python task dashboard is unavailable." });
        }
    }
}

/// <summary>Cancellation is consequential; the normal registry's Human Collaboration approval is mandatory.</summary>
/// <param name="jobs">Python job orchestration service dependency used by the cancel python job function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the cancel python job function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CancelPythonJobFunction(IPythonJobOrchestrationService jobs, IDxAiFunctionJsonService json, ILogger<CancelPythonJobFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the cancel python job function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="CancelPythonJobFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.python.jobs.cancel", "POST", "/api/dxai/functions/localai.python.jobs.cancel/invoke",
        "Request cancellation of one monitored, running Python task by its concrete job identifier.",
        "jobId must reference an existing active task.",
        "Stops user work. Requires exact Human Collaboration approval and does not execute arbitrary Python source.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["jobId"],"properties":{"jobId":{"type":"string","format":"uuid"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="CancelPythonJobFunction"/>, keeping the operation consistent with the state and invariants of the surrounding cancel python job function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var binding = json.Bind<CancelPythonJobFunctionRequest>(request.Parameters);
            if (!binding.Succeeded) return Task.FromResult(json.InvalidParameters(binding.Error));
            return Task.FromResult(json.Success(new { binding.Value.JobId, CancelRequested = jobs.CancelJob(binding.Value.JobId) }));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cancelling Python task through approved DXFunction failed.");
            return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Python task cancellation failed." });
        }
    }
}

/// <summary>Loc may prepare real persisted plugin source, but only an approved function may save it.</summary>
/// <param name="plugins">Runtime plugin service dependency used by the save runtime plugin function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the save runtime plugin function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SaveRuntimePluginFunction(IRuntimePluginService plugins, IDxAiFunctionJsonService json, ILogger<SaveRuntimePluginFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the save runtime plugin function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="SaveRuntimePluginFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "runtime.plugins.save", "POST", "/api/dxai/functions/runtime.plugins.save/invoke",
        "Save a real C#, JavaScript or Python RuntimePluginDefinition in the existing database; never executes the proposed code.",
        "plugin must include a plugin.* functionName, purpose, kind, parameterSchemaJson, sourceCode and approved compilerInstallationId for script kinds.",
        "Persists executable source. Human approval is mandatory; code execution additionally requires approved Build/Load and normal function invocation approval.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true,
        Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["plugin"],"properties":{"plugin":{"type":"object","required":["name","functionName","purpose","kind","sourceCode","parameterSchemaJson"],"properties":{"id":{"type":"string"},"name":{"type":"string","maxLength":200},"functionName":{"type":"string","minLength":8,"maxLength":120},"purpose":{"type":"string","maxLength":2000},"kind":{"type":"integer","enum":[0,1,3]},"sourceCode":{"type":"string","minLength":1},"parameterSchemaJson":{"type":"string"},"compilerInstallationId":{"type":["string","null"]},"isEnabled":{"type":"boolean"},"availableToAi":{"type":"boolean"},"isReadOnly":{"type":"boolean"},"requiresHumanConfirmation":{"type":"boolean"},"supportsAutomaticInvocation":{"type":"boolean"}},"additionalProperties":false}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="SaveRuntimePluginFunction"/>, keeping the operation consistent with the state and invariants of the surrounding save runtime plugin function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<SaveRuntimePluginRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            if (binding.Value.Plugin.Kind is RuntimePluginKind.CSharpScript or RuntimePluginKind.JavaScript or RuntimePluginKind.Python)
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "UseProjectSource", Error = "Script source belongs to a tracked project file. Use project.source.navigate and project.source.publish-plugin after review and human approval." };
            binding.Value.UserConfirmed = true; // central registry has already obtained exact-action approval
            var result = await plugins.SaveAsync(binding.Value, cancellationToken).ConfigureAwait(false);
            return json.Success(new { result.Id, result.FunctionName, result.Kind, result.LastBuildStatus, result.ContentHash });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving the user-reviewed RuntimePluginDefinition failed; source omitted.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Approved runtime plugin could not be saved." };
        }
    }
}

/// <summary>Build/Load executes a separate approved action; Save alone never installs or executes user content.</summary>
/// <param name="plugins">Runtime plugin service dependency used by the build runtime plugin function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the build runtime plugin function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class BuildRuntimePluginFunction(IRuntimePluginService plugins, IDxAiFunctionJsonService json, ILogger<BuildRuntimePluginFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the build runtime plugin function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="BuildRuntimePluginFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "runtime.plugins.build", "POST", "/api/dxai/functions/runtime.plugins.build/invoke",
        "Compile or validate one already-saved runtime extension using its configured toolchain.",
        "pluginId is the stable approved runtime plugin identifier.",
        "Compiles or materializes executable code. Requires a second explicit Human Collaboration approval.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["pluginId"],"properties":{"pluginId":{"type":"string","format":"uuid"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="BuildRuntimePluginFunction"/>, keeping the operation consistent with the state and invariants of the surrounding build runtime plugin function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<BuildRuntimePluginFunctionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var result = await plugins.BuildAndLoadAsync(binding.Value.PluginId, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            return json.Success(result);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the approved plugin failed; code and toolchain output omitted.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Approved runtime plugin could not be built or loaded." };
        }
    }
}
