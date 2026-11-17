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
        /// is capped by the selected hardware road and the advertised model maximum. Oversized work is returned for replanning.
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
                var configuredBeforeModelLimit = configuredContextTokens;
                var modelLimit = modelMaximumContextTokens is > 0 ? modelMaximumContextTokens : null;
                if (modelLimit is not null && configuredContextTokens is > 0)
                    configuredContextTokens = Math.Min(configuredContextTokens.Value, modelLimit.Value);
                var current = configuredContextTokens is > 0
                    ? configuredContextTokens.Value
                    : Math.Max(256, policy.AssumedProviderDefaultContextTokens);
                if (modelLimit is not null)
                    current = Math.Min(current, modelLimit.Value);
                if (required <= current)
                {
                    if (modelLimit is not null)
                        logger.LogInformation(
                            "Ollama context for model {Model}: configured {Configured}, model maximum {ModelMaximum}, effective context {Context} tokens, effective output {NumPredict} tokens (configuration is unchanged).",
                            modelName, configuredBeforeModelLimit?.ToString() ?? "provider default", modelLimit, current, numPredict);
                    logger.LogDebug(
                        "Ollama request for model {Model} fits its context: estimated prompt {PromptTokens} tokens, output {NumPredict} tokens (thinking reserve {ThinkingReserved}), context {Context}.",
                        modelName, promptTokens, numPredict, thinkingRequested, configuredContextTokens?.ToString() ?? "provider default");
                    return new OllamaRequestBudget(numPredict, current, promptTokens, required, false, false);
                }

                // The model's theoretical limit is not a memory budget. The selected
                // road (or conservative explicit default) is the maximum allocation.
                var ceiling = current;
                var exceeds = required > ceiling;
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
                        exceeds = required > ceiling;
                    }
                }
                if (exceeds)
                {
                    logger.LogWarning(
                        "Ollama request for model {Model} needs about {Required} context tokens (prompt {PromptTokens}, output {NumPredict}) but the selected hardware context is {Ceiling}; the request will not be sent. Split the work into smaller bounded requests.",
                        modelName, required, promptTokens, numPredict, ceiling);
                }
                if (modelLimit is not null)
                    logger.LogInformation(
                        "Ollama context for model {Model}: configured {Configured}, model maximum {ModelMaximum}, effective context {Context} tokens, effective output {NumPredict} tokens (configuration is unchanged).",
                        modelName, configuredBeforeModelLimit?.ToString() ?? "provider default", modelLimit, ceiling, numPredict);
                return new OllamaRequestBudget(numPredict, ceiling, promptTokens, required, false, exceeds);
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
        /// <returns>The allocation, bounded by the original hardware and model ceiling.</returns>
        public int? FitOllamaRequestContext(OllamaRequestBudget budget, string modelName, ILogger logger)
        {
            try
            {
                if (budget.PromptExceedsMaximum || budget.NumCtx is not int ceiling)
                    return budget.NumCtx;
                var increment = Math.Max(1, ReadOllamaRequestBudgetPolicy(logger).ContextGrowthStepTokens);
                var rounded = ((long)budget.RequiredContextTokens + increment - 1) / increment * increment;
                var fitted = (int)Math.Min(ceiling, rounded);
                logger.LogInformation("Ollama model {Model}: allocated context {Allocated}, hardware/model ceiling {Ceiling}, required estimate {Required}.",
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
