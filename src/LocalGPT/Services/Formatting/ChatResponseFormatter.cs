using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Formatting;

/// <summary>
/// Creates stateful response formatters that normalize PlainText, think-tag and Harmony streams into one safe Markdown surface.
/// </summary>
/// <param name="loggerFactory">Creates the per-formatter diagnostic logger.</param>
/// <param name="runtimePolicy">Provides database-owned tags, limits and regular expressions.</param>
/// <param name="catalog">Resolves the configured protocol profile.</param>
/// <param name="logger">Writes bounded formatter-factory diagnostics.</param>
[DocumentationUpdated("2.1.20")]
public sealed class ChatResponseFormatterFactory(
    ILoggerFactory loggerFactory,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    IChatProtocolProfileCatalog catalog,
    ILogger<ChatResponseFormatterFactory> logger) : IChatResponseFormatterFactory
{
    /// <summary>
    /// Creates a new independent formatter for one streamed response.
    /// </summary>
    /// <param name="protocol">Requested provider response protocol.</param>
    /// <param name="missingFinalAnswerNotice">Optional visible fallback shown when only thinking was emitted.</param>
    /// <returns>A stateful formatter that must be used for only one response stream.</returns>
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

/// <summary>
/// Parses one provider response stream and emits HTML-safe Markdown plus LocalGPT-owned disclosure panels.
/// </summary>
/// <param name="protocol">Initial protocol selection.</param>
/// <param name="protocolProfile">Normalization profile for the selected protocol.</param>
/// <param name="catalog">Resolves protocol profiles when automatic detection switches modes.</param>
/// <param name="configuredMissingFinalAnswerNotice">Optional missing-final-answer message.</param>
/// <param name="runtimePolicy">Provides maintained response tags, regexes and look-behind limits.</param>
/// <param name="logger">Writes bounded stream-parser diagnostics.</param>
[DocumentationUpdated("2.1.20")]
internal sealed class ChatResponseFormatter(
    ChatResponseProtocol protocol,
    IChatProtocolProfile protocolProfile,
    IChatProtocolProfileCatalog catalog,
    string? configuredMissingFinalAnswerNotice,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ChatResponseFormatter> logger)
    : IChatResponseFormatter
{
    /// <summary>
    /// Gets the harmony thinking regex value that forms part of the chat response formatter state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The harmony thinking regex value exposed by <see cref="ChatResponseFormatter"/>.</value>
    private Regex HarmonyThinkingRegex => runtimePolicy.GetPattern(LocalGptRuntimePattern.ChatHarmonyThinking);
    /// <summary>
    /// Gets the harmony final regex value that forms part of the chat response formatter state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The harmony final regex value exposed by <see cref="ChatResponseFormatter"/>.</value>
    private Regex HarmonyFinalRegex => runtimePolicy.GetPattern(LocalGptRuntimePattern.ChatHarmonyFinal);
    /// <summary>
    /// Gets the harmony marker regex value that forms part of the chat response formatter state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The harmony marker regex value exposed by <see cref="ChatResponseFormatter"/>.</value>
    private Regex HarmonyMarkerRegex => runtimePolicy.GetPattern(LocalGptRuntimePattern.ChatHarmonyMarker);
    /// <summary>
    /// Gets the think start tag value that forms part of the chat response formatter state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The think start tag value exposed by <see cref="ChatResponseFormatter"/>.</value>
    private string ThinkStartTag => runtimePolicy.GetString(LocalGptRuntimeValue.FormattingThinkStartTag);
    /// <summary>
    /// Gets the think end tag value that forms part of the chat response formatter state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The think end tag value exposed by <see cref="ChatResponseFormatter"/>.</value>
    private string ThinkEndTag => runtimePolicy.GetString(LocalGptRuntimeValue.FormattingThinkEndTag);
    /// <summary>Gets the number of trailing characters retained while a streamed tag may be incomplete.</summary>
    /// <value>The tag lookbehind length value exposed by <see cref="ChatResponseFormatter"/>.</value>
    private int TagLookbehindLength => runtimePolicy.GetInt(LocalGptRuntimeValue.FormattingTagLookbehindLength);

    /// <summary>
    /// Stores the internal missing final answer notice state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string missingFinalAnswerNotice = string.IsNullOrWhiteSpace(configuredMissingFinalAnswerNotice)
        ? runtimePolicy.GetString(LocalGptRuntimeValue.FormattingMissingFinalAnswerNotice)
        : configuredMissingFinalAnswerNotice.Trim();
    /// <summary>
    /// Stores the internal content buffer state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StringBuilder contentBuffer = new();
    /// <summary>
    /// Stores the internal harmony buffer state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StringBuilder harmonyBuffer = new();
    /// <summary>
    /// Stores the internal active protocol state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private ChatResponseProtocol activeProtocol = protocol;
    /// <summary>
    /// Stores the chat protocol profile dependency used by <see cref="ChatResponseFormatter"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private IChatProtocolProfile activeProfile = protocolProfile;
    /// <summary>
    /// Stores the internal emitted harmony thinking length state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private int emittedHarmonyThinkingLength;
    /// <summary>
    /// Stores the internal emitted harmony final length state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private int emittedHarmonyFinalLength;
    /// <summary>
    /// Stores the internal in tagged thinking state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private bool inTaggedThinking;
    /// <summary>
    /// Stores the internal emitted explicit thinking state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private bool emittedExplicitThinking;
    /// <summary>
    /// Stores the internal emitted visible content state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private bool emittedVisibleContent;
    /// <summary>
    /// Stores the internal emitted missing final notice state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private bool emittedMissingFinalNotice;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to thinking block open state owned by <see cref="ChatResponseFormatter"/>.
    /// </summary>
    private bool thinkingBlockOpen;
    /// <summary>
    /// Stores the internal harmony protocol detected state used by <see cref="ChatResponseFormatter"/> while executing its surrounding workflow.
    /// </summary>
    private bool harmonyProtocolDetected;

    /// <summary>Appends a provider-owned thinking delta to the current disclosure panel.</summary>
    /// <param name="text">Raw thinking delta.</param>
    /// <returns>Zero or more incremental Markdown/HTML fragments for the chat renderer.</returns>
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

    /// <summary>Appends a visible provider delta and auto-detects Harmony or think-tag framing when configured.</summary>
    /// <param name="text">Raw visible/content delta.</param>
    /// <returns>Zero or more incremental safe-Markdown fragments.</returns>
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
                        yield return EncodeModelMarkdown(text);
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

    /// <summary>Flushes buffered tags/channels and emits a fallback when the provider supplied thinking without a final answer.</summary>
    /// <returns>The final renderer fragments for the response.</returns>
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

    /// <summary>Buffers Harmony content until channel markers can be parsed safely.</summary>
    /// <param name="text">Raw Harmony stream delta.</param>
    /// <returns>Incremental analysis/final fragments ready for rendering.</returns>
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

    /// <summary>
    /// Performs append tagged content for <see cref="ChatResponseFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding chat response formatter workflow.
    /// </summary>
    /// <param name="text">Normalized provider content.</param>
    /// <returns>Incremental thinking or visible fragments.</returns>
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
                                yield return EncodeModelMarkdown(current[..startIndex]);
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
                        yield return EncodeModelMarkdown(current[..flushLength]);
                        contentBuffer.Remove(0, flushLength);
                    }
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.AppendTaggedContent.");
        }
    }

    /// <summary>Flushes any incomplete think-tag buffer at end of stream.</summary>
    /// <returns>The remaining safe renderer fragments.</returns>
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
                            yield return EncodeModelMarkdown(current);
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

    /// <summary>Flushes the Harmony buffer and closes any open disclosure panel.</summary>
    /// <returns>The remaining safe renderer fragments.</returns>
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

    /// <summary>
    /// Finds harmony marker index for <see cref="ChatResponseFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding chat response formatter workflow.
    /// </summary>
    /// <param name="text">Candidate provider text.</param>
    /// <returns>The zero-based marker index, or -1 when no marker is present.</returns>
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

    /// <summary>Extracts and emits only newly observed Harmony analysis and final content.</summary>
    /// <param name="raw">Complete buffered Harmony text.</param>
    /// <returns>New renderer fragments since the preceding call.</returns>
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
                    yield return EncodeModelMarkdown(final[emittedHarmonyFinalLength..]);
                    emittedHarmonyFinalLength = final.Length;
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.EmitHarmonyDeltas.");
        }
    }

    /// <summary>Concatenates channel content captured by a maintained Harmony regex.</summary>
    /// <param name="regex">Channel extraction regex.</param>
    /// <param name="raw">Complete buffered provider text.</param>
    /// <returns>Channel text with Harmony control markers removed.</returns>
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

    /// <summary>
    /// Opens thinking block for <see cref="ChatResponseFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding chat response formatter workflow.
    /// </summary>
    /// <returns>An opening fragment, or no fragment when already open.</returns>
    private IEnumerable<string> OpenThinkingBlock()
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.OpenThinkingBlock.");
                    if (thinkingBlockOpen)
                        yield break;
                    thinkingBlockOpen = true;
                    yield return "<details class=\"model-thinking open\"><summary>Model thinking</summary>\n\n";
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.OpenThinkingBlock.");
        }
    }

    /// <summary>
    /// Closes thinking block for <see cref="ChatResponseFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding chat response formatter workflow.
    /// </summary>
    /// <returns>A closing fragment, or no fragment when already closed.</returns>
    private IEnumerable<string> CloseThinkingBlock()
    {
        try
        {
            logger.LogTrace($"Entering ChatResponseFormatter.CloseThinkingBlock.");
                    if (!thinkingBlockOpen)
                        yield break;
                    thinkingBlockOpen = false;
                    yield return "\n\n</details>\n\n";
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator ChatResponseFormatter.CloseThinkingBlock.");
        }
    }

    /// <summary>Emits the configured missing-final-answer notice at most once.</summary>
    /// <returns>The fallback notice or no fragment when already emitted.</returns>
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


    /// <summary>
    /// Encodes raw model HTML while preserving Markdown punctuation and physical line breaks.
    /// LocalGPT-owned details panels are emitted separately by the formatter and cannot be forged by a model.
    /// </summary>
    /// <param name="text">Raw model-generated visible text.</param>
    /// <returns>HTML-safe Markdown text suitable for the shared Markdig rendering path.</returns>
    private string EncodeModelMarkdown(string text) {
    try
    {
        return WebUtility.HtmlEncode(text);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatResponseFormatter)}.{nameof(EncodeModelMarkdown)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatResponseFormatter)}.{nameof(EncodeModelMarkdown)} failed.");
        throw;
    }
}

    /// <summary>Calculates how much buffered text can be emitted without splitting a possible think tag.</summary>
    /// <param name="current">Current content buffer.</param>
    /// <returns>The safe prefix length.</returns>
    private int GetSafeFlushLength(string current)
    {
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
