using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a DevExpress ai function text recovery request.
/// </summary>
public sealed class DxAiFunctionTextRecoveryRequest
{
    /// <summary>
    /// Gets or sets content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets automatic invocation.
    /// </summary>
    public bool AutomaticInvocation { get; set; } = true;
    /// <summary>
    /// Gets or sets invoke recognized calls.
    /// </summary>
    public bool InvokeRecognizedCalls { get; set; }
    /// <summary>
    /// Gets or sets requested by.
    /// </summary>
    public string RequestedBy { get; set; } = "RecoveredFunctionText";
    /// <summary>
    /// Gets or sets conversation identifier.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project version identifier.
    /// </summary>
    public Guid? ProjectVersionId { get; set; }
    /// <summary>
    /// Gets or sets application version.
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;
}

/// <summary>
/// Represents a recovered DevExpress ai function call.
/// </summary>
public sealed class RecoveredDxAiFunctionCall
{
    /// <summary>
    /// Gets or sets function name.
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets transport name.
    /// </summary>
    public string TransportName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets arguments.
    /// </summary>
    public JsonElement Arguments { get; set; }
    /// <summary>
    /// Gets or sets source format.
    /// </summary>
    public string SourceFormat { get; set; } = string.Empty;
}

/// <summary>
/// Represents a DevExpress ai function text recovery result.
/// </summary>
public sealed class DxAiFunctionTextRecoveryResult
{
    /// <summary>
    /// Gets or sets recognized.
    /// </summary>
    public bool Recognized { get; set; }
    /// <summary>
    /// Gets or sets suppress recovered content.
    /// </summary>
    public bool SuppressRecoveredContent { get; set; }
    /// <summary>
    /// Gets or sets visible content.
    /// </summary>
    public string VisibleContent { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets calls.
    /// </summary>
    public List<RecoveredDxAiFunctionCall> Calls { get; set; } = [];
    /// <summary>
    /// Gets or sets invocations.
    /// </summary>
    public List<DxAiFunctionInvocationResult> Invocations { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
