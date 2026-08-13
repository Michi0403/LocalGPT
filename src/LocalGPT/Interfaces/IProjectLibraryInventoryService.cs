namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for project library inventory behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IProjectLibraryInventoryService
    {
        /// <summary>
        /// Builds DevExpress briefing as part of the project library inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        Task<string> BuildDevExpressBriefingAsync(CancellationToken cancellationToken = default);
    }
}
