using LocalGPT.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents an email logger core options.
    /// </summary>
    public class EmailLoggerCoreOptions
    {
        /// <summary>
        /// Stores email logger core.
        /// </summary>
        public const string EmailLoggerCore = "EmailLoggerCore";
        /// <summary>
        /// Gets or sets bcc recipients.
        /// </summary>
        [JsonInclude]
        public IEnumerable<string> BccRecipients { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets cc recipients.
        /// </summary>
        [JsonInclude]
        public IEnumerable<string> CcRecipients { get; set; } = new List<string>();
        /// <summary>
        /// Gets or sets email recipients.
        /// </summary>
        [JsonInclude]
        public IEnumerable<string> EmailRecipients { get; set; } = new List<string>();
        /// <summary>
        /// Gets or sets enable ssl.
        /// </summary>
        [JsonInclude]
        public bool EnableSsl { get; set; }
        /// <summary>
        /// Gets or sets core log level.
        /// </summary>
        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; }
        /// <summary>
        /// Gets or sets password.
        /// </summary>
        [JsonInclude]
        public string? Password { get; set; }
        /// <summary>
        /// Gets or sets sender email.
        /// </summary>
        [JsonInclude]
        public string? SenderEmail { get; set; }
        /// <summary>
        /// Gets or sets smtp port.
        /// </summary>
        [JsonInclude]
        public int SmtpPort { get; set; }
        /// <summary>
        /// Gets or sets smtp server.
        /// </summary>
        [JsonInclude]
        public string? SmtpServer { get; set; }
        /// <summary>
        /// Gets or sets username.
        /// </summary>
        [JsonInclude]
        public string? Username { get; set; }
    }
}
