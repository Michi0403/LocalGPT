using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using LocalGPT.Services.Formatting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Services;

public sealed class OllamaThinkingChatClient : IChatClient
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
    private readonly OllamaCoreOptions providerOptions;
    private readonly IChatResponseFormatterFactory formatterFactory;
    private readonly IChatProtocolResolver protocolResolver;
    private readonly IPromptConfigService? promptConfigService;

    public OllamaThinkingChatClient(
        OllamaCoreOptions options,
        ILogger logger,
        string? keepAlive = null,
        int? contextLength = null,
        TimeSpan? timeout = null,
        int? numGpu = null,
        IChatResponseFormatterFactory? formatterFactory = null,
        IChatProtocolResolver? protocolResolver = null,
        IPromptConfigService? promptConfigService = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        providerOptions = options;
        this.formatterFactory = formatterFactory ??
            new ChatResponseFormatterFactory(NullLoggerFactory.Instance);
        this.protocolResolver = protocolResolver ?? new ChatProtocolResolver();
        this.promptConfigService = promptConfigService;

        model = string.IsNullOrWhiteSpace(options.ModelName)
            ? throw new ArgumentException("An Ollama model name is required.", nameof(options))
            : options.ModelName.Trim();
        this.keepAlive = string.IsNullOrWhiteSpace(keepAlive) ? "10m" : keepAlive.Trim();
        this.contextLength = contextLength;
        this.numGpu = numGpu;

        var endpoint = options.Uri?.TrimEnd('/');
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var baseAddress) ||
            (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("An absolute HTTP(S) Ollama endpoint is required.", nameof(options));

        http = new HttpClient
        {
            BaseAddress = baseAddress,
            Timeout = timeout ?? TimeSpan.FromMinutes(10)
        };
    }

    public async Task<ChatResponse?> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendAsync(messages, options, stream: false, cancellationToken)
                .ConfigureAwait(false);
            if (response is null)
                return null;

            return new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                await CreateContentsAsync(response, cancellationToken).ConfigureAwait(false)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ollama non-streaming response failed for model {Model}.", model);
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
            var missingFinalAnswerNotice = await GetPromptAsync("MissingFinalAnswerNotice", cancellationToken).ConfigureAwait(false);
            var formatter = formatterFactory.Create(
                protocolResolver.Resolve(providerOptions),
                missingFinalAnswerNotice);
            var request = await CreateRequestAsync(messages, options, stream: true, cancellationToken)
                .ConfigureAwait(false);

            var waitingUpdate = CouncilChatStaticsGeneral.OllamaThinkingChatClientCreateStreamingStatusUpdate(
                $"LocalGPT sent the request to Ollama model {model}. Waiting for the local runtime to accept the stream...",
                logger);
            if (waitingUpdate is not null)
                yield return waitingUpdate;

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };

            using var response = await http.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);

            await CouncilChatStaticsGeneral.OllamaThinkingChatClientEnsureSuccessOrThrowAsync(
                    response,
                    cancellationToken,
                    logger)
                .ConfigureAwait(false);

            var acceptedUpdate = CouncilChatStaticsGeneral.OllamaThinkingChatClientCreateStreamingStatusUpdate(
                "Ollama accepted the request. Waiting for streamed model output...",
                logger);
            if (acceptedUpdate is not null)
                yield return acceptedUpdate;

            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                OllamaChatResponse? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonOptions);
                }
                catch (JsonException ex)
                {
                    logger.LogDebug(ex, "Ignored a malformed Ollama streaming frame for model {Model}.", model);
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
        finally
        {
            logger.LogInformation("Ended Ollama streaming response for model {Model}.", model);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(HttpClient) ? http : null;

    public void Dispose() => http.Dispose();

    private async Task<OllamaChatResponse?> SendAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        bool stream,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await CreateRequestAsync(messages, options, stream, cancellationToken)
                .ConfigureAwait(false);
            using var response = await http
                .PostAsJsonAsync("/api/chat", request, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            await CouncilChatStaticsGeneral.OllamaThinkingChatClientEnsureSuccessOrThrowAsync(
                    response,
                    cancellationToken,
                    logger)
                .ConfigureAwait(false);

            return await response.Content
                .ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ollama request failed for model {Model}; streaming={Streaming}.", model, stream);
            return null;
        }
    }

    private async Task<OllamaChatRequest> CreateRequestAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        bool stream,
        CancellationToken cancellationToken)
    {
        var mappedMessages = messages
            .Select(ToOllamaMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .ToList();

        if (protocolResolver.Resolve(providerOptions) == ChatResponseProtocol.Harmony)
        {
            var harmonyPrompt = await GetPromptAsync("HarmonyResponseProtocol", cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(harmonyPrompt))
                AddSystemPrompt(mappedMessages, harmonyPrompt);
            else
                logger.LogWarning("Harmony protocol is selected for model {Model}, but its database prompt is unavailable.", model);
        }

        return new OllamaChatRequest
        {
            Model = model,
            Stream = stream,
            KeepAlive = keepAlive,
            Messages = mappedMessages,
            Options = new OllamaRequestOptions
            {
                NumPredict = Math.Clamp(options?.MaxOutputTokens ?? 2048, 64, 262144),
                NumCtx = contextLength,
                NumGpu = numGpu,
                Temperature = options?.Temperature
            }
        };
    }

    private async Task<List<AIContent>> CreateContentsAsync(
        OllamaChatResponse response,
        CancellationToken cancellationToken)
    {
        var missingFinalAnswerNotice = await GetPromptAsync("MissingFinalAnswerNotice", cancellationToken).ConfigureAwait(false);
        var formatter = formatterFactory.Create(
            protocolResolver.Resolve(providerOptions),
            missingFinalAnswerNotice);
        var visible = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(response.Message?.Thinking))
        {
            foreach (var chunk in formatter.AppendThinking(response.Message.Thinking))
                visible.Append(chunk);
        }

        if (!string.IsNullOrEmpty(response.Message?.Content))
        {
            foreach (var chunk in formatter.AppendContent(response.Message.Content))
                visible.Append(chunk);
        }

        foreach (var chunk in formatter.Complete())
            visible.Append(chunk);

        return [new TextContent(visible.ToString())];
    }

    private async Task<string> GetPromptAsync(string key, CancellationToken cancellationToken)
    {
        if (promptConfigService is null)
            return string.Empty;

        return await promptConfigService
            .GetPromptAsync(key, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static OllamaChatMessage ToOllamaMessage(ChatMessage message) => new()
    {
        Role = message.Role == ChatRole.System
            ? "system"
            : message.Role == ChatRole.Assistant
                ? "assistant"
                : "user",
        Content = message.Text ?? string.Empty
    };

    private static void AddSystemPrompt(List<OllamaChatMessage> messages, string prompt)
    {
        if (messages.Count > 0 && messages[0].Role.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            if (!messages[0].Content.Contains(prompt, StringComparison.Ordinal))
                messages[0].Content = $"{prompt}\n\n{messages[0].Content}";
            return;
        }

        messages.Insert(0, new OllamaChatMessage
        {
            Role = "system",
            Content = prompt
        });
    }

    private ChatResponseUpdate CreateStreamingUpdate(string text)
    {
        var update = CouncilChatStaticsGeneral.OllamaThinkingChatClientCreateStreamingUpdate(text, logger);
        return update ?? new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(text)]);
    }
}
