using LocalGPT.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Carries the configurable email logger core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class EmailLoggerCoreOptions
    {
        /// <summary>
        /// Defines the email logger core constant used by <see cref="EmailLoggerCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string EmailLoggerCore = "EmailLoggerCore";
        /// <summary>
        /// Gets or sets the bcc recipients collection maintained or exposed by this email logger core instance for downstream processing.
        /// </summary>
        /// <value>The bcc recipients value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public IEnumerable<string> BccRecipients { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the cc recipients collection maintained or exposed by this email logger core instance for downstream processing.
        /// </summary>
        /// <value>The cc recipients value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public IEnumerable<string> CcRecipients { get; set; } = new List<string>();
        /// <summary>
        /// Gets or sets the email recipients collection maintained or exposed by this email logger core instance for downstream processing.
        /// </summary>
        /// <value>The email recipients value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public IEnumerable<string> EmailRecipients { get; set; } = new List<string>();
        /// <summary>
        /// Gets or sets a value indicating whether SSL applies to the email logger core state.
        /// </summary>
        /// <value>The enable SSL value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public bool EnableSsl { get; set; }
        /// <summary>
        /// Gets or sets the core log level value that forms part of the email logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The core log level value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; }
        /// <summary>
        /// Gets or sets the password value that forms part of the email logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The password value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public string? Password { get; set; }
        /// <summary>
        /// Gets or sets the sender email value that forms part of the email logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The sender email value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public string? SenderEmail { get; set; }
        /// <summary>
        /// Gets or sets the smtp port value that forms part of the email logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The smtp port value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public int SmtpPort { get; set; }
        /// <summary>
        /// Gets or sets the smtp server value that forms part of the email logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The smtp server value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public string? SmtpServer { get; set; }
        /// <summary>
        /// Gets or sets the username value that forms part of the email logger core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The username value exposed by <see cref="EmailLoggerCoreOptions"/>.</value>
        [JsonInclude]
        public string? Username { get; set; }
    }
}
