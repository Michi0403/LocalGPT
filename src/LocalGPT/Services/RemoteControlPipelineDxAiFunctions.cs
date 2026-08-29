using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text;

namespace LocalGPT.Services;

/// <summary>Lists user-authored Remote Control action pipelines.</summary>
/// <param name="pipelines">Remote Control pipeline service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class ListRemoteControlPipelinesFunction(IRemoteControlPipelineService pipelines, IDxAiFunctionJsonService json, ILogger<ListRemoteControlPipelinesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list remote control pipelines function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListRemoteControlPipelinesFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.remote_control.pipeline.list", "POST", "/api/dxai/functions/localgpt.remote_control.pipeline.list/invoke",
        "Lists user-authored Remote Control pipelines that compose existing DXFunctions or enabled public-service catalog methods.", "No parameters.",
        "Read-only local configuration metadata. Pipeline steps cannot recursively invoke Remote Control control-plane functions.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ListRemoteControlPipelinesFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list remote control pipelines function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await pipelines.ListAsync(cancellationToken).ConfigureAwait(false);
            var result = rows.Select(item => new
            {
                item.Key, item.DisplayName, item.Description, item.ConnectorKey, item.Triggers, item.IsEnabled,
                StepCount = pipelines.ParseSteps(item.StepsJson).Count,
                Targets = pipelines.ParseSteps(item.StepsJson).Select(step => string.IsNullOrWhiteSpace(step.TargetCatalogKey) ? step.FunctionName : step.TargetCatalogKey).ToArray(),
                item.LastAttemptUtc, item.LastSuccessUtc, item.LastStatus, item.LastError
            }).ToList();
            return json.Success(result);
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing Remote Control pipelines through DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing Remote Control pipelines through DXFunction failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Remote Control pipelines could not be listed. Review LocalGPT logs." }; }
    }
}

/// <summary>Lists enabled DXFunction Catalog targets that can be wired into user-authored Remote Control action steps.</summary>
/// <param name="pipelines">Remote Control pipeline service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class ListRemoteControlTargetsFunction(IRemoteControlPipelineService pipelines, IDxAiFunctionJsonService json, ILogger<ListRemoteControlTargetsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the persisted descriptor that exposes safe pipeline-target discovery to the AI Council.</summary>
    /// <value>The read-only Remote Control target-discovery DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.remote_control.target.list", "POST", "/api/dxai/functions/localgpt.remote_control.target.list/invoke",
        "Lists enabled DXFunction Catalog and published public-service targets that a user-authored Remote Control pipeline may reference.",
        "No parameters. The result includes catalog keys, function names, safety metadata, purpose and parameter schemas but no invocation values or secrets.",
        "Read-only local catalog discovery. Remote Control control-plane functions are excluded to prevent recursive pipeline construction.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>Returns safe metadata for all currently selectable Remote Control action targets.</summary>
    /// <param name="request">DXFunction invocation request; no parameters are required.</param>
    /// <param name="cancellationToken">Cancellation token for catalog discovery.</param>
    /// <returns>A successful DXFunction result containing safe target metadata, or a bounded failure result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await pipelines.ListTargetsAsync(cancellationToken).ConfigureAwait(false);
            return json.Success(rows.Select(item => new
            {
                item.CatalogKey, item.DisplayName, item.FunctionName, item.Kind, item.Source, item.Purpose, item.IsReadOnly,
                item.RequiresFrontendConfirmation, item.ParameterSchemaJson,
                IsPublicServiceMethod = !string.IsNullOrWhiteSpace(item.ServiceMethodName)
            }).ToList());
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Listing Remote Control action targets through DXFunction was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing Remote Control action targets through DXFunction failed.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Remote Control action targets could not be listed. Review LocalGPT logs." };
        }
    }
}

/// <summary>Saves a user-authored Remote Control action pipeline after human confirmation.</summary>
/// <param name="pipelines">Remote Control pipeline service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class SaveRemoteControlPipelineFunction(IRemoteControlPipelineService pipelines, IDxAiFunctionJsonService json, ILogger<SaveRemoteControlPipelineFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the save remote control pipeline function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="SaveRemoteControlPipelineFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.remote_control.pipeline.save", "POST", "/api/dxai/functions/localgpt.remote_control.pipeline.save/invoke",
        "Creates or updates a Remote Control action pipeline. Each step resolves through the persisted DXFunction Catalog; public-service entries are routed through localgpt.public_service.invoke.",
        "JSON body is a RemoteControlPipelineDefinition. stepsJson contains ordered RemoteControlPipelineStepDefinition values with targetCatalogKey/functionName and argumentsTemplateJson.",
        "Configuration mutation requiring human confirmation. This does not confirm any later consequential action executed by the pipeline.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler", SupportsDeferredApprovalRequest: true,
        ParameterSchemaJson: """{"type":"object","required":["key","displayName","stepsJson"],"properties":{"key":{"type":"string","maxLength":96},"displayName":{"type":"string","maxLength":160},"description":{"type":"string"},"connectorKey":{"type":"string","maxLength":96},"triggers":{"type":["integer","string"]},"stepsJson":{"type":"string"},"isEnabled":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="SaveRemoteControlPipelineFunction"/>, keeping the operation consistent with the state and invariants of the surrounding save remote control pipeline function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<RemoteControlPipelineDefinition>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await pipelines.SaveAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Saving a Remote Control pipeline through DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Saving a Remote Control pipeline through DXFunction failed; templates omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = exception.Message }; }
    }
}

/// <summary>
/// Represents a delete remote control pipeline function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="pipelines">Remote Control pipeline service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class DeleteRemoteControlPipelineFunction(IRemoteControlPipelineService pipelines, IDxAiFunctionJsonService json, ILogger<DeleteRemoteControlPipelineFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the delete remote control pipeline function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="DeleteRemoteControlPipelineFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.remote_control.pipeline.delete", "POST", "/api/dxai/functions/localgpt.remote_control.pipeline.delete/invoke",
        "Deletes one user-authored Remote Control action pipeline.", "JSON parameters: key required.", "Destructive local configuration mutation requiring human confirmation.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler", SupportsDeferredApprovalRequest: true,
        ParameterSchemaJson: """{"type":"object","required":["key"],"properties":{"key":{"type":"string","maxLength":96}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="DeleteRemoteControlPipelineFunction"/>, keeping the operation consistent with the state and invariants of the surrounding delete remote control pipeline function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<RemoteControlKeyParameters>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(new { binding.Value.Key, Deleted = await pipelines.DeleteAsync(binding.Value.Key, cancellationToken).ConfigureAwait(false) });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Deleting a Remote Control pipeline through DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Deleting a Remote Control pipeline through DXFunction failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = exception.Message }; }
    }
}

