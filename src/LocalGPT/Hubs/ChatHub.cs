
using Microsoft.AspNetCore.SignalR;

namespace LocalGPT.Hubs
{
    /// <summary>
    /// Represents a chat hub application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class ChatHub : Hub
    {
        /// <summary>
        /// Stores the logger used by <see cref="ChatHub"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<ChatHub> _logger;

        /// <summary>
        /// Initializes a new <see cref="ChatHub"/> instance and captures the dependencies or initial state required by its chat hub workflow.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handles the connected async lifecycle or event notification for <see cref="ChatHub"/>, updating the state required by the surrounding workflow.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
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
        /// Handles the disconnected async lifecycle or event notification for <see cref="ChatHub"/>, updating the state required by the surrounding workflow.
        /// </summary>
        /// <param name="exception">Exception value supplied to the chat hub operation and used when producing its result.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
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
        /// Performs notify new chatbot answer for <see cref="ChatHub"/>, keeping the operation consistent with the state and invariants of the surrounding chat hub workflow.
        /// </summary>
        /// <param name="message">Message value supplied to the chat hub operation and used when producing its result.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
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
