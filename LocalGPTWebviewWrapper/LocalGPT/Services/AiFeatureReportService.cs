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
                .AppendLine("Capability / knowledge classification:")
                .AppendLine(ExtractCapabilityGapSummary(responseText))
                .AppendLine()
                .AppendLine("Helpful sources requested by the AI:")
                .AppendLine(ExtractHelpfulSources(responseText))
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

        private static string ExtractCapabilityGapSummary(string text)
        {
            var match = CapabilityGapBlockPattern().Match(text);
            if (!match.Success)
            {
                return "- No structured <localgpt-capability-gap> block was provided. Ask the model to include requested language, framework, version, local sources, external sources, missing LocalGPT functions, safe workflow, and artifact plan.";
            }

            var body = match.Groups["body"].Value.Trim();
            var fields = new[]
            {
                "user-request-summary",
                "missing-capability",
                "owning-area",
                "target-deliverable",
                "requested-languages",
                "requested-frameworks",
                "requested-versions",
                "requested-domain-knowledge",
                "local-knowledge-sources",
                "external-knowledge-sources",
                "missing-localgpt-functions",
                "safe-workflow",
                "artifact-plan",
                "investigation-status",
                "next-localgpt-improvement",
                "confidence",
                "tags"
            };

            var builder = new StringBuilder();
            foreach (var field in fields)
            {
                var value = ExtractField(body, field);
                if (!string.IsNullOrWhiteSpace(value))
                    builder.Append("- ").Append(field).Append(": ").AppendLine(value);
            }

            return builder.Length == 0
                ? "- Structured block was present but no recognized fields were filled."
                : builder.ToString().TrimEnd();
        }

        private static string ExtractHelpfulSources(string text)
        {
            var matches = HelpfulSourceLinePattern()
                .Matches(text)
                .Select(match => match.Groups["line"].Value.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            if (matches.Count == 0)
            {
                return "- None explicitly requested. If this missing feature depends on external APIs, ask the user for official docs, example projects, or versioned package references before implementation.";
            }

            var builder = new StringBuilder();
            foreach (var match in matches)
                builder.Append("- ").AppendLine(match);

            return builder.ToString().TrimEnd();
        }

        private static string ExtractField(string body, string name)
        {
            var pattern = $@"(?ims)^\s*{Regex.Escape(name)}\s*:\s*(?<value>.*?)(?=^\s*(?:user-request-summary|missing-capability|owning-area|target-deliverable|requested-languages|requested-frameworks|requested-versions|requested-domain-knowledge|local-knowledge-sources|external-knowledge-sources|missing-localgpt-functions|safe-workflow|artifact-plan|investigation-status|next-localgpt-improvement|confidence|tags)\s*:|\z)";
            var match = Regex.Match(body, pattern, RegexOptions.CultureInvariant);
            return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
        }

        [GeneratedRegex("(missing feature|missing capability|not implemented|not yet implemented|blocked by|cannot build|requires implementation|feature gap|capability gap|<localgpt-capability-gap>)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MissingFeaturePattern();

        [GeneratedRegex("(?im)^\\s*(?:[-*]\\s*)?(?<line>(?:helpful sources?|source request|needed sources?|references?|docs?|documentation|official docs?|examples?|sample projects?|spec(?:ification)?s?|tutorials?)\\s*[:\\-].+)$", RegexOptions.CultureInvariant)]
        private static partial Regex HelpfulSourceLinePattern();

        [GeneratedRegex("<localgpt-capability-gap>(?<body>.*?)</localgpt-capability-gap>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex CapabilityGapBlockPattern();
    }
}
