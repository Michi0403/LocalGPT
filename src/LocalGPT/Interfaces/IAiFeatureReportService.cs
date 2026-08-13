namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for AI feature report behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IAiFeatureReportService
    {
        /// <summary>
        /// Gets the report root value that forms part of the AI feature report state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The report root value exposed by <see cref="IAiFeatureReportService"/>.</value>
        string ReportRoot { get; }
        /// <summary>
        /// Writes if missing feature report as part of the AI feature report service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="source">Source value supplied to the AI feature report operation and used when producing its result.</param>
        /// <param name="responseText">Response text value supplied to the AI feature report operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        Task<string?> WriteIfMissingFeatureReportAsync(string source, string responseText, CancellationToken cancellationToken = default);
    }
}
