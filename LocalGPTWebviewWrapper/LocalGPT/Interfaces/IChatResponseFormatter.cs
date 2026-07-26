using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IChatResponseFormatter
{
    IEnumerable<string> AppendThinking(string text);
    IEnumerable<string> AppendContent(string text);
    IEnumerable<string> Complete();
}

public interface IChatResponseFormatterFactory
{
    IChatResponseFormatter Create(ChatResponseProtocol protocol, string? missingFinalAnswerNotice = null);
}

public interface IChatProtocolResolver
{
    ChatResponseProtocol Resolve(OllamaCoreOptions options);
}

/// <summary>
/// A model-family-specific protocol boundary. Profiles are stateless and
/// selected once per response stream; they must never modify another
/// protocol's content.
/// </summary>
public interface IChatProtocolProfile
{
    ChatResponseProtocol Protocol { get; }
    int Priority { get; }
    bool MatchesModel(string modelName);
    string NormalizeThinking(string text);
    string NormalizeContent(string text);
}
