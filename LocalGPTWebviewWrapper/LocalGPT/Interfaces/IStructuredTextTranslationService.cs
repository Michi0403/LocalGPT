using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IStructuredTextTranslationService
{
    StructuredJsonTranslationResult TranslateJson(StructuredJsonTranslationRequest request);

    string TranslatePlainJsonBlocksToMarkdown(string? text);
}
