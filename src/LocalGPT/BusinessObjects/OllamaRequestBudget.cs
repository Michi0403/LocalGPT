namespace LocalGPT.BusinessObjects;

/// <summary>
/// Effective output and context sizes for one native Ollama chat request after the prompt, native tool schemas and
/// thinking reserve were measured against the configured context window.
/// </summary>
/// <param name="NumPredict">Tokens Ollama may generate in total (thinking plus visible answer).</param>
/// <param name="NumCtx">Context window to send; <see langword="null"/> keeps the provider default.</param>
/// <param name="EstimatedPromptTokens">Conservative estimate of the prompt and tool-schema tokens.</param>
/// <param name="RequiredContextTokens">Estimated prompt tokens plus output and safety margin.</param>
/// <param name="ContextRaised">Whether the configured context was raised so the prompt fits.</param>
/// <param name="PromptExceedsMaximum">Whether the request still exceeds the largest permitted context window.</param>
public sealed record OllamaRequestBudget(
    int NumPredict,
    int? NumCtx,
    int EstimatedPromptTokens,
    int RequiredContextTokens,
    bool ContextRaised,
    bool PromptExceedsMaximum);
