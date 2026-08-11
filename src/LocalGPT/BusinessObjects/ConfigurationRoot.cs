

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a configuration root.
    /// </summary>
    public class ConfigurationRoot
    {
        /// <summary>
        /// Stores configuration.
        /// </summary>
        public const string Configuration = "Configuration";
        // Top-level ASP.NET Core bits
        //public string? AllowedHosts { get; set; }                  // e.g. "*"
        //public MsLoggingOptions? Logging { get; set; }                  // standard "Logging" section

        /// <summary>
        /// Gets or sets logging core.
        /// </summary>
        public LoggingCoreOptions? LoggingCore { get; set; }
        /// <summary>
        /// Gets or sets python core.
        /// </summary>
        public PythonCoreOptions? PythonCore { get; set; }
        /// <summary>
        /// Gets or sets connection strings core.
        /// </summary>
        public ConnectionStringsCoreOptions? ConnectionStringsCore { get; set; }
        /// <summary>
        /// Gets or sets aicore.
        /// </summary>
        public AICoreOptions? AICore { get; set; }
        /// <summary>
        /// Gets or sets local gpt.
        /// </summary>
        public LocalGptHostOptions? LocalGPT { get; set; }
    }
    /// <summary>
    /// Represents a ms logging options.
    /// </summary>
    public class MsLoggingOptions
    {
        /// <summary>
        /// Gets or sets log level.
        /// </summary>
        public Dictionary<string, string>? LogLevel { get; set; }
    }
}
