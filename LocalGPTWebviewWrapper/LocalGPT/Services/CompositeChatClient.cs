using Microsoft.Extensions.AI;

namespace LocalGPT.Services;

public class CompositeChatClient : IChatClient
{
    private const int DefaultMaxOutputTokens = 2048;
    public List<ChatClientSession> AvailableChatClients { get; }
    public ChatClientSession? SelectedSession { get; set; }
    private readonly ILogger _logger;
    public CompositeChatClient(ILogger logger,params ChatClientSession[] chatClients)
    {

        AvailableChatClients = chatClients.ToList();
        SelectedSession = AvailableChatClients[0];
        _logger = logger;
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {

            return SelectedSession?.Client.GetResponseAsync(messages, ApplyDefaultOptions(options), cancellationToken)
                ?? throw new InvalidOperationException("No chat client session is selected.");
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

            return SelectedSession?.Client.GetStreamingResponseAsync(messages, ApplyDefaultOptions(options), cancellationToken)
                ?? throw new InvalidOperationException("No chat client session is selected.");
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
}
