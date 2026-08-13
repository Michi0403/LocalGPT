namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for custom version behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface ICustomVersion
    {
        /// <summary>
        /// Gets or sets the version value that forms part of the custom version state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The version value exposed by <see cref="ICustomVersion"/>.</value>
        string Version { get; set; }
    }
}
