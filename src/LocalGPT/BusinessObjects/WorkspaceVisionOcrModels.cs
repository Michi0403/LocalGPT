namespace LocalGPT.BusinessObjects;

/// <summary>Reports whether a configured Ollama host can currently perform workspace-image OCR.</summary>
public sealed class LocalVisionOcrCapability
{
    /// <summary>
    /// Gets or sets a value indicating whether the value is available applies to the local vision OCR capability state.
    /// </summary>
    /// <value>The available value exposed by <see cref="LocalVisionOcrCapability"/>.</value>
    public bool Available { get; set; }
    /// <summary>
    /// Gets or sets the provider URI that identifies the network or application endpoint associated with this local vision OCR capability state.
    /// </summary>
    /// <value>The provider URI value exposed by <see cref="LocalVisionOcrCapability"/>.</value>
    public string ProviderUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model name value that forms part of the local vision OCR capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="LocalVisionOcrCapability"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Ollama version value that forms part of the local vision OCR capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama version value exposed by <see cref="LocalVisionOcrCapability"/>.</value>
    public string OllamaVersion { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether model installed applies to the local vision OCR capability state.
    /// </summary>
    /// <value>The model installed value exposed by <see cref="LocalVisionOcrCapability"/>.</value>
    public bool ModelInstalled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether runtime compatible applies to the local vision OCR capability state.
    /// </summary>
    /// <value>The runtime compatible value exposed by <see cref="LocalVisionOcrCapability"/>.</value>
    public bool RuntimeCompatible { get; set; }
    /// <summary>
    /// Gets or sets the requirement value that forms part of the local vision OCR capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requirement value exposed by <see cref="LocalVisionOcrCapability"/>.</value>
    public string Requirement { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reason value that forms part of the local vision OCR capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="LocalVisionOcrCapability"/>.</value>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Requests OCR for one image that already belongs to a bounded LocalGPT chat-upload workspace.</summary>
public sealed class WorkspaceImageOcrRequest
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the workspace image OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="WorkspaceImageOcrRequest"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the relative path used by this workspace image OCR instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="WorkspaceImageOcrRequest"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt value that forms part of the workspace image OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="WorkspaceImageOcrRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model name value that forms part of the workspace image OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="WorkspaceImageOcrRequest"/>.</value>
    public string ModelName { get; set; } = "deepseek-ocr";
    /// <summary>
    /// Gets or sets the maximum output tokens value that forms part of the workspace image OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum output tokens value exposed by <see cref="WorkspaceImageOcrRequest"/>.</value>
    public int MaximumOutputTokens { get; set; } = 1600;
}

/// <summary>Returns OCR text together with the exact workspace evidence that produced it.</summary>
public sealed class WorkspaceImageOcrResult
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the workspace image OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="WorkspaceImageOcrResult"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the relative path used by this workspace image OCR instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="WorkspaceImageOcrResult"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the OCR value that forms part of the workspace image OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The OCR value exposed by <see cref="WorkspaceImageOcrResult"/>.</value>
    public LocalVisionOcrResult Ocr { get; set; } = new();
}
