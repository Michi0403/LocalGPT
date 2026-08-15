using DevExpress.DataAccess.DataFederation;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
namespace LocalGPT.Services;

/// <summary>
/// Represents a composite chat application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public class CompositeChatClient : IChatClient
{
      /// <summary>
      /// Gets the available chat clients collection maintained or exposed by this composite chat instance for downstream processing.
      /// </summary>
      /// <value>The available chat clients value exposed by <see cref="CompositeChatClient"/>.</value>
      public List<ChatClientSession> AvailableChatClients { get; }
    /// <summary>
    /// Gets or sets the selected session value that forms part of the composite chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected session value exposed by <see cref="CompositeChatClient"/>.</value>
    public ChatClientSession? SelectedSession { get; set; }
    /// <summary>
    /// Gets or sets the locked session name value that forms part of the composite chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The locked session name value exposed by <see cref="CompositeChatClient"/>.</value>
    public string? LockedSessionName { get; set; }
    /// <summary>
    /// Gets or sets the forced max output tokens value that forms part of the composite chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The forced max output tokens value exposed by <see cref="CompositeChatClient"/>.</value>
    public int? ForcedMaxOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the forced max prompt characters value that forms part of the composite chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The forced max prompt characters value exposed by <see cref="CompositeChatClient"/>.</value>
    public int? ForcedMaxPromptCharacters { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether suppress bootstrap context applies to the composite chat state.
    /// </summary>
    /// <value>The suppress bootstrap context value exposed by <see cref="CompositeChatClient"/>.</value>
    public bool SuppressBootstrapContext { get; set; }
    /// <summary>
    /// Stores the logger used by <see cref="CompositeChatClient"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger _logger;
    /// <summary>
    /// Stores the AI feature report service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IAiFeatureReportService? _featureReportService;
    /// <summary>
    /// Stores the AI context bootstrap service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IAiContextBootstrapService? _bootstrapService;
    /// <summary>
    /// Stores the council knowledge service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ICouncilKnowledgeService? _knowledgeService;
    /// <summary>
    /// Stores the chat upload workspace service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IChatUploadWorkspaceService? _chatUploadWorkspaces;
    /// <summary>
    /// Stores the prompt config service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPromptConfigService? _promptConfigService;
    /// <summary>
    /// Stores the variable store service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IVariableStoreService? _variableStoreService;
    /// <summary>
    /// Stores the system variable definition service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ISystemVariableDefinitionService _systemVariables;
    /// <summary>
    /// Stores the council runtime service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly CouncilRuntimeService _councilRuntime;
    /// <summary>
    /// Stores the council text service dependency used by <see cref="CompositeChatClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly CouncilTextService _councilText;

    /// <summary>
    /// Initializes a new <see cref="CompositeChatClient"/> instance and captures the dependencies or initial state required by its composite chat workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="featureReportService">Ai feature report service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="bootstrapService">Ai context bootstrap service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="knowledgeService">Council knowledge service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="chatUploadWorkspaces">Chat upload workspace service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="promptConfigService">Prompt config service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="variableStoreService">Variable store service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="systemVariables">System variable definition service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="councilRuntime">Council runtime service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="councilText">Council text service dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="chatClients">Chat clients value supplied to the composite chat operation and used when producing its result.</param>
    public CompositeChatClient(
        ILogger logger,
        IAiFeatureReportService? featureReportService,
        IAiContextBootstrapService? bootstrapService,
        ICouncilKnowledgeService? knowledgeService,
        IChatUploadWorkspaceService? chatUploadWorkspaces,
        IPromptConfigService? promptConfigService,
        IVariableStoreService? variableStoreService,
        ISystemVariableDefinitionService systemVariables,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText,
        params ChatClientSession[] chatClients)
    {

        AvailableChatClients = chatClients.ToList();
        SelectedSession = AvailableChatClients[0];
        _logger = logger;
        _featureReportService = featureReportService;
        _bootstrapService = bootstrapService;
        _knowledgeService = knowledgeService;
        _chatUploadWorkspaces = chatUploadWorkspaces;
        _promptConfigService = promptConfigService;
        _variableStoreService = variableStoreService;
        _systemVariables = systemVariables ?? throw new ArgumentNullException(nameof(systemVariables));
        _councilRuntime = councilRuntime ?? throw new ArgumentNullException(nameof(councilRuntime));
        _councilText = councilText ?? throw new ArgumentNullException(nameof(councilText));
    }

    /// <summary>
    /// Retrieves response for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="messages">Chat message dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The chat response produced by the operation.</returns>
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {

            var selectedSession = ResolveSelectedSession();
            if (selectedSession is null)
                throw new InvalidOperationException("No chat client session is selected.");

            var enrichedMessages = await AddBootstrapContextAsync(messages, cancellationToken).ConfigureAwait(false);
            var resolvedOptions = await ApplyDefaultOptionsAsync(options, cancellationToken).ConfigureAwait(false);
            return await GetResponseAndReportAsync(selectedSession, enrichedMessages, resolvedOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat response failed for the selected session.");
            return CreateFailureResponse("The selected AI session could not complete the response. Review LocalGPT application logs and verify the configured provider.");
        }
    }

    /// <summary>
    /// Retrieves streaming response for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="messages">Chat message dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i async enumerable chat response update produced by the operation.</returns>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {

            var selectedSession = ResolveSelectedSession();
            if (selectedSession is null)
                throw new InvalidOperationException("No chat client session is selected.");

            return GetStreamingResponseAndReportAsync(selectedSession, messages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming chat could not start for the selected session.");
            return CreateFailureUpdates("The selected AI session could not start streaming. Review LocalGPT application logs and verify the configured provider.");
        }
    }

    /// <summary>
    /// Creates failure response for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the composite chat operation and used when producing its result.</param>
    /// <returns>The chat response produced by the operation.</returns>
    private ChatResponse CreateFailureResponse(string message) {
    try
    {
        return new(new ChatMessage(ChatRole.Assistant, [new TextContent(message)]));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(CompositeChatClient)}.{nameof(CreateFailureResponse)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(CompositeChatClient)}.{nameof(CreateFailureResponse)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates failure updates for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the composite chat operation and used when producing its result.</param>
    /// <returns>The i async enumerable chat response update produced by the operation.</returns>
    private async IAsyncEnumerable<ChatResponseUpdate> CreateFailureUpdates(string message)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(message)]);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether connection refused for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="exception">Exception value supplied to the composite chat operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsConnectionRefused(Exception exception)
    {
    try
    {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is System.Net.Sockets.SocketException socketException
                    && socketException.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
                    return true;
                if (current.Message.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
                    || current.Message.Contains("Verbindung verweigert", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(CompositeChatClient)}.{nameof(IsConnectionRefused)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(CompositeChatClient)}.{nameof(IsConnectionRefused)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="CompositeChatClient"/> and leaves the composite chat workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            for (int i = 0; i < AvailableChatClients.Count; i++)
            {
                try
                {
                    AvailableChatClients[i].Client.Dispose();
                    AvailableChatClients[i].Messages.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fully dispose composite chat resources.");
               
                }

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in Dispose {ex.ToString()}");
        }

    }
    /// <summary>
    /// Retrieves service for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="serviceType">Service type value supplied to the composite chat operation and used when producing its result.</param>
    /// <param name="serviceKey">Service key value supplied to the composite chat operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(serviceType);
            if (serviceType.IsInstanceOfType(this))
                return this;
            return ResolveSelectedSession()?.Client.GetService(serviceType, serviceKey);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(CompositeChatClient)}.{nameof(GetService)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(CompositeChatClient)}.{nameof(GetService)} failed.");
        throw;
    }
}


    /// <summary>
    /// Applies default options for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The chat options produced by the operation.</returns>
    private async Task<ChatOptions> ApplyDefaultOptionsAsync(
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= new ChatOptions();
        if (options.MaxOutputTokens.HasValue)
            return options;

        if (ForcedMaxOutputTokens.HasValue)
        {
            options.MaxOutputTokens = ForcedMaxOutputTokens.Value;
            return options;
        }

        if (_variableStoreService is not null)
        {
            try
            {
                options.MaxOutputTokens = await _variableStoreService
                    .GetAsync<int>(_systemVariables.DefaultMaxOutputTokens.Name, cancellationToken)
                    .ConfigureAwait(false);
                return options;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DefaultMaxOutputTokens could not be read from the system-variable store. The emergency default will be used.");
            }
        }

        options.MaxOutputTokens = _systemVariables.DefaultMaxOutputTokens.DefaultValue;
        return options;
    }

    /// <summary>
    /// Resolves selected session for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <returns>The chat client session produced by the operation.</returns>
    private ChatClientSession? ResolveSelectedSession()
    {
    try
    {
            if (!string.IsNullOrWhiteSpace(LockedSessionName))
            {
                var lockedSession = AvailableChatClients.FirstOrDefault(session =>
                    session.Name.Equals(LockedSessionName, StringComparison.OrdinalIgnoreCase) ||
                    session.Name.Contains(LockedSessionName, StringComparison.OrdinalIgnoreCase));
                if (lockedSession is not null)
                {
                    SelectedSession = lockedSession;
                    return lockedSession;
                }

                _logger.LogWarning("Locked chat session {LockedSessionName} was not found. Falling back to selected session {SelectedSessionName}.",
                    LockedSessionName,
                    SelectedSession?.Name);
            }

            return SelectedSession;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(CompositeChatClient)}.{nameof(ResolveSelectedSession)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(CompositeChatClient)}.{nameof(ResolveSelectedSession)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves response and report for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="session">Session value supplied to the composite chat operation and used when producing its result.</param>
    /// <param name="messages">Chat message dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The chat response produced by the operation.</returns>
    private async Task<ChatResponse> GetResponseAndReportAsync(
        ChatClientSession session,
        IEnumerable<ChatMessage> messages,
        ChatOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await session.Client.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            if (session.Client is not CouncilChatClient)
                await WriteMissingFeatureReportIfNeededAsync(session.Name, response.Text, cancellationToken).ConfigureAwait(false);
            await WriteKnowledgeRequestsIfNeededAsync(session.Name, response.Text, cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Chat response and reporting failed for locked session {LockedSessionName} and selected session {SelectedSessionName}.",
                LockedSessionName,
                SelectedSession?.Name);
            return CreateFailureResponse("The AI response could not be completed or post-processed. Review LocalGPT application logs and try again.");
        }
        
    }

    /// <summary>
    /// Retrieves streaming response and report for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="session">Session value supplied to the composite chat operation and used when producing its result.</param>
    /// <param name="messages">Chat message dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i async enumerable chat response update produced by the operation.</returns>
    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAndReportAsync(
        ChatClientSession session,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatMessage>? enrichedMessages = null;
        ChatOptions? resolvedOptions = null;
        string? startupFailure = null;
        try
        {
            enrichedMessages = await AddBootstrapContextAsync(messages, cancellationToken).ConfigureAwait(false);
            resolvedOptions = await ApplyDefaultOptionsAsync(options, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            yield break;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not prepare streaming chat for session {SessionName}.", session.Name);
            startupFailure = "The selected AI session could not prepare the streaming request. Review LocalGPT application logs and verify the configured provider.";
        }

        if (startupFailure is not null)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(startupFailure)]);
            yield break;
        }

        ArgumentNullException.ThrowIfNull(enrichedMessages);
        ArgumentNullException.ThrowIfNull(resolvedOptions);
        var responseText = new StringBuilder();
        string? streamFailure = null;
        var updates = session.Client
            .GetStreamingResponseAsync(enrichedMessages, resolvedOptions, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                ChatResponseUpdate update;
                try
                {
                    if (!await updates.MoveNextAsync().ConfigureAwait(false))
                        break;
                    update = updates.Current;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Streaming chat failed for session {SessionName}.", session.Name);
                    streamFailure = "The selected AI session stopped streaming unexpectedly. Review LocalGPT application logs and verify the configured provider.";
                    break;
                }

                responseText.Append(update.Text);
                yield return update;
                foreach (var trace in _councilRuntime.BuildUserVisibleProviderTrace(update, _logger))
                {
                    responseText.Append(trace);
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(trace)]);
                }
            }
        }
        finally
        {
            await updates.DisposeAsync().ConfigureAwait(false);
        }

        if (streamFailure is not null && responseText.Length == 0 && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Retrying streaming chat once for session {SessionName} because the first attempt produced no output.", session.Name);
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("The provider did not answer. LocalGPT is retrying once…")]);
            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken).ConfigureAwait(false);
            streamFailure = null;
            var retryUpdates = session.Client
                .GetStreamingResponseAsync(enrichedMessages, resolvedOptions, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            try
            {
                while (true)
                {
                    ChatResponseUpdate update;
                    try
                    {
                        if (!await retryUpdates.MoveNextAsync().ConfigureAwait(false))
                            break;
                        update = retryUpdates.Current;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }
                    catch (Exception retryException)
                    {
                        _logger.LogError(retryException, "Streaming retry failed for session {SessionName}.", session.Name);
                        streamFailure = IsConnectionRefused(retryException)
                            ? "The selected local AI provider is not running or refused the connection. Start Ollama/LM Studio, then refresh models and retry."
                            : "The selected AI session failed twice. Review LocalGPT application logs and the provider health settings.";
                        break;
                    }

                    responseText.Append(update.Text);
                    yield return update;
                    foreach (var trace in _councilRuntime.BuildUserVisibleProviderTrace(update, _logger))
                    {
                        responseText.Append(trace);
                        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(trace)]);
                    }
                }
            }
            finally
            {
                await retryUpdates.DisposeAsync().ConfigureAwait(false);
            }
        }

        if (streamFailure is not null)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(streamFailure)]);
            yield break;
        }
        if (cancellationToken.IsCancellationRequested)
            yield break;

        var text = responseText.ToString();
        try
        {
            if (session.Client is not CouncilChatClient)
                await WriteMissingFeatureReportIfNeededAsync(session.Name, text, cancellationToken).ConfigureAwait(false);
            await WriteKnowledgeRequestsIfNeededAsync(session.Name, text, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Writes missing feature report if needed for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="source">Source value supplied to the composite chat operation and used when producing its result.</param>
    /// <param name="responseText">Response text value supplied to the composite chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task WriteMissingFeatureReportIfNeededAsync(string source, string responseText, CancellationToken cancellationToken)
    {
        try
        {
            if (_featureReportService is null)
                return;

            var path = await _featureReportService.WriteIfMissingFeatureReportAsync(source, responseText, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(path))
                _logger.LogInformation("AI missing feature report written: {Path}", path);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not process a missing-feature report for source {Source}.", source);
        }

    }

    /// <summary>
    /// Writes knowledge requests if needed for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="source">Source value supplied to the composite chat operation and used when producing its result.</param>
    /// <param name="responseText">Response text value supplied to the composite chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task WriteKnowledgeRequestsIfNeededAsync(string source, string responseText, CancellationToken cancellationToken)
    {
        try
        {
            if (_knowledgeService is null || string.IsNullOrWhiteSpace(responseText))
                return;

            foreach (var entry in _councilText.ParseKnowledgeRequests(source, responseText, _logger) ?? new List<CouncilKnowledgeEntry>())
            {
                var saved = await _knowledgeService.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("AI requested unapproved knowledge entry {KnowledgeEntryId} from {Source}.", saved.Id, source);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not process knowledge requests for source {Source}.", source);
        }
    }




    /// <summary>
    /// Adds bootstrap context for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="messages">Chat message dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<ChatMessage>> AddBootstrapContextAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        try
        {
            var messageList = messages.ToList();
            var uploadWorkspacePrompt = await SaveUploadedMessageContentAsync(messageList, cancellationToken).ConfigureAwait(false);
            var runtimeDecisionPolicy = _promptConfigService is null
                ? string.Empty
                : await _promptConfigService
                    .GetPromptAsync("RuntimeDecisionPolicy", cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

            var codeGenerationPolicy = _promptConfigService is null
                ? string.Empty
                : await _promptConfigService.GetPromptAsync("CodeGenerationChangeReviewPolicy", cancellationToken: cancellationToken).ConfigureAwait(false);
            var learningRoundPolicy = _promptConfigService is null
                ? string.Empty
                : await _promptConfigService.GetPromptAsync("LearningRoundPolicy", cancellationToken: cancellationToken).ConfigureAwait(false);
            var codeGenerationRoutingPolicy = _promptConfigService is null
                ? string.Empty
                : await _promptConfigService.GetPromptAsync("CodeGenerationFunctionRoutingPolicy", cancellationToken: cancellationToken).ConfigureAwait(false);

            var systemMessages = new List<ChatMessage>();
            _councilText.AddOptionalSystemMessage(systemMessages, runtimeDecisionPolicy, _logger);
            _councilText.AddOptionalSystemMessage(systemMessages, codeGenerationPolicy, _logger);
            _councilText.AddOptionalSystemMessage(systemMessages, learningRoundPolicy, _logger);
            _councilText.AddOptionalSystemMessage(systemMessages, codeGenerationRoutingPolicy, _logger);
            if (SuppressBootstrapContext || _bootstrapService is null)
            {
                _councilText.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt, _logger);
                return _councilRuntime.LimitPromptSize([.. systemMessages, .. messageList], _logger, ForcedMaxPromptCharacters );
            }

            var bootstrapPrompt = await _bootstrapService.BuildBootstrapPromptAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(bootstrapPrompt))
            {
                _councilText.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt, _logger);
                return _councilRuntime.LimitPromptSize([.. systemMessages, .. messageList] ,_logger, ForcedMaxPromptCharacters);
            }

            systemMessages.Add(new ChatMessage(ChatRole.System, bootstrapPrompt));
            _councilText.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt, _logger);
            return _councilRuntime.LimitPromptSize([.. systemMessages, .. messageList], _logger, ForcedMaxPromptCharacters);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not add bootstrap context to the chat request.");
            return messages.ToList();
        }
       
    }


    /// <summary>
    /// Persists uploaded message content for <see cref="CompositeChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding composite chat workflow.
    /// </summary>
    /// <param name="messages">Chat message dependency used by the composite chat workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> SaveUploadedMessageContentAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_chatUploadWorkspaces is null)
                return string.Empty;

            var latestUserMessage = messages.LastOrDefault(message => message.Role == ChatRole.User);
            if (latestUserMessage is null)
                return string.Empty;

            var files = _councilText.ExtractUploadFiles(latestUserMessage, _logger);
            ArgumentNullException.ThrowIfNull(files);
            var fileList = files.ToList();
            if (fileList.Count == 0)
                return string.Empty;

            try
            {
                var result = await _chatUploadWorkspaces.CreateWorkspaceAsync(
                    latestUserMessage.Text ?? string.Empty,
                    files,
                    cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Created DXAiChat native attachment workspace {WorkspaceName} with {FileCount} files.",
                    result.WorkspaceName,
                    result.FileCount);

                return _councilText.BuildUploadWorkspaceSystemPrompt(result, _logger);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create DXAiChat native attachment workspace.");
                return "LocalGPT upload workspace creation failed. Tell the user the uploaded files could not be saved, then continue only with the visible prompt.";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not persist uploaded chat-message content.");
            return string.Empty;
        }
    }
    
}