/// <summary>Executes a Remote Control pipeline against a manual payload while preserving each nested DXFunction's own approval state.</summary>
/// <param name="pipelines">Remote Control pipeline service.</param>
/// <param name="templates">Remote Control payload parser.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class ExecuteRemoteControlPipelineFunction(IRemoteControlPipelineService pipelines, IRemoteControlTemplateService templates, IDxAiFunctionJsonService json, ILogger<ExecuteRemoteControlPipelineFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the execute remote control pipeline function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ExecuteRemoteControlPipelineFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.remote_control.pipeline.execute", "POST", "/api/dxai/functions/localgpt.remote_control.pipeline.execute/invoke",
        "Executes a user-enabled Remote Control pipeline against a supplied payload. Steps invoke existing DXFunctions or the confirmed public-service bridge, never raw service reflection.",
        "JSON parameters: key required; payload optional JSON/text; contentType optional; connectorKey optional.",
        "The pipeline call itself does not broadly confirm nested actions. Every nested DXFunction independently enforces human confirmation and automatic invocation policy.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["key"],"properties":{"key":{"type":"string","maxLength":96},"payload":{"type":"string"},"contentType":{"type":"string","maxLength":160},"connectorKey":{"type":"string","maxLength":96}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ExecuteRemoteControlPipelineFunction"/>, keeping the operation consistent with the state and invariants of the surrounding execute remote control pipeline function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<RemoteControlExecuteParameters>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var payloadText = parameters.Payload ?? string.Empty;
            var payload = new RemoteControlPayload
            {
                ConnectorKey = string.IsNullOrWhiteSpace(parameters.ConnectorKey) ? "manual" : parameters.ConnectorKey,
                Trigger = RemoteControlTriggerKind.Manual,
                ContentType = string.IsNullOrWhiteSpace(parameters.ContentType) ? "application/json" : parameters.ContentType,
                RawText = payloadText,
                PayloadBytes = Encoding.UTF8.GetByteCount(payloadText)
            };
            payload.Json = templates.ParseSelectedJson(payload.RawText, payload.ContentType, RemoteControlResponseFormat.Auto, string.Empty);
            return json.Success(await pipelines.ExecuteAsync(parameters.Key, payload, request.AutomaticInvocation, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Executing a Remote Control pipeline through DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Executing a Remote Control pipeline through DXFunction failed; payload omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = exception.Message }; }
    }
}

/// <summary>Lists bounded Remote Control execution audit rows.</summary>
/// <param name="connectors">Remote Control connector service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class ListRemoteControlHistoryFunction(IRemoteControlConnectorService connectors, IDxAiFunctionJsonService json, ILogger<ListRemoteControlHistoryFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list remote control history function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListRemoteControlHistoryFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.remote_control.history.list", "POST", "/api/dxai/functions/localgpt.remote_control.history.list/invoke",
        "Lists bounded Remote Control pull, webhook, and action-pipeline audit rows without storing or returning full remote payloads.", "JSON parameter: take optional, 1..500.",
        "Read-only local audit metadata.", IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"take":{"type":"integer","minimum":1,"maximum":500}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ListRemoteControlHistoryFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list remote control history function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<RemoteControlHistoryParameters>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await connectors.GetHistoryAsync(binding.Value.Take <= 0 ? 100 : Math.Min(binding.Value.Take, 500), cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing Remote Control history through DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing Remote Control history through DXFunction failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Remote Control history could not be loaded. Review LocalGPT logs." }; }
    }
}

/// <summary>Contains manual Remote Control pipeline execution parameters.</summary>
public sealed class RemoteControlExecuteParameters
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this remote control execute parameters instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="RemoteControlExecuteParameters"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the payload value that forms part of the remote control execute parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The payload value exposed by <see cref="RemoteControlExecuteParameters"/>.</value>
    public string Payload { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the content type value that forms part of the remote control execute parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content type value exposed by <see cref="RemoteControlExecuteParameters"/>.</value>
    public string ContentType { get; set; } = "application/json";
    /// <summary>Gets or sets the optional connector identity associated with the payload.</summary>
    /// <value>The connector key value exposed by <see cref="RemoteControlExecuteParameters"/>.</value>
    public string ConnectorKey { get; set; } = "manual";
}

/// <summary>Contains history-list parameters for the Remote Control DXFunction.</summary>
public sealed class RemoteControlHistoryParameters
{
    /// <summary>
    /// Gets or sets the take value that forms part of the remote control history parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="RemoteControlHistoryParameters"/>.</value>
    public int Take { get; set; } = 100;
}
