namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents native command log state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
    /// </summary>
    public class NativeCommandLogEntry
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this native command log instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public long Id { get; set; }
        /// <summary>
        /// Gets or sets the started at UTC associated with this native command log state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The started at UTC value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the completed at UTC associated with this native command log state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The completed at UTC value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the feature name value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The feature name value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string FeatureName { get; set; } = "Minecraft Builder";
        /// <summary>
        /// Gets or sets the requested by value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The requested by value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string RequestedBy { get; set; } = "LocalGPT user";
        /// <summary>
        /// Gets or sets the command profile value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The command profile value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string CommandProfile { get; set; } = "CustomAllowlistedCommand";
        /// <summary>
        /// Gets or sets the executable value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The executable value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string Executable { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the arguments value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The arguments value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string Arguments { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the working directory used by this native command log instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The working directory value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string WorkingDirectory { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the exit code value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The exit code value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public int ExitCode { get; set; }
        /// <summary>
        /// Gets or sets the duration milliseconds value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The duration milliseconds value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public double DurationMilliseconds { get; set; }
        /// <summary>
        /// Gets or sets the stdout path used by this native command log instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The stdout path value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string StdoutPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the stderr path used by this native command log instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The stderr path value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string StderrPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the policy decision value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The policy decision value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string PolicyDecision { get; set; } = "Allowed";
        /// <summary>
        /// Gets or sets the policy reason value that forms part of the native command log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The policy reason value exposed by <see cref="NativeCommandLogEntry"/>.</value>
        public string PolicyReason { get; set; } = string.Empty;
    }
}
