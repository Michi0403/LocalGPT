using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Interfaces;

/// <summary>Defines persistence and transport operations for user-owned Remote Control connectors.</summary>
public interface IRemoteControlConnectorService
{
    /// <summary>Lists connector definitions.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RemoteControlConnectorDefinition>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs get as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control connector definition produced by the operation.</returns>
    Task<RemoteControlConnectorDefinition?> GetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs save as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="definition">Definition value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control connector definition produced by the operation.</returns>
    Task<RemoteControlConnectorDefinition> SaveAsync(RemoteControlConnectorDefinition definition, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs delete as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Rotates and returns the connector's webhook token.</summary>
    /// <param name="key">Key value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> RotateWebhookTokenAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Performs one configured REST/OData pull and optionally dispatches matching pipelines.</summary>
    /// <param name="key">Key value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="runPipelines">Value indicating whether run pipelines should apply to this operation.</param>
    /// <param name="automaticInvocation">Value indicating whether automatic invocation should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control payload produced by the operation.</returns>
    Task<RemoteControlPayload> PullAsync(string key, bool runPipelines, bool automaticInvocation, CancellationToken cancellationToken = default);
    /// <summary>Accepts an authenticated webhook payload and dispatches matching pipelines.</summary>
    /// <param name="key">Key value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="content">Content value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="contentType">Content type value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control payload produced by the operation.</returns>
    Task<RemoteControlPayload> ReceiveWebhookAsync(string key, string token, string content, string contentType, CancellationToken cancellationToken = default);
    /// <summary>Lists connectors that are due for automatic polling.</summary>
    /// <param name="utcNow">Utc now value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RemoteControlConnectorDefinition>> ListDueForPollingAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    /// <summary>Returns recent execution audit rows.</summary>
    /// <param name="take">Take value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RemoteControlExecutionRecord>> GetHistoryAsync(int take = 100, CancellationToken cancellationToken = default);
}


/// <summary>Defines bounded outbound HTTP and inbound webhook payload handling for Remote Control connectors.</summary>
public interface IRemoteControlTransportService
{
    /// <summary>Executes one validated HTTP/OData pull.</summary>
    /// <param name="connector">Connector value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control payload produced by the operation.</returns>
    Task<RemoteControlPayload> PullAsync(RemoteControlConnectorDefinition connector, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs accept webhook as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connector">Connector value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="content">Content value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="contentType">Content type value supplied to the remote control transport operation and used when producing its result.</param>
    /// <returns>The remote control payload produced by the operation.</returns>
    RemoteControlPayload AcceptWebhook(RemoteControlConnectorDefinition connector, string content, string contentType);
}

/// <summary>Defines persistence for bounded Remote Control execution audit records.</summary>
public interface IRemoteControlExecutionStoreService
{
    /// <summary>
    /// Performs start as part of the remote control execution store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connectorKey">Connector key value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="pipelineKey">Pipeline key value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="trigger">Trigger value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="payloadBytes">Payload bytes value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="httpStatusCode">Http status code value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control execution record produced by the operation.</returns>
    Task<RemoteControlExecutionRecord> StartAsync(string connectorKey, string pipelineKey, RemoteControlTriggerKind trigger, int payloadBytes, int? httpStatusCode, CancellationToken cancellationToken = default);
    /// <summary>Completes an execution audit record.</summary>
    /// <param name="executionId">Identifier of the execution to use for this operation.</param>
    /// <param name="succeeded">Value indicating whether succeeded should apply to this operation.</param>
    /// <param name="stepCount">Step count value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task CompleteAsync(Guid executionId, bool succeeded, int stepCount, string summary, string error, CancellationToken cancellationToken = default);
    /// <summary>Returns recent execution audit records.</summary>
    /// <param name="take">Take value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RemoteControlExecutionRecord>> ListAsync(int take = 100, CancellationToken cancellationToken = default);
}

/// <summary>Defines user-authored action-pipeline persistence and execution.</summary>
public interface IRemoteControlPipelineService
{
    /// <summary>Lists pipeline definitions.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RemoteControlPipelineDefinition>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs get as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control pipeline definition produced by the operation.</returns>
    Task<RemoteControlPipelineDefinition?> GetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Saves a pipeline after validating all referenced DXFunctions and argument templates.</summary>
    /// <param name="definition">Definition value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control pipeline definition produced by the operation.</returns>
    Task<RemoteControlPipelineDefinition> SaveAsync(RemoteControlPipelineDefinition definition, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs delete as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Executes one pipeline with a caller-supplied or connector payload.</summary>
    /// <param name="key">Key value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="automaticInvocation">Value indicating whether automatic invocation should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control pipeline execution result produced by the operation.</returns>
    Task<RemoteControlPipelineExecutionResult> ExecuteAsync(string key, RemoteControlPayload payload, bool automaticInvocation, CancellationToken cancellationToken = default);
    /// <summary>Executes all enabled pipelines bound to a connector and trigger.</summary>
    /// <param name="payload">Payload value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="automaticInvocation">Value indicating whether automatic invocation should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RemoteControlPipelineExecutionResult>> ExecuteMatchingAsync(RemoteControlPayload payload, bool automaticInvocation, CancellationToken cancellationToken = default);
    /// <summary>Returns enabled catalog targets that may be selected by a Remote Control action step.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DxAiFunctionCatalogEntry>> ListTargetsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Parses steps as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stepsJson">Steps json value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<RemoteControlPipelineStepDefinition> ParseSteps(string stepsJson);
}

/// <summary>Defines the service-owned interpolation and response-selection rules used by Remote Control connectors and pipelines.</summary>
public interface IRemoteControlTemplateService
{
    /// <summary>Resolves connector or action template tokens against the current payload, previous step results, and explicit LocalGPT system variables.</summary>
    /// <param name="template">Template value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="steps">Remote control pipeline step result dependency used by the remote control template workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> ResolveAsync(string template, RemoteControlPayload? payload, IReadOnlyDictionary<string, RemoteControlPipelineStepResult>? steps = null, CancellationToken cancellationToken = default);
    /// <summary>Selects a dotted path from a JSON document and returns the selected value.</summary>
    /// <param name="value">Value value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="selector">Selector value supplied to the remote control template operation and used when producing its result.</param>
    /// <returns>The JSON element produced by the operation.</returns>
    JsonElement SelectJson(JsonElement value, string selector);
    /// <summary>Parses response text according to the configured format and optional selector.</summary>
    /// <param name="content">Content value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="contentType">Content type value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="selector">Selector value supplied to the remote control template operation and used when producing its result.</param>
    /// <returns>The JSON element produced by the operation.</returns>
    JsonElement? ParseSelectedJson(string content, string contentType, RemoteControlResponseFormat format, string selector);
}
