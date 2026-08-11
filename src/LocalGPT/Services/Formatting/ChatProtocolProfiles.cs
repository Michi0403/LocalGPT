using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Formatting;

/// <summary>
/// Provides chat protocol text service operations.
/// </summary>
public sealed class ChatProtocolTextService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ChatProtocolTextService> logger) : IChatProtocolTextService
{
    /// <summary>
    /// Runs the contains any operation.
    /// </summary>
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
    /// Runs the replace all operation.
    /// </summary>
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
/// Provides chat protocol profile catalog operations.
/// </summary>
public sealed class ChatProtocolProfileCatalog(
    IEnumerable<IChatProtocolProfile> profiles,
    ILogger<ChatProtocolProfileCatalog> logger) : IChatProtocolProfileCatalog
{
    /// <summary>
    /// Gets or sets profiles.
    /// </summary>
    public IReadOnlyList<IChatProtocolProfile> Profiles { get; } = profiles
        .OrderByDescending(profile => profile.Priority)
        .ToArray();

    /// <summary>
    /// Resolves exact.
    /// </summary>
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
/// Represents a harmony chat protocol profile.
/// </summary>
public sealed class HarmonyChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<HarmonyChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets or sets protocol.
    /// </summary>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.Harmony;
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Runs the matches model operation.
    /// </summary>
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
    /// Normalizes thinking.
    /// </summary>
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
    /// Normalizes content.
    /// </summary>
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
/// Represents a deep seek chat protocol profile.
/// </summary>
public sealed class DeepSeekChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<DeepSeekChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets or sets protocol.
    /// </summary>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.DeepSeek;
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public int Priority => 90;

    /// <summary>
    /// Runs the matches model operation.
    /// </summary>
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
    /// Normalizes thinking.
    /// </summary>
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
    /// Normalizes content.
    /// </summary>
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
    /// Runs the normalize operation.
    /// </summary>
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
/// Represents a gemma chat protocol profile.
/// </summary>
public sealed class GemmaChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<GemmaChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets or sets protocol.
    /// </summary>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.Gemma;
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public int Priority => 80;

    /// <summary>
    /// Runs the matches model operation.
    /// </summary>
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
    /// Normalizes thinking.
    /// </summary>
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
    /// Normalizes content.
    /// </summary>
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
    /// Runs the normalize operation.
    /// </summary>
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
/// Represents an apple chat protocol profile.
/// </summary>
public sealed class AppleChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<AppleChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets or sets protocol.
    /// </summary>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.Apple;
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public int Priority => 70;

    /// <summary>
    /// Runs the matches model operation.
    /// </summary>
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
    /// Normalizes thinking.
    /// </summary>
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
    /// Normalizes content.
    /// </summary>
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
    /// Runs the normalize operation.
    /// </summary>
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
/// Represents a think tags chat protocol profile.
/// </summary>
public sealed class ThinkTagsChatProtocolProfile(
    IChatProtocolTextService text,
    ILogger<ThinkTagsChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets or sets protocol.
    /// </summary>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.ThinkTags;
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public int Priority => 50;

    /// <summary>
    /// Runs the matches model operation.
    /// </summary>
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
    /// Normalizes thinking.
    /// </summary>
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
    /// Normalizes content.
    /// </summary>
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
/// Represents a plain text chat protocol profile.
/// </summary>
public sealed class PlainTextChatProtocolProfile(
    ILogger<PlainTextChatProtocolProfile> logger) : IChatProtocolProfile
{
    /// <summary>
    /// Gets or sets protocol.
    /// </summary>
    public ChatResponseProtocol Protocol => ChatResponseProtocol.PlainText;
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public int Priority => 0;

    /// <summary>
    /// Runs the matches model operation.
    /// </summary>
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
    /// Normalizes thinking.
    /// </summary>
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
    /// Normalizes content.
    /// </summary>
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
