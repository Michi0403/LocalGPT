using LocalGPT.BusinessObjects;
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
    private const int MaxAutomaticToolRounds = 3;
    private const int MaxToolResultCharacters = 16_000;

    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
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
    private readonly IDxAiFunctionRegistry? functionRegistry;
    private readonly CouncilRuntimeService councilRuntime;

    public OllamaThinkingChatClient(
        OllamaCoreOptions options,
        ILogger logger,
        CouncilRuntimeService councilRuntime,
        string? keepAlive = null,
        int? contextLength = null,
        TimeSpan? timeout = null,
        int? numGpu = null,
        IChatResponseFormatterFactory? formatterFactory = null,
        IChatProtocolResolver? protocolResolver = null,
        IPromptConfigService? promptConfigService = null,
        IDxAiFunctionRegistry? functionRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.councilRuntime = councilRuntime ?? throw new ArgumentNullException(nameof(councilRuntime));
        providerOptions = options;
        this.formatterFactory = formatterFactory ?? new ChatResponseFormatterFactory(NullLoggerFactory.Instance);
        this.protocolResolver = protocolResolver ?? new ChatProtocolResolver();
        this.promptConfigService = promptConfigService;
        this.functionRegistry = functionRegistry;

        model = string.IsNullOrWhiteSpace(options.ModelName)
            ? throw new ArgumentException("An Ollama model name is required.", nameof(options))
            : options.ModelName.Trim();
        this.keepAlive = string.IsNullOrWhiteSpace(keepAlive) ? "10m" : keepAlive.Trim();
        this.contextLength = contextLength;
        this.numGpu = numGpu;

        var endpoint = options.Uri?.TrimEnd('/');
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var baseAddress) ||
            (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("An absolute HTTP(S) Ollama endpoint is required.", nameof(options));
        }

        http = new HttpClient
        {
            BaseAddress = baseAddress,
            Timeout = timeout ?? TimeSpan.FromMinutes(10)
        };
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await CreateConversationAsync(messages, cancellationToken).ConfigureAwait(false);
            OllamaChatResponse? response = null;

            for (var round = 0; round <= MaxAutomaticToolRounds; round++)
            {
                response = await SendAsync(conversation, options, stream: false, cancellationToken).ConfigureAwait(false);
                if (response is null)
                    return CreateFailureResponse("Ollama returned no response. Verify that the selected model is available and the local runtime is healthy.");

                var toolCalls = response.Message?.ToolCalls;
                if (toolCalls is not { Count: > 0 })
                    break;

                if (round == MaxAutomaticToolRounds)
                {
                    logger.LogWarning("Stopped Ollama automatic DXAIFunction loop for model {Model} after {ToolRounds} rounds.", model, MaxAutomaticToolRounds);
                    break;
                }

                conversation.Add(CloneMessage(response.Message!));
                await AppendAutomaticToolResultsAsync(conversation, toolCalls, cancellationToken).ConfigureAwait(false);
            }

            if (response is null)
                return CreateFailureResponse("Ollama returned no response. Verify that the selected model is available and the local runtime is healthy.");

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
            return CreateFailureResponse($"Ollama model '{model}' could not complete the response. Review LocalGPT application logs and verify the local runtime.");
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        using var operationScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId,
            ["Operation"] = "OllamaStreamingChat",
            ["Model"] = model
        });

        try
        {
            var missingFinalAnswerNotice = await GetPromptAsync("MissingFinalAnswerNotice", cancellationToken).ConfigureAwait(false);
            var formatter = formatterFactory.Create(protocolResolver.Resolve(providerOptions), missingFinalAnswerNotice);
            var conversation = await CreateConversationAsync(messages, cancellationToken).ConfigureAwait(false);

            for (var round = 0; round <= MaxAutomaticToolRounds; round++)
            {
                var request = await CreateRequestAsync(conversation, options, stream: true, cancellationToken).ConfigureAwait(false);

                var waitingUpdate = councilRuntime.OllamaThinkingChatClientCreateStreamingStatusUpdate(
                    $"LocalGPT sent the request to Ollama model {model}. Waiting for the local runtime to accept the stream...",
                    logger);
                if (waitingUpdate is not null)
                    yield return waitingUpdate;

                using var response = await SendRequestWithToolFallbackAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);

                await councilRuntime.OllamaThinkingChatClientEnsureSuccessOrThrowAsync(response, cancellationToken, logger)
                    .ConfigureAwait(false);

                var acceptedUpdate = councilRuntime.OllamaThinkingChatClientCreateStreamingStatusUpdate(
                    "Ollama accepted the request. Waiting for streamed model output...",
                    logger);
                if (acceptedUpdate is not null)
                    yield return acceptedUpdate;

                var toolCalls = new List<OllamaToolCall>();
                var assistantContent = new StringBuilder();
                var assistantThinking = new StringBuilder();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    OllamaChatResponse? chunk;
                    try
                    {
                        chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        logger.LogDebug(ex, "Ignored a malformed Ollama streaming frame for model {Model}.", model);
                        continue;
                    }

                    if (chunk?.Message?.ToolCalls is { Count: > 0 } chunkCalls)
                        toolCalls.AddRange(chunkCalls);

                    if (!string.IsNullOrWhiteSpace(chunk?.Message?.Thinking))
                    {
                        assistantThinking.Append(chunk.Message.Thinking);
                        foreach (var text in formatter.AppendThinking(chunk.Message.Thinking))
                            yield return CreateStreamingUpdate(text);
                    }

                    if (!string.IsNullOrEmpty(chunk?.Message?.Content))
                    {
                        assistantContent.Append(chunk.Message.Content);
                        foreach (var text in formatter.AppendContent(chunk.Message.Content))
                            yield return CreateStreamingUpdate(text);
                    }
                }

                toolCalls = DeduplicateToolCalls(toolCalls);
                if (toolCalls.Count == 0)
                {
                    foreach (var text in formatter.Complete())
                        yield return CreateStreamingUpdate(text);
                    yield break;
                }

                if (round == MaxAutomaticToolRounds)
                {
                    logger.LogWarning("Stopped Ollama automatic DXAIFunction loop for model {Model} after {ToolRounds} rounds.", model, MaxAutomaticToolRounds);
                    var stoppedUpdate = councilRuntime.OllamaThinkingChatClientCreateStreamingStatusUpdate(
                        "LocalGPT stopped repeated automatic read-only function calls. Continue manually if more inspection is needed.",
                        logger);
                    if (stoppedUpdate is not null)
                        yield return stoppedUpdate;
                    foreach (var text in formatter.Complete())
                        yield return CreateStreamingUpdate(text);
                    yield break;
                }

                conversation.Add(new OllamaChatMessage
                {
                    Role = "assistant",
                    Content = assistantContent.ToString(),
                    Thinking = assistantThinking.Length == 0 ? null : assistantThinking.ToString(),
                    ToolCalls = toolCalls
                });

                foreach (var call in toolCalls)
                {
                    var registryName = ResolveRegistryFunctionName(call.Function.Name);
                    var toolUpdate = councilRuntime.OllamaThinkingChatClientCreateStreamingStatusUpdate(
                        registryName is null
                            ? $"Ollama requested unknown function {call.Function.Name}; LocalGPT will return a denied tool result."
                            : $"LocalGPT is invoking read-only function {registryName} for the current answer...",
                        logger);
                    if (toolUpdate is not null)
                        yield return toolUpdate;
                }

                await AppendAutomaticToolResultsAsync(conversation, toolCalls, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            logger.LogInformation("Ended Ollama streaming response for model {Model}.", model);
        }
    }

    private static ChatResponse CreateFailureResponse(string message) =>
        new(new ChatMessage(ChatRole.Assistant, [new TextContent(message)]));

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(HttpClient) ? http : null;

    public void Dispose() => http.Dispose();

    private async Task<OllamaChatResponse?> SendAsync(
        IReadOnlyList<OllamaChatMessage> messages,
        ChatOptions? options,
        bool stream,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await CreateRequestAsync(messages, options, stream, cancellationToken).ConfigureAwait(false);
            using var response = await SendRequestWithToolFallbackAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken)
                .ConfigureAwait(false);

            await councilRuntime.OllamaThinkingChatClientEnsureSuccessOrThrowAsync(response, cancellationToken, logger)
                .ConfigureAwait(false);

            return await response.Content.ReadFromJsonAsync<OllamaChatResponse>(jsonOptions, cancellationToken).ConfigureAwait(false);
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

    private async Task<HttpResponseMessage> SendRequestWithToolFallbackAsync(
        OllamaChatRequest request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        var response = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode || request.Tools is not { Count: > 0 } ||
            response.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
        {
            return response;
        }

        logger.LogInformation(
            "Ollama model {Model} rejected native tool metadata with HTTP {StatusCode}; retrying the same chat request without automatic tools.",
            model,
            (int)response.StatusCode);
        response.Dispose();
        request.Tools = null;
        return await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendRequestOnceAsync(
        OllamaChatRequest request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(request, options: jsonOptions)
        };
        return await http.SendAsync(httpRequest, completionOption, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<OllamaChatMessage>> CreateConversationAsync(
        IEnumerable<ChatMessage> messages,
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

        return mappedMessages;
    }

    private Task<OllamaChatRequest> CreateRequestAsync(
        IReadOnlyList<OllamaChatMessage> messages,
        ChatOptions? options,
        bool stream,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requestMessages = messages.Select(CloneMessage).ToList();
        return Task.FromResult(new OllamaChatRequest
        {
            Model = model,
            Stream = stream,
            KeepAlive = keepAlive,
            Messages = requestMessages,
            Tools = BuildAutomaticTools(),
            Options = new OllamaRequestOptions
            {
                NumPredict = Math.Clamp(options?.MaxOutputTokens ?? 2048, 64, 262144),
                NumCtx = contextLength,
                NumGpu = numGpu,
                Temperature = options?.Temperature
            }
        });
    }

    private List<OllamaToolDefinition>? BuildAutomaticTools()
    {
        var functions = GetAutomaticFunctions();
        if (functions.Count == 0)
            return null;

        return functions.Select(function => new OllamaToolDefinition
        {
            Function = new OllamaToolFunctionDefinition
            {
                Name = ToOllamaToolName(function.Name),
                Description = $"{function.Purpose} Parameters: {function.Parameters} Safety: {function.SafetyNotes}",
                Parameters = BuildParametersSchema(function)
            }
        }).ToList();
    }

    private IReadOnlyList<DxaichatFunctionInfo> GetAutomaticFunctions() => functionRegistry?
        .GetFunctions()
        .Where(function => function.AvailableToAi && function.IsReadOnly &&
                           function.SupportsDirectInvocation && function.SupportsAutomaticInvocation &&
                           !function.RequiresHumanConfirmation)
        .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
        .ToList() ?? [];

    private static List<OllamaToolCall> DeduplicateToolCalls(IEnumerable<OllamaToolCall> toolCalls)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<OllamaToolCall>();
        foreach (var call in toolCalls)
        {
            var key = $"{call.Function.Name}\n{NormalizeArguments(call.Function.Arguments).GetRawText()}";
            if (seen.Add(key))
                result.Add(call);
        }
        return result;
    }

    private async Task AppendAutomaticToolResultsAsync(
        List<OllamaChatMessage> conversation,
        IReadOnlyList<OllamaToolCall> toolCalls,
        CancellationToken cancellationToken)
    {
        foreach (var call in toolCalls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var registryName = ResolveRegistryFunctionName(call.Function.Name);
            DxAiFunctionInvocationResult result;

            if (functionRegistry is null || registryName is null)
            {
                result = new DxAiFunctionInvocationResult
                {
                    FunctionName = call.Function.Name,
                    Status = "NotFound",
                    Error = "The requested automatic function is not registered.",
                    Succeeded = false
                };
            }
            else
            {
                result = await functionRegistry.InvokeAsync(
                    registryName,
                    new DxAiFunctionInvocationRequest
                    {
                        Parameters = NormalizeArguments(call.Function.Arguments),
                        AutomaticInvocation = true,
                        UserConfirmed = false,
                        RequestedBy = $"Ollama:{model}"
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            conversation.Add(new OllamaChatMessage
            {
                Role = "tool",
                ToolName = call.Function.Name,
                Content = SerializeToolResult(result)
            });
        }
    }

    private string? ResolveRegistryFunctionName(string toolName) => GetAutomaticFunctions()
        .FirstOrDefault(function => ToOllamaToolName(function.Name).Equals(toolName, StringComparison.OrdinalIgnoreCase))
        ?.Name;

    private static string ToOllamaToolName(string registryName)
    {
        var builder = new StringBuilder(registryName.Length);
        foreach (var character in registryName)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        return builder.ToString();
    }

    private JsonElement BuildParametersSchema(DxaichatFunctionInfo function)
    {
        try
        {
            using var document = JsonDocument.Parse(function.ParameterSchemaJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("DXAIFunction parameter schema must be a JSON object.");
            return root.Clone();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "DXAIFunction {FunctionName} has invalid parameter schema metadata; an empty object schema will be used.", function.Name);
            return JsonSerializer.SerializeToElement(new { type = "object", properties = new { } });
        }
    }

    private static JsonElement NormalizeArguments(JsonElement arguments) => arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
        ? JsonSerializer.SerializeToElement(new { })
        : arguments.Clone();

    private string SerializeToolResult(DxAiFunctionInvocationResult result)
    {
        var json = JsonSerializer.Serialize(result, jsonOptions);
        return json.Length <= MaxToolResultCharacters
            ? json
            : json[..MaxToolResultCharacters] + "\n{\"truncated\":true}";
    }

    private async Task<List<AIContent>> CreateContentsAsync(
        OllamaChatResponse response,
        CancellationToken cancellationToken)
    {
        var missingFinalAnswerNotice = await GetPromptAsync("MissingFinalAnswerNotice", cancellationToken).ConfigureAwait(false);
        var formatter = formatterFactory.Create(protocolResolver.Resolve(providerOptions), missingFinalAnswerNotice);
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

        return await promptConfigService.GetPromptAsync(key, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static OllamaChatMessage CloneMessage(OllamaChatMessage message) => new()
    {
        Role = message.Role,
        Content = message.Content,
        Thinking = message.Thinking,
        ToolName = message.ToolName,
        ToolCalls = message.ToolCalls?.Select(call => new OllamaToolCall
        {
            Function = new OllamaToolFunctionCall
            {
                Name = call.Function.Name,
                Arguments = NormalizeArguments(call.Function.Arguments)
            }
        }).ToList()
    };

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
        var update = councilRuntime.OllamaThinkingChatClientCreateStreamingUpdate(text, logger);
        return update ?? new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(text)]);
    }
}
