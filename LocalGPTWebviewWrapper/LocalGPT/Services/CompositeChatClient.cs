using DevExpress.DataAccess.DataFederation;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
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

    public CompositeChatClient(ILogger logger, params ChatClientSession[] chatClients)
        : this(logger, null, null, null, null, chatClients)
    {
    }

    public CompositeChatClient(ILogger logger, IAiFeatureReportService? featureReportService, params ChatClientSession[] chatClients)
        : this(logger, featureReportService, null, null, null, chatClients)
    {
    }

    public CompositeChatClient(
        ILogger logger,
        IAiFeatureReportService? featureReportService,
        IAiContextBootstrapService? bootstrapService,
        ICouncilKnowledgeService? knowledgeService,
        IChatUploadWorkspaceService? chatUploadWorkspaces,
        params ChatClientSession[] chatClients)
    {

        AvailableChatClients = chatClients.ToList();
        SelectedSession = AvailableChatClients[0];
        _logger = logger;
        _featureReportService = featureReportService;
        _bootstrapService = bootstrapService;
        _knowledgeService = knowledgeService;
        _chatUploadWorkspaces = chatUploadWorkspaces;
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
            return await GetResponseAndReportAsync(selectedSession, enrichedMessages, ApplyDefaultOptions(options), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetResponseAsync {ex.ToString()}");
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

            return GetStreamingResponseAndReportAsync(selectedSession, messages, ApplyDefaultOptions(options), cancellationToken);
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
                    _logger.LogWarning(ex, $"Error in Dispose clients and messages {ex.ToString()}");
               
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


    private ChatOptions? ApplyDefaultOptions(ChatOptions? options)
    {
        try
        {
            options ??= new ChatOptions();
            options.MaxOutputTokens ??= ForcedMaxOutputTokens ?? GlobalVariableSlopCollectionToRemove.DefaultMaxOutputTokens;
            return options;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in ApplyDefaultOptions options {options?.ToString()}");
            return null;
        }

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
        catch (Exception ex)
        {
            _logger.LogError(ex,$"GetResponseAndReportAsync {LockedSessionName} {SelectedSession}.",
         LockedSessionName,
         SelectedSession);
            return new();
        }
        
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAndReportAsync(
        ChatClientSession session,
        IEnumerable<ChatMessage> messages,
        ChatOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
            IReadOnlyList<ChatMessage> enrichedMessages;
            try
            {
                enrichedMessages = await AddBootstrapContextAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var responseText = new StringBuilder();
            var updates = session.Client.GetStreamingResponseAsync(enrichedMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, $"WriteMissingFeatureReportIfNeededAsync source {source} responseText {responseText}.",
         LockedSessionName,
         SelectedSession);
           
        }

    }

    private async Task WriteKnowledgeRequestsIfNeededAsync(string source, string responseText, CancellationToken cancellationToken)
    {
        try
        {
            if (_knowledgeService is null || string.IsNullOrWhiteSpace(responseText))
                return;

            foreach (var entry in CouncilChatStringFunctions.ParseKnowledgeRequests(source, responseText, _logger) ?? new List<CouncilKnowledgeEntry>())
            {
                var saved = await _knowledgeService.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("AI requested unapproved knowledge entry {KnowledgeEntryId} from {Source}.", saved.Id, source);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"WriteKnowledgeRequestsIfNeededAsync source {source} responseText {responseText}.",
         LockedSessionName,
         SelectedSession);

        }
    }




    private async Task<IReadOnlyList<ChatMessage>> AddBootstrapContextAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        try
        {
            var messageList = messages.ToList();
            var uploadWorkspacePrompt = await SaveUploadedMessageContentAsync(messageList, cancellationToken).ConfigureAwait(false);
            var policyMessage = new ChatMessage(ChatRole.System, GlobalVariableSlopCollectionToRemove.RuntimeDecisionPolicy);

            var systemMessages = new List<ChatMessage> { policyMessage };
            if (SuppressBootstrapContext || _bootstrapService is null)
            {
                CouncilChatStringFunctions.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt, _logger);
                return CouncilChatStaticsGeneral.LimitPromptSize([.. systemMessages, .. messageList], _logger, ForcedMaxPromptCharacters );
            }

            var bootstrapPrompt = await _bootstrapService.BuildBootstrapPromptAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(bootstrapPrompt))
            {
                CouncilChatStringFunctions.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt, _logger);
                return CouncilChatStaticsGeneral.LimitPromptSize([.. systemMessages, .. messageList] ,_logger, ForcedMaxPromptCharacters);
            }

            systemMessages.Add(new ChatMessage(ChatRole.System, bootstrapPrompt));
            CouncilChatStringFunctions.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt, _logger);
            return CouncilChatStaticsGeneral.LimitPromptSize([.. systemMessages, .. messageList], _logger, ForcedMaxPromptCharacters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"AddBootstrapContextAsync messages {messages.ToString()}",
         LockedSessionName,
         SelectedSession);
            return new List<ChatMessage>();
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

            var files = CouncilChatStringFunctions.ExtractUploadFiles(latestUserMessage, _logger);
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

                return CouncilChatStringFunctions.BuildUploadWorkspaceSystemPrompt(result, _logger);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, $"SaveUploadedMessageContentAsync messages {messages.ToString()}",
         LockedSessionName,
         SelectedSession);
            return string.Empty;
        }
    }
    
}
