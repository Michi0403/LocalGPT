namespace LocalGPT.BusinessObjects;

/// <summary>Reports whether a configured Ollama host can currently perform workspace-image OCR.</summary>
public sealed class LocalVisionOcrCapability
{
    public bool Available { get; set; }
    public string ProviderUri { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string OllamaVersion { get; set; } = string.Empty;
    public bool ModelInstalled { get; set; }
    public bool RuntimeCompatible { get; set; }
    public string Requirement { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Requests OCR for one image that already belongs to a bounded LocalGPT chat-upload workspace.</summary>
public sealed class WorkspaceImageOcrRequest
{
    public string WorkspaceName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string ModelName { get; set; } = "deepseek-ocr";
    public int MaximumOutputTokens { get; set; } = 1600;
}

/// <summary>Returns OCR text together with the exact workspace evidence that produced it.</summary>
public sealed class WorkspaceImageOcrResult
{
    public string WorkspaceName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public LocalVisionOcrResult Ocr { get; set; } = new();
}
