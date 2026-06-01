using System.Net;
using System.Net.Http.Json;
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
            var request = CreateRequest(messages, options, stream: true);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };

            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            using (response)
            {
                response.EnsureSuccessStatusCode();

                Stream stream;
                try
                {
                    stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                await using (stream)
                using (var reader = new StreamReader(stream))
                {
                    var formatter = new VisibleThinkingStreamFormatter();
                    while (!reader.EndOfStream)
                    {
                        string? line;
                        try
                        {
                            line = await reader.ReadLineAsync(cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            yield break;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        OllamaChatResponse? chunk;
                        try
                        {
                            chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonOptions);
                        }
                        catch (JsonException)
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(chunk?.Message?.Thinking))
                        {
                            foreach (var text in formatter.AppendThinking(chunk.Message.Thinking))
                                yield return CreateStreamingUpdate(text);
                        }

                        if (!string.IsNullOrEmpty(chunk?.Message?.Content))
                        {
                            foreach (var text in formatter.AppendContent(chunk.Message.Content))
                                yield return CreateStreamingUpdate(text);
                        }
                    }

                    foreach (var text in formatter.Complete())
                        yield return CreateStreamingUpdate(text);
                }
            }
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
            var request = CreateRequest(messages, options, stream);

            using var response = await http.PostAsJsonAsync("/api/chat", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var ollama = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken);
            return ollama ?? new OllamaChatResponse();
        }

        private OllamaChatRequest CreateRequest(IEnumerable<ChatMessage> messages, ChatOptions? options, bool stream)
        {
            return new OllamaChatRequest
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
        }

        private static ChatResponseUpdate CreateStreamingUpdate(string text) =>
            new(ChatRole.Assistant, [new TextContent(text)]);

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
                    .AppendLine("<pre>")
                    .AppendLine(WebUtility.HtmlEncode(normalizedThinking.Trim()))
                    .AppendLine("</pre>")
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

        private sealed class VisibleThinkingStreamFormatter
        {
            private const string ThinkStartTag = "<think>";
            private const string ThinkEndTag = "</think>";
            private const int TagLookbehindLength = 16;
            private readonly StringBuilder contentBuffer = new();
            private bool inTaggedThinking;
            private bool thinkingBlockOpen;

            public IEnumerable<string> AppendThinking(string text)
            {
                if (string.IsNullOrEmpty(text))
                    yield break;

                foreach (var chunk in OpenThinkingBlock())
                    yield return chunk;

                yield return WebUtility.HtmlEncode(text);
            }

            public IEnumerable<string> AppendContent(string text)
            {
                if (string.IsNullOrEmpty(text))
                    yield break;

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

                            yield return current[..startIndex];
                        }

                        contentBuffer.Remove(0, startIndex + ThinkStartTag.Length);
                        foreach (var chunk in OpenThinkingBlock())
                            yield return chunk;

                        inTaggedThinking = true;
                        continue;
                    }

                    var flushLength = GetSafeFlushLength(current);
                    if (flushLength <= 0)
                        yield break;

                    foreach (var chunk in CloseThinkingBlock())
                        yield return chunk;

                    yield return current[..flushLength];
                    contentBuffer.Remove(0, flushLength);
                }
            }

            public IEnumerable<string> Complete()
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

                        yield return current;
                    }
                }

                foreach (var chunk in CloseThinkingBlock())
                    yield return chunk;

                inTaggedThinking = false;
            }

            private IEnumerable<string> OpenThinkingBlock()
            {
                if (thinkingBlockOpen)
                    yield break;

                thinkingBlockOpen = true;
                yield return "<details class=\"model-thinking\" open><summary>Model thinking</summary><pre>";
            }

            private IEnumerable<string> CloseThinkingBlock()
            {
                if (!thinkingBlockOpen)
                    yield break;

                thinkingBlockOpen = false;
                yield return "</pre></details>\n\n";
            }

            private static int GetSafeFlushLength(string current)
            {
                if (current.Length <= TagLookbehindLength)
                    return 0;

                return current.Length - TagLookbehindLength;
            }
        }
    }
}
