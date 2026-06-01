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
    public List<ChatClientSession> AvailableChatClients { get; }
    public ChatClientSession? SelectedSession { get; set; }
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

            if (SelectedSession is null)
                throw new InvalidOperationException("No chat client session is selected.");

            var enrichedMessages = await AddBootstrapContextAsync(messages, cancellationToken);
            return await GetResponseAndReportAsync(SelectedSession, enrichedMessages, ApplyDefaultOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetResponseAsync {ex.ToString()}");
            throw;
        }
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken()) {
        try
        {

            if (SelectedSession is null)
                throw new InvalidOperationException("No chat client session is selected.");

            return GetStreamingResponseAndReportAsync(SelectedSession, messages, ApplyDefaultOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetStreamingResponseAsync {ex.ToString()}");
            throw;
        }
    }

    public void Dispose() {
        for (int i = 0; i < AvailableChatClients.Count; i++)
        {
            AvailableChatClients[i].Client.Dispose();
            AvailableChatClients[i].Messages.Clear();
        }
    }
    public object? GetService(Type serviceType, object? serviceKey = null) {
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


    private static ChatOptions ApplyDefaultOptions(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.MaxOutputTokens ??= DefaultMaxOutputTokens;
        return options;
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
        if (_bootstrapService is null)
            return messageList;

        var bootstrapPrompt = await _bootstrapService.BuildBootstrapPromptAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(bootstrapPrompt))
            return messageList;

        return [new ChatMessage(ChatRole.System, bootstrapPrompt), .. messageList];
    }
}
