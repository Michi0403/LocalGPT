
using Microsoft.AspNetCore.SignalR;

namespace LocalGPT.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                _logger.LogInformation($"Client connected: {Context.ConnectionId}");
                await base.OnConnectedAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR connection setup failed.");
            }

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
                await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR disconnect handling failed.");
            }
        }

        public async Task NotifyNewChatbotAnswer(string message)
        {
            try
            {
                _logger.LogInformation("Broadcasting new message...");
                await Clients.All.SendAsync("NotifyNewChatbotAnswer", message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR answer notification failed.");
            }
        }
    }
}
