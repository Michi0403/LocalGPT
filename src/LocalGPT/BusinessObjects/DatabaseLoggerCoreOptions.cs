using LocalGPT.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    public class DatabaseLoggerCoreOptions
    {
        public const string DatabaseLoggerCore = "DatabaseLoggerCore";

        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; } = CoreLogLevel.Warning;

        [JsonInclude]
        public int MaxQueueLength { get; set; } = 2000;

        [JsonInclude]
        public int BatchSize { get; set; } = 50;

        [JsonInclude]
        public int FlushIntervalSeconds { get; set; } = 3;

        [JsonInclude]
        public int RetentionDays { get; set; } = 21;
    }
}
