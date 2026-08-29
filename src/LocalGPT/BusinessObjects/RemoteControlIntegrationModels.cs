using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Defines the transport selected by a user-owned Remote Control connector.</summary>
public enum RemoteControlTransportKind
{
    /// <summary>Pulls a conventional HTTP REST resource.</summary>
    Rest,
    /// <summary>Pulls an OData resource through its HTTP endpoint and query string.</summary>
    OData,
    /// <summary>Accepts payloads pushed into LocalGPT's token-protected webhook endpoint.</summary>
    Webhook
}

/// <summary>Defines the HTTP method used by a Remote Control pull connector.</summary>
public enum RemoteControlHttpMethod
{
    /// <summary>
    /// Selects the get option for <see cref="RemoteControlHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Get,
    /// <summary>
    /// Selects the post option for <see cref="RemoteControlHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Post,
    /// <summary>
    /// Selects the put option for <see cref="RemoteControlHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Put,
    /// <summary>
    /// Selects the patch option for <see cref="RemoteControlHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Patch,
    /// <summary>
    /// Selects the delete option for <see cref="RemoteControlHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Delete
}

/// <summary>Defines how a Remote Control response should be interpreted before pipeline interpolation.</summary>
public enum RemoteControlResponseFormat
{
    /// <summary>Infers JSON, XML, or text from the content type and leading content.</summary>
    Auto,
    /// <summary>
    /// Selects the JSON option for <see cref="RemoteControlResponseFormat"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Json,
    /// <summary>Treats the payload as opaque text.</summary>
    Text,
    /// <summary>Treats the payload as XML text.</summary>
    Xml
}

/// <summary>Defines the event that initiated a Remote Control pipeline execution.</summary>
[Flags]
public enum RemoteControlTriggerKind
{
    /// <summary>No trigger is configured.</summary>
    None = 0,
    /// <summary>The user or a confirmed DXFunction explicitly requested execution.</summary>
    Manual = 1,
    /// <summary>A configured HTTP/OData pull completed successfully.</summary>
    Pull = 2,
    /// <summary>A token-authenticated webhook payload arrived.</summary>
    Webhook = 4
}

/// <summary>
/// Stores one user-created external data connector. Network access is disabled by default and must be enabled explicitly per connector.
/// </summary>
public sealed class RemoteControlConnectorDefinition
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this remote control connector definition instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this remote control connector definition instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the transport value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public RemoteControlTransportKind Transport { get; set; } = RemoteControlTransportKind.Rest;
    /// <summary>
    /// Gets or sets the method value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public RemoteControlHttpMethod Method { get; set; } = RemoteControlHttpMethod.Get;
    /// <summary>Gets or sets the URL template. It is never contacted while <see cref="NetworkEnabled"/> is false.</summary>
    /// <value>The URL template value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string UrlTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets a JSON object containing header-name/template pairs.</summary>
    /// <value>The headers JSON value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string HeadersJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the request body template value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request body template value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string RequestBodyTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the request content type used when a request body is present.</summary>
    /// <value>The request content type value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string RequestContentType { get; set; } = "application/json";
    /// <summary>
    /// Gets or sets the response format value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The response format value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public RemoteControlResponseFormat ResponseFormat { get; set; } = RemoteControlResponseFormat.Auto;
    /// <summary>Gets or sets an optional dotted JSON selector applied before pipeline interpolation.</summary>
    /// <value>The response selector value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string ResponseSelector { get; set; } = string.Empty;
    /// <summary>Gets or sets the automatic pull interval in seconds; zero disables polling.</summary>
    /// <value>The poll interval seconds value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public int PollIntervalSeconds { get; set; }
    /// <summary>
    /// Gets or sets the timeout seconds value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeout seconds value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public int TimeoutSeconds { get; set; } = 0;
    /// <summary>
    /// Gets or sets the max payload bytes value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max payload bytes value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public int MaxPayloadBytes { get; set; } = int.MaxValue;
    /// <summary>Gets or sets whether the connector exists as an enabled user capability.</summary>
    /// <value>The is enabled value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public bool IsEnabled { get; set; }
    /// <summary>Gets or sets whether LocalGPT may perform outbound network I/O for this connector.</summary>
    /// <value>The network enabled value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public bool NetworkEnabled { get; set; }
    /// <summary>Gets or sets whether plain HTTP is permitted. HTTPS remains the default.</summary>
    /// <value>The allow insecure HTTP value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public bool AllowInsecureHttp { get; set; }
    /// <summary>Gets or sets the explicit JSON array of hosts allowed for outbound requests.</summary>
    /// <value>The allowed hosts JSON value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string AllowedHostsJson { get; set; } = "[]";
    /// <summary>Gets or sets the token used to authenticate inbound webhook requests. Token values are omitted from JSON APIs and logs.</summary>
    /// <value>The webhook token value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    [JsonIgnore]
    public string WebhookToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the created at UTC associated with this remote control connector definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC time the row was last changed.</summary>
    /// <value>The updated at UTC value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC time of the latest pull or webhook attempt.</summary>
    /// <value>The last attempt UTC value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public DateTime? LastAttemptUtc { get; set; }
    /// <summary>Gets or sets the UTC time of the latest successful pull or webhook.</summary>
    /// <value>The last success UTC value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public DateTime? LastSuccessUtc { get; set; }
    /// <summary>
    /// Gets or sets the last status value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last status value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string LastStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last content type value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last content type value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string LastContentType { get; set; } = string.Empty;
    /// <summary>Gets or sets the last bounded payload preview for operator diagnostics.</summary>
    /// <value>The last payload preview value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string LastPayloadPreview { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last error value that forms part of the remote control connector definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last error value exposed by <see cref="RemoteControlConnectorDefinition"/>.</value>
    public string LastError { get; set; } = string.Empty;
}

