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
    private const int EmergencyDefaultMaxOutputTokens = 262144;
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
            return new();
        }
    }

    public IAsyncEnumerable<ChatResponseUpdate>? GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
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
            _logger.LogError(ex, $"Error in GetStreamingResponseAsync {ex.ToString()}");
            return null;
        }
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
        try
        {

            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetService {ex.ToString()}");
            return null;
        }
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
                    .GetAsync<int>("DefaultMaxOutputTokens", cancellationToken)
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

        options.MaxOutputTokens = EmergencyDefaultMaxOutputTokens;
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
            return new();
        }
        
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAndReportAsync(
        ChatClientSession session,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
            IReadOnlyList<ChatMessage> enrichedMessages;
            ChatOptions resolvedOptions;
            try
            {
                enrichedMessages = await AddBootstrapContextAsync(messages, cancellationToken).ConfigureAwait(false);
                resolvedOptions = await ApplyDefaultOptionsAsync(options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var responseText = new StringBuilder();
            var updates = session.Client.GetStreamingResponseAsync(enrichedMessages, resolvedOptions, cancellationToken).GetAsyncEnumerator(cancellationToken);
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

                    responseText.Append(update.Text);
                    yield return update;
                }
            }
            finally
            {
                await updates.DisposeAsync().ConfigureAwait(false);
            }

            if (cancellationToken.IsCancellationRequested)
                yield break;

            var text = responseText.ToString();
            try
            {
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
