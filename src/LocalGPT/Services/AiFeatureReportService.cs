using DevExpress.DataAccess.DataFederation;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates AI feature report behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="councilText">Council text service dependency used by the AI feature report workflow to provide the corresponding application capability.</param>
    public partial class AiFeatureReportService(ILogger<AiFeatureReportService> logger,
        CouncilTextService councilText) : IAiFeatureReportService
    {
        /// <summary>
        /// Gets the report root value that forms part of the AI feature report state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The report root value exposed by <see cref="AiFeatureReportService"/>.</value>
        public string ReportRoot { get; } = LocalGptApplicationDataPaths.ResolveUserPath("AIReports");

        /// <summary>
        /// Writes if missing feature report as part of the AI feature report service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="source">Source value supplied to the AI feature report operation and used when producing its result.</param>
        /// <param name="responseText">Response text value supplied to the AI feature report operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
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
