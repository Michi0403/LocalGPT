using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Formatting;
using Microsoft.Extensions.AI;
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
    private readonly IDxAiFunctionCallRecoveryService? functionCallRecovery;
    private readonly bool automaticToolsEnabled;
    private readonly bool throwOnFailure;
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
        IDxAiFunctionRegistry? functionRegistry = null,
        IDxAiFunctionCallRecoveryService? functionCallRecovery = null,
        bool enableAutomaticTools = true,
        bool throwOnFailure = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.councilRuntime = councilRuntime ?? throw new ArgumentNullException(nameof(councilRuntime));
        providerOptions = options;
        this.formatterFactory = formatterFactory
            ?? throw new ArgumentNullException(nameof(formatterFactory));
        this.protocolResolver = protocolResolver
            ?? throw new ArgumentNullException(nameof(protocolResolver));
        this.promptConfigService = promptConfigService
            ?? throw new ArgumentNullException(nameof(promptConfigService));
        this.functionRegistry = functionRegistry;
        this.functionCallRecovery = functionCallRecovery;
        automaticToolsEnabled = enableAutomaticTools;
        this.throwOnFailure = throwOnFailure;

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
                {
                    if (throwOnFailure)
                        throw new InvalidOperationException($"Ollama model '{model}' returned no response.");
                    return CreateFailureResponse("Ollama returned no response. Verify that the selected model is available and the local runtime is healthy.");
                }

                var toolCalls = response.Message?.ToolCalls;
                if (toolCalls is not { Count: > 0 } && response.Message is { Content.Length: > 0 } message && functionCallRecovery is not null)
                {
                    var recovered = functionCallRecovery.Recover(message.Content, automaticInvocation: true);
                    if (recovered.Recognized)
                    {
                        toolCalls = ToOllamaToolCalls(recovered.Calls);
                        message.Content = recovered.VisibleContent;
                        message.ToolCalls = toolCalls;
                    }
                }
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
            {
                if (throwOnFailure)
                    throw new InvalidOperationException($"Ollama model '{model}' returned no response.");
                return CreateFailureResponse("Ollama returned no response. Verify that the selected model is available and the local runtime is healthy.");
            }

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
            if (throwOnFailure)
                throw;
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
                var pendingContent = new StringBuilder();
                var contentModeDecided = false;
                var bufferPotentialFunctionText = false;

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
                        if (!contentModeDecided)
                        {
                            pendingContent.Append(chunk.Message.Content);
                            if (!string.IsNullOrWhiteSpace(pendingContent.ToString()))
                            {
                                bufferPotentialFunctionText = functionCallRecovery?.LooksLikeStructuredFunctionCall(pendingContent.ToString()) == true;
                                contentModeDecided = true;
                                if (!bufferPotentialFunctionText)
                                {
                                    foreach (var text in formatter.AppendContent(pendingContent.ToString()))
                                        yield return CreateStreamingUpdate(text);
                                    pendingContent.Clear();
                                }
                            }
                        }
                        else if (bufferPotentialFunctionText)
                        {
                            pendingContent.Append(chunk.Message.Content);
                        }
                        else
                        {
                            foreach (var text in formatter.AppendContent(chunk.Message.Content))
                                yield return CreateStreamingUpdate(text);
                        }
                    }
                }

                toolCalls = DeduplicateToolCalls(toolCalls);
                if (toolCalls.Count == 0 && bufferPotentialFunctionText && functionCallRecovery is not null)
                {
                    var recovered = functionCallRecovery.Recover(assistantContent.ToString(), automaticInvocation: true);
                    if (recovered.Recognized)
                    {
                        toolCalls = ToOllamaToolCalls(recovered.Calls);
                        assistantContent.Clear();
                        assistantContent.Append(recovered.VisibleContent);
                        if (!string.IsNullOrWhiteSpace(recovered.VisibleContent))
                        {
                            foreach (var text in formatter.AppendContent(recovered.VisibleContent))
                                yield return CreateStreamingUpdate(text);
                        }
                        var recoveryUpdate = councilRuntime.OllamaThinkingChatClientCreateStreamingStatusUpdate(
                            $"LocalGPT recovered {toolCalls.Count} structured DX function call(s) from provider text and routed them through the normal function policy.",
                            logger);
                        if (recoveryUpdate is not null)
                            yield return recoveryUpdate;
                    }
                }
                else if (toolCalls.Count == 0 && pendingContent.Length > 0)
                {
                    foreach (var text in formatter.AppendContent(pendingContent.ToString()))
                        yield return CreateStreamingUpdate(text);
                }

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
                        "LocalGPT stopped repeated automatic/deferred DX function rounds. Continue manually if more work is needed.",
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
                            : $"LocalGPT is routing policy-approved function {registryName} through the DX function registry...",
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

    private ChatResponse CreateFailureResponse(string message) {
    try
    {
        return new(new ChatMessage(ChatRole.Assistant, [new TextContent(message)]));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateFailureResponse)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateFailureResponse)} failed.");
        throw;
    }
}

    public object? GetService(Type serviceType, object? serviceKey = null) {
    try
    {
        return serviceType == typeof(HttpClient) ? http : null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetService)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetService)} failed.");
        throw;
    }
}

    public void Dispose() {
    try
    {
        http.Dispose();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(Dispose)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(Dispose)} failed.");
        throw;
    }
}

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
    try
    {
            if (request.Tools is { Count: > 0 } &&
                councilRuntime.OllamaThinkingChatClientShouldSkipNativeTools(http.BaseAddress, model, logger))
            {
                logger.LogDebug(
                    "Skipping native tool metadata for Ollama model {Model} because this provider-qualified model rejected it earlier in the current LocalGPT process.",
                    model);
                EnsureTextualDxFunctionFallbackPrompt(request);
                request.Tools = null;
            }

            var response = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode || request.Tools is not { Count: > 0 } ||
                response.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
            {
                return response;
            }

            councilRuntime.OllamaThinkingChatClientRememberNativeToolsRejected(http.BaseAddress, model, logger);
            logger.LogInformation(
                "Ollama model {Model} rejected native tool metadata with HTTP {StatusCode}; retrying the same chat request without automatic tools.",
                model,
                (int)response.StatusCode);
            response.Dispose();
            EnsureTextualDxFunctionFallbackPrompt(request);
            request.Tools = null;
            return await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SendRequestWithToolFallbackAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SendRequestWithToolFallbackAsync)} failed.");
        throw;
    }
}

    private void EnsureTextualDxFunctionFallbackPrompt(OllamaChatRequest request)
    {
        try
        {
            const string marker = "[LOCALGPT_TEXTUAL_DXFUNCTION_FALLBACK]";
            if (request.Messages.Any(message =>
                    message.Role.Equals("system", StringComparison.OrdinalIgnoreCase) &&
                    message.Content.Contains(marker, StringComparison.Ordinal)))
            {
                return;
            }

            var functions = GetAutomaticFunctions();
            if (functions.Count == 0)
                return;

            var functionDirectory = string.Join(
           Environment.NewLine,
           functions.Select(function => $"- {function.Name}: {function.Parameters}"));
            request.Messages.Insert(0, new OllamaChatMessage
            {
                Role = "system",
                Content = $$$"""
                    {{{marker}}}
                    This exact provider-qualified Ollama model does not accept native tool metadata. LocalGPT still supports policy-checked DXFunctions through textual call recovery.
                    If a function is genuinely needed, emit one standalone JSON object with exactly this shape and no invented function name:
                    {"functionName":"exact.registry.name","arguments":{}}
                    LocalGPT will validate the exact registry name, apply normal automatic/deferred approval policy, execute it when permitted, display the tool activity, return the tool result, and then continue your response.
                    Do not guess a function name. If the capability you want is not in the directory below, report it as a requested capability instead of pretending it exists.
                    Exact automatic/deferred function directory for this request:
                    {{{functionDirectory}}}
                    """
            });
            logger.LogInformation(
                "Attached textual DXFunction fallback instructions with {FunctionCount} exact registry name(s) for Ollama model {Model} because native tool metadata is unavailable.",
                functions.Count,
                model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not attach textual DXFunction fallback instructions for Ollama model {Model}.", model);
            throw;
        }
    }

    private async Task<HttpResponseMessage> SendRequestOnceAsync(
        OllamaChatRequest request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
    try
    {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = JsonContent.Create(request, options: jsonOptions)
            };
            return await http.SendAsync(httpRequest, completionOption, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SendRequestOnceAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SendRequestOnceAsync)} failed.");
        throw;
    }
}

    private async Task<List<OllamaChatMessage>> CreateConversationAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateConversationAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateConversationAsync)} failed.");
        throw;
    }
}

    private Task<OllamaChatRequest> CreateRequestAsync(
        IReadOnlyList<OllamaChatMessage> messages,
        ChatOptions? options,
        bool stream,
        CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateRequestAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateRequestAsync)} failed.");
        throw;
    }
}

    private List<OllamaToolDefinition>? BuildAutomaticTools()
    {
    try
    {
            if (!automaticToolsEnabled)
                return null;

            if (functionRegistry is null)
            {
                logger.LogWarning("Ollama model {Model} has no DXFunction registry, so native tool metadata cannot be attached.", model);
                return null;
            }

            var functions = GetAutomaticFunctions();
            if (functions.Count == 0)
            {
                logger.LogWarning("Ollama model {Model} has no policy-approved automatic DXFunctions to attach.", model);
                return null;
            }

            logger.LogInformation("Attaching {FunctionCount} policy-approved automatic DXFunctions to Ollama model {Model}.", functions.Count, model);
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(BuildAutomaticTools)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(BuildAutomaticTools)} failed.");
        throw;
    }
}

    private IReadOnlyList<DxaichatFunctionInfo> GetAutomaticFunctions() {
    try
    {
        return functionRegistry?
        .GetFunctions()
        .Where(function => function.AvailableToAi &&
                           function.SupportsDirectInvocation &&
                           (function.RequiresHumanConfirmation
                               ? function.SupportsDeferredApprovalRequest
                               : function.SupportsAutomaticInvocation &&
                                 (function.IsReadOnly || function.IsCoordinationOnly)))
        .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
        .ToList() ?? [];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetAutomaticFunctions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetAutomaticFunctions)} failed.");
        throw;
    }
}

    private List<OllamaToolCall> ToOllamaToolCalls(IEnumerable<RecoveredDxAiFunctionCall> calls) {
    try
    {
        return calls.Select(call => new OllamaToolCall
        {
            Function = new OllamaToolFunctionCall
            {
                Name = ToTransportToolName(call.FunctionName),
                Arguments = call.Arguments.Clone()
            }
        }).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaToolCalls)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaToolCalls)} failed.");
        throw;
    }
}

    private string ToTransportToolName(string registryName)
    {
    try
    {
            var builder = new StringBuilder(registryName.Length);
            foreach (var character in registryName)
                builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToTransportToolName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToTransportToolName)} failed.");
        throw;
    }
}

    private List<OllamaToolCall> DeduplicateToolCalls(IEnumerable<OllamaToolCall> toolCalls)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(DeduplicateToolCalls)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(DeduplicateToolCalls)} failed.");
        throw;
    }
}

    private async Task AppendAutomaticToolResultsAsync(
        List<OllamaChatMessage> conversation,
        IReadOnlyList<OllamaToolCall> toolCalls,
        CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(AppendAutomaticToolResultsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(AppendAutomaticToolResultsAsync)} failed.");
        throw;
    }
}

    private string? ResolveRegistryFunctionName(string toolName) {
    try
    {
        return GetAutomaticFunctions()
        .FirstOrDefault(function => ToOllamaToolName(function.Name).Equals(toolName, StringComparison.OrdinalIgnoreCase))
        ?.Name;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ResolveRegistryFunctionName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ResolveRegistryFunctionName)} failed.");
        throw;
    }
}

    private string ToOllamaToolName(string registryName)
    {
    try
    {
            var builder = new StringBuilder(registryName.Length);
            foreach (var character in registryName)
                builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaToolName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaToolName)} failed.");
        throw;
    }
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

    private JsonElement NormalizeArguments(JsonElement arguments) {
    try
    {
        return arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
        ? JsonSerializer.SerializeToElement(new { })
        : arguments.Clone();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(NormalizeArguments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(NormalizeArguments)} failed.");
        throw;
    }
}

    private string SerializeToolResult(DxAiFunctionInvocationResult result)
    {
    try
    {
            var json = JsonSerializer.Serialize(result, jsonOptions);
            return json.Length <= MaxToolResultCharacters
                ? json
                : json[..MaxToolResultCharacters] + "\n{\"truncated\":true}";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SerializeToolResult)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SerializeToolResult)} failed.");
        throw;
    }
}

    private async Task<List<AIContent>> CreateContentsAsync(
        OllamaChatResponse response,
        CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateContentsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateContentsAsync)} failed.");
        throw;
    }
}

    private async Task<string> GetPromptAsync(string key, CancellationToken cancellationToken)
    {
    try
    {
            if (promptConfigService is null)
                return string.Empty;

            return await promptConfigService.GetPromptAsync(key, cancellationToken: cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetPromptAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetPromptAsync)} failed.");
        throw;
    }
}

    private OllamaChatMessage CloneMessage(OllamaChatMessage message) {
    try
    {
        return new()
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CloneMessage)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CloneMessage)} failed.");
        throw;
    }
}

    private OllamaChatMessage ToOllamaMessage(ChatMessage message) {
    try
    {
        return new()
    {
        Role = message.Role == ChatRole.System
            ? "system"
            : message.Role == ChatRole.Assistant
                ? "assistant"
                : "user",
        Content = message.Text ?? string.Empty
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaMessage)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaMessage)} failed.");
        throw;
    }
}

    private void AddSystemPrompt(List<OllamaChatMessage> messages, string prompt)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(AddSystemPrompt)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(AddSystemPrompt)} failed.");
        throw;
    }
}

    private ChatResponseUpdate CreateStreamingUpdate(string text)
    {
    try
    {
            var update = councilRuntime.OllamaThinkingChatClientCreateStreamingUpdate(text, logger);
            return update ?? new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(text)]);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateStreamingUpdate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateStreamingUpdate)} failed.");
        throw;
    }
}
}
