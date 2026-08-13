namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Carries the configurable python core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class PythonCoreOptions
    {
        /// <summary>
        /// Defines the python core constant used by <see cref="PythonCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string PythonCore = "PythonCore";

        /// <summary>
        /// Gets or sets the python runtime value that forms part of the python core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The python runtime value exposed by <see cref="PythonCoreOptions"/>.</value>
        public string? PythonRuntime { get; set; }
    }
}
