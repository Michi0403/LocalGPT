namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a connection strings core options.
    /// </summary>
    public class ConnectionStringsCoreOptions()
    {
        /// <summary>
        /// Stores connection strings core.
        /// </summary>
        public const string ConnectionStringsCore = "ConnectionStringsCore";

        /// <summary>
        /// Gets or sets connection string.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets easy test connection string.
        /// </summary>
        public string? EasyTestConnectionString { get; set; }

        /// <summary>
        /// Gets or sets default connection.
        /// </summary>
        public string? DefaultConnection { get; set; }
    }
}