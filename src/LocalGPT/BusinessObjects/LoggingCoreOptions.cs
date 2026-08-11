using LocalGPT.BusinessObjects.Enums;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a logging core options.
    /// </summary>
    public class LoggingCoreOptions
    {
        /// <summary>
        /// Stores logging core.
        /// </summary>
        public const string LoggingCore = "LoggingCore";
        /// <summary>
        /// Gets or sets email core.
        /// </summary>
        public EmailLoggerCoreOptions? EmailCore { get; set; }

        /// <summary>
        /// Gets or sets file core.
        /// </summary>
        public FileLoggerCoreOptions? FileCore { get; set; }

        /// <summary>
        /// Gets or sets database core.
        /// </summary>
        public DatabaseLoggerCoreOptions? DatabaseCore { get; set; }

        /// <summary>
        /// Gets or sets core log level.
        /// </summary>
        public CoreLogLevel CoreLogLevel { get; set; }
    }
}
