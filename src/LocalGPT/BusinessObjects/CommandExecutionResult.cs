namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a command execution result.
    /// </summary>
    public class CommandExecutionResult
    {
        /// <summary>
        /// Gets or sets file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets arguments.
        /// </summary>
        public string Arguments { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets working directory.
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets started at UTC.
        /// </summary>
        public DateTime StartedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets completed at UTC.
        /// </summary>
        public DateTime CompletedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets exit code.
        /// </summary>
        public int ExitCode { get; set; }
        /// <summary>
        /// Gets or sets standard output.
        /// </summary>
        public string StandardOutput { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets standard error.
        /// </summary>
        public string StandardError { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets duration.
        /// </summary>
        public TimeSpan Duration { get; set; }
        /// <summary>
        /// Gets or sets stdout path.
        /// </summary>
        public string StdoutPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets stderr path.
        /// </summary>
        public string StderrPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets command profile.
        /// </summary>
        public string CommandProfile { get; set; } = "CustomAllowlistedCommand";
        /// <summary>
        /// Gets or sets policy decision.
        /// </summary>
        public string PolicyDecision { get; set; } = "Allowed";
        /// <summary>
        /// Gets or sets policy reason.
        /// </summary>
        public string PolicyReason { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets succeeded.
        /// </summary>
        public bool Succeeded => ExitCode == 0;
    }
}
