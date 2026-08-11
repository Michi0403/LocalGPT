
using Microsoft.AspNetCore.SignalR;

namespace LocalGPT.Hubs
{
    /// <summary>
    /// Represents a chat hub.
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        /// <summary>
        /// Runs the chat hub operation.
        /// </summary>
        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Runs the on connected async operation.
        /// </summary>
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

        /// <summary>
        /// Runs the on disconnected async operation.
        /// </summary>
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

        /// <summary>
        /// Runs the notify new chatbot answer operation.
        /// </summary>
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
