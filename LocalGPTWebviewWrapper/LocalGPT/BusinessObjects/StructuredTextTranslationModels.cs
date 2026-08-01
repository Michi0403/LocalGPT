namespace LocalGPT.BusinessObjects;

public sealed class StructuredJsonTranslationRequest
{
    public string Text { get; set; } = string.Empty;
    public bool IncludeRawJson { get; set; } = true;
    public int MaximumDocuments { get; set; } = 20;
}

public sealed class StructuredJsonDocument
{
    public int Index { get; set; }
    public string RootKind { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public string NormalizedJson { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int Length { get; set; }
}

public sealed class StructuredJsonTranslationResult
{
    public bool Succeeded { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public List<StructuredJsonDocument> Documents { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
