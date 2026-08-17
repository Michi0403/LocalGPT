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
        /// Stores the council text pattern data service dependency used by <see cref="CouncilTextService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilTextPatternDataService patterns;
        /// <summary>
        /// Stores the local GPT catalog service dependency used by <see cref="CouncilTextService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly LocalGptCatalogService catalog;
        /// <summary>
        /// Stores the logger used by <see cref="CouncilTextService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<CouncilTextService> serviceLogger;

        /// <summary>
        /// Initializes the service with its dependency-injected collaborators.
        /// </summary>
        /// <param name="patterns">Injected dependency used by the service.</param>
        /// <param name="catalog">Injected dependency used by the service.</param>
        /// <param name="serviceLogger">Injected dependency used by the service.</param>
        public CouncilTextService(
            ICouncilTextPatternDataService patterns,
            LocalGptCatalogService catalog,
            ILogger<CouncilTextService> serviceLogger)
        {
            this.patterns = patterns;
            this.catalog = catalog;
            this.serviceLogger = serviceLogger;
        }

   

        /// <summary>
        /// Decodes HTML entities for a plain-text human-visible surface. Callers must render the returned value through an encoding text surface rather than as raw markup.
        /// </summary>
        /// <param name="value">Encoded or plain text intended for a human-visible non-markup surface.</param>
        /// <returns>The once-decoded text, or an empty string when no value was supplied.</returns>
        public string DecodeHumanVisibleText(string? value)
        {
            try
            {
                serviceLogger.LogTrace("Council text operation {Operation} started.", nameof(DecodeHumanVisibleText));
                return string.IsNullOrEmpty(value) ? string.Empty : WebUtility.HtmlDecode(value);
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "Council text operation {Operation} failed; returning the original plain text.", nameof(DecodeHumanVisibleText));
                return value ?? string.Empty;
            }
        }

        /// <summary>
        /// Builds the safe visible attachment presentation shared by live and persisted chat messages.
        /// </summary>
        /// <param name="content">Content value supplied to the council text operation and used when producing its result.</param>
        /// <param name="fileNames">String dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildAttachmentPresentation(string? content, IEnumerable<string>? fileNames)
        {
            try
            {
                serviceLogger.LogTrace("Council text operation {Operation} started.", nameof(BuildAttachmentPresentation));
                var safeContent = content ?? string.Empty;
                if (fileNames is null)
                    return safeContent;

                var names = fileNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => WebUtility.HtmlEncode(Path.GetFileName(name.Trim())))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (names.Length == 0)
                    return safeContent;

                var chips = string.Join(
                    string.Empty,
                    names.Select(name => $"<span class=\"localgpt-restored-attachment\">📎 {name}</span>"));
                return $"{safeContent}\n<div class=\"localgpt-restored-attachments\" data-localgpt-restored-attachments=\"true\">{chips}</div>";
            }
            catch (Exception ex)
            {
                serviceLogger.LogWarning(
                    ex,
                    "Council text operation {Operation} failed; attachment names were omitted from the visible chat content.",
                    nameof(BuildAttachmentPresentation));
                return content ?? string.Empty;
            }
        }

        /// <summary>
        /// Performs format live council session option as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="startedAtUtc">Started at utc value supplied to the council text operation and used when producing its result.</param>
        /// <param name="runState">Run state value supplied to the council text operation and used when producing its result.</param>
        /// <param name="councilMembers">String dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <returns>The string produced by the operation.</returns>
        public string FormatLiveCouncilSessionOption(
            DateTime startedAtUtc,
            string runState,
            IReadOnlyList<string> councilMembers)
        {
            try
            {
                serviceLogger.LogTrace("Council text operation {Operation} started.", nameof(FormatLiveCouncilSessionOption));
                var memberText = string.Join(", ", councilMembers.Take(3));
                return $"{startedAtUtc.ToLocalTime():g} · {runState} · {memberText}";
            }
            catch (Exception ex)
            {
                serviceLogger.LogError(ex, "Council text operation {Operation} failed.", nameof(FormatLiveCouncilSessionOption));
                return $"{startedAtUtc.ToLocalTime():g} · {runState}";
            }
        }

        /// <summary>
        /// Normalizes former thought as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeFormerThought(string? value, ILogger logger)
        {
            try
            {
                serviceLogger.LogTrace("Council text operation {Operation} started.", nameof(NormalizeFormerThought));
                if (string.IsNullOrWhiteSpace(value))
                {
                    logger.LogDebug($"{nameof(NormalizeFormerThought)} received no former-thought content.");
                    return string.Empty;
                }

                var text = value.Trim();
                text = WebUtility.HtmlDecode(WebUtility.HtmlDecode(text));
                text = patterns.FormerThoughtBreakPattern.Replace(text, Environment.NewLine);
                text = patterns.FormerThoughtCodeWrapperPattern.Replace(text, string.Empty);
                text = patterns.FormerThoughtOpeningFencePattern.Replace(text, string.Empty);
                text = patterns.FormerThoughtClosingFencePattern.Replace(text, string.Empty);
                text = patterns.FormerThoughtPresentationWrapperPattern.Replace(text, match =>
                    match.Value.StartsWith("</", StringComparison.Ordinal) ? Environment.NewLine : string.Empty);
                var normalized = patterns.FormerThoughtExcessLineBreakPattern.Replace(
                    text,
                    Environment.NewLine + Environment.NewLine).Trim();
                logger.LogDebug($"{nameof(NormalizeFormerThought)} normalized former-thought presentation markup.");
                return normalized;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"{nameof(NormalizeFormerThought)} failed; the original text will be shown without normalization.");
                return value?.Trim() ?? string.Empty;
            }
        }

        /// <summary>
        /// Builds role coordination explanation as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="details">String dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildRoleCoordinationExplanation(IReadOnlyCollection<string> details, ILogger logger)
        {
            try
            {
                if (details.Count == 0)
                    return "No cross-role assignment or pairing rule is configured.";

                var explanation = $"Coordination: {string.Join("; ", details)}.";
                logger.LogDebug(
                    "{MethodName} prepared a role-coordination explanation with {DetailCount} detail(s).",
                    nameof(BuildRoleCoordinationExplanation),
                    details.Count);
                return explanation;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{MethodName} failed; role-coordination details were omitted.",
                    nameof(BuildRoleCoordinationExplanation));
                return "Role coordination is configured, but its explanation could not be displayed.";
            }
        }

        /// <summary>
        /// Builds feedback preview as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="content">Content value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildFeedbackPreview(string? content, ILogger logger)
        {
            try
            {
                var singleLine = patterns.WhitespacePattern.Replace(content ?? string.Empty, " ").Trim();
                var preview = singleLine.Length <= 180 ? singleLine : singleLine[..177] + "...";
                logger.LogDebug($"{nameof(BuildFeedbackPreview)} prepared a feedback preview without logging its content.");
                return preview;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"{nameof(BuildFeedbackPreview)} failed; feedback content was omitted from logs.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds architecture poll message as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="languageToolchain">Language toolchain value supplied to the council text operation and used when producing its result.</param>
        /// <param name="uiStack">Ui stack value supplied to the council text operation and used when producing its result.</param>
        /// <param name="solutionShape">Solution shape value supplied to the council text operation and used when producing its result.</param>
        /// <param name="renderMode">Render mode value supplied to the council text operation and used when producing its result.</param>
        /// <param name="referenceLook">Reference look value supplied to the council text operation and used when producing its result.</param>
        /// <param name="allowSafeDefaults">Value indicating whether allow safe defaults should apply to this operation.</param>
        /// <param name="extraDirection">Extra direction value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildArchitecturePollMessage(
            string languageToolchain,
            string uiStack,
            string solutionShape,
            string renderMode,
            string referenceLook,
            bool allowSafeDefaults,
            string? extraDirection,
            ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                    .AppendLine("# LocalGPT Architecture Poll Decision")
                    .AppendLine()
                    .AppendLine("Treat explicit non-Ask values as my current decision for the next answer. Treat my normal chat request and extra direction as binding design input too; do not downgrade a user-stated design into an unresolved Ask value.")
                    .AppendLine($"- Language/toolchain: {languageToolchain}")
                    .AppendLine($"- UI stack: {uiStack}")
                    .AppendLine($"- Solution shape: {solutionShape}")
                    .AppendLine($"- Runtime/rendering: {renderMode}")
                    .AppendLine($"- Reference look: {referenceLook}")
                    .AppendLine($"- Prior consent for safe sandbox details: {(allowSafeDefaults ? "granted" : "not granted")}");

                if (!string.IsNullOrWhiteSpace(extraDirection))
                    builder.AppendLine($"- Extra direction: {extraDirection.Trim()}");

                builder
                    .AppendLine()
                    .AppendLine("If any selected value says \"Ask me\", first check whether my chat prompt or extra direction already answers it. If yes, treat the stated design as selected.")
                    .AppendLine("If an Ask value remains materially unresolved and prior consent is granted, choose a safe sandbox default, name that choice, and continue with a downloadable artifact.")
                    .AppendLine("If an Ask value remains materially unresolved and prior consent is not granted, stop before generating code or files. Return a concise runtime poll with concrete options and wait for my answer.")
                    .AppendLine("Do not assume C#/.NET, Minecraft, Blazor, DevExpress, Java, C++, PowerShell, or any other ecosystem unless I chose it, the target repository already requires it, or the request clearly specifies it.")
                    .AppendLine("When the requested language or ecosystem has no CodeDOM specialization, use the reviewed generic source/workspace file-generation path and preserve the target repository's build/project conventions instead of forcing a C# solution shape.")
                    .AppendLine("When recreating a goal application, compare its layout, navigation, data flows, API routes, settings, build/toolchain conventions, and user workflows, then recreate the recognizable structure with the selected architecture.");

                logger.LogDebug($"{nameof(BuildArchitecturePollMessage)} created a service-owned architecture decision message without logging user content.");
                return builder.ToString().Trim();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"{nameof(BuildArchitecturePollMessage)} failed; architecture choices were omitted from logs.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses model names as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IReadOnlyList<string> ParseModelNames(string? value, ILogger logger)
        {
            try
            {
                var names = (value ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(modelName => !string.IsNullOrWhiteSpace(modelName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                logger.LogDebug($"{nameof(ParseModelNames)} parsed {names.Count} distinct model names without logging their values.");
                return names;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"{nameof(ParseModelNames)} failed; model names were omitted from logs.");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Parses a user-editable list of stable names from newline, comma, or semicolon separated text.
        /// </summary>
        /// <param name="value">User-edited text containing zero or more names.</param>
        /// <returns>A case-insensitively distinct, ordinally sorted list suitable for persisted configuration.</returns>
        public List<string> ParseUserEditableNameList(string? value)
        {
            try
            {
                return (value ?? string.Empty)
                    .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(item => item.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "Council text operation {Operation} failed while parsing a user-editable name list.", nameof(ParseUserEditableNameList));
                return [];
            }
        }

        /// <summary>Extracts a fixed N-format GUID that immediately follows a stable marker in text.</summary>
        /// <param name="content">Content value supplied to the council text operation and used when producing its result.</param>
        /// <param name="marker">Marker value supplied to the council text operation and used when producing its result.</param>
        /// <param name="runId">Identifier of the run to use for this operation.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool TryExtractMarkedGuid(string? content, string marker, out Guid runId)
        {
            runId = Guid.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(content) || string.IsNullOrEmpty(marker))
                    return false;
                var markerIndex = content.IndexOf(marker, StringComparison.Ordinal);
                if (markerIndex < 0)
                    return false;
                var runIdStart = markerIndex + marker.Length;
                return content.Length >= runIdStart + 32 && Guid.TryParseExact(content.AsSpan(runIdStart, 32), "N", out runId);
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "Extracting a marked GUID from text failed; content was omitted.");
                return false;
            }
        }

        /// <summary>Tests text membership through the service boundary so UI and controllers do not own filtering semantics.</summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="fragment">Fragment value supplied to the council text operation and used when producing its result.</param>
        /// <param name="comparison">Comparison value supplied to the council text operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool ContainsText(string? value, string? fragment, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            try { return value.ContainsText(fragment, comparison); }
            catch (Exception exception) { serviceLogger.LogError(exception, "Text containment evaluation failed."); return false; }
        }

        /// <summary>Tests a text prefix through the service boundary.</summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="prefix">Prefix value supplied to the council text operation and used when producing its result.</param>
        /// <param name="comparison">Comparison value supplied to the council text operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool StartsWithText(string? value, string? prefix, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            try { return value.StartsWithText(prefix, comparison); }
            catch (Exception exception) { serviceLogger.LogError(exception, "Text prefix evaluation failed."); return false; }
        }

        /// <summary>Tests a text suffix through the service boundary.</summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="suffix">Suffix value supplied to the council text operation and used when producing its result.</param>
        /// <param name="comparison">Comparison value supplied to the council text operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool EndsWithText(string? value, string? suffix, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            try { return value.EndsWithText(suffix, comparison); }
            catch (Exception exception) { serviceLogger.LogError(exception, "Text suffix evaluation failed."); return false; }
        }

        /// <summary>Joins display values using a caller-selected separator while keeping the formatting operation service-owned.</summary>
        /// <param name="values">String dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="separator">Separator value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string FormatJoinedList(IEnumerable<string>? values, string separator)
        {
            try { return values.JoinText(separator); }
            catch (Exception exception) { serviceLogger.LogError(exception, "Joining display text failed."); return string.Empty; }
        }

        /// <summary>Joins values using the platform newline sequence.</summary>
        /// <param name="values">String dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <returns>The string produced by the operation.</returns>
        public string FormatLines(IEnumerable<string>? values)
        {
            try
            {
                return FormatJoinedList(values, Environment.NewLine);
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "Formatting display lines failed.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Normalizes artifact URL as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeArtifactUrl(string? value)
        {
            try { return (value ?? string.Empty).ReplaceText("\\/", "/", StringComparison.Ordinal); }
            catch (Exception exception) { serviceLogger.LogError(exception, "Normalizing an artifact URL failed."); return value ?? string.Empty; }
        }

        /// <summary>Returns the final non-empty slash-delimited path segment for display.</summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string LastPathSegment(string? value)
        {
            try
            {
                var normalized = value ?? string.Empty;
                var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
                return segments.LastOrDefault() ?? normalized;
            }
            catch (Exception exception) { serviceLogger.LogError(exception, "Resolving the final path segment failed."); return value ?? string.Empty; }
        }

        /// <summary>Normalizes a theme or UI label into a stable lower-risk CSS token fragment.</summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ToCssToken(string? value)
        {
            try { return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ReplaceText(" ", "-", StringComparison.Ordinal).ToLowerInvariant(); }
            catch (Exception exception) { serviceLogger.LogError(exception, "Normalizing a CSS token failed."); return string.Empty; }
        }

        /// <summary>
        /// Formats stable names for compact inline display without moving list formatting into a component.
        /// </summary>
        /// <param name="values">Names to present.</param>
        /// <returns>The comma-separated presentation string.</returns>
        public string FormatInlineNameList(IEnumerable<string>? values)
        {
            try
            {
                return values is null ? string.Empty : string.Join(", ", values);
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "Council text operation {Operation} failed while formatting an inline name list.", nameof(FormatInlineNameList));
                return string.Empty;
            }
        }

        /// <summary>
        /// Formats stable names as one value per line for a user-editable text surface.
        /// </summary>
        /// <param name="values">Names to present.</param>
        /// <returns>The newline-separated presentation string.</returns>
        public string FormatMultilineNameList(IEnumerable<string>? values)
        {
            try
            {
                return values is null ? string.Empty : string.Join(Environment.NewLine, values);
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "Council text operation {Operation} failed while formatting a multiline name list.", nameof(FormatMultilineNameList));
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs escape JSON as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string EscapeJson(string value) {
    try
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(EscapeJson)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(EscapeJson)} failed.");
        throw;
    }
}




        /// <summary>
        /// Performs looks like missing feature report as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool LooksLikeMissingFeatureReport(string text, ILogger<AiFeatureReportService> logger)
        {
            try
            {
                return patterns.MissingFeaturePattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LooksLikeMissingFeatureReport text {text.ToString()}");
                return false;
            }
        }
        /// <summary>
        /// Performs sanitize file name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string SanitizeFileName(string value, ILogger<BuildDebugInventoryService> logger)
        {
            try
            {
                var invalid = Path.GetInvalidFileNameChars();
                var builder = new StringBuilder(value.Length);
                foreach (var character in value)
                    builder.Append(invalid.Contains(character) || char.IsWhiteSpace(character) ? '-' : character);

                return builder.ToString().Trim('-');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SanitizeFileName value {value.ToString()}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Builds import directories as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="rootPath">Root path value supplied to the council text operation and used when producing its result.</param>
        /// <param name="maxProjects">Max projects value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<string> BuildImportDirectories(string rootPath, int maxProjects, ILogger logger)
        {
            try
            {
                var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var directory in EnumerateImportDirectoryCandidates(rootPath, logger))
                {
                    if (emitted.Count >= maxProjects)
                        yield break;

                    var directoryName = Path.GetFileName(directory);
                    if (catalog.ExcludedDirectoryNames.Contains(directoryName) || !emitted.Add(directory))
                        continue;

                    yield return directory;
                }
            }
            finally
            {
                logger.LogInformation($"Ended BuildImportDirectories rootPath {rootPath?.ToString()} maxProjects {maxProjects.ToString()}");
            }
        }
        /// <summary>
        /// Performs enumerate import directory candidates as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="rootPath">Root path value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<string> EnumerateImportDirectoryCandidates(string rootPath, ILogger logger)
        {
            try
            {
                if (LooksLikeArchitectureRoot(rootPath, logger))
                    yield return rootPath;

                foreach (var directory in SafeEnumerateDirectories(rootPath, logger))
                    yield return directory;

                foreach (var directory in EnumerateNestedArchitectureRoots(rootPath, logger))
                    yield return directory;
            }
            finally
            {
                logger.LogInformation($"Ended EnumerateImportDirectoryCandidates rootPath {rootPath?.ToString()}");
            }
        }
        /// <summary>
        /// Performs extract target frameworks as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<string> ExtractTargetFrameworks(string text, ILogger logger)
        {
            try
            {
                return patterns.TargetFrameworkPattern.Matches(text)
                    .Select(match => match.Groups["value"].Value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(ExtractTargetFrameworks)} could not extract target frameworks; source text was omitted from logs.");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Performs extract package references as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<string> ExtractPackageReferences(string text, ILogger logger)
        {
            try
            {
                return patterns.PackageReferencePattern.Matches(text)
                    .Select(match => match.Groups["value"].Value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(ExtractPackageReferences)} could not extract package references; source text was omitted from logs.");
                return Array.Empty<string>();
            }
        }
        /// <summary>
        /// Determines whether important file as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="fileName">File name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="extension">Extension value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsImportantFile(string fileName, string extension, ILogger logger)
        {
            try
            {
                return IsProjectRootFile(fileName, extension, logger) ||
    extension is ".razor" or ".xaml" or ".py" or ".json" or ".sql" or ".md" or ".mdx" or ".go" or ".gotmpl" ||
    fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ||
    fileName.StartsWith("Startup.", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("App.razor", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("_Imports.razor", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("Routes.razor", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsImportantFile fileName {fileName?.ToString()} extension {extension?.ToString()}");
                return false;
            }

        }
        /// <summary>
        /// Determines whether project root file as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="fileName">File name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="extension">Extension value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsProjectRootFile(string fileName, string extension, ILogger logger)
        {
            try
            {
                return extension is ".sln" or ".csproj" or ".fsproj" or ".vbproj" ||
    fileName.Equals("go.mod", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("go.sum", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("CMakeLists.txt", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsProjectRootFile fileName {fileName?.ToString()} extension {extension?.ToString()}");
                return false;
            }
        }
        /// <summary>
        /// Performs contains ZIP entry as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="zipEntries">Zip entries value supplied to the council text operation and used when producing its result.</param>
        /// <param name="required">Required value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool ContainsZipEntry(HashSet<string> zipEntries, string required, ILogger logger)
        {
            try
            {
                var normalized = required.Replace('\\', '/').Trim('/');
                return zipEntries.Any(entry =>
                    string.Equals(entry.Trim('/'), normalized, StringComparison.OrdinalIgnoreCase) ||
                    entry.Contains($"/{normalized}", StringComparison.OrdinalIgnoreCase) ||
                    entry.StartsWith($"{normalized}/", StringComparison.OrdinalIgnoreCase) ||
                    entry.Contains($"{normalized}/", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ContainsZipEntry zipEntries {zipEntries.ToString()} required {required.ToString()}");
                return false;
            }

        }
        /// <summary>
        /// Performs redact sensitive name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string RedactSensitiveName(string value, ILogger logger)
        {
            try
            {

                return patterns.SensitiveNamePattern.Replace(value, "[redacted-name]");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RedactSensitiveName value {value?.ToString()}");
                return string.Empty;
            }
        }
  
        /// <summary>
        /// Builds file policy summary as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildFilePolicySummary(ILogger logger)
        {
            try
            {
                var sourceExtensions = string.Join(", ", catalog.SourceExtensions.Order(StringComparer.OrdinalIgnoreCase));
                var binaryExtensions = string.Join(", ", catalog.BinaryExtensions.Order(StringComparer.OrdinalIgnoreCase));
                var excludedDirectories = string.Join(", ", catalog.ExcludedDirectoryNames.Order(StringComparer.OrdinalIgnoreCase));
                return "Reads source/documentation-like files: " + sourceExtensions +
                    ". Counts but does not store binary/package files: " + binaryExtensions +
                    ". Skips noisy build/cache directories: " + excludedDirectories + ".";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildFilePolicySummary");
                return string.Empty;
            }
        }
        /// <summary>
        /// Normalizes task set as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="taskSet">Task set value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeTaskSet(string? taskSet, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(taskSet))
                    return "engineering";

                return taskSet.Trim().ToLowerInvariant() switch
                {
                    "replacement" or "replacements" or "apps" or "app-replacements" => "replacement",
                    "all" or "full" => "all",
                    _ => "engineering"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeTaskSet taskSet {taskSet?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Builds engineering tasks as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The collection produced by the operation.</returns>
        public IReadOnlyList<BenchmarkTaskDefinition> BuildEngineeringTasks()
        {
    try
    {
                return
                [
                    new(
                        "devexpress-webshop-efcore",
                        "DevExpress Blazor webshop with EF Core",
                        "Generate a downloadable whole solution zip for a DevExpress Blazor webshop with EF Core, SQLite seed data, products, carts, orders, admin CRUD grid, detail form, Bootstrap v5 layout, and README.",
                        "A strong answer contains a .NET solution, EF Core DbContext/entities/migration guidance, DevExpress product/admin grids, cart/order services, seed data, app navigation, build/run steps, and no client-side privileged commands.",
                        "Benchmark answer: create a full solution zip with DevExpress Blazor pages, EF Core entities, services, product/cart/order workflows, and README. Include Implementation artifact request.",
                        6,
                        ["PROJECT_INDEX.md", ".localgpt-generation.json", "src/"],
                        ["DevExpress", "Blazor", "service", "model"],
                        ["Components/Pages", "Services", "Models"]),
                    new(
                        "blazor-admin-crud-dashboard",
                        "Blazor admin dashboard with CRUD grid and detail form",
                        "Generate a downloadable whole solution zip for a Blazor admin dashboard with DevExpress DxGrid CRUD, detail form, validation, SQLite persistence, audit log, and Bootstrap v5 navigation.",
                        "A strong answer contains DxGrid, EditForm/DxFormLayout detail editing, validation, EF Core persistence, audit logging, clear service boundaries, and buildable project files.",
                        "Benchmark answer: create a full solution zip with DxGrid CRUD, detail form, validation, SQLite persistence, audit notes, and README. Include Implementation artifact request.",
                        6,
                        ["PROJECT_INDEX.md", ".localgpt-generation.json", "src/"],
                        ["DevExpress", "Blazor", "grid"],
                        ["Components/Pages", "Services", "Models"]),
                    new(
                        "msix-winui-blazor-packaging",
                        "MSIX/WinUI/Blazor packaging error diagnosis",
                        "Diagnose and produce a downloadable LocalGPT-style implementation note for an MSIX WinUI WebView2 Blazor packaging error involving static web assets, LocalGPT.deps.json, IncludeLocalGptPublishedPayload, and APPX1111 duplicate paths.",
                        "A strong answer separates SDK dotnet build from Visual Studio MSBuild, preserves thin WinUI wrapper, explains IncludeLocalGptPublishedPayload=false for Debug/F5 and release opt-in, and names static web asset payload risks.",
                        "Benchmark answer: produce a concise .cs artifact note and optional solution zip explaining DesktopBridge diagnosis, package-map duplicate risks, and verification commands. Include Implementation artifact request.",
                        5,
                        [],
                        ["MSIX", "WebView2", "WinUI"],
                        []),
                    new(
                        "minecraft-datapack-workspace",
                        "Minecraft datapack workspace",
                        "Generate a downloadable Minecraft Java datapack zip for a prompt-driven city simulation datapack named Benchmark Borough with scoreboards, storage, load/tick tags, debug function, docs, and Minecraft 1.21.4 pack format.",
                        "A strong answer contains zip root pack.mcmeta and data/ directly, singular 1.21 function folders, valid load/tick tags, lowercase namespace, no .mcfunction.txt files, no leading slash commands, and install/test steps.",
                        "Benchmark answer: generate a prompt-driven datapack zip for Benchmark Borough, not a hard-coded Living Cities artifact. Include pack.mcmeta and data/ at zip root.",
                        9,
                        ["pack.mcmeta", "data/minecraft/tags/function/load.json", "data/minecraft/tags/function/tick.json"],
                        ["datapack", "pack.mcmeta"],
                        ["pack.mcmeta", "data/"]),
                    new(
                        "minecraft-loader-skeletons",
                        "Fabric/Paper/NeoForge project skeleton distinction",
                        "Generate a downloadable Minecraft Java project skeleton distinction zip that contains separate Fabric, Paper, and NeoForge skeletons for Minecraft 1.21.4, with each loader using its own metadata and Gradle dependency conventions.",
                        "A strong answer keeps Fabric metadata, Paper plugin.yml, and NeoForge mods.toml/dependencies separate; it does not reuse one loader template for all three.",
                        "Benchmark answer: create a loader matrix zip with distinct Fabric, Paper, and NeoForge workspaces. Include project skeleton distinction in the answer.",
                        8,
                        ["fabric/", "paper/", "neoforge/"],
                        ["Fabric", "Paper", "NeoForge"],
                        ["fabric", "paper", "neoforge"])
                ];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(BuildEngineeringTasks)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(BuildEngineeringTasks)} failed.");
        throw;
    }
}
        /// <summary>
        /// Builds replacement tasks as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The collection produced by the operation.</returns>
        public IReadOnlyList<BenchmarkTaskDefinition> BuildReplacementTasks()
        {
    try
    {
                return
                [
                    new(
                        "localgpt-replacement",
                        "LocalGPT replacement workbench",
                        "Generate a downloadable whole solution zip that can stand in for LocalGPT as a local-first AI workbench. It must include DXAiChat, AI Council with minimum two-member feedback talk, SQLite memory and knowledge approval markers, artifact download routes, Minecraft builder, install/setup, and Test Lab surfaces. No missing feature is acceptable; if a capability is not implemented, represent it as a visible backend service boundary and capability gap.",
                        "A strong answer is a buildable .NET/Blazor/DevExpress solution with recognizable LocalGPT navigation: DXAiChat, AI Council, SQLite Database, Minecraft Mod Builder, Install, Help/Test Lab, artifact routes, memory/knowledge services, logs, and missing-feature feedback capture.",
                        "Benchmark answer: create a full LocalGPT-like workbench solution zip with distinct pages for DXAiChat, AI Council, SQLite, Minecraft, Install, and Test Lab. Include Implementation artifact request.",
                        7,
                        ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Chat.razor", "Components/Pages/ModelCouncil.razor", "Components/Pages/Database.razor", "Components/Pages/MinecraftModBuilder.razor", "Components/Pages/TestLab.razor", "Components/Pages/Install.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                        ["DXAiChat", "AI Council", "SQLite", "Minecraft", "Test Lab", "Source Fidelity", "Artifact"],
                        ["Components/Pages/Chat.razor", "Components/Pages/ModelCouncil.razor", "Components/Pages/Database.razor", "Services/GeneratedSourceFidelityService.cs"]),
                    new(
                        "tacosportalopen-replacement",
                        "TacosPortalOpen replacement portal",
                        "Generate a downloadable whole solution zip that can stand in for TacosPortalOpen as a server-interactive DevExpress/Blazor system. It must represent the real architecture: multi-project/core service topology, Telegram or message-event ingestion, normalized persistence, worker services, notifications/logging, custom security/admin UI, optional WASM client, WinUI/WebView2 wrapper boundary, and a sanitized simpler bot backend implementation.",
                        "A strong answer is a buildable .NET/Blazor/DevExpress solution with pages and service boundaries for Telegram ingestion, persistence, workers, admin/security, client shells, notification/logging, EF/SQLite or provider-backed data, validation, and build/run docs. A generic menu/orders/reservations restaurant portal is the wrong template.",
                        "Benchmark answer: create a full TacosPortalOpen-style multi-host/event-ingestion solution zip with Telegram ingestion, persistence, workers, admin, client-shell boundaries, and source-fidelity docs. Include Implementation artifact request.",
                        7,
                        ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/TelegramIngestion.razor", "Components/Pages/Persistence.razor", "Components/Pages/Workers.razor", "Components/Pages/Admin.razor", "Components/Pages/ClientShells.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                        ["Telegram", "Persistence", "Workers", "WebView2", "WASM", "DevExpress", "Source Fidelity"],
                        ["Components/Pages/TelegramIngestion.razor", "Components/Pages/Persistence.razor", "Components/Pages/Workers.razor", "Services/GeneratedSourceFidelityService.cs"]),
                    new(
                        "ai-host-replacement",
                        "Provider-compatible AI host replacement",
                        "Generate a downloadable whole solution zip for a provider-neutral AI host replacement in .NET 10, ASP.NET Core, Blazor, and DevExpress. It must include model catalog, chat, downloads, running models, API console, logs, settings, templates, hardware, runner/plugins, /api/version, /api/tags, /api/ps, /api/chat, /api/generate, OpenAI-compatible routes, direct local model-file runner interfaces, Python.NET/PowerShell extension boundaries, and SQLite/appsettings state.",
                        "A strong answer is a buildable AI-host solution with provider-compatible routes, DevExpress navigation, model/download/runtime pages, native local-model-file runner interfaces, no upstream provider proxying, and explicit runner setup/status.",
                        "Benchmark answer: create a buildable AI-host replacement solution zip with DevExpress pages, provider-compatible routes, runner/plugin service contracts, and no Go dependency. Include Implementation artifact request.",
                        9,
                        ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Chat.razor", "Components/Pages/RunningModels.razor", "Components/Pages/ModelDownloads.razor", "Components/Pages/RunnerPlugins.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedAiHostArchitectureServices.cs", "Services/GeneratedSourceFidelityService.cs"],
                        ["IInferenceProvider", "IInferenceRunner", "RunnerPlugins", "/api/chat", "Source Fidelity"],
                        ["Components/Pages/RunnerPlugins.razor", "Services/GeneratedAiHostArchitectureServices.cs", "Services/GeneratedSourceFidelityService.cs"]),
                    new(
                        "simple-bot-backend",
                        "Simpler bot backend implementation",
                        "Generate a downloadable whole solution zip for a simpler bot backend inspired by legacy Telegram-style integrations, but sanitized. It must include webhooks, conversation state, command routing, moderation/retry queues, optional Python.NET boundary for speech/translation/media helpers, settings, logs, EF/SQLite, and a DevExpress Blazor operator UI.",
                        "A strong answer is a buildable .NET/Blazor/DevExpress bot backend with Webhooks, Conversations, Bot Settings, Python Interop pages, services, safe permission gates, and no private database dump requirement.",
                        "Benchmark answer: create a simple bot backend solution zip with webhook/conversation/settings/python-interop pages and safe backend service boundaries. Include Implementation artifact request.",
                        7,
                        ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Webhooks.razor", "Components/Pages/Conversations.razor", "Components/Pages/BotSettings.razor", "Components/Pages/PythonInterop.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                        ["Webhooks", "Conversations", "Python Interop", "SQLite"],
                        ["Components/Pages/Webhooks.razor", "Components/Pages/PythonInterop.razor", "Services/GeneratedSourceFidelityService.cs"])
                ];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(BuildReplacementTasks)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(BuildReplacementTasks)} failed.");
        throw;
    }
}
        /// <summary>
        /// Normalizes OpenAI endpoint as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="endpoint">Endpoint value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeOpenAIEndpoint(string endpoint, ILogger<AiConnectivityProbe> logger)
        {
            try
            {
                var normalized = endpoint.Trim().TrimEnd('/');
                return normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                    ? normalized[..^3]
                    : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeOpenAIEndpoint endpoint {endpoint.ToString()}");
                return string.Empty;
            }

        }
}
}
