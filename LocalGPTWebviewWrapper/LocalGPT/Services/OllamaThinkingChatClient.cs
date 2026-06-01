using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services
{
    public sealed partial class OllamaThinkingChatClient : IChatClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient http;
        private readonly string model;
        private readonly string keepAlive;
        private readonly int? contextLength;
        private readonly int? numGpu;

        public OllamaThinkingChatClient(OllamaCoreOptions options, string? keepAlive = null, int? contextLength = null, TimeSpan? timeout = null, int? numGpu = null)
        {
            model = options.ModelName;
            this.keepAlive = string.IsNullOrWhiteSpace(keepAlive) ? "10m" : keepAlive.Trim();
            this.contextLength = contextLength;
            this.numGpu = numGpu;
            http = new HttpClient
            {
                BaseAddress = new Uri(options.Uri.TrimEnd('/')),
                Timeout = timeout ?? TimeSpan.FromMinutes(10)
            };
        }

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(messages, options, stream: false, cancellationToken);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, CreateContents(response)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(messages, options, stream: false, cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, CreateContents(response));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
            http.Dispose();
        }

        private async Task<OllamaChatResponse> SendAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            bool stream,
            CancellationToken cancellationToken)
        {
            var request = new OllamaChatRequest
            {
                Model = model,
                Stream = stream,
                KeepAlive = keepAlive,
                Messages = messages.Select(ToOllamaMessage).Where(m => !string.IsNullOrWhiteSpace(m.Content)).ToList(),
                Options = new OllamaRequestOptions
                {
                    NumPredict = Math.Clamp(options?.MaxOutputTokens ?? 2048, 64, 8192),
                    NumCtx = contextLength,
                    NumGpu = numGpu,
                    Temperature = options?.Temperature
                }
            };

            using var response = await http.PostAsJsonAsync("/api/chat", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var ollama = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken);
            return ollama ?? new OllamaChatResponse();
        }

        private static OllamaChatMessage ToOllamaMessage(ChatMessage message)
        {
            return new OllamaChatMessage
            {
                Role = message.Role == ChatRole.System ? "system"
                    : message.Role == ChatRole.Assistant ? "assistant"
                    : "user",
                Content = message.Text
            };
        }

        private List<AIContent> CreateContents(OllamaChatResponse response)
        {
            var visible = FormatVisibleResponse(response.Message?.Content, response.Message?.Thinking);
            return
            [
                new TextContent(visible)
            ];
        }

        private string? NormalizeVisibleContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var text = content.Trim();
            if (IsHarmonyModel())
            {
                var finalMatches = HarmonyFinalPattern().Matches(text);
                if (finalMatches.Count > 0)
                    text = finalMatches[^1].Groups["content"].Value;

                text = HarmonyMarkerPattern().Replace(text, string.Empty);
            }

            text = ThinkTagPattern().Replace(text, string.Empty);
            return text.Trim();
        }

        private string? ExtractHarmonyThinking(string? content)
        {
            if (!IsHarmonyModel() || string.IsNullOrWhiteSpace(content))
                return null;

            var matches = HarmonyThinkingPattern().Matches(content);
            if (matches.Count == 0)
                return null;

            var thinking = string.Join(
                Environment.NewLine,
                matches
                    .Select(match => HarmonyMarkerPattern().Replace(match.Groups["content"].Value, string.Empty).Trim())
                    .Where(text => !string.IsNullOrWhiteSpace(text)));

            return string.IsNullOrWhiteSpace(thinking) ? null : thinking;
        }

        private static string? ExtractTaggedThinking(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var matches = ThinkTagPattern().Matches(content);
            if (matches.Count == 0)
                return null;

            var thinking = string.Join(
                Environment.NewLine,
                matches
                    .Select(match => match.Groups["thinking"].Value.Trim())
                    .Where(text => !string.IsNullOrWhiteSpace(text)));

            return string.IsNullOrWhiteSpace(thinking) ? null : thinking;
        }

        private bool IsHarmonyModel()
        {
            return model.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("harmony", StringComparison.OrdinalIgnoreCase);
        }

        private string FormatVisibleResponse(string? content, string? thinking)
        {
            var builder = new StringBuilder();
            var normalizedContent = NormalizeVisibleContent(content);
            var thinkingParts = new[]
            {
                string.IsNullOrWhiteSpace(thinking) ? null : thinking.Trim(),
                ExtractHarmonyThinking(content),
                ExtractTaggedThinking(content)
            }.Where(text => !string.IsNullOrWhiteSpace(text));
            var normalizedThinking = string.Join(Environment.NewLine, thinkingParts);

            if (!string.IsNullOrWhiteSpace(normalizedThinking))
            {
                builder
                    .AppendLine("<details class=\"model-thinking\" open>")
                    .AppendLine("<summary>Model thinking</summary>")
                    .AppendLine()
                    .AppendLine(normalizedThinking.Trim())
                    .AppendLine()
                    .AppendLine("</details>")
                    .AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(normalizedContent))
                builder.AppendLine(normalizedContent.Trim());
            else if (!string.IsNullOrWhiteSpace(normalizedThinking))
                builder.AppendLine("_The model returned thinking but no final visible answer. Increase the output token budget or ask for a shorter final answer._");

            return builder.ToString().Trim();
        }

        [GeneratedRegex("<\\|start\\|>assistant<\\|channel\\|>final<\\|message\\|>(?<content>.*?)(?=<\\|end\\|>|$)|<\\|channel\\|>final<\\|message\\|>(?<content>.*?)(?=<\\|end\\|>|<\\|start\\|>|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex HarmonyFinalPattern();

        [GeneratedRegex("<\\|start\\|>assistant<\\|channel\\|>(analysis|commentary)<\\|message\\|>(?<content>.*?)(?=<\\|channel\\|>|<\\|end\\|>|$)|<\\|channel\\|>(analysis|commentary)<\\|message\\|>(?<content>.*?)(?=<\\|channel\\|>|<\\|end\\|>|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex HarmonyThinkingPattern();

        [GeneratedRegex("<\\|[^>]+\\|>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex HarmonyMarkerPattern();

        [GeneratedRegex("<think>(?<thinking>.*?)</think>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex ThinkTagPattern();

        private sealed class OllamaChatRequest
        {
            public string Model { get; set; } = string.Empty;
            public bool Stream { get; set; }
            public string KeepAlive { get; set; } = "10m";
            public List<OllamaChatMessage> Messages { get; set; } = new();
            public OllamaRequestOptions? Options { get; set; }
        }

        private sealed class OllamaRequestOptions
        {
            [JsonPropertyName("num_predict")]
            public int NumPredict { get; set; }

            [JsonPropertyName("num_ctx")]
            public int? NumCtx { get; set; }

            [JsonPropertyName("num_gpu")]
            public int? NumGpu { get; set; }

            [JsonPropertyName("temperature")]
            public double? Temperature { get; set; }
        }

        private sealed class OllamaChatMessage
        {
            public string Role { get; set; } = "user";
            public string Content { get; set; } = string.Empty;
            public string? Thinking { get; set; }
        }

        private sealed class OllamaChatResponse
        {
            public OllamaChatMessage? Message { get; set; }
        }
    }
}
