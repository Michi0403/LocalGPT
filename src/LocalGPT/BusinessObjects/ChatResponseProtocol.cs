namespace LocalGPT.BusinessObjects;

/// <summary>
/// Identifies the wire/text convention emitted by a model. Each non-auto
/// protocol has its own DI profile so control tokens from one model family
/// are never applied to another family's response stream.
/// </summary>
public enum ChatResponseProtocol
{
    /// <summary>
    /// Selects the auto option for <see cref="ChatResponseProtocol"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Auto,
    /// <summary>
    /// Selects the plain text option for <see cref="ChatResponseProtocol"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PlainText,
    /// <summary>
    /// Selects the think tags option for <see cref="ChatResponseProtocol"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ThinkTags,
    /// <summary>
    /// Selects the harmony option for <see cref="ChatResponseProtocol"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Harmony,
    /// <summary>
    /// Selects the deep seek option for <see cref="ChatResponseProtocol"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DeepSeek,
    /// <summary>
    /// Selects the gemma option for <see cref="ChatResponseProtocol"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Gemma,
    /// <summary>
    /// Selects the apple option for <see cref="ChatResponseProtocol"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Apple
}
