namespace LocalGPT.BusinessObjects;

/// <summary>
/// Database-backed sizing rules (runtime value <c>OllamaRequestBudgetPolicyJson</c>) that keep one native Ollama chat
/// request inside its context window: conversation messages, native tool schemas, model thinking and the visible answer.
/// </summary>
public sealed class OllamaRequestBudgetPolicy
{
    /// <summary>Gets or sets the average number of prompt characters per token used for the conservative size estimate.</summary>
    /// <value>Characters per token; values outside 0.5-16 fall back to the built-in estimate.</value>
    public double CharactersPerToken { get; set; } = 3.2;

    /// <summary>Gets or sets the extra output tokens reserved for model thinking when LocalGPT requests explicit thinking.</summary>
    /// <value>Thinking reserve added on top of the caller's visible-answer budget.</value>
    public int ThinkingReserveTokens { get; set; } = 2048;

    /// <summary>Gets or sets the tokens kept free between the estimated prompt plus output and the context window.</summary>
    /// <value>Safety margin in tokens.</value>
    public int ContextSafetyMarginTokens { get; set; } = 512;

    /// <summary>Gets or sets the granularity used when the context window is raised so a prompt fits.</summary>
    /// <value>Growth step in tokens.</value>
    public int ContextGrowthStepTokens { get; set; } = 1024;

    /// <summary>Gets or sets the upper bound the context window may be raised to for one request.</summary>
    /// <value>Maximum fitted context in tokens; the configured road context is never lowered.</value>
    public int MaximumFittedContextTokens { get; set; } = 32768;

    /// <summary>Gets or sets the context Ollama is assumed to use when LocalGPT sends no explicit context size.</summary>
    /// <value>Assumed provider default context in tokens.</value>
    public int AssumedProviderDefaultContextTokens { get; set; } = 4096;

    /// <summary>Gets or sets the upper bound for the number of predicted tokens (thinking plus visible answer).</summary>
    /// <value>Maximum output tokens.</value>
    public int MaximumOutputTokens { get; set; } = 262144;
}
