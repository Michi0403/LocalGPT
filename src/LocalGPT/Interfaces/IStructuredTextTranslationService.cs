using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the structured text translation service contract.
/// </summary>
public interface IStructuredTextTranslationService
{
    /// <summary>
    /// Runs the translate JSON operation.
    /// </summary>
    StructuredJsonTranslationResult TranslateJson(StructuredJsonTranslationRequest request);

    /// <summary>
    /// Runs the translate plain JSON blocks to markdown operation.
    /// </summary>
    string TranslatePlainJsonBlocksToMarkdown(string? text);
}
