using System.Runtime.CompilerServices;
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

    public CompositeChatClient(ILogger logger, params ChatClientSession[] chatClients)
        : this(logger, null, null, chatClients)
    {
    }

    public CompositeChatClient(ILogger logger, IAiFeatureReportService? featureReportService, params ChatClientSession[] chatClients)
        : this(logger, featureReportService, null, chatClients)
    {
    }

    public CompositeChatClient(
        ILogger logger,
        IAiFeatureReportService? featureReportService,
        IAiContextBootstrapService? bootstrapService,
        params ChatClientSession[] chatClients)
    {

        AvailableChatClients = chatClients.ToList();
        SelectedSession = AvailableChatClients[0];
        _logger = logger;
        _featureReportService = featureReportService;
        _bootstrapService = bootstrapService;
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
        return response;
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAndReportAsync(
        ChatClientSession session,
        IEnumerable<ChatMessage> messages,
        ChatOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enrichedMessages = await AddBootstrapContextAsync(messages, cancellationToken);
        await foreach (var update in session.Client.GetStreamingResponseAsync(enrichedMessages, options, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return update;
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
