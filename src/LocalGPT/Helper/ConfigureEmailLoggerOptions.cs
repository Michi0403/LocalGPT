using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Options;
namespace LocalGPT.Helper
{
    /// <summary>
    /// Carries the configurable configure email logger settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    /// <param name="loggingOptions">Email logger core options dependency used by the configure email logger workflow to provide the corresponding application capability.</param>
    public class ConfigureEmailLoggerOptions(IOptionsMonitor<EmailLoggerCoreOptions> loggingOptions) : IConfigureOptions<EmailLoggerCoreOptions>
    {

        /// <summary>
        /// Performs configure for <see cref="ConfigureEmailLoggerOptions"/>, keeping the operation consistent with the state and invariants of the surrounding configure email logger workflow.
        /// </summary>
        /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
        public void Configure(EmailLoggerCoreOptions options)
        {
            loggingOptions.CurrentValue.SmtpServer = options.SmtpServer;
            loggingOptions.CurrentValue.SmtpPort = options.SmtpPort;
            loggingOptions.CurrentValue.SenderEmail = options.SenderEmail;
            loggingOptions.CurrentValue.EmailRecipients = options.EmailRecipients;
            loggingOptions.CurrentValue.CcRecipients = options.CcRecipients;
            loggingOptions.CurrentValue.BccRecipients = options.BccRecipients;
            loggingOptions.CurrentValue.Username = options.Username;
            loggingOptions.CurrentValue.CoreLogLevel = options.CoreLogLevel;
            loggingOptions.CurrentValue.Password = options.Password;
            loggingOptions.CurrentValue.EnableSsl = options.EnableSsl;
        }
    }
}
