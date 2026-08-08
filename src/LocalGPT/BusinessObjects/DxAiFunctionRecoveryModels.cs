using System.Text.Json;

namespace LocalGPT.BusinessObjects;

public sealed class DxAiFunctionTextRecoveryRequest
{
    public string Content { get; set; } = string.Empty;
    public bool AutomaticInvocation { get; set; } = true;
    public bool InvokeRecognizedCalls { get; set; }
    public string RequestedBy { get; set; } = "RecoveredFunctionText";
    public Guid? ConversationId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectVersionId { get; set; }
    public string ApplicationVersion { get; set; } = string.Empty;
}

public sealed class RecoveredDxAiFunctionCall
{
    public string FunctionName { get; set; } = string.Empty;
    public string TransportName { get; set; } = string.Empty;
    public JsonElement Arguments { get; set; }
    public string SourceFormat { get; set; } = string.Empty;
}

public sealed class DxAiFunctionTextRecoveryResult
{
    public bool Recognized { get; set; }
    public bool SuppressRecoveredContent { get; set; }
    public string VisibleContent { get; set; } = string.Empty;
    public List<RecoveredDxAiFunctionCall> Calls { get; set; } = [];
    public List<DxAiFunctionInvocationResult> Invocations { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
