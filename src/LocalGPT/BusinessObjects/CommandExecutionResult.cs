namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents the outcome of command execution, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    public class CommandExecutionResult
    {
        /// <summary>
        /// Gets or sets the file name used by this command execution instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The file name value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the arguments value that forms part of the command execution state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The arguments value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string Arguments { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the working directory used by this command execution instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The working directory value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string WorkingDirectory { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the started at UTC associated with this command execution state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The started at UTC value exposed by <see cref="CommandExecutionResult"/>.</value>
        public DateTime StartedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets the completed at UTC associated with this command execution state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The completed at UTC value exposed by <see cref="CommandExecutionResult"/>.</value>
        public DateTime CompletedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets the exit code value that forms part of the command execution state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The exit code value exposed by <see cref="CommandExecutionResult"/>.</value>
        public int ExitCode { get; set; }
        /// <summary>
        /// Gets or sets the standard output value that forms part of the command execution state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The standard output value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string StandardOutput { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the standard error value that forms part of the command execution state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The standard error value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string StandardError { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the duration duration used to control timing in the command execution workflow.
        /// </summary>
        /// <value>The duration value exposed by <see cref="CommandExecutionResult"/>.</value>
        public TimeSpan Duration { get; set; }
        /// <summary>
        /// Gets or sets the stdout path used by this command execution instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The stdout path value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string StdoutPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the stderr path used by this command execution instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The stderr path value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string StderrPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the command profile value that forms part of the command execution state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The command profile value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string CommandProfile { get; set; } = "CustomAllowlistedCommand";
        /// <summary>
        /// Gets or sets the policy decision value that forms part of the command execution state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The policy decision value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string PolicyDecision { get; set; } = "Allowed";
        /// <summary>
        /// Gets or sets the policy reason value that forms part of the command execution state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The policy reason value exposed by <see cref="CommandExecutionResult"/>.</value>
        public string PolicyReason { get; set; } = string.Empty;
        /// <summary>
        /// Gets a value indicating whether the operation succeeded applies to the command execution state.
        /// </summary>
        /// <value>The succeeded value exposed by <see cref="CommandExecutionResult"/>.</value>
        public bool Succeeded => ExitCode == 0;
    }
}