/// <summary>Stores one user-authored action pipeline that converts connector payloads into existing DXFunction calls.</summary>
public sealed class RemoteControlPipelineDefinition
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this remote control pipeline definition instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this remote control pipeline definition instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the remote control pipeline definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the remote control pipeline definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the connector key whose payload feeds this pipeline. Empty means manual payload only.</summary>
    /// <value>The connector key value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public string ConnectorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the triggers value that forms part of the remote control pipeline definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The triggers value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public RemoteControlTriggerKind Triggers { get; set; } = RemoteControlTriggerKind.Manual;
    /// <summary>
    /// Gets or sets the steps JSON value that forms part of the remote control pipeline definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The steps JSON value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public string StepsJson { get; set; } = "[]";
    /// <summary>Gets or sets whether the pipeline may be selected for execution.</summary>
    /// <value>The is enabled value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this remote control pipeline definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC time the row was last changed.</summary>
    /// <value>The updated at UTC value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC time of the latest execution attempt.</summary>
    /// <value>The last attempt UTC value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public DateTime? LastAttemptUtc { get; set; }
    /// <summary>Gets or sets the UTC time of the latest successful execution.</summary>
    /// <value>The last success UTC value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public DateTime? LastSuccessUtc { get; set; }
    /// <summary>
    /// Gets or sets the last status value that forms part of the remote control pipeline definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last status value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public string LastStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last error value that forms part of the remote control pipeline definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last error value exposed by <see cref="RemoteControlPipelineDefinition"/>.</value>
    public string LastError { get; set; } = string.Empty;
}

/// <summary>Stores the audit outcome of one pull, webhook, or pipeline execution without persisting unbounded remote payloads.</summary>
public sealed class RemoteControlExecutionRecord
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this remote control execution instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable connector key used to identify or correlate this remote control execution instance with related application state.
    /// </summary>
    /// <value>The connector key value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public string ConnectorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable pipeline key used to identify or correlate this remote control execution instance with related application state.
    /// </summary>
    /// <value>The pipeline key value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public string PipelineKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the trigger value that forms part of the remote control execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trigger value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public RemoteControlTriggerKind Trigger { get; set; }
    /// <summary>
    /// Gets or sets the started at UTC associated with this remote control execution state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the completed at UTC associated with this remote control execution state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the remote control execution state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>Gets or sets the HTTP status code when a pull produced one.</summary>
    /// <value>The HTTP status code value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public int? HttpStatusCode { get; set; }
    /// <summary>
    /// Gets or sets the payload bytes value that forms part of the remote control execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The payload bytes value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public int PayloadBytes { get; set; }
    /// <summary>
    /// Gets or sets the step count that quantifies the associated remote control execution data.
    /// </summary>
    /// <value>The step count value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public int StepCount { get; set; }
    /// <summary>
    /// Gets or sets the summary value that forms part of the remote control execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the remote control execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="RemoteControlExecutionRecord"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>Describes one user-authored action in a Remote Control pipeline.</summary>
