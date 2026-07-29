using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Formatting;

public sealed class ChatResponseFormatterFactory : IChatResponseFormatterFactory
{
    private readonly ILoggerFactory loggerFactory;
    private readonly IReadOnlyList<IChatProtocolProfile> profiles;
    private readonly ILocalGptRuntimePolicyDataService runtimePolicy;

    public ChatResponseFormatterFactory(
        ILoggerFactory loggerFactory,
        ILocalGptRuntimePolicyDataService runtimePolicy,
        IEnumerable<IChatProtocolProfile>? profiles = null)
    {
        this.loggerFactory = loggerFactory;
        this.runtimePolicy = runtimePolicy;
        this.profiles = (profiles ?? ChatProtocolProfileCatalog.CreateDefaults()).ToList();
    }

    public IChatResponseFormatter Create(ChatResponseProtocol protocol, string? missingFinalAnswerNotice = null) =>
        new ChatResponseFormatter(
            protocol,
            ChatProtocolProfileCatalog.ResolveExact(profiles, protocol),
            profiles,
            missingFinalAnswerNotice,
            runtimePolicy,
            loggerFactory.CreateLogger<ChatResponseFormatter>());
}

internal sealed class ChatResponseFormatter(
    ChatResponseProtocol protocol,
    IChatProtocolProfile protocolProfile,
    IReadOnlyList<IChatProtocolProfile> availableProfiles,
    string? configuredMissingFinalAnswerNotice,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ChatResponseFormatter> logger)
    : IChatResponseFormatter
{
    private const string ThinkStartTag = "<think>";
    private const string ThinkEndTag = "</think>";
    private const int TagLookbehindLength = 16;
    private const string DefaultMissingFinalAnswerNotice =
        "**No final answer was emitted.** The model only sent thinking. Send a short continuation request or increase the answer-token budget.";

    private readonly Regex HarmonyThinkingRegex = new(
        @"<\|start\|>assistant<\|channel\|>(analysis|commentary)<\|message\|>(?<content>.*?)(?=<\|channel\|>|<\|end\|>|$)|<\|channel\|>(analysis|commentary)<\|message\|>(?<content>.*?)(?=<\|channel\|>|<\|end\|>|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex HarmonyFinalRegex = new(
        @"<\|start\|>assistant<\|channel\|>final<\|message\|>(?<content>.*?)(?=<\|end\|>|$)|<\|channel\|>final<\|message\|>(?<content>.*?)(?=<\|end\|>|<\|start\|>|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex HarmonyMarkerRegex = new(
        @"<\|[^>]+\|>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);

    private readonly string missingFinalAnswerNotice = string.IsNullOrWhiteSpace(configuredMissingFinalAnswerNotice)
        ? DefaultMissingFinalAnswerNotice
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
        if (string.IsNullOrEmpty(text))
            yield break;

        foreach (var chunk in OpenThinkingBlock())
            yield return chunk;

        emittedExplicitThinking = true;
        var normalizedThinking = activeProfile.NormalizeThinking(text);
        if (!string.IsNullOrEmpty(normalizedThinking))
            yield return WebUtility.HtmlEncode(normalizedThinking);
    }

    public IEnumerable<string> AppendContent(string text)
    {
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
                activeProfile = ChatProtocolProfileCatalog.ResolveExact(availableProfiles, activeProtocol);
                foreach (var chunk in AppendHarmonyContent(candidate[harmonyMarkerIndex..]))
                    yield return chunk;
                yield break;
            }
            if (candidate.Contains(ThinkStartTag, StringComparison.OrdinalIgnoreCase))
            {
                activeProtocol = ChatResponseProtocol.ThinkTags;
                activeProfile = ChatProtocolProfileCatalog.ResolveExact(availableProfiles, activeProtocol);
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

    public IEnumerable<string> Complete()
    {
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

    private IEnumerable<string> AppendHarmonyContent(string text)
    {
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

    private IEnumerable<string> AppendTaggedContent(string text)
    {
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

    private IEnumerable<string> CompleteTaggedContent()
    {
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

    private IEnumerable<string> CompleteHarmonyContent()
    {
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

    private int FindHarmonyMarkerIndex(string text)
    {
        var startIndex = text.IndexOf("<|start|>", StringComparison.OrdinalIgnoreCase);
        var channelIndex = text.IndexOf("<|channel|>", StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
            return channelIndex;
        if (channelIndex < 0)
            return startIndex;
        return Math.Min(startIndex, channelIndex);
    }

    private IEnumerable<string> EmitHarmonyDeltas(string raw)
    {
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

    private string ExtractHarmonyText(Regex regex, string raw)
    {
        var matches = regex.Matches(raw);
        return string.Concat(matches.Select(match =>
            HarmonyMarkerRegex.Replace(match.Groups["content"].Value, string.Empty)));
    }

    private IEnumerable<string> OpenThinkingBlock()
    {
        if (thinkingBlockOpen)
            yield break;
        thinkingBlockOpen = true;
        yield return "<details class=\"model-thinking open\"><summary>Model thinking</summary><pre>";
    }

    private IEnumerable<string> CloseThinkingBlock()
    {
        if (!thinkingBlockOpen)
            yield break;
        thinkingBlockOpen = false;
        yield return "</pre></details>\n\n";
    }

    private IEnumerable<string> EmitMissingFinalNotice()
    {
        if (emittedMissingFinalNotice)
            yield break;
        emittedMissingFinalNotice = true;
        logger.LogWarning("A streamed response contained thinking but no visible final answer.");
        yield return missingFinalAnswerNotice;
    }

    private int GetSafeFlushLength(string current) =>
        current.Length <= TagLookbehindLength ? 0 : current.Length - TagLookbehindLength;
}
