using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

public sealed class LocalGptVocabularyService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<LocalGptVocabularyService> logger) : ILocalGptVocabularyService
{
    public LocalGptVocabularySnapshot Get()
    {
        try
        {
            var vocabulary = runtimePolicy.GetJson<LocalGptVocabularySnapshot>(LocalGptRuntimeValue.VocabularyJson);
            logger.LogTrace($"Resolved the persisted LocalGPT vocabulary.");
            return vocabulary;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve the persisted LocalGPT vocabulary: {exception.Message}");
            throw;
        }
    }
}
