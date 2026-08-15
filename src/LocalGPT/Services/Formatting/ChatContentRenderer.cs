using LocalGPT.Interfaces;
using Markdig;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Formatting;

/// <summary>
/// Renders the complete response snapshot supplied by DXAIChat on every stream
/// update. Human quote/apostrophe entities are normalized for readable chat and
/// history output, while markup-significant entities stay encoded so model text
/// cannot turn into active HTML merely by passing through the display boundary.
/// </summary>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the chat content workflow to provide the corresponding application capability.</param>
/// <param name="structuredText">Structured text translation service dependency used by the chat content workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ChatContentRenderer(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    IStructuredTextTranslationService structuredText,
    ILogger<ChatContentRenderer> logger) : IChatContentRenderer
{
    /// <summary>
    /// Defines the automatic structured translation limit constant used by <see cref="ChatContentRenderer"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int AutomaticStructuredTranslationLimit = 120_000;

    /// <summary>
    /// Stores the internal harmony marker regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex HarmonyMarkerRegex = new(
        @"<\|[^>]+\|>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal thinking details start regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex ThinkingDetailsStartRegex = new(
        "<details\\s+class=\"model-thinking(?:\\s+open)?\"(?:\\s+open)?\\s*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal council completion marker regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex CouncilCompletionMarkerRegex = new(
        @"<!--localgpt-council-stream-complete:(?<id>[a-f0-9]{32})-->",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal list after HTML regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex ListAfterHtmlRegex = new(
        @"(</(?:p|details|pre|div)>)\s*((?:[-*]|\d+\.)\s+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal controlled details start regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex ControlledDetailsStartRegex = new(
        "<details\\s+class=\"(?:model-thinking(?:\\s+open)?|council-step(?:\\s+council-live)?|council-prompt)\"[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal details end regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex DetailsEndRegex = new(
        @"</details>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal stable panel start regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex StablePanelStartRegex = new(
        "<details\\s+class=\"(?<class>model-thinking(?:\\s+open)?|council-step(?:\\s+council-live)?|council-prompt)\"(?<attributes>[^>]*)>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal stream identifier attribute regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex StreamIdAttributeRegex = new(
        "data-localgpt-stream-id=\"(?<id>[a-f0-9]{32})\"",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal pre start regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex PreStartRegex = new(
        @"<pre(?:\s[^>]*)?>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal pre end regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex PreEndRegex = new(
        @"</pre>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Stores the internal ascii frame regex state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex AsciiFrameRegex = new(
        @"\[\[ASCII_FRAME(?:\s+(?<attributes>[^\]]+))?\]\]\s*(?<frame>.*?)\s*\[\[/ASCII_FRAME\]\]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Decodes only human-facing quote and apostrophe entities before structured-text recognition.
    /// Markup-significant entities such as &amp;lt;, &amp;gt;, and &amp;amp; deliberately remain encoded.
    /// </summary>
    /// <param name="text">Text snapshot that may contain HTML-encoded punctuation.</param>
    /// <returns>The text with quote/apostrophe entities normalized exactly once.</returns>
    private string DecodeHumanTextEntities(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
                .Replace("&#34;", "\"", StringComparison.OrdinalIgnoreCase)
                .Replace("&#x22;", "\"", StringComparison.OrdinalIgnoreCase)
                .Replace("&apos;", "'", StringComparison.OrdinalIgnoreCase)
                .Replace("&#39;", "'", StringComparison.OrdinalIgnoreCase)
                .Replace("&#x27;", "'", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Human-facing quote/apostrophe entity normalization failed.");
            throw;
        }
    }

    /// <summary>
    /// Repairs a small set of known prose label/number boundaries emitted without whitespace by some local models.
    /// </summary>
    private readonly Regex ProseLabelBoundaryRegex = new(
        @"\b(?<label>output|context|input|timeout|connected|detailed)(?=(?:\d|1-Wire\b))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    /// <summary>
    /// Repairs a missing boundary between a numeric value and common prose units without touching identifiers.
    /// </summary>
    private readonly Regex ProseUnitBoundaryRegex = new(
        @"(?<=\d)(?=(?:tokens?|models?|members?|capabilit(?:y|ies)|rounds?|seconds?|minutes?|messages?|files?|functions?|skills?|organs?|peers?|roads?)\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);

    /// <summary>
    /// Stores the internal markdown pipeline state used by <see cref="ChatContentRenderer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly MarkdownPipeline markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Performs render for <see cref="ChatContentRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding chat content workflow.
    /// </summary>
    /// <param name="content">Content value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Render(string? content)
    {
        try
        {
            logger.LogTrace("Rendering a LocalGPT chat-content snapshot; content was omitted.");
            var normalized = NormalizeForRender(content);
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            return Markdown.ToHtml(normalized, markdownPipeline).Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Chat content rendering failed. LocalGPT will show a safely encoded plain-text fallback; message content was omitted from logs.");
            return BuildSafeFallback(content);
        }
    }

    /// <summary>
    /// Normalizes for render.
    /// </summary>
    /// <param name="content">Content value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeForRender(string? content)
    {
        try
        {
            logger.LogTrace("Normalizing a LocalGPT chat-content snapshot; content was omitted.");
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Local model streams can occasionally contain an isolated UTF-16
            // surrogate. Markdig's automatic heading identifiers normalize text
            // and reject such strings. Repair only malformed code units while
            // preserving valid surrogate pairs and every other character.
            var text = SanitizeInvalidUnicode(content);
            text = DecodeHumanTextEntities(text);
            text = HarmonyMarkerRegex.Replace(text, string.Empty);
            text = RepairCommonProseSpacing(text);
            text = RenderAsciiFrames(text);
            // The renderer is called for every streaming snapshot. Re-scanning a large, still-live
            // Council transcript for balanced JSON on every token can monopolize the Blazor circuit
            // and make Stop appear unresponsive. Keep automatic translation bounded and defer large
            // or active streams to the explicit structured-text controller/DXFunctions.
            if (ShouldTranslateStructuredText(text))
                text = structuredText.TranslatePlainJsonBlocksToMarkdown(text);
            // Provider-supplied reasoning is a first-class part of the user-visible transcript.
            // Keep completed thought panels expanded in both live chat and restored sessions; only
            // the temporary `open` CSS class is reserved for an unfinished/live thinking block.
            text = ThinkingDetailsStartRegex.Replace(
                text,
                "<details class=\"model-thinking\" open>");

            foreach (Match marker in CouncilCompletionMarkerRegex.Matches(text))
            {
                var streamId = marker.Groups["id"].Value;
                var liveStart = $"<details class=\"council-step council-live\" data-localgpt-stream-id=\"{streamId}\" open>";
                var completedStart = $"<details class=\"council-step\" data-localgpt-stream-id=\"{streamId}\">";
                text = text.Replace(liveStart, completedStart, StringComparison.OrdinalIgnoreCase);
            }
            text = CouncilCompletionMarkerRegex.Replace(text, string.Empty);

            // Every provider-supplied thinking block stays expanded. Mark only the currently
            // unfinished block as live so the UI can show its streaming indicator without
            // mislabeling completed reasoning as still running.
            var lastThinkingStart = text.LastIndexOf(
                "<details class=\"model-thinking\" open>",
                StringComparison.OrdinalIgnoreCase);
            var lastDetailsEnd = text.LastIndexOf("</details>", StringComparison.OrdinalIgnoreCase);
            if (lastThinkingStart > lastDetailsEnd)
            {
                const string visibleStart = "<details class=\"model-thinking\" open>";
                const string liveStart = "<details class=\"model-thinking open\" open>";
                text = text.Remove(lastThinkingStart, visibleStart.Length)
                    .Insert(lastThinkingStart, liveStart);
            }

            text = AddStablePanelKeys(text);

            text = text.Replace("</details>\n", "</details>\n\n", StringComparison.OrdinalIgnoreCase);
            text = ListAfterHtmlRegex.Replace(text, "$1\n\n$2");

            // DXAIChat rerenders the whole accumulated response for every chunk.
            // Close LocalGPT-owned live markup only in the render snapshot so an
            // unfinished thinking/council block is visible immediately. The stored
            // stream remains untouched and receives its real closing tags later.
            var missingPreClosures = Math.Max(0, PreStartRegex.Matches(text).Count - PreEndRegex.Matches(text).Count);
            var missingDetailsClosures = Math.Max(
                0,
                ControlledDetailsStartRegex.Matches(text).Count - DetailsEndRegex.Matches(text).Count);

            if (missingPreClosures == 0 && missingDetailsClosures == 0)
                return text.Trim();

            var suffix = string.Concat(Enumerable.Repeat("</pre>", missingPreClosures)) +
                         string.Concat(Enumerable.Repeat("</details>", missingDetailsClosures));
            return $"{text}{suffix}".Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Chat content normalization failed. LocalGPT will retain a sanitized snapshot; message content was omitted from logs.");
            return SanitizeInvalidUnicode(content ?? string.Empty).Trim();
        }
    }


    /// <summary>
    /// Performs should translate structured text for <see cref="ChatContentRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding chat content workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool ShouldTranslateStructuredText(string text)
    {
    try
    {
            if (text.Length > AutomaticStructuredTranslationLimit)
                return false;

            return !text.Contains(
                "<details class=\"council-step council-live\"",
                StringComparison.OrdinalIgnoreCase);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatContentRenderer)}.{nameof(ShouldTranslateStructuredText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatContentRenderer)}.{nameof(ShouldTranslateStructuredText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Repairs conservative, display-only spacing omissions in prose without applying spacing edits inside fenced code, inline code or raw HTML lines.
    /// </summary>
    /// <param name="text">Text value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RepairCommonProseSpacing(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var builder = new StringBuilder(text.Length + 32);
            var inFence = false;

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var trimmed = line.TrimStart();
                var fenceLine = trimmed.StartsWith("```", StringComparison.Ordinal) ||
                                trimmed.StartsWith("~~~", StringComparison.Ordinal);
                if (fenceLine)
                {
                    inFence = !inFence;
                }
                else if (!inFence &&
                         !line.Contains('`') &&
                         !trimmed.StartsWith('<'))
                {
                    line = ProseLabelBoundaryRegex.Replace(line, "${label} ");
                    line = ProseUnitBoundaryRegex.Replace(line, " ");
                }

                if (index > 0)
                    builder.Append('\n');
                builder.Append(line);
            }

            return builder.ToString();
        }
        catch (RegexMatchTimeoutException ex)
        {
            logger.LogWarning(ex, "Prose spacing repair timed out; LocalGPT will render the original model text.");
            return text;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Prose spacing repair failed; LocalGPT will render the original model text.");
            return text;
        }
    }

    /// <summary>
    /// Performs render ascii frames for <see cref="ChatContentRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding chat content workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderAsciiFrames(string text)
    {
        try
        {
            return AsciiFrameRegex.Replace(text, match =>
            {
                var frame = match.Groups["frame"].Value
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Trim('\n', '\r');
                var attributes = match.Groups["attributes"].Value.Trim();
                var encodedFrame = HtmlEncoder.Default.Encode(frame);
                var encodedAttributes = HtmlEncoder.Default.Encode(attributes);
                var label = string.IsNullOrWhiteSpace(attributes)
                    ? "ASCII game frame"
                    : $"ASCII game frame ({encodedAttributes})";
                return $"<pre class=\"localgpt-ascii-frame\" role=\"img\" aria-label=\"{label}\"><code>{encodedFrame}</code></pre>";
            });
        }
        catch (RegexMatchTimeoutException ex)
        {
            logger.LogWarning(ex, "ASCII frame marker parsing timed out; the original markers remain visible.");
            return text;
        }
    }

    /// <summary>
    /// Adds stable panel keys for <see cref="ChatContentRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding chat content workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string AddStablePanelKeys(string text)
    {
        try
        {
            var thinkingIndex = 0;
            var councilIndex = 0;
            var promptIndex = 0;

            return StablePanelStartRegex.Replace(text, match =>
            {
                var className = match.Groups["class"].Value;
                var attributes = match.Groups["attributes"].Value;
                if (attributes.Contains("data-localgpt-panel-key=", StringComparison.OrdinalIgnoreCase))
                    return match.Value;

                var streamIdMatch = StreamIdAttributeRegex.Match(attributes);
                string key;
                if (streamIdMatch.Success)
                {
                    key = $"council-stream-{streamIdMatch.Groups["id"].Value}";
                }
                else if (className.StartsWith("model-thinking", StringComparison.OrdinalIgnoreCase))
                {
                    key = $"thinking-{thinkingIndex++}";
                }
                else if (className.StartsWith("council-prompt", StringComparison.OrdinalIgnoreCase))
                {
                    key = $"council-prompt-{promptIndex++}";
                }
                else
                {
                    key = $"council-step-{councilIndex++}";
                }

                return $"<details class=\"{className}\" data-localgpt-panel-key=\"{key}\"{attributes}>";
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Stable chat-panel key generation failed. LocalGPT will render the snapshot without generated panel keys; content was omitted from logs.");
            return text;
        }
    }

    /// <summary>
    /// Performs sanitize invalid unicode for <see cref="ChatContentRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding chat content workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SanitizeInvalidUnicode(string value)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            StringBuilder? repaired = null;
            var replacementCount = 0;
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (char.IsHighSurrogate(current))
                {
                    if (index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                    {
                        if (repaired is not null)
                        {
                            repaired.Append(current);
                            repaired.Append(value[++index]);
                        }
                        else
                        {
                            index++;
                        }
                        continue;
                    }

                    repaired ??= new StringBuilder(value.Length + 8).Append(value, 0, index);
                    repaired.Append('\uFFFD');
                    replacementCount++;
                    continue;
                }

                if (char.IsLowSurrogate(current))
                {
                    repaired ??= new StringBuilder(value.Length + 8).Append(value, 0, index);
                    repaired.Append('\uFFFD');
                    replacementCount++;
                    continue;
                }

                repaired?.Append(current);
            }

            if (replacementCount == 0)
                return value;

            logger.LogWarning(
                "Repaired {ReplacementCount} malformed UTF-16 code unit(s) before rendering a LocalGPT chat snapshot; content was omitted from logs.",
                replacementCount);
            return repaired?.ToString() ?? value;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unicode sanitization failed for a LocalGPT chat snapshot; content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Builds safe fallback for <see cref="ChatContentRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding chat content workflow.
    /// </summary>
    /// <param name="content">Content value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildSafeFallback(string? content)
    {
        try
        {
            var sanitized = SanitizeInvalidUnicode(content ?? string.Empty);
            if (string.IsNullOrWhiteSpace(sanitized))
                return string.Empty;

            return $"<pre class=\"localgpt-render-fallback\">{HtmlEncoder.Default.Encode(sanitized)}</pre>";
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Plain-text chat rendering fallback failed; content was omitted from logs.");
            return "<p class=\"localgpt-render-fallback\">This message could not be rendered. Review the LocalGPT log for technical details.</p>";
        }
    }
}
