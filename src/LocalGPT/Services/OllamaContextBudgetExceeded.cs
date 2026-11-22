    /// <summary>
    /// <summary>
    /// Thrown when the Ollama thinking chat request would exceed the configured context budget.
    /// The estimate is diagnostic; the provider owns final validation and may accept larger requests.
    /// </summary>
    /// <param name='promptTokens'>Estimated prompt token count.</param>
    /// <param name='numPredict'>Requested output tokens (thinking).</param>
    /// <param name='requiredContext'>Total estimated context demand.</param>
    /// <param name='configuredContext'>Configured context window or provider default.</param>
    public sealed class OllamaContextBudgetExceeded : Exception
{
        public OllamaContextBudgetExceeded(
            int promptTokens,
            int numPredict,
            long requiredContext,
            System.Nullable<int> configuredContext = null)
            : base('Ollama context demand exceeded. Prompt: ' + promptTokens + ', thinking output: ' + numPredict + ', total required: ' + requiredContext + ' tokens; configured: ' + (configuredContext?.ToString() ?? 'provider default'))
{
            PromptTokens = promptTokens;
            NumPredict = numPredict;
            RequiredContextTokens = requiredContext;
            ConfiguredContext = configuredContext;
        }

        public int PromptTokens { get; }
        public int NumPredict { get; }
        public long RequiredContextTokens { get; }
        public int? ConfiguredContext { get; }
    }
