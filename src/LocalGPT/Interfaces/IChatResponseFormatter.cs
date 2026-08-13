using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for chat response formatter behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IChatResponseFormatter
{
    /// <summary>
    /// Performs append thinking for <see cref="IChatResponseFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding chat response formatter workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the chat response formatter operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IEnumerable<string> AppendThinking(string text);
    /// <summary>
    /// Performs append content for <see cref="IChatResponseFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding chat response formatter workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the chat response formatter operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IEnumerable<string> AppendContent(string text);
    /// <summary>
    /// Performs complete for <see cref="IChatResponseFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding chat response formatter workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IEnumerable<string> Complete();
}

/// <summary>
/// Defines the contract for chat response formatter behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IChatResponseFormatterFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="IChatResponseFormatterFactory"/>.
    /// </summary>
    /// <param name="protocol">Protocol value supplied to the chat response formatter operation and used when producing its result.</param>
    /// <param name="missingFinalAnswerNotice">Missing final answer notice value supplied to the chat response formatter operation and used when producing its result.</param>
    /// <returns>The i chat response formatter produced by the operation.</returns>
    IChatResponseFormatter Create(ChatResponseProtocol protocol, string? missingFinalAnswerNotice = null);
}

/// <summary>
/// Defines the contract for chat protocol behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IChatProtocolResolver
{
    /// <summary>
    /// Performs resolve for <see cref="IChatProtocolResolver"/>, keeping the operation consistent with the state and invariants of the surrounding chat protocol workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The chat response protocol produced by the operation.</returns>
    ChatResponseProtocol Resolve(OllamaCoreOptions options);
}

/// <summary>
/// Defines the contract for chat protocol profile behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IChatProtocolProfileCatalog
{
    /// <summary>
    /// Gets the profiles collection maintained or exposed by this chat protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The profiles value exposed by <see cref="IChatProtocolProfileCatalog"/>.</value>
    IReadOnlyList<IChatProtocolProfile> Profiles { get; }
    /// <summary>
    /// Resolves exact in the chat protocol profile directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="protocol">Protocol value supplied to the chat protocol profile operation and used when producing its result.</param>
    /// <returns>The i chat protocol profile produced by the operation.</returns>
    IChatProtocolProfile ResolveExact(ChatResponseProtocol protocol);
}

/// <summary>
/// Defines the contract for chat protocol text behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IChatProtocolTextService
{
    /// <summary>
    /// Performs contains any as part of the chat protocol text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the chat protocol text operation and used when producing its result.</param>
    /// <param name="collection">Collection value supplied to the chat protocol text operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool ContainsAny(string value, LocalGptRuntimeCollection collection);
    /// <summary>
    /// Performs replace all as part of the chat protocol text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the chat protocol text operation and used when producing its result.</param>
    /// <param name="collection">Collection value supplied to the chat protocol text operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string ReplaceAll(string text, LocalGptRuntimeCollection collection);
}

/// <summary>
/// A model-family-specific protocol boundary. Profiles are stateless and
/// selected once per response stream; they must never modify another
/// protocol's content.
/// </summary>
public interface IChatProtocolProfile
{
    /// <summary>
    /// Gets the protocol value that forms part of the chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol value exposed by <see cref="IChatProtocolProfile"/>.</value>
    ChatResponseProtocol Protocol { get; }
    /// <summary>
    /// Gets the priority value that forms part of the chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="IChatProtocolProfile"/>.</value>
    int Priority { get; }
    /// <summary>
    /// Performs matches model for <see cref="IChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding chat protocol profile workflow.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the chat protocol profile operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool MatchesModel(string modelName);
    /// <summary>
    /// Normalizes thinking for <see cref="IChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding chat protocol profile workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string NormalizeThinking(string text);
    /// <summary>
    /// Normalizes content for <see cref="IChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding chat protocol profile workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string NormalizeContent(string text);
}
