using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>Policy-filtered search input shared by the chat, API, Council, and DX workbench.</summary>
public sealed class DxAiFunctionDiscoveryRequest
{
    /// <summary>
    /// Gets or sets the query value that forms part of the DevExpress AI function discovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The query value exposed by <see cref="DxAiFunctionDiscoveryRequest"/>.</value>
    public string Query { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the skip value that forms part of the DevExpress AI function discovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The skip value exposed by <see cref="DxAiFunctionDiscoveryRequest"/>.</value>
    public int Skip { get; set; }
    /// <summary>
    /// Gets or sets the take value that forms part of the DevExpress AI function discovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="DxAiFunctionDiscoveryRequest"/>.</value>
    public int Take { get; set; } = 12;
    /// <summary>
    /// Gets or sets a value indicating whether schemas applies to the DevExpress AI function discovery state.
    /// </summary>
    /// <value>The include schemas value exposed by <see cref="DxAiFunctionDiscoveryRequest"/>.</value>
    public bool IncludeSchemas { get; set; }
}

/// <summary>A discoverable, directly callable operation; no authorization is implied by discovery.</summary>
public sealed class DxAiFunctionDiscoveryItem
{
    /// <summary>
    /// Gets or sets the function name value that forms part of the DevExpress AI function discovery item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="DxAiFunctionDiscoveryItem"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the purpose value that forms part of the DevExpress AI function discovery item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="DxAiFunctionDiscoveryItem"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameter schema JSON value that forms part of the DevExpress AI function discovery item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter schema JSON value exposed by <see cref="DxAiFunctionDiscoveryItem"/>.</value>
    public string ParameterSchemaJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the descriptor hash value that forms part of the DevExpress AI function discovery item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor hash value exposed by <see cref="DxAiFunctionDiscoveryItem"/>.</value>
    public string DescriptorHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether requires frontend confirmation applies to the DevExpress AI function discovery item state.
    /// </summary>
    /// <value>The requires frontend confirmation value exposed by <see cref="DxAiFunctionDiscoveryItem"/>.</value>
    public bool RequiresFrontendConfirmation { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether automatic invocation applies to the DevExpress AI function discovery item state.
    /// </summary>
    /// <value>The supports automatic invocation value exposed by <see cref="DxAiFunctionDiscoveryItem"/>.</value>
    public bool SupportsAutomaticInvocation { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether read only applies to the DevExpress AI function discovery item state.
    /// </summary>
    /// <value>The is read only value exposed by <see cref="DxAiFunctionDiscoveryItem"/>.</value>
    public bool IsReadOnly { get; set; }
}

/// <summary>Bounded, paged discovery response to prevent burying small models in unneeded schemas.</summary>
public sealed class DxAiFunctionDiscoveryResult
{
    /// <summary>
    /// Gets or sets the total value that forms part of the DevExpress AI function discovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The total value exposed by <see cref="DxAiFunctionDiscoveryResult"/>.</value>
    public int Total { get; set; }
    /// <summary>
    /// Gets or sets the functions collection maintained or exposed by this DevExpress AI function discovery instance for downstream processing.
    /// </summary>
    /// <value>The functions value exposed by <see cref="DxAiFunctionDiscoveryResult"/>.</value>
    public List<DxAiFunctionDiscoveryItem> Functions { get; set; } = [];
}

/// <summary>Structured diagnostic returned to the model and user without logging parameter content.</summary>
public sealed class DxAiFunctionValidationIssue
{
    /// <summary>
    /// Gets or sets the code value that forms part of the DevExpress AI function validation issue state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The code value exposed by <see cref="DxAiFunctionValidationIssue"/>.</value>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the path used by this DevExpress AI function validation issue instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path value exposed by <see cref="DxAiFunctionValidationIssue"/>.</value>
    public string Path { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the message value that forms part of the DevExpress AI function validation issue state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="DxAiFunctionValidationIssue"/>.</value>
    public string Message { get; set; } = string.Empty;
}

/// <summary>Non-executing preview bound to the exact arguments, runtime descriptor and saved policy.</summary>
public sealed class DxAiFunctionInvocationPreview
{
    /// <summary>
    /// Gets or sets the function name value that forms part of the DevExpress AI function invocation preview state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="DxAiFunctionInvocationPreview"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the preview fingerprint value that forms part of the DevExpress AI function invocation preview state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preview fingerprint value exposed by <see cref="DxAiFunctionInvocationPreview"/>.</value>
    public string PreviewFingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether requires human approval applies to the DevExpress AI function invocation preview state.
    /// </summary>
    /// <value>The requires human approval value exposed by <see cref="DxAiFunctionInvocationPreview"/>.</value>
    public bool RequiresHumanApproval { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether invoke applies to the DevExpress AI function invocation preview state.
    /// </summary>
    /// <value>The can invoke value exposed by <see cref="DxAiFunctionInvocationPreview"/>.</value>
    public bool CanInvoke { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether read only applies to the DevExpress AI function invocation preview state.
    /// </summary>
    /// <value>The is read only value exposed by <see cref="DxAiFunctionInvocationPreview"/>.</value>
    public bool IsReadOnly { get; set; }
    /// <summary>
    /// Gets or sets the parameter schema JSON value that forms part of the DevExpress AI function invocation preview state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter schema JSON value exposed by <see cref="DxAiFunctionInvocationPreview"/>.</value>
    public string ParameterSchemaJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the issues collection maintained or exposed by this DevExpress AI function invocation preview instance for downstream processing.
    /// </summary>
    /// <value>The issues value exposed by <see cref="DxAiFunctionInvocationPreview"/>.</value>
    public List<DxAiFunctionValidationIssue> Issues { get; set; } = [];
}

/// <summary>Sanitized durable invocation metadata, not arguments, prompts or tool output.</summary>
public sealed class DxAiFunctionInvocationAuditEntry
{
    /// <summary>
    /// Gets or sets the stable operation identifier used to identify or correlate this DevExpress AI function invocation audit instance with related application state.
    /// </summary>
    /// <value>The operation identifier value exposed by <see cref="DxAiFunctionInvocationAuditEntry"/>.</value>
    public Guid OperationId { get; set; }
    /// <summary>
    /// Gets or sets the function name value that forms part of the DevExpress AI function invocation audit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="DxAiFunctionInvocationAuditEntry"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the DevExpress AI function invocation audit state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="DxAiFunctionInvocationAuditEntry"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the DevExpress AI function invocation audit state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="DxAiFunctionInvocationAuditEntry"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this DevExpress AI function invocation audit state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="DxAiFunctionInvocationAuditEntry"/>.</value>
    public DateTime CreatedAtUtc { get; set; }
}
