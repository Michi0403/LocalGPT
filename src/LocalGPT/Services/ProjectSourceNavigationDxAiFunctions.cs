using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Loc can discover versioned source, reviewed regex relationships and exact installed compiler evidence.</summary>
/// <param name="navigation">Project source navigation service dependency used by the navigate project source function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the navigate project source function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class NavigateProjectSourceFunction(IProjectSourceNavigationService navigation, IDxAiFunctionJsonService json, ILogger<NavigateProjectSourceFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the navigate project source function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="NavigateProjectSourceFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.source.navigate", "POST", "/api/dxai/functions/project.source.navigate/invoke",
        "Inspect a selected project/revision's scanned files, reviewed hashes, curator-approved related regex patterns, and validated versioned toolchains. Useful for code, literature or media projects.",
        "projectId is required; revisionId and focusTrackedFileId optional; query optional; take 1-100. Only explicitly reviewed patterns affect classification.",
        "Read-only; returns project metadata and curator provenance, never full source text or a compiler compatibility assertion.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["projectId"],"properties":{"projectId":{"type":"string","format":"uuid"},"revisionId":{"type":["string","null"],"format":"uuid"},"focusTrackedFileId":{"type":["string","null"],"format":"uuid"},"query":{"type":"string","maxLength":200},"take":{"type":"integer","minimum":1,"maximum":100}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="NavigateProjectSourceFunction"/>, keeping the operation consistent with the state and invariants of the surrounding navigate project source function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectSourceNavigationRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await navigation.NavigateAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Project source discovery failed; project source and queries omitted.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Project source navigation failed; inspect project selection/curator status." };
        }
    }
}

/// <summary>Only reviewed, hash-pinned project sources can be promoted to a runtime DXFunction after exact-action approval.</summary>
/// <param name="navigation">Project source navigation service dependency used by the publish project script function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the publish project script function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublishProjectScriptFunction(IProjectSourceNavigationService navigation, IDxAiFunctionJsonService json, ILogger<PublishProjectScriptFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the publish project script function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="PublishProjectScriptFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.source.publish-plugin", "POST", "/api/dxai/functions/project.source.publish-plugin/invoke",
        "Publish one existing, scanned, completely reviewed and user-approved project source file into the runtime function catalog.",
        "projectId, trackedFileId and plugin (name, functionName, purpose, kind, schema, compilerInstallationId) required; revisionId optional. SourceCode is loaded from the tracked project file, not AI input.",
        "Consequential, explicitly approved publication only. The source file hash must still match the reviewed revision. Build/load and invocation require separate approval.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["projectId","trackedFileId","plugin"],"properties":{"projectId":{"type":"string","format":"uuid"},"revisionId":{"type":["string","null"],"format":"uuid"},"trackedFileId":{"type":"string","format":"uuid"},"plugin":{"type":"object","required":["name","functionName","purpose","kind","parameterSchemaJson"],"properties":{"id":{"type":"string","format":"uuid"},"name":{"type":"string"},"functionName":{"type":"string"},"purpose":{"type":"string"},"kind":{"type":"integer","enum":[0,1,3]},"parameterSchemaJson":{"type":"string"},"compilerInstallationId":{"type":["string","null"],"format":"uuid"},"availableToAi":{"type":"boolean"},"isReadOnly":{"type":"boolean"},"requiresHumanConfirmation":{"type":"boolean"},"supportsAutomaticInvocation":{"type":"boolean"},"isEnabled":{"type":"boolean"}},"additionalProperties":false}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="PublishProjectScriptFunction"/>, keeping the operation consistent with the state and invariants of the surrounding publish project script function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<PublishTrackedProjectScriptRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true; // the central registry already confirmed this exact invocation
            return json.Success(await navigation.PublishApprovedScriptAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Project script publication failed; source and executable content omitted.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Source publication blocked; check exact hash/revision/review/toolchain and human approval." };
        }
    }
}
