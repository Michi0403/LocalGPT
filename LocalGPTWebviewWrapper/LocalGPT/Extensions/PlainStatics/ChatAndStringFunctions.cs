using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.Extensions.PlainStatics;
namespace LocalGPT.Extensions.PlainStatics
{
    public static class ChatAndStringFunctions
    {
        public static string CreateMessageSignature(IEnumerable<BlazorChatMessage> messages)
        {
            return string.Join("|", messages
                .Where(message => !message.Typing)
                .Select(message => $"{message.Role}:{message.Content.GetHashCode(StringComparison.Ordinal)}:{message.Content.Length}"));
        }

        public static string RenderChatMarkdown(string? content)
        {
            var normalized = NormalizeChatMarkdown(content);
            return Markdig.Markdown.ToHtml(normalized, GlobalVariableSlopCollectionToRemove.ChatMarkdownPipeline).Trim();
        }

        public static string NormalizeChatMarkdown(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var text = System.Net.WebUtility.HtmlDecode(content);
            text = GlobalVariableSlopCollectionToRemove.HarmonyMarkerCleanupRegex.Replace(text, string.Empty);
            text = GlobalVariableSlopCollectionToRemove.OpenThinkingDetailsRegex.Replace(text, "<details class=\"model-thinking\">");
            text = text.Replace("</details>\n", "</details>\n\n", StringComparison.OrdinalIgnoreCase);
            text = GlobalVariableSlopCollectionToRemove.ListAfterHtmlRegex.Replace(text, "$1\n\n$2");
            return text.Trim();
        }

    }
}
