using LocalGPT.Services;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for chat client behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IChatClientFactory
    {
        /// <summary>
        /// Performs build using the configuration and dependencies owned by <see cref="IChatClientFactory"/>.
        /// </summary>
        /// <returns>The composite chat client produced by the operation.</returns>
        CompositeChatClient Build();
    }
}
