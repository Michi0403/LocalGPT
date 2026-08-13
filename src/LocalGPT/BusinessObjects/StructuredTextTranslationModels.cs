namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for structured JSON translation, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class StructuredJsonTranslationRequest
{
    /// <summary>
    /// Gets or sets the text value that forms part of the structured JSON translation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="StructuredJsonTranslationRequest"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether raw JSON applies to the structured JSON translation state.
    /// </summary>
    /// <value>The include raw JSON value exposed by <see cref="StructuredJsonTranslationRequest"/>.</value>
    public bool IncludeRawJson { get; set; } = true;
    /// <summary>
    /// Gets or sets the maximum documents value that forms part of the structured JSON translation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum documents value exposed by <see cref="StructuredJsonTranslationRequest"/>.</value>
    public int MaximumDocuments { get; set; } = 20;
}

/// <summary>
/// Represents structured JSON state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class StructuredJsonDocument
{
    /// <summary>
    /// Gets or sets the index value that forms part of the structured JSON state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The index value exposed by <see cref="StructuredJsonDocument"/>.</value>
    public int Index { get; set; }
    /// <summary>
    /// Gets or sets the root kind value that forms part of the structured JSON state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The root kind value exposed by <see cref="StructuredJsonDocument"/>.</value>
    public string RootKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the markdown value that forms part of the structured JSON state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The markdown value exposed by <see cref="StructuredJsonDocument"/>.</value>
    public string Markdown { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the normalized JSON value that forms part of the structured JSON state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The normalized JSON value exposed by <see cref="StructuredJsonDocument"/>.</value>
    public string NormalizedJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the start index value that forms part of the structured JSON state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start index value exposed by <see cref="StructuredJsonDocument"/>.</value>
    public int StartIndex { get; set; }
    /// <summary>
    /// Gets or sets the length that quantifies the associated structured JSON data.
    /// </summary>
    /// <value>The length value exposed by <see cref="StructuredJsonDocument"/>.</value>
    public int Length { get; set; }
}

/// <summary>
/// Represents the outcome of structured JSON translation, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class StructuredJsonTranslationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the structured JSON translation state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="StructuredJsonTranslationResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the structured JSON translation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="StructuredJsonTranslationResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the translated text value that forms part of the structured JSON translation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The translated text value exposed by <see cref="StructuredJsonTranslationResult"/>.</value>
    public string TranslatedText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the documents collection maintained or exposed by this structured JSON translation instance for downstream processing.
    /// </summary>
    /// <value>The documents value exposed by <see cref="StructuredJsonTranslationResult"/>.</value>
    public List<StructuredJsonDocument> Documents { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this structured JSON translation instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="StructuredJsonTranslationResult"/>.</value>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Represents a structured JSON candidate application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Start">Start value supplied to the structured JSON candidate operation and used when producing its result.</param>
/// <param name="Length">Length value supplied to the structured JSON candidate operation and used when producing its result.</param>
/// <param name="Json">Json value supplied to the structured JSON candidate operation and used when producing its result.</param>
public sealed record StructuredJsonCandidate(int Start, int Length, string Json);
