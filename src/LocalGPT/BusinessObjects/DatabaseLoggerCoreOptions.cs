using LocalGPT.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a database logger core options.
    /// </summary>
    public class DatabaseLoggerCoreOptions
    {
        /// <summary>
        /// Stores database logger core.
        /// </summary>
        public const string DatabaseLoggerCore = "DatabaseLoggerCore";

        /// <summary>
        /// Gets or sets core log level.
        /// </summary>
        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; } = CoreLogLevel.Warning;

        /// <summary>
        /// Gets or sets max queue length.
        /// </summary>
        [JsonInclude]
        public int MaxQueueLength { get; set; } = 2000;

        /// <summary>
        /// Gets or sets batch size.
        /// </summary>
        [JsonInclude]
        public int BatchSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets flush interval seconds.
        /// </summary>
        [JsonInclude]
        public int FlushIntervalSeconds { get; set; } = 3;

        /// <summary>
        /// Gets or sets retention days.
        /// </summary>
        [JsonInclude]
        public int RetentionDays { get; set; } = 21;
    }
}
