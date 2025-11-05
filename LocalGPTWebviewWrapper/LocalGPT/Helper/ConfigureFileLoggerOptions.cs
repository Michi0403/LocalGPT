using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Options;

namespace LocalGPT.Helper
{
    public class ConfigureFileLoggerOptions(IOptionsMonitor<FileLoggerCoreOptions> loggingOptions) : IConfigureOptions<FileLoggerCoreOptions>
    {

        public void Configure(FileLoggerCoreOptions options)
        {
            loggingOptions.CurrentValue.FilePath = options.FilePath;

            loggingOptions.CurrentValue.CoreLogLevel = options.CoreLogLevel;


        }
    }
}

