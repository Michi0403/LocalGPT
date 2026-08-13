using LocalGPT.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Carries the configurable file logger core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class FileLoggerCoreOptions
    {
        /// <summary>
        /// Defines the file logger core constant used by <see cref="FileLoggerCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string FileLoggerCore = "FileLoggerCore";
        /// <summary>
        /// Performs clone options for <see cref="FileLoggerCoreOptions"/>, keeping the operation consistent with the state and invariants of the surrounding file logger core workflow.
        /// </summary>
        /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
        /// <returns>The file logger core options produced by the operation.</returns>
        public FileLoggerCoreOptions CloneOptions(FileLoggerCoreOptions options)
        {
            return new FileLoggerCoreOptions
            {
                CoreLogLevel = options.CoreLogLevel,
                FilePath = options.FilePath
            };
        }
        /// <summary>
        /// Gets or sets the file path used by this file logger core instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The file path value exposed by <see cref="FileLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public string? FilePath { get; set; }
        /// <summary>
        /// Gets or sets the core log level value that forms part of the file logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The core log level value exposed by <see cref="FileLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; }
    }
}
