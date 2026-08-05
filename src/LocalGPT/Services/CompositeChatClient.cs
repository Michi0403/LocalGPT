using DevExpress.DataAccess.DataFederation;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
namespace LocalGPT.Services;

public class CompositeChatClient : IChatClient
{
      public List<ChatClientSession> AvailableChatClients { get; }
    public ChatClientSession? SelectedSession { get; set; }
    public string? LockedSessionName { get; set; }
    public int? ForcedMaxOutputTokens { get; set; }
    public int? ForcedMaxPromptCharacters { get; set; }
    public bool SuppressBootstrapContext { get; set; }
    private readonly ILogger _logger;
    private readonly IAiFeatureReportService? _featureReportService;
    private readonly IAiContextBootstrapService? _bootstrapService;
    private readonly ICouncilKnowledgeService? _knowledgeService;
    private readonly IChatUploadWorkspaceService? _chatUploadWorkspaces;
    private readonly IPromptConfigService? _promptConfigService;
    private readonly IVariableStoreService? _variableStoreService;
    private readonly ISystemVariableDefinitionService _systemVariables;
    private readonly CouncilRuntimeService _councilRuntime;
    private readonly CouncilTextService _councilText;

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

    private ChatResponse CreateFailureResponse(string message) =>
        new(new ChatMessage(ChatRole.Assistant, [new TextContent(message)]));

    private async IAsyncEnumerable<ChatResponseUpdate> CreateFailureUpdates(string message)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(message)]);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private bool IsConnectionRefused(Exception exception)
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
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (serviceType.IsInstanceOfType(this))
            return this;
        return ResolveSelectedSession()?.Client.GetService(serviceType, serviceKey);
    }


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

    private ChatClientSession? ResolveSelectedSession()
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

            var systemMessages = new List<ChatMessage>();
            _councilText.AddOptionalSystemMessage(systemMessages, runtimeDecisionPolicy, _logger);
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
