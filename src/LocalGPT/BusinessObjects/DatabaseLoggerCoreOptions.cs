using LocalGPT.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Carries the configurable database logger core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class DatabaseLoggerCoreOptions
    {
        /// <summary>
        /// Defines the database logger core constant used by <see cref="DatabaseLoggerCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string DatabaseLoggerCore = "DatabaseLoggerCore";

        /// <summary>
        /// Gets or sets the core log level value that forms part of the database logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The core log level value exposed by <see cref="DatabaseLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; } = CoreLogLevel.Warning;

        /// <summary>
        /// Gets or sets the max queue length that quantifies the associated database logger core data.
        /// </summary>
        /// <value>The max queue length value exposed by <see cref="DatabaseLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public int MaxQueueLength { get; set; } = 2000;

        /// <summary>
        /// Gets or sets the batch size that quantifies the associated database logger core data.
        /// </summary>
        /// <value>The batch size value exposed by <see cref="DatabaseLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public int BatchSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets the flush interval seconds value that forms part of the database logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The flush interval seconds value exposed by <see cref="DatabaseLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public int FlushIntervalSeconds { get; set; } = 3;

        /// <summary>
        /// Gets or sets the retention days value that forms part of the database logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The retention days value exposed by <see cref="DatabaseLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public int RetentionDays { get; set; } = 21;
    }
}
