using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    public class ApplicationLogEntry
    {
        [Key]
        public long Id { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "Information";
        public int LogLevelValue { get; set; }
        public string Category { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string? EventName { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string MachineName { get; set; } = Environment.MachineName;
        public int ProcessId { get; set; } = Environment.ProcessId;
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
    }

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
