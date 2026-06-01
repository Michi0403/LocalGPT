using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public partial class AiFeatureReportService(ILogger<AiFeatureReportService> logger) : IAiFeatureReportService
    {
        public string ReportRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "AIReports");

        public async Task<string?> WriteIfMissingFeatureReportAsync(string source, string responseText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(responseText) || !LooksLikeMissingFeatureReport(responseText))
                return null;

            Directory.CreateDirectory(ReportRoot);

            var fileName = $"missing-features-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.txt";
            var path = Path.Combine(ReportRoot, fileName);
            var content = new StringBuilder()
                .AppendLine("LocalGPT AI Missing Feature Report")
                .AppendLine($"Created: {DateTimeOffset.Now:O}")
                .AppendLine($"Source: {source}")
                .AppendLine()
                .AppendLine(responseText)
                .ToString();

            await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken);
            logger.LogInformation("Wrote AI missing feature report to {Path}", path);
            return path;
        }

        private static bool LooksLikeMissingFeatureReport(string text)
        {
            return MissingFeaturePattern().IsMatch(text);
        }

        [GeneratedRegex("(missing feature|missing capability|not implemented|not yet implemented|blocked by|cannot build|requires implementation|feature gap)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MissingFeaturePattern();
    }
}
