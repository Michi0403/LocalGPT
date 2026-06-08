using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static LocalGPT.Extensions.PlainStatics.GlobalVariableSlopCollectionToRemove;

namespace LocalGPT.Services
{
    public sealed partial class OllamaThinkingChatClient(ILogger logger) : IChatClient
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

        public OllamaThinkingChatClient(OllamaCoreOptions options, ILogger logger, string? keepAlive = null, int? contextLength = null, TimeSpan? timeout = null, int? numGpu = null) : this(logger)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Counstructor OllamaThinkingChatClient options {options.ToString()} keepAlive {options}  contextLength {contextLength}  timeout {timeout}  numGpu {numGpu} ");
            }

        }

        public async Task<ChatResponse?> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await SendAsync(messages, options, stream: false, cancellationToken);
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, CreateContents(response)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetResponseAsync messages {messages.ToString()} options {options?.ToString()}");
                return null;
            }

        }

        public async IAsyncEnumerable<ChatResponseUpdate>? GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                var request = CreateRequest(messages, options, stream: true);
                yield return CreateStreamingStatusUpdate($"LocalGPT sent the request to Ollama model {model}. Waiting for the local runtime to accept the stream...",logger);

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
                    await EnsureSuccessOrThrowAsync(response, cancellationToken, logger);
                    yield return CreateStreamingStatusUpdate("Ollama accepted the request. Waiting for streamed model output...", logger);

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
                        var formatter = new VisibleThinkingStreamFormatter(IsHarmonyModel());
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
                                    yield return CreateStreamingUpdate(text, logger);
                            }

                            if (!string.IsNullOrEmpty(chunk?.Message?.Content))
                            {
                                foreach (var text in formatter.AppendContent(chunk.Message.Content))
                                    yield return CreateStreamingUpdate(text, logger);
                            }
                        }

                        foreach (var text in formatter.Complete())
                            yield return CreateStreamingUpdate(text, logger);
                    }
                }
            }
            finally 
            {
                logger.LogInformation($"Error in GetStreamingResponseAsync messages {messages.ToString()} options {options?.ToString()}");
               
            }
            
            
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
            try
            {
                //??
                http.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Dispose messages");
         
            }

        }

        private async Task<OllamaChatResponse?> SendAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            bool stream,
            CancellationToken cancellationToken)
        {
            try
            {
                var request = CreateRequest(messages, options, stream);

                HttpResponseMessage response;
                try
                {
                    response = await http.PostAsJsonAsync("/api/chat", request, JsonOptions, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return new OllamaChatResponse();
                }

                using (response)
                {
                    await EnsureSuccessOrThrowAsync(response, cancellationToken, logger);

                    var ollama = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken);
                    return ollama ?? new OllamaChatResponse();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SendAsync messages {messages.ToString()} options {options?.ToString()} stream {stream.ToString()}");
                return null;
            }

        }

        private OllamaChatRequest? CreateRequest(IEnumerable<ChatMessage> messages, ChatOptions? options, bool stream)
        {
            try
            {
                return new OllamaChatRequest
                {
                    Model = model,
                    Stream = stream,
                    KeepAlive = keepAlive,
                    Messages = BuildOllamaMessages(messages),
                    Options = new OllamaRequestOptions
                    {
                        NumPredict = Math.Clamp(options?.MaxOutputTokens ?? 2048, 64, 262144),
                        NumCtx = contextLength,
                        NumGpu = numGpu,
                        Temperature = options?.Temperature
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateRequest messages {messages.ToString()} options {options?.ToString()} stream {stream.ToString()}");
                return null;
            }
        }

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                if (response.IsSuccessStatusCode)
                    return;

                var body = await ReadErrorBodyAsync(response, cancellationToken, logger);
                var message = string.IsNullOrWhiteSpace(body)
                    ? $"Ollama returned {(int)response.StatusCode} {response.StatusCode}."
                    : $"Ollama returned {(int)response.StatusCode} {response.StatusCode}: {body}";
                throw new HttpRequestException(message, null, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EnsureSuccessOrThrowAsync response {response.ToString()}");
                
            }
        }

        private static async Task<string> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(body))
                    return string.Empty;

                return body.Length <= 4000 ? body.Trim() : body[..4000].Trim() + "...";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadErrorBodyAsync response {response.ToString()}");
                return string.Empty;
            }
        }

        private static ChatResponseUpdate? CreateStreamingUpdate(string text, ILogger logger)
        {
            try
            {
                return new(ChatRole.Assistant, [new TextContent(text)]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStreamingUpdate text {text.ToString()}");
                return null;
            }
        }


        private static ChatResponseUpdate? CreateStreamingStatusUpdate(string text, ILogger logger)
        {
            try
            {
               return CreateStreamingUpdate($"<p class=\"localgpt-stream-status\"><em>{WebUtility.HtmlEncode(text)}</em></p>\n\n", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStreamingStatusUpdate text {text.ToString()}");
                return null;
            }
        }
           

        private List<OllamaChatMessage>? BuildOllamaMessages(IEnumerable<ChatMessage> messages)
        {
            try
            {
                var ollamaMessages = messages
             .Select(filter => ToOllamaMessage(filter,logger)?? new())
             .Where(message => !string.IsNullOrWhiteSpace(message.Content))
             .ToList();

                if (IsHarmonyModel())
                    AddHarmonyResponseProtocol(ollamaMessages,logger);

                return ollamaMessages;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildOllamaMessages messages {messages.ToString()}");
                return null;
            }

        }

        private static void AddHarmonyResponseProtocol(List<OllamaChatMessage> messages, ILogger logger)
        {
            try
            {
                if (messages.Count > 0 &&
          messages[0].Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                {
                    if (!messages[0].Content.Contains(HarmonyResponseProtocol, StringComparison.Ordinal))
                        messages[0].Content = $"{HarmonyResponseProtocol}\n\n{messages[0].Content}";

                    return;
                }

                messages.Insert(0, new OllamaChatMessage
                {
                    Role = "system",
                    Content = HarmonyResponseProtocol
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddHarmonyResponseProtocol messages {messages.ToString()}");
            }
        }

        private static OllamaChatMessage? ToOllamaMessage(ChatMessage message, ILogger logger)
        {
            try
            {
                return new OllamaChatMessage
                {
                    Role = message.Role == ChatRole.System ? "system"
                      : message.Role == ChatRole.Assistant ? "assistant"
                      : "user",
                    Content = message.Text
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToOllamaMessage message {message.ToString()}");
                return null;
            }
        }

        public List<AIContent>? CreateContents(OllamaChatResponse response)
        {
            try
            {
                var visible = FormatVisibleResponse(response.Message?.Content, response.Message?.Thinking);
                return
                [
                    new TextContent(visible)
                ];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateContents response {response.ToString()}");
                return null;
            }
        }



    }
}
