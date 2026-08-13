using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates LocalGPT vocabulary behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the LocalGPT vocabulary workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class LocalGptVocabularyService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<LocalGptVocabularyService> logger) : ILocalGptVocabularyService
{
    /// <summary>
    /// Performs get as part of the LocalGPT vocabulary service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT vocabulary snapshot produced by the operation.</returns>
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
