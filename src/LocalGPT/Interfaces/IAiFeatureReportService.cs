namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the ai feature report service contract.
    /// </summary>
    public interface IAiFeatureReportService
    {
        string ReportRoot { get; }
        /// <summary>
        /// Writes if missing feature report async.
        /// </summary>
        Task<string?> WriteIfMissingFeatureReportAsync(string source, string responseText, CancellationToken cancellationToken = default);
    }
}
