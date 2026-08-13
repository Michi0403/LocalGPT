namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Carries the configurable connection strings core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class ConnectionStringsCoreOptions()
    {
        /// <summary>
        /// Defines the connection strings core constant used by <see cref="ConnectionStringsCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string ConnectionStringsCore = "ConnectionStringsCore";

        /// <summary>
        /// Gets or sets the connection string value that forms part of the connection strings core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The connection string value exposed by <see cref="ConnectionStringsCoreOptions"/>.</value>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the easy test connection string value that forms part of the connection strings core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The easy test connection string value exposed by <see cref="ConnectionStringsCoreOptions"/>.</value>
        public string? EasyTestConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the default connection value that forms part of the connection strings core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default connection value exposed by <see cref="ConnectionStringsCoreOptions"/>.</value>
        public string? DefaultConnection { get; set; }
    }
}
