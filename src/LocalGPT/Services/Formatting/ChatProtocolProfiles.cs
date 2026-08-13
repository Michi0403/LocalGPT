using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Formatting;

/// <summary>
/// Coordinates chat protocol text behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the chat protocol text workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ChatProtocolTextService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ChatProtocolTextService> logger) : IChatProtocolTextService
{
    /// <summary>
    /// Performs contains any as part of the chat protocol text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the chat protocol text operation and used when producing its result.</param>
    /// <param name="collection">Collection value supplied to the chat protocol text operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool ContainsAny(string value, LocalGptRuntimeCollection collection)
    {
        try
        {
            var source = value ?? string.Empty;
            var result = runtimePolicy.GetCollection(collection)
                .Any(needle => source.Contains(needle, StringComparison.OrdinalIgnoreCase));
            logger.LogTrace($"Checked chat protocol hints from {collection}; matched={result}.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not check chat protocol hints from {collection}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs replace all as part of the chat protocol text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the chat protocol text operation and used when producing its result.</param>
    /// <param name="collection">Collection value supplied to the chat protocol text operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ReplaceAll(string text, LocalGptRuntimeCollection collection)
    {
        try
        {
            var result = text ?? string.Empty;
            foreach (var token in runtimePolicy.GetCollection(collection))
                result = result.Replace(token, string.Empty, StringComparison.OrdinalIgnoreCase);
            logger.LogTrace($"Normalized chat protocol content using {collection}.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize chat protocol content using {collection}: {exception.Message}");
            throw;
        }
    }
}

/// <summary>
/// Maintains the authoritative directory of chat protocol profile entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="profiles">Chat protocol profile dependency used by the chat protocol profile workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ChatProtocolProfileCatalog(
    IEnumerable<IChatProtocolProfile> profiles,
    ILogger<ChatProtocolProfileCatalog> logger) : IChatProtocolProfileCatalog
{
    /// <summary>
    /// Gets the profiles collection maintained or exposed by this chat protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The profiles value exposed by <see cref="ChatProtocolProfileCatalog"/>.</value>
    public IReadOnlyList<IChatProtocolProfile> Profiles { get; } = profiles
        .OrderByDescending(profile => profile.Priority)
        .ToArray();

    /// <summary>
    /// Resolves exact in the chat protocol profile directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="protocol">Protocol value supplied to the chat protocol profile operation and used when producing its result.</param>
    /// <returns>The i chat protocol profile produced by the operation.</returns>
    public IChatProtocolProfile ResolveExact(ChatResponseProtocol protocol)
    {
        try
        {
            var profile = Profiles.FirstOrDefault(candidate => candidate.Protocol == protocol)
                ?? Profiles.FirstOrDefault(candidate => candidate.Protocol == ChatResponseProtocol.PlainText)
                ?? throw new InvalidOperationException("No plain-text chat protocol profile is registered.");
            logger.LogTrace($"Resolved exact chat protocol profile {profile.Protocol} for {protocol}.");
            return profile;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve exact chat protocol profile {protocol}: {exception.Message}");
            throw;
        }
    }
}

/// <summary>
/// Represents a harmony chat protocol profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="text">Chat protocol text service dependency used by the harmony chat protocol profile workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class HarmonyChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<HarmonyChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets the protocol value that forms part of the harmony chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol value exposed by <see cref="HarmonyChatProtocolProfile"/>.</value>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.Harmony;
    /// <summary>
    /// Gets the priority value that forms part of the harmony chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="HarmonyChatProtocolProfile"/>.</value>
    public int Priority => 100;

    /// <summary>
    /// Performs matches model for <see cref="HarmonyChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding harmony chat protocol profile workflow.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the harmony chat protocol profile operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool MatchesModel(string modelName)
    {
        try
        {
            var result = text.ContainsAny(modelName, LocalGptRuntimeCollection.ChatHarmonyModelHints);
            logger.LogTrace($"Evaluated Harmony chat protocol match; matched={result}.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not evaluate Harmony chat protocol match: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes thinking for <see cref="HarmonyChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding harmony chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the harmony chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeThinking(string value)
    {
        try
        {
            logger.LogTrace($"Normalized Harmony thinking content without token removal.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize Harmony thinking content: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes content for <see cref="HarmonyChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding harmony chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the harmony chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeContent(string value)
    {
        try
        {
            logger.LogTrace($"Normalized Harmony visible content without token removal.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize Harmony visible content: {exception.Message}");
            throw;
        }
    }
}

/// <summary>
/// Represents a deep seek chat protocol profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="text">Chat protocol text service dependency used by the deep seek chat protocol profile workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DeepSeekChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<DeepSeekChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets the protocol value that forms part of the deep seek chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol value exposed by <see cref="DeepSeekChatProtocolProfile"/>.</value>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.DeepSeek;
    /// <summary>
    /// Gets the priority value that forms part of the deep seek chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="DeepSeekChatProtocolProfile"/>.</value>
    public int Priority => 90;

    /// <summary>
    /// Performs matches model for <see cref="DeepSeekChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding deep seek chat protocol profile workflow.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the deep seek chat protocol profile operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool MatchesModel(string modelName)
    {
        try
        {
            var result = text.ContainsAny(modelName, LocalGptRuntimeCollection.ChatDeepSeekModelHints);
            logger.LogTrace($"Evaluated DeepSeek chat protocol match; matched={result}.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not evaluate DeepSeek chat protocol match: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes thinking for <see cref="DeepSeekChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding deep seek chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the deep seek chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeThinking(string value) {
        try
        {
            logger.LogTrace($"Entering DeepSeekChatProtocolProfile.NormalizeThinking.");
            return Normalize(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"DeepSeekChatProtocolProfile.NormalizeThinking failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Normalizes content for <see cref="DeepSeekChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding deep seek chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the deep seek chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeContent(string value) {
        try
        {
            logger.LogTrace($"Entering DeepSeekChatProtocolProfile.NormalizeContent.");
            return Normalize(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"DeepSeekChatProtocolProfile.NormalizeContent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs normalize for <see cref="DeepSeekChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding deep seek chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the deep seek chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Normalize(string value)
    {
        try
        {
            var result = text.ReplaceAll(value, LocalGptRuntimeCollection.ChatDeepSeekControlTokens);
            logger.LogTrace($"Normalized DeepSeek chat protocol content.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize DeepSeek chat protocol content: {exception.Message}");
            throw;
        }
    }
}

/// <summary>
/// Represents a gemma chat protocol profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="text">Chat protocol text service dependency used by the gemma chat protocol profile workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GemmaChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<GemmaChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets the protocol value that forms part of the gemma chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol value exposed by <see cref="GemmaChatProtocolProfile"/>.</value>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.Gemma;
    /// <summary>
    /// Gets the priority value that forms part of the gemma chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="GemmaChatProtocolProfile"/>.</value>
    public int Priority => 80;

    /// <summary>
    /// Performs matches model for <see cref="GemmaChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding gemma chat protocol profile workflow.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the gemma chat protocol profile operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool MatchesModel(string modelName)
    {
        try
        {
            var result = text.ContainsAny(modelName, LocalGptRuntimeCollection.ChatGemmaModelHints);
            logger.LogTrace($"Evaluated Gemma chat protocol match; matched={result}.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not evaluate Gemma chat protocol match: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes thinking for <see cref="GemmaChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding gemma chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the gemma chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeThinking(string value) {
        try
        {
            logger.LogTrace($"Entering GemmaChatProtocolProfile.NormalizeThinking.");
            return Normalize(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"GemmaChatProtocolProfile.NormalizeThinking failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Normalizes content for <see cref="GemmaChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding gemma chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the gemma chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeContent(string value) {
        try
        {
            logger.LogTrace($"Entering GemmaChatProtocolProfile.NormalizeContent.");
            return Normalize(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"GemmaChatProtocolProfile.NormalizeContent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs normalize for <see cref="GemmaChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding gemma chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the gemma chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Normalize(string value)
    {
        try
        {
            var result = text.ReplaceAll(value, LocalGptRuntimeCollection.ChatGemmaControlTokens);
            logger.LogTrace($"Normalized Gemma chat protocol content.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize Gemma chat protocol content: {exception.Message}");
            throw;
        }
    }
}

/// <summary>
/// Represents an apple chat protocol profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="text">Chat protocol text service dependency used by the apple chat protocol profile workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class AppleChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<AppleChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets the protocol value that forms part of the apple chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol value exposed by <see cref="AppleChatProtocolProfile"/>.</value>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.Apple;
    /// <summary>
    /// Gets the priority value that forms part of the apple chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="AppleChatProtocolProfile"/>.</value>
    public int Priority => 70;

    /// <summary>
    /// Performs matches model for <see cref="AppleChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding apple chat protocol profile workflow.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the apple chat protocol profile operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool MatchesModel(string modelName)
    {
        try
        {
            var result = text.ContainsAny(modelName, LocalGptRuntimeCollection.ChatAppleModelHints);
            logger.LogTrace($"Evaluated Apple chat protocol match; matched={result}.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not evaluate Apple chat protocol match: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes thinking for <see cref="AppleChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding apple chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the apple chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeThinking(string value) {
        try
        {
            logger.LogTrace($"Entering AppleChatProtocolProfile.NormalizeThinking.");
            return Normalize(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"AppleChatProtocolProfile.NormalizeThinking failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Normalizes content for <see cref="AppleChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding apple chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the apple chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeContent(string value) {
        try
        {
            logger.LogTrace($"Entering AppleChatProtocolProfile.NormalizeContent.");
            return Normalize(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"AppleChatProtocolProfile.NormalizeContent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs normalize for <see cref="AppleChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding apple chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the apple chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Normalize(string value)
    {
        try
        {
            var result = text.ReplaceAll(value, LocalGptRuntimeCollection.ChatAppleControlTokens);
            logger.LogTrace($"Normalized Apple chat protocol content.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize Apple chat protocol content: {exception.Message}");
            throw;
        }
    }
}

/// <summary>
/// Represents a think tags chat protocol profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="text">Chat protocol text service dependency used by the think tags chat protocol profile workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ThinkTagsChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<ThinkTagsChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets the protocol value that forms part of the think tags chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol value exposed by <see cref="ThinkTagsChatProtocolProfile"/>.</value>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.ThinkTags;
    /// <summary>
    /// Gets the priority value that forms part of the think tags chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="ThinkTagsChatProtocolProfile"/>.</value>
    public int Priority => 50;

    /// <summary>
    /// Performs matches model for <see cref="ThinkTagsChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding think tags chat protocol profile workflow.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the think tags chat protocol profile operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool MatchesModel(string modelName)
    {
        try
        {
            var result = text.ContainsAny(modelName, LocalGptRuntimeCollection.ChatThinkTagsModelHints);
            logger.LogTrace($"Evaluated think-tag chat protocol match; matched={result}.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not evaluate think-tag chat protocol match: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes thinking for <see cref="ThinkTagsChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding think tags chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the think tags chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeThinking(string value)
    {
        try
        {
            logger.LogTrace($"Normalized think-tag thinking content without token removal.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize think-tag thinking content: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes content for <see cref="ThinkTagsChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding think tags chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the think tags chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeContent(string value)
    {
        try
        {
            logger.LogTrace($"Normalized think-tag visible content without token removal.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize think-tag visible content: {exception.Message}");
            throw;
        }
    }
}

/// <summary>
/// Represents a plain text chat protocol profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PlainTextChatProtocolProfile(
    ILogger<PlainTextChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets the protocol value that forms part of the plain text chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol value exposed by <see cref="PlainTextChatProtocolProfile"/>.</value>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.PlainText;
    /// <summary>
    /// Gets the priority value that forms part of the plain text chat protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="PlainTextChatProtocolProfile"/>.</value>
    public int Priority => 0;

    /// <summary>
    /// Performs matches model for <see cref="PlainTextChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding plain text chat protocol profile workflow.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the plain text chat protocol profile operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool MatchesModel(string modelName)
    {
        try
        {
            logger.LogTrace($"Plain-text chat protocol is the fallback and does not match model names directly.");
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not evaluate plain-text chat protocol match: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes thinking for <see cref="PlainTextChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding plain text chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the plain text chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeThinking(string value)
    {
        try
        {
            logger.LogTrace($"Normalized plain-text thinking content without token removal.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize plain-text thinking content: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes content for <see cref="PlainTextChatProtocolProfile"/>, keeping the operation consistent with the state and invariants of the surrounding plain text chat protocol profile workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the plain text chat protocol profile operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeContent(string value)
    {
        try
        {
            logger.LogTrace($"Normalized plain-text visible content without token removal.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize plain-text visible content: {exception.Message}");
            throw;
        }
    }
}
