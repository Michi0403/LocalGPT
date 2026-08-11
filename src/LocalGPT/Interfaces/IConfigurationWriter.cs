namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the configuration writer contract.
    /// </summary>
    public interface IConfigurationWriter
    {
        /// <summary>
        /// Saves async.
        /// </summary>
        Task SaveAsync(BusinessObjects.ConfigurationRoot root, CancellationToken ct = default);
    }
}
