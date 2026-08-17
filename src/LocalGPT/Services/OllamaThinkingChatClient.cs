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
public sealed partial class OllamaThinkingChatClient : IChatClient
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

    /// <summary>
    /// Stores the HTTP client dependency used by <see cref="OllamaThinkingChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
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
    /// <summary>Stores an optional exact registered-function allow-list for provider-native automatic tools.</summary>
    private readonly HashSet<string>? automaticFunctionAllowList;
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
    /// <param name="automaticFunctionAllowList">Optional exact registered-function allow-list. Null or empty preserves the historical policy-approved catalog.</param>
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
        bool throwOnFailure = false,
        IReadOnlyCollection<string>? automaticFunctionAllowList = null)
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
        this.automaticFunctionAllowList = automaticFunctionAllowList is { Count: > 0 }
            ? automaticFunctionAllowList.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : null;
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
            var automaticToolTrace = new StringBuilder();
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
                foreach (var call in toolCalls)
                    automaticToolTrace.Append(BuildOllamaFunctionCallTrace(call));
                var nonStreamingToolResults = await AppendAutomaticToolResultsAsync(conversation, toolCalls, cancellationToken).ConfigureAwait(false);
                foreach (var trace in nonStreamingToolResults)
                    automaticToolTrace.Append(trace);
            }

            if (response is null)
            {
                if (throwOnFailure)
                    throw new InvalidOperationException($"Ollama model '{model}' returned no response.");
                return CreateFailureResponse("Ollama returned no response. Verify that the selected model is available and the local runtime is healthy.");
            }

            var responseContents = await CreateContentsAsync(response, cancellationToken).ConfigureAwait(false);
            if (automaticToolTrace.Length > 0)
                responseContents.Insert(0, new TextContent(automaticToolTrace.ToString()));
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, responseContents));
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

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
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
                    yield return CreateStreamingUpdate(BuildOllamaFunctionCallTrace(call));
                }

                var streamedToolResults = await AppendAutomaticToolResultsAsync(conversation, toolCalls, cancellationToken).ConfigureAwait(false);
                foreach (var trace in streamedToolResults)
                    yield return CreateStreamingUpdate(trace);
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
}
