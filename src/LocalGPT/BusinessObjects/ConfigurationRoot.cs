

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a configuration root application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class ConfigurationRoot
    {
        /// <summary>
        /// Defines the configuration constant used by <see cref="ConfigurationRoot"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string Configuration = "Configuration";
        // Top-level ASP.NET Core bits
        //public string? AllowedHosts { get; set; }                  // e.g. "*"
        //public MsLoggingOptions? Logging { get; set; }                  // standard "Logging" section

        /// <summary>
        /// Gets or sets the logging core value that forms part of the configuration root state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The logging core value exposed by <see cref="ConfigurationRoot"/>.</value>
        public LoggingCoreOptions? LoggingCore { get; set; }
        /// <summary>
        /// Gets or sets the python core value that forms part of the configuration root state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The python core value exposed by <see cref="ConfigurationRoot"/>.</value>
        public PythonCoreOptions? PythonCore { get; set; }
        /// <summary>
        /// Gets or sets the connection strings core value that forms part of the configuration root state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The connection strings core value exposed by <see cref="ConfigurationRoot"/>.</value>
        public ConnectionStringsCoreOptions? ConnectionStringsCore { get; set; }
        /// <summary>
        /// Gets or sets the AI core value that forms part of the configuration root state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The AI core value exposed by <see cref="ConfigurationRoot"/>.</value>
        public AICoreOptions? AICore { get; set; }
        /// <summary>
        /// Gets or sets the LocalGPT value that forms part of the configuration root state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The LocalGPT value exposed by <see cref="ConfigurationRoot"/>.</value>
        public LocalGptHostOptions? LocalGPT { get; set; }
    }
    /// <summary>
    /// Carries the configurable ms logging settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class MsLoggingOptions
    {
        /// <summary>
        /// Gets or sets the log level collection maintained or exposed by this ms logging instance for downstream processing.
        /// </summary>
        /// <value>The log level value exposed by <see cref="MsLoggingOptions"/>.</value>
        public Dictionary<string, string>? LogLevel { get; set; }
    }
}
