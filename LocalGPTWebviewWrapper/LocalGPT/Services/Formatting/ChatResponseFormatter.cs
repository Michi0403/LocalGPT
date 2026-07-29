using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Formatting;

public sealed class ChatResponseFormatterFactory : IChatResponseFormatterFactory
{
        private readonly ILogger<ChatResponseFormatterFactory> logger;

    private readonly ILoggerFactory loggerFactory;
    private readonly IChatProtocolProfileCatalog catalog;
    private readonly ILocalGptRuntimePolicyDataService runtimePolicy;

    public ChatResponseFormatterFactory(
        ILoggerFactory loggerFactory,
        ILocalGptRuntimePolicyDataService runtimePolicy,
        IChatProtocolProfileCatalog catalog,
        ILogger<ChatResponseFormatterFactory> logger)
    {
        this.logger = logger;
        this.loggerFactory = loggerFactory;
        this.runtimePolicy = runtimePolicy;
        this.catalog = catalog;
    }

    public IChatResponseFormatter Create(ChatResponseProtocol protocol, string? missingFinalAnswerNotice = null)
    {
        try
        {
            var formatter = new ChatResponseFormatter(
                protocol,
                catalog.ResolveExact(protocol),
                catalog,
                missingFinalAnswerNotice,
                runtimePolicy,
                loggerFactory.CreateLogger<ChatResponseFormatter>());
            logger.LogTrace($"Created a chat response formatter for protocol {protocol}.");
            return formatter;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create a chat response formatter for protocol {protocol}: {exception.Message}");
            throw;
        }
    }
}

