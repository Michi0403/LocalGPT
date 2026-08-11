namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a native command log entry.
    /// </summary>
    public class NativeCommandLogEntry
    {
        /// <summary>
        /// Gets or sets identifier.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Gets or sets started at UTC.
        /// </summary>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets completed at UTC.
        /// </summary>
        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets feature name.
        /// </summary>
        public string FeatureName { get; set; } = "Minecraft Builder";
        /// <summary>
        /// Gets or sets requested by.
        /// </summary>
        public string RequestedBy { get; set; } = "LocalGPT user";
        /// <summary>
        /// Gets or sets command profile.
        /// </summary>
        public string CommandProfile { get; set; } = "CustomAllowlistedCommand";
        /// <summary>
        /// Gets or sets executable.
        /// </summary>
        public string Executable { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets arguments.
        /// </summary>
        public string Arguments { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets working directory.
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets exit code.
        /// </summary>
        public int ExitCode { get; set; }
        /// <summary>
        /// Gets or sets duration milliseconds.
        /// </summary>
        public double DurationMilliseconds { get; set; }
        /// <summary>
        /// Gets or sets stdout path.
        /// </summary>
        public string StdoutPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets stderr path.
        /// </summary>
        public string StderrPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets policy decision.
        /// </summary>
        public string PolicyDecision { get; set; } = "Allowed";
        /// <summary>
        /// Gets or sets policy reason.
        /// </summary>
        public string PolicyReason { get; set; } = string.Empty;
    }
}
