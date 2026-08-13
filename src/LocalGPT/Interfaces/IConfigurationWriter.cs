namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for configuration behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IConfigurationWriter
    {
        /// <summary>
        /// Performs save for <see cref="IConfigurationWriter"/>, keeping the operation consistent with the state and invariants of the surrounding configuration workflow.
        /// </summary>
        /// <param name="root">Root value supplied to the configuration operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task SaveAsync(BusinessObjects.ConfigurationRoot root, CancellationToken ct = default);
    }
}
