using LocalGPT.BusinessObjects.Enums;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Carries the configurable logging core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class LoggingCoreOptions
    {
        /// <summary>
        /// Defines the logging core constant used by <see cref="LoggingCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string LoggingCore = "LoggingCore";
        /// <summary>
        /// Gets or sets the email core value that forms part of the logging core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The email core value exposed by <see cref="LoggingCoreOptions"/>.</value>
        public EmailLoggerCoreOptions? EmailCore { get; set; }

        /// <summary>
        /// Gets or sets the file core value that forms part of the logging core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The file core value exposed by <see cref="LoggingCoreOptions"/>.</value>
        public FileLoggerCoreOptions? FileCore { get; set; }

        /// <summary>
        /// Gets or sets the database core value that forms part of the logging core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The database core value exposed by <see cref="LoggingCoreOptions"/>.</value>
        public DatabaseLoggerCoreOptions? DatabaseCore { get; set; }

        /// <summary>
        /// Gets or sets the core log level value that forms part of the logging core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The core log level value exposed by <see cref="LoggingCoreOptions"/>.</value>
        public CoreLogLevel CoreLogLevel { get; set; }
    }
}