internal sealed class ChatResponseFormatter(
    ChatResponseProtocol protocol,
    IChatProtocolProfile protocolProfile,
    IChatProtocolProfileCatalog catalog,
    string? configuredMissingFinalAnswerNotice,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ChatResponseFormatter> logger)
    : IChatResponseFormatter
{
    private Regex HarmonyThinkingRegex => runtimePolicy.GetPattern(LocalGptRuntimePattern.ChatHarmonyThinking);
    private Regex HarmonyFinalRegex => runtimePolicy.GetPattern(LocalGptRuntimePattern.ChatHarmonyFinal);
    private Regex HarmonyMarkerRegex => runtimePolicy.GetPattern(LocalGptRuntimePattern.ChatHarmonyMarker);
    private string ThinkStartTag => runtimePolicy.GetString(LocalGptRuntimeValue.FormattingThinkStartTag);
    private string ThinkEndTag => runtimePolicy.GetString(LocalGptRuntimeValue.FormattingThinkEndTag);
    private int TagLookbehindLength => runtimePolicy.GetInt(LocalGptRuntimeValue.FormattingTagLookbehindLength);

    private readonly string missingFinalAnswerNotice = string.IsNullOrWhiteSpace(configuredMissingFinalAnswerNotice)
        ? runtimePolicy.GetString(LocalGptRuntimeValue.FormattingMissingFinalAnswerNotice)
        : configuredMissingFinalAnswerNotice.Trim();
    private readonly StringBuilder contentBuffer = new();
    private readonly StringBuilder harmonyBuffer = new();
    private ChatResponseProtocol activeProtocol = protocol;
    private IChatProtocolProfile activeProfile = protocolProfile;
    private int emittedHarmonyThinkingLength;
    private int emittedHarmonyFinalLength;
    private bool inTaggedThinking;
    private bool emittedExplicitThinking;
    private bool emittedVisibleContent;
    private bool emittedMissingFinalNotice;
    private bool thinkingBlockOpen;
    private bool harmonyProtocolDetected;

    public IEnumerable<string> AppendThinking(string text)
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.AppendThinking.");
                    if (string.IsNullOrEmpty(text))
                        yield break;

                    foreach (var chunk in OpenThinkingBlock())
                        yield return chunk;

                    emittedExplicitThinking = true;
                    var normalizedThinking = activeProfile.NormalizeThinking(text);
                    if (!string.IsNullOrEmpty(normalizedThinking))
                        yield return WebUtility.HtmlEncode(normalizedThinking);
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.AppendThinking.");
        }
    }

    public IEnumerable<string> AppendContent(string text)
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.AppendContent.");
                    if (string.IsNullOrEmpty(text))
                        yield break;

                    if (activeProtocol == ChatResponseProtocol.Auto)
                    {
                        var candidate = contentBuffer.ToString() + text;
                        var harmonyMarkerIndex = FindHarmonyMarkerIndex(candidate);
                        if (harmonyMarkerIndex >= 0)
                        {
                            contentBuffer.Clear();
                            if (harmonyMarkerIndex > 0)
                            {
                                foreach (var chunk in AppendTaggedContent(candidate[..harmonyMarkerIndex]))
                                    yield return chunk;
                                foreach (var chunk in CompleteTaggedContent())
                                    yield return chunk;
                            }

                            activeProtocol = ChatResponseProtocol.Harmony;
                            activeProfile = catalog.ResolveExact(activeProtocol);
                            foreach (var chunk in AppendHarmonyContent(candidate[harmonyMarkerIndex..]))
                                yield return chunk;
                            yield break;
                        }
                        if (candidate.Contains(ThinkStartTag, StringComparison.OrdinalIgnoreCase))
                        {
                            activeProtocol = ChatResponseProtocol.ThinkTags;
                            activeProfile = catalog.ResolveExact(activeProtocol);
                        }
                    }

                    text = activeProtocol == ChatResponseProtocol.Harmony
                        ? text
                        : activeProfile.NormalizeContent(text);
                    if (string.IsNullOrEmpty(text))
                        yield break;

                    if (activeProtocol == ChatResponseProtocol.Harmony)
                    {
                        foreach (var chunk in AppendHarmonyContent(text))
                            yield return chunk;
                        yield break;
                    }

                    if (activeProtocol == ChatResponseProtocol.PlainText)
                    {
                        foreach (var chunk in CloseThinkingBlock())
                            yield return chunk;
                        emittedVisibleContent = true;
                        yield return text;
                        yield break;
                    }

                    foreach (var chunk in AppendTaggedContent(text))
                        yield return chunk;
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.AppendContent.");
        }
    }

    public IEnumerable<string> Complete()
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.Complete.");
                    if (activeProtocol == ChatResponseProtocol.Harmony)
                    {
                        foreach (var chunk in CompleteHarmonyContent())
                            yield return chunk;
                    }
                    else
                    {
                        foreach (var chunk in CompleteTaggedContent())
                            yield return chunk;
                    }

                    if (!emittedVisibleContent && emittedExplicitThinking)
                    {
                        foreach (var chunk in EmitMissingFinalNotice())
                            yield return chunk;
                    }
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.Complete.");
        }
    }

    private IEnumerable<string> AppendHarmonyContent(string text)
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.AppendHarmonyContent.");
                    harmonyBuffer.Append(text);
                    var raw = harmonyBuffer.ToString();

                    if (!harmonyProtocolDetected)
                    {
                        var markerIndex = FindHarmonyMarkerIndex(raw);
                        if (markerIndex < 0)
                            yield break;

                        if (markerIndex > 0)
                        {
                            foreach (var chunk in AppendTaggedContent(raw[..markerIndex]))
                                yield return chunk;
                            foreach (var chunk in CompleteTaggedContent())
                                yield return chunk;
                            harmonyBuffer.Remove(0, markerIndex);
                            raw = harmonyBuffer.ToString();
                        }

                        harmonyProtocolDetected = true;
                    }

                    foreach (var chunk in EmitHarmonyDeltas(raw))
                        yield return chunk;
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.AppendHarmonyContent.");
        }
    }

    private IEnumerable<string> AppendTaggedContent(string text)
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.AppendTaggedContent.");
                    contentBuffer.Append(text);
                    while (contentBuffer.Length > 0)
                    {
                        var current = contentBuffer.ToString();
                        if (inTaggedThinking)
                        {
                            var endIndex = current.IndexOf(ThinkEndTag, StringComparison.OrdinalIgnoreCase);
                            if (endIndex >= 0)
                            {
                                if (endIndex > 0)
                                    yield return WebUtility.HtmlEncode(current[..endIndex]);
                                contentBuffer.Remove(0, endIndex + ThinkEndTag.Length);
                                foreach (var chunk in CloseThinkingBlock())
                                    yield return chunk;
                                inTaggedThinking = false;
                                continue;
                            }

                            var safeLength = GetSafeFlushLength(current);
                            if (safeLength <= 0)
                                yield break;
                            yield return WebUtility.HtmlEncode(current[..safeLength]);
                            contentBuffer.Remove(0, safeLength);
                            continue;
                        }

                        var startIndex = current.IndexOf(ThinkStartTag, StringComparison.OrdinalIgnoreCase);
                        if (startIndex >= 0)
                        {
                            if (startIndex > 0)
                            {
                                foreach (var chunk in CloseThinkingBlock())
                                    yield return chunk;
                                emittedVisibleContent = true;
                                yield return current[..startIndex];
                            }
                            contentBuffer.Remove(0, startIndex + ThinkStartTag.Length);
                            foreach (var chunk in OpenThinkingBlock())
                                yield return chunk;
                            emittedExplicitThinking = true;
                            inTaggedThinking = true;
                            continue;
                        }

                        var flushLength = GetSafeFlushLength(current);
                        if (flushLength <= 0)
                            yield break;
                        foreach (var chunk in CloseThinkingBlock())
                            yield return chunk;
                        emittedVisibleContent = true;
                        yield return current[..flushLength];
                        contentBuffer.Remove(0, flushLength);
                    }
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.AppendTaggedContent.");
        }
    }

    private IEnumerable<string> CompleteTaggedContent()
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.CompleteTaggedContent.");
                    if (contentBuffer.Length > 0)
                    {
                        var current = contentBuffer.ToString();
                        contentBuffer.Clear();
                        if (inTaggedThinking)
                            yield return WebUtility.HtmlEncode(current);
                        else
                        {
                            foreach (var chunk in CloseThinkingBlock())
                                yield return chunk;
                            emittedVisibleContent = true;
                            yield return current;
                        }
                    }
                    foreach (var chunk in CloseThinkingBlock())
                        yield return chunk;
                    inTaggedThinking = false;
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.CompleteTaggedContent.");
        }
    }

    private IEnumerable<string> CompleteHarmonyContent()
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.CompleteHarmonyContent.");
                    if (harmonyBuffer.Length > 0)
                    {
                        var raw = harmonyBuffer.ToString();
                        harmonyBuffer.Clear();
                        if (!harmonyProtocolDetected)
                        {
                            foreach (var chunk in AppendTaggedContent(raw))
                                yield return chunk;
                            foreach (var chunk in CompleteTaggedContent())
                                yield return chunk;
                            yield break;
                        }

                        foreach (var chunk in EmitHarmonyDeltas(raw))
                            yield return chunk;

                        if (emittedHarmonyFinalLength == 0 && emittedHarmonyThinkingLength > 0)
                        {
                            foreach (var chunk in CloseThinkingBlock())
                                yield return chunk;
                            foreach (var chunk in EmitMissingFinalNotice())
                                yield return chunk;
                        }
                    }
                    foreach (var chunk in CompleteTaggedContent())
                        yield return chunk;
                    foreach (var chunk in CloseThinkingBlock())
                        yield return chunk;
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.CompleteHarmonyContent.");
        }
    }

    private int FindHarmonyMarkerIndex(string text)
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.FindHarmonyMarkerIndex.");
                    var startIndex = text.IndexOf("<|start|>", StringComparison.OrdinalIgnoreCase);
                    var channelIndex = text.IndexOf("<|channel|>", StringComparison.OrdinalIgnoreCase);
                    if (startIndex < 0)
                        return channelIndex;
                    if (channelIndex < 0)
                        return startIndex;
                    return Math.Min(startIndex, channelIndex);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"ChatResponseFormatter.FindHarmonyMarkerIndex failed: {exception.Message}");
            throw;
        }
    }

    private IEnumerable<string> EmitHarmonyDeltas(string raw)
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.EmitHarmonyDeltas.");
                    var thinking = ExtractHarmonyText(HarmonyThinkingRegex, raw);
                    if (thinking.Length > emittedHarmonyThinkingLength)
                    {
                        foreach (var chunk in OpenThinkingBlock())
                            yield return chunk;
                        yield return WebUtility.HtmlEncode(thinking[emittedHarmonyThinkingLength..]);
                        emittedHarmonyThinkingLength = thinking.Length;
                        emittedExplicitThinking = true;
                    }

                    var final = ExtractHarmonyText(HarmonyFinalRegex, raw);
                    if (final.Length <= emittedHarmonyFinalLength)
                        yield break;
                    foreach (var chunk in CloseThinkingBlock())
                        yield return chunk;
                    emittedVisibleContent = true;
                    yield return final[emittedHarmonyFinalLength..];
                    emittedHarmonyFinalLength = final.Length;
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.EmitHarmonyDeltas.");
        }
    }

    private string ExtractHarmonyText(Regex regex, string raw)
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.ExtractHarmonyText.");
                    var matches = regex.Matches(raw);
                    return string.Concat(matches.Select(match =>
                        HarmonyMarkerRegex.Replace(match.Groups["content"].Value, string.Empty)));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"ChatResponseFormatter.ExtractHarmonyText failed: {exception.Message}");
            throw;
        }
    }

    private IEnumerable<string> OpenThinkingBlock()
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.OpenThinkingBlock.");
                    if (thinkingBlockOpen)
                        yield break;
                    thinkingBlockOpen = true;
                    yield return "<details class=\"model-thinking open\"><summary>Model thinking</summary><pre>";
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.OpenThinkingBlock.");
        }
    }

    private IEnumerable<string> CloseThinkingBlock()
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.CloseThinkingBlock.");
                    if (!thinkingBlockOpen)
                        yield break;
                    thinkingBlockOpen = false;
                    yield return "</pre></details>\n\n";
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.CloseThinkingBlock.");
        }
    }

    private IEnumerable<string> EmitMissingFinalNotice()
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.EmitMissingFinalNotice.");
                    if (emittedMissingFinalNotice)
                        yield break;
                    emittedMissingFinalNotice = true;
                    logger.LogWarning("A streamed response contained thinking but no visible final answer.");
                    yield return missingFinalAnswerNotice;
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.EmitMissingFinalNotice.");
        }
    }

    private int GetSafeFlushLength(string current) {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.GetSafeFlushLength.");
            return current.Length <= TagLookbehindLength ? 0 : current.Length - TagLookbehindLength;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"ChatResponseFormatter.GetSafeFlushLength failed: {exception.Message}");
            throw;
        }
    }
}
