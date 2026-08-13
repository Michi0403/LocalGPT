using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for structured text translation behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IStructuredTextTranslationService
{
    /// <summary>
    /// Performs translate JSON as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The structured JSON translation result produced by the operation.</returns>
    StructuredJsonTranslationResult TranslateJson(StructuredJsonTranslationRequest request);

    /// <summary>
    /// Performs translate plain JSON blocks to markdown as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string TranslatePlainJsonBlocksToMarkdown(string? text);
}
