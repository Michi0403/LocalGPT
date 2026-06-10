using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
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
        private readonly ILogger logger;
        public OllamaThinkingChatClient(OllamaCoreOptions options, ILogger logger, string? keepAlive = null, int? contextLength = null, TimeSpan? timeout = null, int? numGpu = null) 
        {
            try
            {
                this.logger = logger;
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
                ArgumentNullException.ThrowIfNull(response);
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
                var streamingStatusUpdate = CouncilChatStaticsGeneral.OllamaThinkingChatClientCreateStreamingStatusUpdate($"LocalGPT sent the request to Ollama model {model}. Waiting for the local runtime to accept the stream...", logger);
                ArgumentNullException.ThrowIfNull(streamingStatusUpdate);
                yield return streamingStatusUpdate;

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
                    await CouncilChatStaticsGeneral.OllamaThinkingChatClientEnsureSuccessOrThrowAsync(response, cancellationToken, logger);
                    var statusUpdate = CouncilChatStaticsGeneral.OllamaThinkingChatClientCreateStreamingStatusUpdate("Ollama accepted the request. Waiting for streamed model output...", logger);
                    ArgumentNullException.ThrowIfNull(statusUpdate);
                    yield return statusUpdate;

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
                                foreach (var text in CouncilChatStringFunctions.AppendThinking(chunk.Message.Thinking,false,logger))
                                {
                                    var streamingUpdate = CouncilChatStaticsGeneral.OllamaThinkingChatClientCreateStreamingUpdate(text, logger);
                                    ArgumentNullException.ThrowIfNull(streamingUpdate);
                                    yield return (ChatResponseUpdate)streamingUpdate;
                                }
                            }

                            if (!string.IsNullOrEmpty(chunk?.Message?.Content))
                            {
                                foreach (var text in CouncilChatStringFunctions.AppendContent(chunk.Message.Content, false, logger))
                                {
                                    var streamingUpdate = CouncilChatStaticsGeneral.OllamaThinkingChatClientCreateStreamingUpdate(text, logger);
                                    ArgumentNullException.ThrowIfNull(streamingUpdate);
                                    yield return (ChatResponseUpdate)streamingUpdate;
                                }
                            }
                        }

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
                    await CouncilChatStaticsGeneral.OllamaThinkingChatClientEnsureSuccessOrThrowAsync(response, cancellationToken, logger);

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
                var messagesForThinkingClient = OllamaThinkingChatClientBuildOllamaMessages(messages);
                ArgumentNullException.ThrowIfNull(messagesForThinkingClient);
                return new OllamaChatRequest
                {
                    Model = model,
                    Stream = stream,
                    KeepAlive = keepAlive,
                    Messages = messagesForThinkingClient,
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

        public List<OllamaChatMessage>? OllamaThinkingChatClientBuildOllamaMessages(IEnumerable<ChatMessage> messages)
        {
            try
            {
                var ollamaMessages = messages
             .Select(filter => CouncilChatStaticsGeneral.OllamaThinkingChatClientToOllamaMessage(filter,logger)?? new())
             .Where(message => !string.IsNullOrWhiteSpace(message.Content))
             .ToList();

                if (CouncilChatStringFunctions.IsHarmonyModel(model,logger))
                    CouncilChatStaticsGeneral.OllamaThinkingChatClientAddHarmonyResponseProtocol(ollamaMessages,logger);

                return ollamaMessages;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildOllamaMessages messages {messages.ToString()}");
                return null;
            }
        }

  

        public List<AIContent>? CreateContents(OllamaChatResponse response)
        {
            try
            {
                var visible = CouncilChatStringFunctions.FormatVisibleResponse(model,response.Message?.Content, response.Message?.Thinking, logger);
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
