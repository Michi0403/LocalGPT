namespace LocalGPT.BusinessObjects;

/// <summary>
/// Identifies the wire/text convention emitted by a model. Each non-auto
/// protocol has its own DI profile so control tokens from one model family
/// are never applied to another family's response stream.
/// </summary>
public enum ChatResponseProtocol
{
    Auto,
    PlainText,
    ThinkTags,
    Harmony,
    DeepSeek,
    Gemma,
    Apple
}
