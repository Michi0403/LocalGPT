using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for DevExpress AI function text recovery, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class DxAiFunctionTextRecoveryRequest
{
    /// <summary>
    /// Gets or sets the content value that forms part of the DevExpress AI function text recovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="DxAiFunctionTextRecoveryRequest"/>.</value>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether automatic invocation applies to the DevExpress AI function text recovery state.
    /// </summary>
    /// <value>The automatic invocation value exposed by <see cref="DxAiFunctionTextRecoveryRequest"/>.</value>
    public bool AutomaticInvocation { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether invoke recognized calls applies to the DevExpress AI function text recovery state.
    /// </summary>
    /// <value>The invoke recognized calls value exposed by <see cref="DxAiFunctionTextRecoveryRequest"/>.</value>
    public bool InvokeRecognizedCalls { get; set; }
    /// <summary>
    /// Gets or sets the requested by value that forms part of the DevExpress AI function text recovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested by value exposed by <see cref="DxAiFunctionTextRecoveryRequest"/>.</value>
    public string RequestedBy { get; set; } = "RecoveredFunctionText";
    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this DevExpress AI function text recovery instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="DxAiFunctionTextRecoveryRequest"/>.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this DevExpress AI function text recovery instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="DxAiFunctionTextRecoveryRequest"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable project version identifier used to identify or correlate this DevExpress AI function text recovery instance with related application state.
    /// </summary>
    /// <value>The project version identifier value exposed by <see cref="DxAiFunctionTextRecoveryRequest"/>.</value>
    public Guid? ProjectVersionId { get; set; }
    /// <summary>
    /// Gets or sets the application version value that forms part of the DevExpress AI function text recovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The application version value exposed by <see cref="DxAiFunctionTextRecoveryRequest"/>.</value>
    public string ApplicationVersion { get; set; } = string.Empty;
}

/// <summary>
/// Represents a recovered DevExpress AI function call application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RecoveredDxAiFunctionCall
{
    /// <summary>
    /// Gets or sets the function name value that forms part of the recovered DevExpress AI function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="RecoveredDxAiFunctionCall"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the transport name value that forms part of the recovered DevExpress AI function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport name value exposed by <see cref="RecoveredDxAiFunctionCall"/>.</value>
    public string TransportName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the arguments value that forms part of the recovered DevExpress AI function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The arguments value exposed by <see cref="RecoveredDxAiFunctionCall"/>.</value>
    public JsonElement Arguments { get; set; }
    /// <summary>
    /// Gets or sets the source format value that forms part of the recovered DevExpress AI function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source format value exposed by <see cref="RecoveredDxAiFunctionCall"/>.</value>
    public string SourceFormat { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of DevExpress AI function text recovery, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class DxAiFunctionTextRecoveryResult
{
    /// <summary>
    /// Gets or sets a value indicating whether recognized applies to the DevExpress AI function text recovery state.
    /// </summary>
    /// <value>The recognized value exposed by <see cref="DxAiFunctionTextRecoveryResult"/>.</value>
    public bool Recognized { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether suppress recovered content applies to the DevExpress AI function text recovery state.
    /// </summary>
    /// <value>The suppress recovered content value exposed by <see cref="DxAiFunctionTextRecoveryResult"/>.</value>
    public bool SuppressRecoveredContent { get; set; }
    /// <summary>
    /// Gets or sets the visible content value that forms part of the DevExpress AI function text recovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The visible content value exposed by <see cref="DxAiFunctionTextRecoveryResult"/>.</value>
    public string VisibleContent { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the calls collection maintained or exposed by this DevExpress AI function text recovery instance for downstream processing.
    /// </summary>
    /// <value>The calls value exposed by <see cref="DxAiFunctionTextRecoveryResult"/>.</value>
    public List<RecoveredDxAiFunctionCall> Calls { get; set; } = [];
    /// <summary>
    /// Gets or sets the invocations collection maintained or exposed by this DevExpress AI function text recovery instance for downstream processing.
    /// </summary>
    /// <value>The invocations value exposed by <see cref="DxAiFunctionTextRecoveryResult"/>.</value>
    public List<DxAiFunctionInvocationResult> Invocations { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this DevExpress AI function text recovery instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="DxAiFunctionTextRecoveryResult"/>.</value>
    public List<string> Warnings { get; set; } = [];
}
