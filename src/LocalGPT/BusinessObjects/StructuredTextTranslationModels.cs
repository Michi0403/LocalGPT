namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a structured JSON translation request.
/// </summary>
public sealed class StructuredJsonTranslationRequest
{
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets include raw JSON.
    /// </summary>
    public bool IncludeRawJson { get; set; } = true;
    /// <summary>
    /// Gets or sets maximum documents.
    /// </summary>
    public int MaximumDocuments { get; set; } = 20;
}

/// <summary>
/// Represents a structured JSON document.
/// </summary>
public sealed class StructuredJsonDocument
{
    /// <summary>
    /// Gets or sets index.
    /// </summary>
    public int Index { get; set; }
    /// <summary>
    /// Gets or sets root kind.
    /// </summary>
    public string RootKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets markdown.
    /// </summary>
    public string Markdown { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets normalized JSON.
    /// </summary>
    public string NormalizedJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets start index.
    /// </summary>
    public int StartIndex { get; set; }
    /// <summary>
    /// Gets or sets length.
    /// </summary>
    public int Length { get; set; }
}

/// <summary>
/// Represents a structured JSON translation result.
/// </summary>
public sealed class StructuredJsonTranslationResult
{
    /// <summary>
    /// Gets or sets succeeded.
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets translated text.
    /// </summary>
    public string TranslatedText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets documents.
    /// </summary>
    public List<StructuredJsonDocument> Documents { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Represents a structured JSON candidate.
/// </summary>
public sealed record StructuredJsonCandidate(int Start, int Length, string Json);
