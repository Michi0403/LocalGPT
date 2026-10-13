using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>Policy-filtered search input shared by the chat, API, Council, and DX workbench.</summary>
public sealed class DxAiFunctionDiscoveryRequest
{
    public string Query { get; set; } = string.Empty;
    public int Skip { get; set; }
    public int Take { get; set; } = 12;
    public bool IncludeSchemas { get; set; }
}

/// <summary>A discoverable, directly callable operation; no authorization is implied by discovery.</summary>
public sealed class DxAiFunctionDiscoveryItem
{
    public string FunctionName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string ParameterSchemaJson { get; set; } = string.Empty;
    public string DescriptorHash { get; set; } = string.Empty;
    public bool RequiresFrontendConfirmation { get; set; }
    public bool SupportsAutomaticInvocation { get; set; }
    public bool IsReadOnly { get; set; }
}

/// <summary>Bounded, paged discovery response to prevent burying small models in unneeded schemas.</summary>
public sealed class DxAiFunctionDiscoveryResult
{
    public int Total { get; set; }
    public List<DxAiFunctionDiscoveryItem> Functions { get; set; } = [];
}

/// <summary>Structured diagnostic returned to the model and user without logging parameter content.</summary>
public sealed class DxAiFunctionValidationIssue
{
    public string Code { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>Non-executing preview bound to the exact arguments, runtime descriptor and saved policy.</summary>
public sealed class DxAiFunctionInvocationPreview
{
    public string FunctionName { get; set; } = string.Empty;
    public string PreviewFingerprint { get; set; } = string.Empty;
    public bool RequiresHumanApproval { get; set; }
    public bool CanInvoke { get; set; }
    public bool IsReadOnly { get; set; }
    public string ParameterSchemaJson { get; set; } = string.Empty;
    public List<DxAiFunctionValidationIssue> Issues { get; set; } = [];
}

/// <summary>Sanitized durable invocation metadata, not arguments, prompts or tool output.</summary>
public sealed class DxAiFunctionInvocationAuditEntry
{
    public Guid OperationId { get; set; }
    public string FunctionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
