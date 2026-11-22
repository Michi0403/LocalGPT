using System;
using System.Linq;
using LocalGPT.Services.OllamaContextBudgetExceeded;

namespace LocalGPT.Services;

/// <summary>
/// Budget policy for Ollama request contexts.
/// </summary>
public sealed partial class CouncilRuntimeService 
{
    /// <summary>
    /// Creates a new budget policy for Ollama requests.
    /// </summary>
    public static OllamaRequestBudgetPolicy CreateOllamaRequestBudgetPolicy()
    {
        return new OllamaRequestBudgetPolicy();
    }
    
    /// <summary>
    /// Measures request characters within budget constraints.
    /// </summary>
    public static int MeasureRequestCharacters(
        string systemPrompt,
        IEnumerable<string> userMessages)
    {
        int count = 0;
        if (!string.IsNullOrEmpty(systemPrompt)) count += systemPrompt.Length;
        foreach (var msg in userMessages) count += msg.Length;
        return count;
    }
}
