using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>
    /// Estimates native Ollama prompt and output demand for diagnostics and optional allocation fitting.
    /// An estimate cannot prove the provider's actual token count or replace its context decision.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
        /// <summary>Runtime value that stores the <see cref="OllamaRequestBudgetPolicy"/> document.</summary>
        private const string OllamaRequestBudgetPolicyKey = "OllamaRequestBudgetPolicyJson";

        /// <summary>Visible-answer default for native Ollama requests without caller-supplied ChatOptions, read from persisted runtime policy.</summary>
        /// <value>Configured default native Ollama output tokens.</value>
        public int OllamaThinkingChatClientDefaultOutputTokens => runtimePolicy.GetInt("DefaultOllamaRequestOutputTokens");

        /// <summary>Classifies explicit Ollama provider context errors without interpreting LocalGPT's own size estimate as a failure.</summary>
        /// <param name="error">Provider HTTP response body or exception text.</param>
        /// <returns>Whether the provider identified a context-capacity problem.</returns>
        public bool OllamaThinkingChatClientIsProviderContextRejection(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return false;
            return (error.Contains("context", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("num_ctx", StringComparison.OrdinalIgnoreCase)) &&
                   (error.Contains("exceed", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("too long", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("too large", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("maximum", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("limit", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Reads the operator's choice to use approximate rather than full configured Ollama context allocations.</summary>
        /// <param name="logger">Logger of the calling Ollama client.</param>
        /// <returns>True only when the persisted request policy opts into estimated fitting.</returns>
        public bool OllamaThinkingChatClientUseEstimatedContextFitting(ILogger logger) =>
            ReadOllamaRequestBudgetPolicy(logger).UseEstimatedContextFitting;

        /// <summary>
        /// Computes the effective <c>num_predict</c> and <c>num_ctx</c> for one Ollama chat request. Explicit thinking
        /// receives its own output reserve so it no longer consumes the visible answer budget, and the context window
        /// follows the configured hardware road, not the model_info advisory value. Estimates are diagnostic;
        /// actual context validation belongs to Ollama, which can use a different tokenizer or runtime configuration.
        /// </summary>
        /// <param name="configuredContextTokens">Context chosen by the hardware road or caller; <see langword="null"/> means provider default.</param>
        /// <param name="requestedOutputTokens">Visible-answer budget requested by the caller.</param>
        /// <param name="thinkingRequested">Whether LocalGPT requests explicit model thinking for this call.</param>
        /// <param name="promptCharacters">Characters of all messages plus serialized native tool schemas.</param>
        /// <param name="modelName">Ollama model used for diagnostics.</param>
        /// <param name="logger">Logger of the calling chat client.</param>
        /// <param name="modelMaximumContextTokens">Largest context the model supports according to Ollama; <see langword="null"/> when unknown.</param>
        /// <returns>The effective request budget.</returns>
        public OllamaRequestBudget OllamaThinkingChatClientResolveRequestBudget(
            int? configuredContextTokens,
            int requestedOutputTokens,
            bool thinkingRequested,
            long promptCharacters,
            string modelName,
            ILogger logger,
            int? modelMaximumContextTokens = null)
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
                // The provider's model_info context_length is capability metadata, not a
                // command to override an explicitly selected hardware-road num_ctx. Ollama
                // owns validation of the actual request; this estimate is diagnostic only.
                var configuredContext = configuredContextTokens is > 0 ? configuredContextTokens : null;
                var assumedContext = Math.Max(256, policy.AssumedProviderDefaultContextTokens);
                var comparisonContext = configuredContext ?? assumedContext;
                var exceedsEstimate = required > comparisonContext;
                if (modelMaximumContextTokens is > 0 && configuredContext is > 0 &&
                    configuredContext > modelMaximumContextTokens)
                    logger.LogInformation(
                        "Ollama model {Model} advertises {Advertised} context tokens; the selected hardware road requests {Configured}. Sending that configured num_ctx unchanged and letting Ollama validate it.",
                        modelName, modelMaximumContextTokens, configuredContext);
                if (exceedsEstimate)
                    logger.LogWarning(
                        "Ollama estimated context demand for {Model}: prompt {PromptTokens}, output {OutputTokens}, safety margin {Margin}, total {Required}, " +
                        "configured context {Configured} (advertised model context {Advertised}). This is not an Ollama rejection; the request will be sent without reducing the caller's output or selected road context.",
                        modelName, promptTokens, numPredict, Math.Max(0, policy.ContextSafetyMarginTokens), required,
                        configuredContext?.ToString() ?? $"provider default (estimated {assumedContext})",
                        modelMaximumContextTokens?.ToString() ?? "unknown");
                else
                    logger.LogDebug(
                        "Ollama estimated context for {Model}: prompt {PromptTokens}, output {OutputTokens}, total {Required}; configured {Configured}, advertised {Advertised} (provider determines actual fit).",
                        modelName, promptTokens, numPredict, required,
                        configuredContext?.ToString() ?? "provider default",
                        modelMaximumContextTokens?.ToString() ?? "unknown");
                return new OllamaRequestBudget(numPredict, configuredContext, promptTokens, required, false, exceedsEstimate);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not size the Ollama request for model {Model}; an unmeasured request will not be sent.", modelName);
                // Unknown fit is not a successful preflight.
                throw;
            }
        }

        /// <summary>Allocates the smallest configured context increment covering the complete fitted payload.</summary>
        /// <param name="budget">Measured request including output and safety reserve.</param>
        /// <param name="modelName">Selected provider model for diagnostics.</param>
        /// <param name="logger">Calling provider logger.</param>
        /// <returns>The allocation within the explicitly configured road, or the provider default when unspecified.</returns>
        public int? FitOllamaRequestContext(OllamaRequestBudget budget, string modelName, ILogger logger)
        {
            try
            {
                if (budget.PromptExceedsMaximum || budget.NumCtx is not int ceiling)
                    return budget.NumCtx;
                var increment = Math.Max(1, ReadOllamaRequestBudgetPolicy(logger).ContextGrowthStepTokens);
                var rounded = ((long)budget.RequiredContextTokens + increment - 1) / increment * increment;
                var fitted = (int)Math.Min(ceiling, rounded);
                logger.LogInformation("Ollama model {Model}: opt-in estimated context allocation {Allocated}, configured road ceiling {Ceiling}, required estimate {Required}.",
                    modelName, fitted, ceiling, budget.RequiredContextTokens);
                return fitted;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not fit the measured context allocation for {Model}.", modelName);
                throw;
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
