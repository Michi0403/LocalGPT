using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the chat response formatter contract.
/// </summary>
public interface IChatResponseFormatter
{
    /// <summary>
    /// Runs the append thinking operation.
    /// </summary>
    IEnumerable<string> AppendThinking(string text);
    /// <summary>
    /// Runs the append content operation.
    /// </summary>
    IEnumerable<string> AppendContent(string text);
    /// <summary>
    /// Runs the complete operation.
    /// </summary>
    IEnumerable<string> Complete();
}

/// <summary>
/// Defines the chat response formatter factory contract.
/// </summary>
public interface IChatResponseFormatterFactory
{
    /// <summary>
    /// Runs the create operation.
    /// </summary>
    IChatResponseFormatter Create(ChatResponseProtocol protocol, string? missingFinalAnswerNotice = null);
}

/// <summary>
/// Defines the chat protocol resolver contract.
/// </summary>
public interface IChatProtocolResolver
{
    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
    ChatResponseProtocol Resolve(OllamaCoreOptions options);
}

/// <summary>
/// Defines the chat protocol profile catalog contract.
/// </summary>
public interface IChatProtocolProfileCatalog
{
    IReadOnlyList<IChatProtocolProfile> Profiles { get; }
    /// <summary>
    /// Resolves exact.
    /// </summary>
    IChatProtocolProfile ResolveExact(ChatResponseProtocol protocol);
}

/// <summary>
/// Defines the chat protocol text service contract.
/// </summary>
public interface IChatProtocolTextService
{
    /// <summary>
    /// Runs the contains any operation.
    /// </summary>
    bool ContainsAny(string value, LocalGptRuntimeCollection collection);
    /// <summary>
    /// Runs the replace all operation.
    /// </summary>
    string ReplaceAll(string text, LocalGptRuntimeCollection collection);
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
    /// <summary>
    /// Runs the matches model operation.
    /// </summary>
    bool MatchesModel(string modelName);
    /// <summary>
    /// Normalizes thinking.
    /// </summary>
    string NormalizeThinking(string text);
    /// <summary>
    /// Normalizes content.
    /// </summary>
    string NormalizeContent(string text);
}
