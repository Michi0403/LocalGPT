namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the ai context bootstrap service contract.
    /// </summary>
    public interface IAiContextBootstrapService
    {
        /// <summary>
        /// Builds bootstrap prompt async.
        /// </summary>
        Task<string> BuildBootstrapPromptAsync(CancellationToken cancellationToken = default);
    }
}
