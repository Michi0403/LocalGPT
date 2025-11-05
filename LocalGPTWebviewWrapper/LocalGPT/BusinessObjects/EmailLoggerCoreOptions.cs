using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    public class EmailLoggerCoreOptions
    {
        public const string EmailLoggerCore = "EmailLoggerCore";
        [JsonInclude]
        public IEnumerable<string> BccRecipients { get; set; } = new List<string>();

        [JsonInclude]
        public IEnumerable<string> CcRecipients { get; set; } = new List<string>();
        [JsonInclude]
        public IEnumerable<string> EmailRecipients { get; set; } = new List<string>();
        [JsonInclude]
        public bool EnableSsl { get; set; }
        [JsonInclude]
        public CoreLogLevel CoreLogLevel { get; set; }
        [JsonInclude]
        public string? Password { get; set; }
        [JsonInclude]
        public string? SenderEmail { get; set; }
        [JsonInclude]
        public int SmtpPort { get; set; }
        [JsonInclude]
        public string? SmtpServer { get; set; }
        [JsonInclude]
        public string? Username { get; set; }
    }
}
