using LocalGPT.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a file logger core options.
    /// </summary>
    public class FileLoggerCoreOptions
    {
        /// <summary>
        /// Stores file logger core.
        /// </summary>
        public const string FileLoggerCore = "FileLoggerCore";
        /// <summary>
        /// Runs the clone options operation.
        /// </summary>
        public FileLoggerCoreOptions CloneOptions(FileLoggerCoreOptions options)
        {
            return new FileLoggerCoreOptions
            {
                CoreLogLevel = options.CoreLogLevel,
                FilePath = options.FilePath
            };
        }
        /// <summary>
        /// Gets or sets file path.
        /// </summary>
        [JsonInclude]
        public string? FilePath { get; set; }
        /// <summary>
        /// Gets or sets core log level.
        /// </summary>
        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; }
    }
}
