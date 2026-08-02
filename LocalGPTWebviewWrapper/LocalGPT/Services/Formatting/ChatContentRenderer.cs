using LocalGPT.Interfaces;
using Markdig;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Formatting;

/// <summary>
/// Renders the complete response snapshot supplied by DXAIChat on every stream
/// update. It deliberately does not decode HTML entities: thinking text is
/// encoded by <see cref="IChatResponseFormatter"/> and must stay text.
/// </summary>
public sealed class ChatContentRenderer(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    IStructuredTextTranslationService structuredText,
    ILogger<ChatContentRenderer> logger) : IChatContentRenderer
{
    private const int AutomaticStructuredTranslationLimit = 120_000;

    private readonly Regex HarmonyMarkerRegex = new(
        @"<\|[^>]+\|>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex ThinkingDetailsStartRegex = new(
        "<details\\s+class=\"model-thinking(?:\\s+open)?\"(?:\\s+open)?\\s*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex CouncilCompletionMarkerRegex = new(
        @"<!--localgpt-council-stream-complete:(?<id>[a-f0-9]{32})-->",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex ListAfterHtmlRegex = new(
        @"(</(?:p|details|pre|div)>)\s*((?:[-*]|\d+\.)\s+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex ControlledDetailsStartRegex = new(
        "<details\\s+class=\"(?:model-thinking(?:\\s+open)?|council-step(?:\\s+council-live)?|council-prompt)\"[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex DetailsEndRegex = new(
        @"</details>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex StablePanelStartRegex = new(
        "<details\\s+class=\"(?<class>model-thinking(?:\\s+open)?|council-step(?:\\s+council-live)?|council-prompt)\"(?<attributes>[^>]*)>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex StreamIdAttributeRegex = new(
        "data-localgpt-stream-id=\"(?<id>[a-f0-9]{32})\"",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex PreStartRegex = new(
        @"<pre(?:\s[^>]*)?>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex PreEndRegex = new(
        @"</pre>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        runtimePolicy.RegexTimeout);
    private readonly Regex AsciiFrameRegex = new(
        @"\[\[ASCII_FRAME(?:\s+(?<attributes>[^\]]+))?\]\]\s*(?<frame>.*?)\s*\[\[/ASCII_FRAME\]\]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline,
        runtimePolicy.RegexTimeout);

    private readonly MarkdownPipeline markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

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
            text = HarmonyMarkerRegex.Replace(text, string.Empty);
            text = RenderAsciiFrames(text);
            // The renderer is called for every streaming snapshot. Re-scanning a large, still-live
            // Council transcript for balanced JSON on every token can monopolize the Blazor circuit
            // and make Stop appear unresponsive. Keep automatic translation bounded and defer large
            // or active streams to the explicit structured-text controller/DXFunctions.
            if (ShouldTranslateStructuredText(text))
                text = structuredText.TranslatePlainJsonBlocksToMarkdown(text);
            text = ThinkingDetailsStartRegex.Replace(
                text,
                "<details class=\"model-thinking\">");

            foreach (Match marker in CouncilCompletionMarkerRegex.Matches(text))
            {
                var streamId = marker.Groups["id"].Value;
                var liveStart = $"<details class=\"council-step council-live\" data-localgpt-stream-id=\"{streamId}\" open>";
                var completedStart = $"<details class=\"council-step\" data-localgpt-stream-id=\"{streamId}\">";
                text = text.Replace(liveStart, completedStart, StringComparison.OrdinalIgnoreCase);
            }
            text = CouncilCompletionMarkerRegex.Replace(text, string.Empty);

            // Only the currently unfinished thinking block is expanded. Completed
            // blocks collapse automatically as soon as visible answer text starts.
            var lastThinkingStart = text.LastIndexOf(
                "<details class=\"model-thinking\">",
                StringComparison.OrdinalIgnoreCase);
            var lastDetailsEnd = text.LastIndexOf("</details>", StringComparison.OrdinalIgnoreCase);
            if (lastThinkingStart > lastDetailsEnd)
            {
                const string collapsedStart = "<details class=\"model-thinking\">";
                const string liveStart = "<details class=\"model-thinking open\" open>";
                text = text.Remove(lastThinkingStart, collapsedStart.Length)
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


    private static bool ShouldTranslateStructuredText(string text)
    {
        if (text.Length > AutomaticStructuredTranslationLimit)
            return false;

        return !text.Contains(
            "<details class=\"council-step council-live\"",
            StringComparison.OrdinalIgnoreCase);
    }

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
