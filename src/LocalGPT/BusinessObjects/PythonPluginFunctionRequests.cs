namespace LocalGPT.BusinessObjects;

/// <summary>Identifies one already-persisted runtime extension for a separately approved build action.</summary>
public sealed class BuildRuntimePluginFunctionRequest
{
    /// <summary>
    /// Gets or sets the stable plugin identifier used to identify or correlate this build runtime plugin function instance with related application state.
    /// </summary>
    /// <value>The plugin identifier value exposed by <see cref="BuildRuntimePluginFunctionRequest"/>.</value>
    public Guid PluginId { get; set; }
}

/// <summary>References one existing Python task without exposing script arguments.</summary>
public sealed class CancelPythonJobFunctionRequest
{
    /// <summary>
    /// Gets or sets the stable job identifier used to identify or correlate this cancel python job function instance with related application state.
    /// </summary>
    /// <value>The job identifier value exposed by <see cref="CancelPythonJobFunctionRequest"/>.</value>
    public Guid JobId { get; set; }
}
