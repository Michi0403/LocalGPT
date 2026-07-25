using DevExpress.DataAccess.DataFederation;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    public partial class AiFeatureReportService(ILogger<AiFeatureReportService> logger,
        CouncilTextService councilText) : IAiFeatureReportService
    {
        public string ReportRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "AIReports");

        public async Task<string?> WriteIfMissingFeatureReportAsync(string source, string responseText, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseText) || !councilText.LooksLikeMissingFeatureReport(responseText, logger))
                    return null;

                Directory.CreateDirectory(ReportRoot);

                var fileName = $"missing-features-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.txt";
                var path = Path.Combine(ReportRoot, fileName);
                var content = new StringBuilder()
                    .AppendLine("LocalGPT AI Missing Feature Report")
                    .AppendLine($"Created: {DateTimeOffset.Now:O}")
                    .AppendLine($"Source: {source}")
                    .AppendLine()
                    .AppendLine("Capability / knowledge classification:")
                    .AppendLine(councilText.ExtractCapabilityGapSummary(responseText, logger))
                    .AppendLine()
                    .AppendLine("Helpful sources requested by the AI:")
                    .AppendLine(councilText.ExtractHelpfulSources(responseText, logger))
                    .AppendLine()
                    .AppendLine(responseText)
                    .ToString();

                await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Wrote AI missing feature report to {Path}", path);
                return path;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not write a missing-feature report for source {Source}.", source);
                return null;
            }
        }

    }
}
