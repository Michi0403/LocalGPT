namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for AI context bootstrap behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IAiContextBootstrapService
    {
        /// <summary>
        /// Builds bootstrap prompt as part of the AI context bootstrap service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        Task<string> BuildBootstrapPromptAsync(CancellationToken cancellationToken = default);
    }
}
