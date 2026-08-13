using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Options;

namespace LocalGPT.Helper
{
    /// <summary>
    /// Carries the configurable configure file logger settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    /// <param name="loggingOptions">File logger core options dependency used by the configure file logger workflow to provide the corresponding application capability.</param>
    public class ConfigureFileLoggerOptions(IOptionsMonitor<FileLoggerCoreOptions> loggingOptions) : IConfigureOptions<FileLoggerCoreOptions>
    {

        /// <summary>
        /// Performs configure for <see cref="ConfigureFileLoggerOptions"/>, keeping the operation consistent with the state and invariants of the surrounding configure file logger workflow.
        /// </summary>
        /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
        public void Configure(FileLoggerCoreOptions options)
        {
            loggingOptions.CurrentValue.FilePath = options.FilePath;

            loggingOptions.CurrentValue.CoreLogLevel = options.CoreLogLevel;


        }
    }
}

