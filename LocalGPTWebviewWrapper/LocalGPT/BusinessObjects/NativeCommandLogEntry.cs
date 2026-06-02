namespace LocalGPT.BusinessObjects
{
    public class NativeCommandLogEntry
    {
        public long Id { get; set; }
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
        public string FeatureName { get; set; } = "Minecraft Builder";
        public string RequestedBy { get; set; } = "LocalGPT user";
        public string CommandProfile { get; set; } = "CustomAllowlistedCommand";
        public string Executable { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public double DurationMilliseconds { get; set; }
        public string StdoutPath { get; set; } = string.Empty;
        public string StderrPath { get; set; } = string.Empty;
        public string PolicyDecision { get; set; } = "Allowed";
        public string PolicyReason { get; set; } = string.Empty;
    }
}
