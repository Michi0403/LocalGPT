using LocalGPT.Services;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the chat client factory contract.
    /// </summary>
    public interface IChatClientFactory
    {
        /// <summary>
        /// Runs the build operation.
        /// </summary>
        CompositeChatClient Build();
    }
}