public sealed class RemoteControlPipelineStepDefinition
{
    /// <summary>Gets or sets the stable step key used for later-step interpolation.</summary>
    /// <value>The key value exposed by <see cref="RemoteControlPipelineStepDefinition"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the remote control pipeline step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="RemoteControlPipelineStepDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional database-backed DXFunction Catalog key. Service-method catalog keys are routed through the confirmed public-service bridge.</summary>
    /// <value>The target catalog key value exposed by <see cref="RemoteControlPipelineStepDefinition"/>.</value>
    public string TargetCatalogKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the exact registered DXFunction name to invoke when no catalog key is supplied.</summary>
    /// <value>The function name value exposed by <see cref="RemoteControlPipelineStepDefinition"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the JSON argument template. Tokens are resolved by <c>RemoteControlTemplateService</c>.</summary>
    /// <value>The arguments template JSON value exposed by <see cref="RemoteControlPipelineStepDefinition"/>.</value>
    public string ArgumentsTemplateJson { get; set; } = "{}";
    /// <summary>Gets or sets whether the next step may run after this step fails.</summary>
    /// <value>The continue on failure value exposed by <see cref="RemoteControlPipelineStepDefinition"/>.</value>
    public bool ContinueOnFailure { get; set; }
}

/// <summary>Represents a connector pull or accepted webhook payload after response selection.</summary>
public sealed class RemoteControlPayload
{
    /// <summary>
    /// Gets or sets the stable connector key used to identify or correlate this remote control payload instance with related application state.
    /// </summary>
    /// <value>The connector key value exposed by <see cref="RemoteControlPayload"/>.</value>
    public string ConnectorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the trigger value that forms part of the remote control payload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trigger value exposed by <see cref="RemoteControlPayload"/>.</value>
    public RemoteControlTriggerKind Trigger { get; set; }
    /// <summary>
    /// Gets or sets the content type value that forms part of the remote control payload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content type value exposed by <see cref="RemoteControlPayload"/>.</value>
    public string ContentType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the raw text value that forms part of the remote control payload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The raw text value exposed by <see cref="RemoteControlPayload"/>.</value>
    public string RawText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the JSON value that forms part of the remote control payload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The JSON value exposed by <see cref="RemoteControlPayload"/>.</value>
    public JsonElement? Json { get; set; }
    /// <summary>
    /// Gets or sets the payload bytes value that forms part of the remote control payload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The payload bytes value exposed by <see cref="RemoteControlPayload"/>.</value>
    public int PayloadBytes { get; set; }
    /// <summary>Gets or sets the HTTP status code when the payload came from a pull.</summary>
    /// <value>The HTTP status code value exposed by <see cref="RemoteControlPayload"/>.</value>
    public int? HttpStatusCode { get; set; }
}

/// <summary>Represents one pipeline step outcome.</summary>
public sealed class RemoteControlPipelineStepResult
{
    /// <summary>
    /// Gets or sets the stable step key used to identify or correlate this remote control pipeline step instance with related application state.
    /// </summary>
    /// <value>The step key value exposed by <see cref="RemoteControlPipelineStepResult"/>.</value>
    public string StepKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the function name value that forms part of the remote control pipeline step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="RemoteControlPipelineStepResult"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the remote control pipeline step state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="RemoteControlPipelineStepResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the remote control pipeline step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="RemoteControlPipelineStepResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the DXFunction result value for interpolation by later steps.</summary>
    /// <value>The value value exposed by <see cref="RemoteControlPipelineStepResult"/>.</value>
    public object? Value { get; set; }
    /// <summary>
    /// Gets or sets the error value that forms part of the remote control pipeline step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="RemoteControlPipelineStepResult"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>Represents one completed Remote Control pipeline run.</summary>
public sealed class RemoteControlPipelineExecutionResult
{
    /// <summary>
    /// Gets or sets the stable execution identifier used to identify or correlate this remote control pipeline execution instance with related application state.
    /// </summary>
    /// <value>The execution identifier value exposed by <see cref="RemoteControlPipelineExecutionResult"/>.</value>
    public Guid ExecutionId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable pipeline key used to identify or correlate this remote control pipeline execution instance with related application state.
    /// </summary>
    /// <value>The pipeline key value exposed by <see cref="RemoteControlPipelineExecutionResult"/>.</value>
    public string PipelineKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable connector key used to identify or correlate this remote control pipeline execution instance with related application state.
    /// </summary>
    /// <value>The connector key value exposed by <see cref="RemoteControlPipelineExecutionResult"/>.</value>
    public string ConnectorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the trigger value that forms part of the remote control pipeline execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trigger value exposed by <see cref="RemoteControlPipelineExecutionResult"/>.</value>
    public RemoteControlTriggerKind Trigger { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the remote control pipeline execution state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="RemoteControlPipelineExecutionResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the remote control pipeline execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="RemoteControlPipelineExecutionResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the remote control pipeline execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="RemoteControlPipelineExecutionResult"/>.</value>
    public string Error { get; set; } = string.Empty;
    /// <summary>
    /// Gets the steps collection maintained or exposed by this remote control pipeline execution instance for downstream processing.
    /// </summary>
    /// <value>The steps value exposed by <see cref="RemoteControlPipelineExecutionResult"/>.</value>
    public List<RemoteControlPipelineStepResult> Steps { get; } = [];
}

