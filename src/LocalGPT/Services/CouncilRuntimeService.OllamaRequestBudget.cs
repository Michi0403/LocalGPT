using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>
    /// Sizes native Ollama requests so the prompt, the native tool schemas, model thinking and the visible answer fit
    /// the context window instead of being silently truncated by the provider.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
        /// <summary>Runtime value that stores the <see cref="OllamaRequestBudgetPolicy"/> document.</summary>
        private const string OllamaRequestBudgetPolicyKey = "OllamaRequestBudgetPolicyJson";

        /// <summary>
        /// Computes the effective <c>num_predict</c> and <c>num_ctx</c> for one Ollama chat request. Explicit thinking
        /// receives its own output reserve so it no longer consumes the visible answer budget, and the context window
        /// is raised (never lowered) up to the configured maximum when the measured prompt would otherwise be cut.
        /// </summary>
        /// <param name="configuredContextTokens">Context chosen by the hardware road or caller; <see langword="null"/> means provider default.</param>
        /// <param name="requestedOutputTokens">Visible-answer budget requested by the caller.</param>
        /// <param name="thinkingRequested">Whether LocalGPT requests explicit model thinking for this call.</param>
        /// <param name="promptCharacters">Characters of all messages plus serialized native tool schemas.</param>
        /// <param name="modelName">Ollama model used for diagnostics.</param>
        /// <param name="logger">Logger of the calling chat client.</param>
        /// <returns>The effective request budget.</returns>
        public OllamaRequestBudget OllamaThinkingChatClientResolveRequestBudget(
            int? configuredContextTokens,
            int requestedOutputTokens,
            bool thinkingRequested,
            long promptCharacters,
            string modelName,
            ILogger logger)
        {
            try
            {
                var policy = ReadOllamaRequestBudgetPolicy(logger);
                var maximumOutput = Math.Max(64, policy.MaximumOutputTokens);
                var visibleOutput = Math.Clamp(requestedOutputTokens, 64, maximumOutput);
                var numPredict = thinkingRequested
                    ? (int)Math.Min(maximumOutput, (long)visibleOutput + Math.Max(0, policy.ThinkingReserveTokens))
                    : visibleOutput;
                var charactersPerToken = policy.CharactersPerToken is > 0.5 and < 16 ? policy.CharactersPerToken : 3.2;
                var promptTokens = (int)Math.Min(int.MaxValue / 4, Math.Ceiling(Math.Max(0, promptCharacters) / charactersPerToken));
                var required = (int)Math.Min(int.MaxValue / 2, (long)promptTokens + numPredict + Math.Max(0, policy.ContextSafetyMarginTokens));
                var current = configuredContextTokens is > 0
                    ? configuredContextTokens.Value
                    : Math.Max(256, policy.AssumedProviderDefaultContextTokens);
                if (required <= current)
                {
                    logger.LogDebug(
                        "Ollama request for model {Model} fits its context: estimated prompt {PromptTokens} tokens, output {NumPredict} tokens (thinking reserve {ThinkingReserved}), context {Context}.",
                        modelName, promptTokens, numPredict, thinkingRequested, configuredContextTokens?.ToString() ?? "provider default");
                    return new OllamaRequestBudget(numPredict, configuredContextTokens, promptTokens, required, false, false);
                }

                var step = Math.Max(256, policy.ContextGrowthStepTokens);
                var ceiling = Math.Max(current, policy.MaximumFittedContextTokens);
                var raised = (int)Math.Min(ceiling, (long)Math.Ceiling(required / (double)step) * step);
                var exceeds = required > raised;
                if (exceeds)
                {
                    // The configured output stays the upper bound; only this request's output is reduced so it fits.
                    var margin = Math.Max(0, policy.ContextSafetyMarginTokens);
                    var minimumFitted = Math.Max(64, policy.MinimumFittedOutputTokens);
                    var fittedOutput = (int)Math.Min(numPredict, Math.Max(minimumFitted, (long)ceiling - promptTokens - margin));
                    if (fittedOutput < numPredict)
                    {
                        logger.LogInformation(
                            "Fitted the Ollama output for model {Model} to the context: configured output {ConfiguredOutput} tokens, fitted output {FittedOutput} tokens, prompt estimate {PromptTokens} tokens, context {Ceiling} tokens (configuration is unchanged).",
                            modelName, numPredict, fittedOutput, promptTokens, ceiling);
                        numPredict = fittedOutput;
                        required = (int)Math.Min(int.MaxValue / 2, (long)promptTokens + numPredict + margin);
                        raised = (int)Math.Min(ceiling, (long)Math.Ceiling(required / (double)step) * step);
                        exceeds = required > raised;
                    }
                }
                if (exceeds)
                {
                    logger.LogWarning(
                        "Ollama request for model {Model} needs about {Required} context tokens (prompt {PromptTokens}, output {NumPredict}) but the configured maximum is {Ceiling}; the provider may cut the oldest prompt content. Reduce attached tools or raise {PolicyKey}.MaximumFittedContextTokens.",
                        modelName, required, promptTokens, numPredict, ceiling, OllamaRequestBudgetPolicyKey);
                }
                else
                {
                    logger.LogInformation(
                        "Raised the Ollama context for model {Model} from {Configured} to {Raised} tokens for this request so the prompt ({PromptTokens} estimated tokens incl. native tool schemas) and {NumPredict} output tokens fit.",
                        modelName, configuredContextTokens?.ToString() ?? "provider default", raised, promptTokens, numPredict);
                }

                return new OllamaRequestBudget(numPredict, raised, promptTokens, required, raised > current, exceeds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not size the Ollama request for model {Model}; the caller's output and context values are used unchanged.", modelName);
                return new OllamaRequestBudget(Math.Clamp(requestedOutputTokens, 64, 262144), configuredContextTokens, 0, 0, false, false);
            }
        }

        /// <summary>Reads the database-backed request budget policy, falling back to the documented defaults with a warning.</summary>
        /// <param name="logger">Logger of the calling chat client.</param>
        /// <returns>The current policy.</returns>
        private OllamaRequestBudgetPolicy ReadOllamaRequestBudgetPolicy(ILogger logger)
        {
            try
            {
                return runtimePolicy.GetJson<OllamaRequestBudgetPolicy>(OllamaRequestBudgetPolicyKey);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Runtime value {PolicyKey} is unavailable; using the built-in Ollama request budget defaults.", OllamaRequestBudgetPolicyKey);
                return new OllamaRequestBudgetPolicy();
            }
        }
    }
}
