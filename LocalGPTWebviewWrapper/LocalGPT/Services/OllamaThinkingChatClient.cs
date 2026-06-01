using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services
{
    public sealed class OllamaThinkingChatClient : IChatClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient http;
        private readonly string model;

        public OllamaThinkingChatClient(OllamaCoreOptions options)
        {
            model = options.ModelName;
            http = new HttpClient
            {
                BaseAddress = new Uri(options.Uri.TrimEnd('/')),
                Timeout = TimeSpan.FromMinutes(10)
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
                KeepAlive = "10m",
                Messages = messages.Select(ToOllamaMessage).Where(m => !string.IsNullOrWhiteSpace(m.Content)).ToList(),
                Options = new OllamaRequestOptions
                {
                    NumPredict = Math.Max(512, options?.MaxOutputTokens ?? 2048),
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

        private static List<AIContent> CreateContents(OllamaChatResponse response)
        {
            var visible = FormatVisibleResponse(response.Message?.Content, response.Message?.Thinking);
            return
            [
                new TextContent(visible)
            ];
        }

        private static string FormatVisibleResponse(string? content, string? thinking)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(thinking))
            {
                builder
                    .AppendLine("<details class=\"model-thinking\" open>")
                    .AppendLine("<summary>Model thinking</summary>")
                    .AppendLine()
                    .AppendLine(thinking.Trim())
                    .AppendLine()
                    .AppendLine("</details>")
                    .AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(content))
                builder.AppendLine(content.Trim());

            return builder.ToString().Trim();
        }

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
            public int NumPredict { get; set; }
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
