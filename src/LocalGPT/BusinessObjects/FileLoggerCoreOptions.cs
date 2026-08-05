using LocalGPT.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    public class FileLoggerCoreOptions
    {
        public const string FileLoggerCore = "FileLoggerCore";
        public FileLoggerCoreOptions CloneOptions(FileLoggerCoreOptions options)
        {
            return new FileLoggerCoreOptions
            {
                CoreLogLevel = options.CoreLogLevel,
                FilePath = options.FilePath
            };
        }
        [JsonInclude]
        public string? FilePath { get; set; }
        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; }
    }
}
