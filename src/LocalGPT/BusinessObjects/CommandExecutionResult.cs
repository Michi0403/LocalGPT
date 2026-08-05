namespace LocalGPT.BusinessObjects
{
    public class CommandExecutionResult
    {
        public string FileName { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string StdoutPath { get; set; } = string.Empty;
        public string StderrPath { get; set; } = string.Empty;
        public string CommandProfile { get; set; } = "CustomAllowlistedCommand";
        public string PolicyDecision { get; set; } = "Allowed";
        public string PolicyReason { get; set; } = string.Empty;
        public bool Succeeded => ExitCode == 0;
    }
}
