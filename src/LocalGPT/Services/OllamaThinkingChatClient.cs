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

/// <summary>
/// Represents an Ollama thinking chat application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaThinkingChatClient : IChatClient
{
    /// <summary>
    /// Defines the max automatic tool rounds constant used by <see cref="OllamaThinkingChatClient"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxAutomaticToolRounds = 3;
    /// <summary>
    /// Defines the max tool result characters constant used by <see cref="OllamaThinkingChatClient"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxToolResultCharacters = 16_000;

    /// <summary>
    /// Stores the internal JSON options state used by <see cref="OllamaThinkingChatClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient http;
    /// <summary>
    /// Stores the internal model state used by <see cref="OllamaThinkingChatClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string model;
    /// <summary>
    /// Stores the internal keep alive state used by <see cref="OllamaThinkingChatClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string keepAlive;
    /// <summary>
    /// Stores the internal context length state used by <see cref="OllamaThinkingChatClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int? contextLength;
    /// <summary>
    /// Stores the internal num GPU state used by <see cref="OllamaThinkingChatClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int? numGpu;
    /// <summary>
    /// Stores the logger used by <see cref="OllamaThinkingChatClient"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger logger;
    /// <summary>
    /// Stores the internal provider options state used by <see cref="OllamaThinkingChatClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly OllamaCoreOptions providerOptions;
    /// <summary>
    /// Stores the chat response formatter factory dependency used by <see cref="OllamaThinkingChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IChatResponseFormatterFactory formatterFactory;
    /// <summary>
    /// Stores the chat protocol resolver dependency used by <see cref="OllamaThinkingChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IChatProtocolResolver protocolResolver;
    /// <summary>
    /// Stores the prompt config service dependency used by <see cref="OllamaThinkingChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPromptConfigService? promptConfigService;
    /// <summary>
    /// Stores the DevExpress AI function registry dependency used by <see cref="OllamaThinkingChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDxAiFunctionRegistry? functionRegistry;
    /// <summary>
    /// Stores the DevExpress AI function call recovery service dependency used by <see cref="OllamaThinkingChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDxAiFunctionCallRecoveryService? functionCallRecovery;
    /// <summary>
    /// Stores the internal automatic tools enabled state used by <see cref="OllamaThinkingChatClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly bool automaticToolsEnabled;
    /// <summary>
    /// Stores the internal throw on failure state used by <see cref="OllamaThinkingChatClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly bool throwOnFailure;
    /// <summary>
    /// Stores the council runtime service dependency used by <see cref="OllamaThinkingChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly CouncilRuntimeService councilRuntime;

    /// <summary>
    /// Initializes a new <see cref="OllamaThinkingChatClient"/> instance and captures the dependencies or initial state required by its Ollama thinking chat workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="councilRuntime">Council runtime service dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="keepAlive">Keep alive value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="contextLength">Context length value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="timeout">Timeout value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="numGpu">Num gpu value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="formatterFactory">Chat response formatter factory dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="protocolResolver">Chat protocol resolver dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="promptConfigService">Prompt config service dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="functionRegistry">Devexpress ai function registry dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="functionCallRecovery">Devexpress ai function call recovery service dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="enableAutomaticTools">Value indicating whether enable automatic tools should apply to this operation.</param>
    /// <param name="throwOnFailure">Value indicating whether throw on failure should apply to this operation.</param>
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

    /// <summary>
    /// Retrieves response for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Chat message dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The chat response produced by the operation.</returns>
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

    /// <summary>
    /// Retrieves streaming response for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Chat message dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i async enumerable chat response update produced by the operation.</returns>
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

    /// <summary>
    /// Creates failure response for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The chat response produced by the operation.</returns>
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

    /// <summary>
    /// Retrieves service for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="serviceType">Service type value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="serviceKey">Service key value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
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

    /// <summary>
    /// Releases resources owned by <see cref="OllamaThinkingChatClient"/> and leaves the Ollama thinking chat workflow in a safely disposed state.
    /// </summary>
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

    /// <summary>
    /// Performs send for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Ollama chat message dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="stream">Value indicating whether stream should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama chat response produced by the operation.</returns>
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

    /// <summary>
    /// Performs send request with tool fallback for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="completionOption">Completion option value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP response message produced by the operation.</returns>
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

    /// <summary>
    /// Ensures textual DevExpress function fallback prompt for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
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

    /// <summary>
    /// Performs send request once for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="completionOption">Completion option value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP response message produced by the operation.</returns>
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

    /// <summary>
    /// Creates conversation for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Chat message dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Creates request for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Ollama chat message dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="stream">Value indicating whether stream should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama chat request produced by the operation.</returns>
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

    /// <summary>
    /// Builds automatic tools for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Retrieves automatic functions for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs to Ollama tool calls for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="calls">Recovered devexpress ai function call dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs to transport tool name for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="registryName">Registry name value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs deduplicate tool calls for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="toolCalls">Ollama tool call dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs append automatic tool results for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="conversation">Conversation value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="toolCalls">Ollama tool call dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Resolves registry function name for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="toolName">Tool name value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs to Ollama tool name for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="registryName">Registry name value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Builds parameters schema for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="function">Function value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The JSON element produced by the operation.</returns>
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

    /// <summary>
    /// Normalizes arguments for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="arguments">Arguments value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The JSON element produced by the operation.</returns>
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

    /// <summary>
    /// Performs serialize tool result for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="result">Result value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Creates contents for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="response">Response value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Retrieves prompt for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs clone message for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The Ollama chat message produced by the operation.</returns>
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

    /// <summary>
    /// Performs to Ollama message for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The Ollama chat message produced by the operation.</returns>
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

    /// <summary>
    /// Adds system prompt for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Messages value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="prompt">Prompt value supplied to the Ollama thinking chat operation and used when producing its result.</param>
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

    /// <summary>
    /// Creates streaming update for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The chat response update produced by the operation.</returns>
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
