namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the project library inventory service contract.
    /// </summary>
    public interface IProjectLibraryInventoryService
    {
        /// <summary>
        /// Builds dev express briefing async.
        /// </summary>
        Task<string> BuildDevExpressBriefingAsync(CancellationToken cancellationToken = default);
    }
}
