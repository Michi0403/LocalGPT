namespace LocalGPT.BusinessObjects;

/// <summary>Content-free, bounded monitoring projection for an embedded or process-isolated Python task.</summary>
public sealed class PythonJobSnapshot
{
    /// <summary>Latest recognized training phase and optional step count.</summary>
    public PythonTrainingProgress? TrainingProgress { get; set; }

    /// <summary>
    /// Gets or sets the stable job identifier used to identify or correlate this python job snapshot instance with related application state.
    /// </summary>
    /// <value>The job identifier value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the operation value that forms part of the python job snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The operation value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public string Operation { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the lane value that forms part of the python job snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The lane value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public string Lane { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the python job snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the created at UTC associated with this python job snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the started at UTC associated with this python job snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public DateTime? StartedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the completed at UTC associated with this python job snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether cancellation requested applies to the python job snapshot state.
    /// </summary>
    /// <value>The cancellation requested value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public bool CancellationRequested { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the python job snapshot state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="PythonJobSnapshot"/>.</value>
    public bool Succeeded { get; set; }
}

/// <summary>Payload-free progress from the packaged model trainer.</summary>
/// <param name="Phase">Recognized phase of the trainer.</param>
/// <param name="Step">Most recent training step, when reported.</param>
/// <param name="TotalSteps">Expected number of training steps, when reported.</param>
public sealed record PythonTrainingProgress(string Phase, int? Step, int? TotalSteps)
{
    /// <summary>Formats the phase and any reported counts for the task monitor.</summary>
    public override string ToString() => TotalSteps is int total ? $"{Phase} {Step ?? 0}/{total}" : Phase;
}

/// <summary>Bounded output from an approved and configured Python runtime plugin.</summary>
public sealed class PythonPluginProcessResult
{
    /// <summary>
    /// Gets or sets the exit code value that forms part of the python plugin process state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The exit code value exposed by <see cref="PythonPluginProcessResult"/>.</value>
    public int ExitCode { get; set; }
    /// <summary>
    /// Gets or sets the output value that forms part of the python plugin process state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output value exposed by <see cref="PythonPluginProcessResult"/>.</value>
    public string Output { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the python plugin process state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="PythonPluginProcessResult"/>.</value>
    public string Error { get; set; } = string.Empty;
}
