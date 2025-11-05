
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
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnConnectedAsync {ex.ToString()}");
            }

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnDisconnectedAsync {ex.ToString()}");
            }
        }

        public async Task NotifyNewChatbotAnswer(string message)
        {
            try
            {
                _logger.LogInformation("Broadcasting new message...");
                await Clients.All.SendAsync("NotifyNewChatbotAnswer", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in NotifyNewChatbotAnswer {ex.ToString()}");
            }
        }
    }
}
