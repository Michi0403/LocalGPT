using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public class CompositeChatClient : IChatClient
{
    private const int DefaultMaxOutputTokens = 2048;
    private const int DefaultMaxPromptCharacters = 12000;
    private const int MaxBootstrapCharacters = 6000;
    private const int MaxSingleConversationMessageCharacters = 5000;
    private const string RuntimeDecisionPolicy =
        "LocalGPT runtime decision policy: When the user asks to generate, scaffold, implement, modify, or package code/artifacts and important architecture choices are unresolved, do not start coding yet. " +
        "First return a short section titled \"Decision poll required\" with concrete choices and tradeoffs, then stop and wait for the user's answer. " +
        "Ask only for decisions that materially affect the result, such as target platform/runtime, language/framework, UI stack, solution shape, data/persistence model, deployment target, security boundary, reference-app fidelity, and whether downloadable artifacts are expected. " +
        "Do not assume Blazor, DevExpress, ASP.NET Core, or a split frontend/backend unless the user selected it, the existing repository requires it, or the requested target clearly calls for it. " +
        "If the user already supplied the needed decisions, proceed normally and restate the selected path briefly.";
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

    public CompositeChatClient(ILogger logger, params ChatClientSession[] chatClients)
        : this(logger, null, null, null, chatClients)
    {
    }

    public CompositeChatClient(ILogger logger, IAiFeatureReportService? featureReportService, params ChatClientSession[] chatClients)
        : this(logger, featureReportService, null, null, chatClients)
    {
    }

    public CompositeChatClient(
        ILogger logger,
        IAiFeatureReportService? featureReportService,
        IAiContextBootstrapService? bootstrapService,
        ICouncilKnowledgeService? knowledgeService,
        params ChatClientSession[] chatClients)
    {

        AvailableChatClients = chatClients.ToList();
        SelectedSession = AvailableChatClients[0];
        _logger = logger;
        _featureReportService = featureReportService;
        _bootstrapService = bootstrapService;
        _knowledgeService = knowledgeService;
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
        options.MaxOutputTokens ??= ForcedMaxOutputTokens ?? DefaultMaxOutputTokens;
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

        foreach (var entry in ParseKnowledgeRequests(source, responseText))
        {
            var saved = await _knowledgeService.SaveEntryAsync(entry, cancellationToken);
            _logger.LogInformation("AI requested unapproved knowledge entry {KnowledgeEntryId} from {Source}.", saved.Id, source);
        }
    }

    private static IEnumerable<CouncilKnowledgeEntry> ParseKnowledgeRequests(string source, string responseText)
    {
        foreach (Match match in Regex.Matches(responseText, "<localgpt-knowledge>(?<body>.*?)</localgpt-knowledge>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant))
        {
            var body = match.Groups["body"].Value.Trim();
            if (string.IsNullOrWhiteSpace(body))
                continue;

            var content = ExtractField(body, "content");
            if (string.IsNullOrWhiteSpace(content))
                content = body;

            yield return new CouncilKnowledgeEntry
            {
                Topic = ExtractField(body, "topic", "AI model knowledge request"),
                Scope = ExtractField(body, "scope", "DXAiChat"),
                Source = $"AI model request: {source}",
                Content = content,
                HelpfulSources = ExtractField(body, "helpful-sources", "None explicitly requested."),
                Tags = MergeTags(ExtractField(body, "tags"), "model-written; unapproved"),
                Confidence = ParseConfidence(ExtractField(body, "confidence")),
                IsUserApproved = false,
                IsPinned = false,
                IsArchived = false
            };
        }
    }

    private static string ExtractField(string body, string name, string fallback = "")
    {
        var pattern = $@"(?ims)^\s*{Regex.Escape(name)}\s*:\s*(?<value>.*?)(?=^\s*(?:topic|scope|confidence|tags|helpful-sources|content)\s*:|\z)";
        var match = Regex.Match(body, pattern, RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value.Trim() : fallback;
    }

    private static int ParseConfidence(string value) =>
        int.TryParse(Regex.Match(value ?? string.Empty, "\\d+").Value, out var confidence)
            ? Math.Clamp(confidence, 0, 100)
            : 40;

    private static string MergeTags(string requestedTags, string requiredTags) =>
        string.IsNullOrWhiteSpace(requestedTags)
            ? requiredTags
            : $"{requestedTags.Trim()}; {requiredTags}";

    private async Task<IReadOnlyList<ChatMessage>> AddBootstrapContextAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var messageList = messages.ToList();
        var policyMessage = new ChatMessage(ChatRole.System, RuntimeDecisionPolicy);
        if (SuppressBootstrapContext || _bootstrapService is null)
            return LimitPromptSize([policyMessage, .. messageList], ForcedMaxPromptCharacters);

        var bootstrapPrompt = await _bootstrapService.BuildBootstrapPromptAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(bootstrapPrompt))
            return LimitPromptSize([policyMessage, .. messageList], ForcedMaxPromptCharacters);

        return LimitPromptSize([policyMessage, new ChatMessage(ChatRole.System, bootstrapPrompt), .. messageList], ForcedMaxPromptCharacters);
    }

    private static IReadOnlyList<ChatMessage> LimitPromptSize(IReadOnlyList<ChatMessage> messages, int? forcedMaxPromptCharacters = null)
    {
        var maxPromptCharacters = Math.Clamp(forcedMaxPromptCharacters ?? DefaultMaxPromptCharacters, 512, DefaultMaxPromptCharacters);
        if (messages.Sum(EstimateTextLength) <= maxPromptCharacters)
            return messages;

        var result = new List<ChatMessage>();
        var usedCharacters = 0;
        var remainingSystemBudget = Math.Min(MaxBootstrapCharacters, Math.Max(maxPromptCharacters / 2, 0));

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
            var messageBudget = Math.Min(MaxSingleConversationMessageCharacters, remainingBudget);
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
