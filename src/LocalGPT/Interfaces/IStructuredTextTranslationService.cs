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

    /// <summary>
    /// Converts LocalGPT self-assessment tagged JSON blocks into controlled structured/code markup so
    /// model-produced tags, HTML entities and JSON Unicode escapes remain readable without becoming active HTML.
    /// </summary>
    /// <param name="text">Chat text that may contain encoded or literal LocalGPT self-assessment blocks.</param>
    /// <returns>The text with recognized self-assessment blocks rendered as controlled structured data.</returns>
    string TranslateSelfAssessmentBlocksToMarkdown(string? text);
}
