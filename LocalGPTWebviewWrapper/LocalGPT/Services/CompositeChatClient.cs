using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;
using LocalGPT.Interfaces;
using LocalGPT.Extensions.PlainStatics;
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

            var enrichedMessages = await AddBootstrapContextAsync(messages, cancellationToken);
            return await GetResponseAndReportAsync(selectedSession, enrichedMessages, ApplyDefaultOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetResponseAsync {ex.ToString()}");
            throw;
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

            return GetStreamingResponseAndReportAsync(selectedSession, messages, ApplyDefaultOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetStreamingResponseAsync {ex.ToString()}");
            throw;
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < AvailableChatClients.Count; i++)
        {
            AvailableChatClients[i].Client.Dispose();
            AvailableChatClients[i].Messages.Clear();
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


    private ChatOptions ApplyDefaultOptions(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.MaxOutputTokens ??= ForcedMaxOutputTokens ?? GlobalVariableSlopCollectionToRemove.DefaultMaxOutputTokens;
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
        var response = await session.Client.GetResponseAsync(messages, options, cancellationToken);
        await WriteMissingFeatureReportIfNeededAsync(session.Name, response.Text, cancellationToken);
        await WriteKnowledgeRequestsIfNeededAsync(session.Name, response.Text, cancellationToken);
        return response;
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
            enrichedMessages = await AddBootstrapContextAsync(messages, cancellationToken);
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
                    if (!await updates.MoveNextAsync())
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
            await updates.DisposeAsync();
        }

        if (cancellationToken.IsCancellationRequested)
            yield break;

        var text = responseText.ToString();
        try
        {
            await WriteMissingFeatureReportIfNeededAsync(session.Name, text, cancellationToken);
            await WriteKnowledgeRequestsIfNeededAsync(session.Name, text, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task WriteMissingFeatureReportIfNeededAsync(string source, string responseText, CancellationToken cancellationToken)
    {
        if (_featureReportService is null)
            return;

        var path = await _featureReportService.WriteIfMissingFeatureReportAsync(source, responseText, cancellationToken);
        if (!string.IsNullOrWhiteSpace(path))
            _logger.LogInformation("AI missing feature report written: {Path}", path);
    }

    private async Task WriteKnowledgeRequestsIfNeededAsync(string source, string responseText, CancellationToken cancellationToken)
    {
        if (_knowledgeService is null || string.IsNullOrWhiteSpace(responseText))
            return;

        foreach (var entry in CouncilChatStringFunctions.ParseKnowledgeRequests(source, responseText))
        {
            var saved = await _knowledgeService.SaveEntryAsync(entry, cancellationToken);
            _logger.LogInformation("AI requested unapproved knowledge entry {KnowledgeEntryId} from {Source}.", saved.Id, source);
        }
    }




    private async Task<IReadOnlyList<ChatMessage>> AddBootstrapContextAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var messageList = messages.ToList();
        var uploadWorkspacePrompt = await SaveUploadedMessageContentAsync(messageList, cancellationToken);
        var policyMessage = new ChatMessage(ChatRole.System, GlobalVariableSlopCollectionToRemove.RuntimeDecisionPolicy);

        var systemMessages = new List<ChatMessage> { policyMessage };
        if (SuppressBootstrapContext || _bootstrapService is null)
        {
            CouncilChatStringFunctions.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt);
            return LimitPromptSize([.. systemMessages, .. messageList], ForcedMaxPromptCharacters);
        }

        var bootstrapPrompt = await _bootstrapService.BuildBootstrapPromptAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(bootstrapPrompt))
        {
            CouncilChatStringFunctions.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt);
            return LimitPromptSize([.. systemMessages, .. messageList], ForcedMaxPromptCharacters);
        }

        systemMessages.Add(new ChatMessage(ChatRole.System, bootstrapPrompt));
        CouncilChatStringFunctions.AddOptionalSystemMessage(systemMessages, uploadWorkspacePrompt);
        return LimitPromptSize([.. systemMessages, .. messageList], ForcedMaxPromptCharacters);
    }


    private async Task<string> SaveUploadedMessageContentAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        if (_chatUploadWorkspaces is null)
            return string.Empty;

        var latestUserMessage = messages.LastOrDefault(message => message.Role == ChatRole.User);
        if (latestUserMessage is null)
            return string.Empty;

        var files = CouncilChatStringFunctions.ExtractUploadFiles(latestUserMessage).ToList();
        if (files.Count == 0)
            return string.Empty;

        try
        {
            var result = await _chatUploadWorkspaces.CreateWorkspaceAsync(
                latestUserMessage.Text ?? string.Empty,
                files,
                cancellationToken);
            _logger.LogInformation(
                "Created DXAiChat native attachment workspace {WorkspaceName} with {FileCount} files.",
                result.WorkspaceName,
                result.FileCount);

            return CouncilChatStringFunctions.BuildUploadWorkspaceSystemPrompt(result);
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






    private static IReadOnlyList<ChatMessage> LimitPromptSize(IReadOnlyList<ChatMessage> messages, int? forcedMaxPromptCharacters = null)
    {
        var maxPromptCharacters = Math.Clamp(forcedMaxPromptCharacters ?? GlobalVariableSlopCollectionToRemove.DefaultMaxPromptCharacters, 512, GlobalVariableSlopCollectionToRemove.MaxPromptCharacters);
        if (messages.Sum(EstimateTextLength) <= maxPromptCharacters)
            return messages;

        var result = new List<ChatMessage>();
        var usedCharacters = 0;
        var remainingSystemBudget = Math.Min(GlobalVariableSlopCollectionToRemove.MaxBootstrapCharacters, Math.Max(maxPromptCharacters / 2, 0));

        foreach (var message in messages.Where(message => message.Role == ChatRole.System))
        {
            var text = message.Text ?? string.Empty;
            var budget = Math.Min(remainingSystemBudget, maxPromptCharacters - usedCharacters);
            if (budget <= 0)
                break;

            var trimmed = TrimForPrompt(text, budget, keepBothEnds: false);
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            result.Add(new ChatMessage(message.Role, trimmed));
            usedCharacters += trimmed.Length;
            remainingSystemBudget -= trimmed.Length;
        }

        var conversationMessages = messages
            .Where(message => message.Role != ChatRole.System)
            .ToList();
        var keptConversationMessages = new Stack<ChatMessage>();

        for (var index = conversationMessages.Count - 1; index >= 0; index--)
        {
            var remainingBudget = maxPromptCharacters - usedCharacters;
            if (remainingBudget <= 0)
                break;

            var message = conversationMessages[index];
            var text = message.Text ?? string.Empty;
            var messageBudget = Math.Min(GlobalVariableSlopCollectionToRemove.MaxSingleConversationMessageCharacters, remainingBudget);
            var trimmed = TrimForPrompt(text, messageBudget, keepBothEnds: true);
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            keptConversationMessages.Push(new ChatMessage(message.Role, trimmed));
            usedCharacters += trimmed.Length;
        }

        result.AddRange(keptConversationMessages);
        return result;
    }

    private static int EstimateTextLength(ChatMessage message) => message.Text?.Length ?? 0;

    private static string TrimForPrompt(string text, int maxCharacters, bool keepBothEnds)
    {
        var normalized = text.Replace("\r\n", "\n").Trim();
        if (maxCharacters <= 0 || string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (normalized.Length <= maxCharacters)
            return normalized;

        const string omission = "\n\n[...older context trimmed by LocalGPT to fit the local model context window...]\n\n";
        if (maxCharacters <= omission.Length + 40)
            return normalized[..Math.Min(normalized.Length, maxCharacters)].Trim();

        if (!keepBothEnds)
            return $"{normalized[..(maxCharacters - omission.Length)].TrimEnd()}{omission.TrimEnd()}";

        var remaining = maxCharacters - omission.Length;
        var head = Math.Max(remaining / 2, 1);
        var tail = Math.Max(remaining - head, 1);
        return $"{normalized[..head].TrimEnd()}{omission}{normalized[^tail..].TrimStart()}";
    }
}
