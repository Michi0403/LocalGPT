using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Options;

namespace LocalGPT.Helper
{
    /// <summary>
    /// Represents a configure file logger options.
    /// </summary>
    public class ConfigureFileLoggerOptions(IOptionsMonitor<FileLoggerCoreOptions> loggingOptions) : IConfigureOptions<FileLoggerCoreOptions>
    {

        /// <summary>
        /// Runs the configure operation.
        /// </summary>
        public void Configure(FileLoggerCoreOptions options)
        {
            loggingOptions.CurrentValue.FilePath = options.FilePath;

            loggingOptions.CurrentValue.CoreLogLevel = options.CoreLogLevel;


        }
    }
}

