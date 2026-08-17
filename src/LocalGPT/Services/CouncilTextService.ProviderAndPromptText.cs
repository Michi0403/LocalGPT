using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.Blazor.Viewer.Internal;
using DevExpress.DataAccess.DataFederation;
using DevExpress.Utils.About;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.Serialization;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.AI;
using SQLitePCL;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Net;
using System.Reactive;
using System.Security.AccessControl;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.Extensions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council text behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilTextService
    {

        /// <summary>
        /// Builds Ollama details as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="details">Details value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string? BuildOllamaDetails(OllamaModelDetails? details, ILogger<AiConnectivityProbe> logger)
        {
            try
            {
                if (details is null)
                    return null;

                var parts = new[] { details.Family, details.ParameterSize, details.QuantizationLevel }
                    .Where(p => !string.IsNullOrWhiteSpace(p));
                var text = string.Join(", ", parts);
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildOllamaDetails details {details?.ToString()}");
                return null;
            }

        }
        /// <summary>
        /// Performs trim for display as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="maxCharacters">Max characters value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string TrimForDisplay(string text, int maxCharacters, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return string.Empty;

                var trimmed = text.Trim();
                return trimmed.Length <= maxCharacters
                    ? trimmed
                    : $"{trimmed[..maxCharacters].TrimEnd()}{Environment.NewLine}... prompt truncated for display; full prompt is stored in the CouncilLogs markdown file and SQLite user message ...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TrimForDisplay text {text.ToString()} maxCharacters {maxCharacters.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Performs looks likely truncated as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool LooksLikelyTruncated(string text, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                var trimmed = text.TrimEnd();
                if (trimmed.Length < 1000)
                    return false;

                if (trimmed.EndsWith("...", StringComparison.Ordinal) ||
                    trimmed.EndsWith("…", StringComparison.Ordinal) ||
                    trimmed.EndsWith(".", StringComparison.Ordinal) ||
                    trimmed.EndsWith("!", StringComparison.Ordinal) ||
                    trimmed.EndsWith("?", StringComparison.Ordinal) ||
                    trimmed.EndsWith("]", StringComparison.Ordinal) ||
                    trimmed.EndsWith(")", StringComparison.Ordinal) ||
                    trimmed.EndsWith("}", StringComparison.Ordinal) ||
                    trimmed.EndsWith("```", StringComparison.Ordinal))
                {
                    return false;
                }

                return patterns.TruncatedTailPattern.IsMatch(trimmed) ||
                    !char.IsPunctuation(trimmed[^1]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LooksLikelyTruncated text {text.ToString()}");
                return false;
            }
        }
        /// <summary>
        /// Normalizes recovered prompt as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="prompt">Prompt value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeRecoveredPrompt(string prompt, ILogger logger)
        {
            try
            {
                var normalized = prompt.Trim();
                return normalized.Length <= 60000
                    ? normalized
                    : $"{normalized[..60000].TrimEnd()}{Environment.NewLine}... prompt truncated while reconstructing legacy DXAiChat memory ...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not normalize a recovered council prompt.");
                return string.Empty;
            }
        }
        /// <summary>
        /// Attempts to find council prompt section as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="content">Content value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string? TryFindCouncilPromptSection(string content, ILogger logger)
        {
            try
            {
                var markerIndex = content.IndexOf("## Original request", StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0)
                    markerIndex = content.IndexOf("Prompt sent to the AI Council", StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0)
                    return null;

                return content[markerIndex..];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not locate the council prompt section.");
                return null;
            }
        }
        /// <summary>
        /// Attempts to recover prompt from title.
        /// </summary>
        /// <param name="title">Title value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string? TryRecoverPromptFromTitle(string title, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) ||
                !title.Contains("AI Council request", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return $"""
                Recovered legacy council prompt:
                {title}

                LocalGPT recovered this from the saved conversation title because this older memory row did not store a separate user prompt message. New council saves keep the full original prompt visible in DXAiChat and CouncilLogs.
                """.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not recover a council prompt from the conversation title.");
                return null;
            }
        }
        /// <summary>
        /// Performs extract thinking as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="content">Content value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string? ExtractThinking(string content, ILogger logger)
        {
            try
            {
                var thinking = string.Join(
                    Environment.NewLine,
                    patterns.ThinkingBlockPattern
                        .Matches(content)
                        .Cast<Match>()
                        .Select(match => WebUtility.HtmlDecode(match.Groups["thinking"].Value).Trim())
                        .Where(value => !string.IsNullOrWhiteSpace(value)));
                return string.IsNullOrWhiteSpace(thinking) ? null : thinking;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ExtractThinking");
                return string.Empty;
            }
        }
        /// <summary>
        /// Performs strip thinking as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="content">Content value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string StripThinking(string content, ILogger logger)
        {
            try
            {
                return patterns.ThinkingBlockPattern.Replace(content, string.Empty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "StripThinking");
                return string.Empty;
            }
        }
        /// <summary>
        /// Performs decode text as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="bytes">Bytes value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string DecodeText(byte[] bytes, ILogger logger)
        {
            try
            {
                return SanitizeForPrompt(Encoding.UTF8.GetString(bytes), logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not decode uploaded text content; byte count {ByteCount}.", bytes.Length);
                try
                {

                    return SanitizeForPrompt(Encoding.Latin1.GetString(bytes), logger);
                }
                catch (Exception ex2)
                {

                    logger.LogError(ex2, $"Error DecodeText bytes {bytes.ToString()}");
                    return string.Empty;
                }

            }
        }

        /// <summary>
        /// Performs extract printable strings as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="bytes">Bytes value supplied to the council text operation and used when producing its result.</param>
        /// <param name="maxCharacters">Max characters value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ExtractPrintableStrings(byte[] bytes, int maxCharacters, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder();
                var current = new StringBuilder();

                foreach (var value in bytes)
                {
                    var printable = value is >= 32 and <= 126 || value is 9;
                    if (printable)
                    {
                        current.Append((char)value);
                        continue;
                    }

                    FlushCurrentString(builder, current, maxCharacters, logger);
                    if (builder.Length >= maxCharacters)
                        break;
                }

                FlushCurrentString(builder, current, maxCharacters, logger);
                return SanitizeForPrompt(builder.ToString(), logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error DecodeText bytes {bytes.ToString()} maxCharacters {maxCharacters.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Performs to forward slash as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="path">Path value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ToForwardSlash(string path, ILogger logger)
        {
            try
            {
                return path.Replace('\\', '/');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToForwardSlash path {path}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Performs flush current string as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="builder">Builder value supplied to the council text operation and used when producing its result.</param>
        /// <param name="current">Current value supplied to the council text operation and used when producing its result.</param>
        /// <param name="maxCharacters">Max characters value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        public void FlushCurrentString(StringBuilder builder, StringBuilder current, int maxCharacters, ILogger logger)
        {
            try
            {
                if (current.Length >= 4 && builder.Length < maxCharacters)
                {
                    builder.AppendLine(current.ToString());
                }

                current.Clear();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error FlushCurrentString builder {builder.ToString()} current {current.ToString()} maxCharacters {maxCharacters.ToString()}");
            }

        }

        /// <summary>
        /// Performs extract capability gap summary as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ExtractCapabilityGapSummary(string text, ILogger<AiFeatureReportService> logger)
        {
            try
            {
                var match = patterns.CapabilityGapBlockPattern.Match(text);
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
                    var value = ExtractField(body, field, logger);
                    if (!string.IsNullOrWhiteSpace(value))
                        builder.Append("- ").Append(field).Append(": ").AppendLine(value);
                }

                return builder.Length == 0
                    ? "- Structured block was present but no recognized fields were filled."
                    : builder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractCapabilityGapSummary text {text.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Performs extract helpful sources as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ExtractHelpfulSources(string text, ILogger<AiFeatureReportService> logger)
        {
            try
            {
                var matches = patterns.HelpfulSourceLinePattern
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractHelpfulSources text {text.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Performs extract field as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="body">Body value supplied to the council text operation and used when producing its result.</param>
        /// <param name="name">Name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ExtractField(string body, string name, ILogger<AiFeatureReportService> logger)
        {
            try
            {
                return patterns.ExtractStructuredField(body, name) ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not extract a named field; source content was omitted from logs.");
                return string.Empty;
            }
        }
        /// <summary>
        /// Builds unique file name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="directory">Directory value supplied to the council text operation and used when producing its result.</param>
        /// <param name="fileName">File name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildUniqueFileName(string directory, string fileName, ILogger logger)
        {
            try
            {
                var safe = SanitizeFileName(fileName, logger);
                var candidate = Path.Combine(directory, safe);
                if (!System.IO.File.Exists(candidate))
                    return safe;

                var name = Path.GetFileNameWithoutExtension(safe);
                var extension = Path.GetExtension(safe);
                for (var i = 1; i < 1000; i++)
                {
                    candidate = Path.Combine(directory, $"{name}-{i}{extension}");
                    if (!System.IO.File.Exists(candidate))
                        return Path.GetFileName(candidate);
                }

                return $"{name}-{Guid.NewGuid():N}{extension}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error BuildUniqueFileName directory {directory} fileName {fileName}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs sanitize file name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="fileName">File name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string SanitizeFileName(string fileName, ILogger logger)
        {
            try
            {
                var safe = Path.GetFileName(fileName);
                foreach (var invalid in Path.GetInvalidFileNameChars())
                    safe = safe.Replace(invalid, '_');

                return string.IsNullOrWhiteSpace(safe) ? "upload.bin" : safe;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error SanitizeFileName fileName {fileName}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds safe ZIP relative path as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="fullName">Full name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string? BuildSafeZipRelativePath(string fullName, ILogger logger)
        {
            try
            {
                var parts = fullName
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => part is not "." and not "..")
                .Select(filter => SanitizeFileName(filter, logger))
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

                return parts.Length == 0 ? null : Path.Combine(parts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error BuildSafeZipRelativePath fullName {fullName}");
                return null;
            }
        }

    }
}
