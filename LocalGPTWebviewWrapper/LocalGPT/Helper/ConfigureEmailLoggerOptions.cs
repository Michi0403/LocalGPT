using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Options;
namespace LocalGPT.Helper
{
    public class ConfigureEmailLoggerOptions(IOptionsMonitor<EmailLoggerCoreOptions> loggingOptions) : IConfigureOptions<EmailLoggerCoreOptions>
    {

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
