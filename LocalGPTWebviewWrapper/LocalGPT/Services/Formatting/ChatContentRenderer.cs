using LocalGPT.Interfaces;
using Markdig;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Formatting;

/// <summary>
/// Renders the complete response snapshot supplied by DXAIChat on every stream
/// update. It deliberately does not decode HTML entities: thinking text is
/// encoded by <see cref="IChatResponseFormatter"/> and must stay text.
/// </summary>
public sealed class ChatContentRenderer(ILocalGptRuntimePolicyDataService runtimePolicy) : IChatContentRenderer
{
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

    private readonly MarkdownPipeline markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string Render(string? content)
    {
        var normalized = NormalizeForRender(content);
        return string.IsNullOrWhiteSpace(normalized)
            ? string.Empty
            : Markdown.ToHtml(normalized, markdownPipeline).Trim();
    }

    public string NormalizeForRender(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var text = HarmonyMarkerRegex.Replace(content, string.Empty);
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
    private string AddStablePanelKeys(string text)
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

}
