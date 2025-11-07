using LocalGPT.BusinessObjects.Enums;

namespace LocalGPT.BusinessObjects
{
    public class LoggingCoreOptions
    {
        public const string LoggingCore = "LoggingCore";
        public EmailLoggerCoreOptions? EmailCore { get; set; }

        public FileLoggerCoreOptions? FileCore { get; set; }

        public CoreLogLevel CoreLogLevel { get; set; }
    }
}
