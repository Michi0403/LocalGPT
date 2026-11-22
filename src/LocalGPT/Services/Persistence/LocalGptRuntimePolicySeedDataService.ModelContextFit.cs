using LocalGPT.BusinessObjects;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Runtime values introduced with LocalGPT 7.2.6: model-aware Ollama context fitting and placeholder-token output detection.
/// Missing rows are added on startup; existing user-edited rows are never overwritten.
/// </summary>
public sealed partial class LocalGptRuntimePolicySeedDataService
{
    /// <summary>Creates the 7.2.6 model context fit runtime value seeds.</summary>
    /// <returns>Seed values.</returns>
    private IReadOnlyList<LocalGptRuntimeSystemVariableSeed> CreateModelContextFitValues()
    {
        try
        {
            return
            [
                new("OllamaModelContextLengthKeySuffix", ".context_length", "System.String"),
                new("OllamaModelDetailsLookupTimeoutSeconds", "10", "System.Int32"),
                new("CouncilPlaceholderTokenPrefix", "<unused", "System.String"),
                new("CouncilPlaceholderOutputMinimumPercent", "50", "System.Int32"),
                new("AiBootstrapReferenceContextPercent", "15", "System.Int32"),
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the 7.2.6 runtime value seeds failed.");
            throw;
        }
    }
}
