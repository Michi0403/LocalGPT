namespace LocalGPT.Interfaces
{
    public interface IAiFeatureReportService
    {
        string ReportRoot { get; }
        Task<string?> WriteIfMissingFeatureReportAsync(string source, string responseText, CancellationToken cancellationToken = default);
    }
}
