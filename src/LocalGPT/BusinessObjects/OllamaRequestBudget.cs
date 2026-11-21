namespace LocalGPT.BusinessObjects;

/// <summary>
/// Effective output and configured context sizes for a native Ollama chat request, alongside an advisory
/// prompt/output estimate that never substitutes for the Ollama provider's actual context validation.
/// </summary>
/// <param name="NumPredict">Tokens Ollama may generate in total (thinking plus visible answer).</param>
/// <param name="NumCtx">Context window to send; <see langword="null"/> keeps the provider default.</param>
/// <param name="EstimatedPromptTokens">Conservative estimate of the prompt and tool-schema tokens.</param>
/// <param name="RequiredContextTokens">Estimated prompt tokens plus output and safety margin.</param>
/// <param name="ContextRaised">Whether the configured context was raised so the prompt fits.</param>
/// <param name="PromptExceedsMaximum">Whether the estimated demand exceeds the configured context (not proof of provider failure).</param>
public sealed record OllamaRequestBudget(
    int NumPredict,
    int? NumCtx,
    int EstimatedPromptTokens,
    int RequiredContextTokens,
    bool ContextRaised,
    bool PromptExceedsMaximum);
