using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents an application log entry.
    /// </summary>
    public class ApplicationLogEntry
    {
        /// <summary>
        /// Gets or sets identifier.
        /// </summary>
        [Key]
        public long Id { get; set; }
        /// <summary>
        /// Gets or sets timestamp UTC.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets level.
        /// </summary>
        public string Level { get; set; } = "Information";
        /// <summary>
        /// Gets or sets log level value.
        /// </summary>
        public int LogLevelValue { get; set; }
        /// <summary>
        /// Gets or sets category.
        /// </summary>
        public string Category { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets event identifier.
        /// </summary>
        public int EventId { get; set; }
        /// <summary>
        /// Gets or sets event name.
        /// </summary>
        public string? EventName { get; set; }
        /// <summary>
        /// Gets or sets message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets exception.
        /// </summary>
        public string? Exception { get; set; }
        /// <summary>
        /// Gets or sets machine name.
        /// </summary>
        public string MachineName { get; set; } = Environment.MachineName;
        /// <summary>
        /// Gets or sets process identifier.
        /// </summary>
        public int ProcessId { get; set; } = Environment.ProcessId;
        /// <summary>
        /// Gets or sets thread identifier.
        /// </summary>
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// Represents an application log summary.
    /// </summary>
    public sealed record ApplicationLogSummary(
        long Id,
        DateTime TimestampUtc,
        string Level,
        int LogLevelValue,
        string Category,
        int EventId,
        string? EventName,
        string Message,
        string? Exception);
}
