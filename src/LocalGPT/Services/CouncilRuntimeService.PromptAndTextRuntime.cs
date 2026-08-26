using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council runtime behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
        /// <summary>
        /// Reconstructs a bounded DXAiChat prompt without recursively feeding complete prior Council transcripts back into later runs.
        /// </summary>
        /// <param name="messages">Current DXAiChat message history.</param>
        /// <param name="logger">Writes bounded prompt-reconstruction diagnostics.</param>
        /// <returns>The compact Council prompt containing user turns and at most the latest cleaned assistant consensus.</returns>
        public string BuildPrompt(IEnumerable<ChatMessage> messages, ILogger logger)
        {
            try
            {
                var history = messages
                    .Where(message => message.Role != ChatRole.System && !string.IsNullOrWhiteSpace(message.Text))
                    .ToList();
                var latestAssistantIndex = history.FindLastIndex(message => message.Role == ChatRole.Assistant);
                var normalizedHistory = new List<(ChatRole Role, string Text)>();
                var seenUserTurns = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < history.Count; index++)
                {
                    var message = history[index];
                    if (message.Role == ChatRole.Assistant && index != latestAssistantIndex)
                        continue;

                    var normalized = NormalizeCouncilHistoryText(
                        message.Text ?? string.Empty,
                        message.Role == ChatRole.Assistant,
                        logger);
                    if (string.IsNullOrWhiteSpace(normalized))
                        continue;
                    if (message.Role == ChatRole.User && !seenUserTurns.Add(normalized))
                        continue;
                    normalizedHistory.Add((message.Role, normalized));
                }

                const int maximumUserTurns = 12;
                var retainedUserTurns = normalizedHistory.Count(item => item.Role == ChatRole.User);
                var usersToSkip = Math.Max(0, retainedUserTurns - maximumUserTurns);
                var builder = new StringBuilder()
                    .AppendLine("Answer this DXAiChat conversation as the LocalGPT AI Council.")
                    .AppendLine("Use the selected members, preserve user intent, and include a concise consensus.")
                    .AppendLine();
                foreach (var item in normalizedHistory)
                {
                    if (item.Role == ChatRole.User && usersToSkip > 0)
                    {
                        usersToSkip--;
                        continue;
                    }
                    builder
                        .Append(item.Role == ChatRole.Assistant ? "Previous assistant consensus" : "User")
                        .AppendLine(":")
                        .AppendLine(item.Text)
                        .AppendLine();
                }

                var prompt = builder.ToString().Trim();
                return prompt.Length <= catalog.MaxDxAiChatPromptCharacters
                    ? prompt
                    : prompt[^catalog.MaxDxAiChatPromptCharacters..];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the provider prompt from chat messages.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Removes LocalGPT-owned rendering panels and recursive Council wrapper text from one chat-history item.
        /// </summary>
        /// <param name="value">Raw stored chat text.</param>
        /// <param name="assistantMessage">Whether the item is an assistant response.</param>
        /// <param name="logger">Writes bounded normalization diagnostics.</param>
        /// <returns>Plain Markdown suitable for inclusion in a later Council prompt.</returns>
        private string NormalizeCouncilHistoryText(string value, bool assistantMessage, ILogger logger)
        {
            try
            {
                var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
                normalized = assistantMessage
                    ? ExtractLatestAssistantConsensus(normalized)
                    : ExtractInnermostUserRequest(normalized);
                normalized = StripLocalGptRenderingPanels(normalized);
                normalized = Regex.Replace(
                    normalized,
                    @"<!--localgpt-council-stream-complete:[a-f0-9]{32}-->|<p\s+class=""localgpt-stream-status""[^>]*>.*?</p>",
                    string.Empty,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
                normalized = WebUtility.HtmlDecode(normalized);
                normalized = RepairUnbalancedMarkdownFence(normalized);
                normalized = Regex.Replace(
                    normalized,
                    @"(?m)^[ \t]+$",
                    string.Empty,
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
                normalized = Regex.Replace(
                    normalized,
                    @"\n{4,}",
                    "\n\n\n",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2)).Trim();

                const int maximumHistoryCharacters = 32_000;
                return normalized.Length <= maximumHistoryCharacters
                    ? normalized
                    : normalized[^maximumHistoryCharacters..];
            }
            catch (RegexMatchTimeoutException exception)
            {
                logger.LogWarning(exception, "Council chat-history cleanup timed out; a bounded plain-text fallback will be used.");
                var fallback = WebUtility.HtmlDecode(value).Trim();
                return fallback.Length <= 8_000 ? fallback : fallback[^8_000..];
            }
        }

        /// <summary>
        /// Removes a lone trailing fence or closes a lone opening fence so one history item cannot break later Markdown.
        /// </summary>
        /// <param name="value">Normalized history Markdown.</param>
        /// <returns>Markdown with balanced fenced-code delimiters.</returns>
        private string RepairUnbalancedMarkdownFence(string value)
        {
    try
    {
                var matches = Regex.Matches(
                    value,
                    @"(?m)^[ \t]*(?<fence>`{3,})[^\r\n]*$",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
                if (matches.Count == 0 || matches.Count % 2 == 0)
                    return value;

                var finalMatch = matches[^1];
                if (string.IsNullOrWhiteSpace(value[(finalMatch.Index + finalMatch.Length)..]))
                    return value.Remove(finalMatch.Index, finalMatch.Length).TrimEnd();

                var openingFence = matches[0].Groups["fence"].Value;
                return value.TrimEnd() + Environment.NewLine + openingFence;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(RepairUnbalancedMarkdownFence)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(RepairUnbalancedMarkdownFence)} failed.");
        throw;
    }
}

        /// <summary>
        /// Extracts the last user-authored body when an older Council wrapper was accidentally persisted as user text.
        /// </summary>
        /// <param name="value">Stored user message text.</param>
        /// <returns>The innermost user request without generated Council wrapper headings.</returns>
        private string ExtractInnermostUserRequest(string value)
        {
    try
    {
                if (!value.Contains("AI Council request:", StringComparison.OrdinalIgnoreCase) &&
                    !value.Contains("Answer this DXAiChat conversation", StringComparison.OrdinalIgnoreCase))
                    return value;

                var matches = Regex.Matches(
                    value,
                    @"(?ms)^User:\s*(?<body>.*?)(?=^Previous assistant consensus:|^User:|\z)",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
                var candidate = matches
                    .Select(match => match.Groups["body"].Value.Trim())
                    .LastOrDefault(body => !string.IsNullOrWhiteSpace(body) &&
                        !body.StartsWith("AI Council request:", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(candidate))
                    return candidate;

                return Regex.Replace(
                    value,
                    @"(?im)^(?:AI Council request:|Council members:.*|Answer this DXAiChat conversation.*|Use the selected members.*)\s*$",
                    string.Empty,
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2)).Trim();
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(ExtractInnermostUserRequest)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(ExtractInnermostUserRequest)} failed.");
        throw;
    }
}

        /// <summary>
        /// Keeps only the latest visible consensus/final-answer section from a stored assistant Council response.
        /// </summary>
        /// <param name="value">Stored assistant response text.</param>
        /// <returns>The latest final section or the original value when no known heading exists.</returns>
        private string ExtractLatestAssistantConsensus(string value)
        {
    try
    {
                var markers = new[]
                {
                    "\n## Consensus\n",
                    "\n## Final Answer\n",
                    "\n## Final council answer\n",
                    "\n### Final Answer\n"
                };
                var markerIndex = -1;
                var markerLength = 0;
                foreach (var marker in markers)
                {
                    var index = value.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (index <= markerIndex)
                        continue;
                    markerIndex = index;
                    markerLength = marker.Length;
                }

                var result = markerIndex >= 0 ? value[(markerIndex + markerLength)..] : value;
                var boundaries = new[]
                {
                    "\n## Continue Action",
                    "\n## Generated Artifact Links",
                    "\n## User Decision Poll",
                    "\n## User decision poll",
                    "\n## Artifacts"
                };
                var boundary = boundaries
                    .Select(item => result.IndexOf(item, StringComparison.OrdinalIgnoreCase))
                    .Where(index => index >= 0)
                    .DefaultIfEmpty(-1)
                    .Min();
                return (boundary >= 0 ? result[..boundary] : result).Trim();
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(ExtractLatestAssistantConsensus)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(ExtractLatestAssistantConsensus)} failed.");
        throw;
    }
}

        /// <summary>
        /// Removes LocalGPT-owned details/pre panels so model thinking and live process markup never become later prompt content.
        /// </summary>
        /// <param name="value">Stored response text containing optional LocalGPT markup.</param>
        /// <returns>Response text without controlled rendering panels or orphaned panel tags.</returns>
        private string StripLocalGptRenderingPanels(string value)
        {
    try
    {
                var result = value;
                const string controlledPanelPattern = @"<details\s+class=""(?:model-thinking(?:\s+open)?|council-step(?:\s+council-live)?|council-prompt)""[^>]*>.*?</details>";
                for (var pass = 0; pass < 6; pass++)
                {
                    var cleaned = Regex.Replace(
                        result,
                        controlledPanelPattern,
                        string.Empty,
                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
                        TimeSpan.FromSeconds(2));
                    if (string.Equals(cleaned, result, StringComparison.Ordinal))
                        break;
                    result = cleaned;
                }

                return Regex.Replace(
                    result,
                    @"</?(?:details|summary|pre)(?:\s[^>]*)?>",
                    string.Empty,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(StripLocalGptRenderingPanels)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(StripLocalGptRenderingPanels)} failed.");
        throw;
    }
}
        /// <summary>Executes the format step progress operation.</summary>
        /// <param name="step">Input value for step.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string FormatStepProgress(MultiModelCouncilStep step, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                .AppendLine()
                .AppendLine("<p class=\"localgpt-stream-status\"><em>")
                .Append(WebUtility.HtmlEncode($"{step.ModelName} finished {step.Phase} / {step.Role} in {step.DurationSeconds:n1}s. Step details were streamed above; final consensus appears below."))
                .AppendLine("</em></p>")
                .AppendLine();

                if (!string.IsNullOrWhiteSpace(step.Error))
                {
                    builder
                        .AppendLine($"<details class=\"council-step\" open>")
                        .Append("<summary>")
                        .Append(WebUtility.HtmlEncode($"{step.ModelName} error during {step.Phase}"))
                        .AppendLine("</summary>")
                        .AppendLine()
                        .AppendLine("**Error:**")
                        .AppendLine()
                        .AppendLine(step.Error.Trim())
                        .AppendLine()
                        .AppendLine("</details>")
                        .AppendLine();
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FormatStepProgress step {step.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the create update operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ChatResponseUpdate CreateUpdate(string text, ILogger logger)
        {
            try
            {

                return new(ChatRole.Assistant, [new TextContent(text)]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create an AI Council stream update.");
                return new ChatResponseUpdate(
                    ChatRole.Assistant,
                    [new TextContent("The AI Council produced an update that could not be rendered. Review LocalGPT logs.")]);
            }
        }
        /// <summary>Executes the build summary operation.</summary>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="length">Input value for length.</param>
        /// <param name="kind">Input value for kind.</param>
        /// <param name="includedInPrompt">Input value for includedInPrompt.</param>
        /// <param name="note">Input value for note.</param>
        /// <param name="excerpt">Input value for excerpt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public AnalyzedUploadFile? BuildSummary(
    string relativePath,
    long length,
    string kind,
    bool includedInPrompt,
    string note,
    string excerpt, ILogger logger)
        {
            try
            {
                return new AnalyzedUploadFile(
                new ChatUploadWorkspaceFileSummary(
                    relativePath,
                    kind,
                    length,
                    DateTime.UtcNow,
                    includedInPrompt,
                    note),
                text.TrimForPrompt(excerpt, catalog.MaxExcerptCharactersPerFile, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the source summary for {RelativePath}; length {Length}; kind {Kind}; included {IncludedInPrompt}.", relativePath, length, kind, includedInPrompt);
                return null;
            }
        }

        /// <summary>Executes the build binary summary operation.</summary>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="length">Input value for length.</param>
        /// <param name="kind">Input value for kind.</param>
        /// <param name="includedInPrompt">Input value for includedInPrompt.</param>
        /// <param name="note">Input value for note.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public AnalyzedUploadFile? BuildBinarySummary(
            string relativePath,
            long length,
            string kind,
            bool includedInPrompt,
            string note, ILogger logger)
        {
            try
            {
                return BuildSummary(relativePath, length, kind, includedInPrompt, note, string.Empty, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the binary summary for {RelativePath}; length {Length}; kind {Kind}; included {IncludedInPrompt}.", relativePath, length, kind, includedInPrompt);
                return null;
            }
        }


        /// <summary>Executes the sanitize for prompt operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string SanitizeForPrompt(string text, ILogger logger)
        {
            try
            {
                var userName = Environment.UserName;
                if (!string.IsNullOrWhiteSpace(userName))
                    text = text.Replace(userName, "%USER%", StringComparison.OrdinalIgnoreCase);

                return text.Replace("\0", string.Empty, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not sanitize text for a prompt.");
                return string.Empty;
            }
        }
        /// <summary>Executes the analyze bytes operation.</summary>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="bytes">Input value for bytes.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public AnalyzedUploadFile? AnalyzeBytes(string relativePath, byte[] bytes, ILogger logger)
        {
            try
            {
                if (IsZip(relativePath, logger))
                {
                    return BuildBinarySummary(
                        relativePath,
                        bytes.Length,
                        "zip",
                        false,
                        "Zip file saved as uploaded. Extracted safe entries are represented separately.", logger);
                }

                var isText = IsTextLike(relativePath, logger) || LooksLikeText(bytes, logger);
                if (isText)
                {
                    var texti = text.DecodeText(bytes, logger);
                    return BuildSummary(relativePath, bytes.Length, "text", true, "Text excerpt included.", texti, logger);
                }

                var extension = Path.GetExtension(relativePath);
                if (catalog.BinaryDiagnosticExtensions.Contains(extension))
                {
                    var strings = text.ExtractPrintableStrings(bytes, catalog.MaxBinaryStringCharacters, logger);
                    var note = extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase)
                        ? "PDB/debug file summarized with printable strings only."
                        : "Binary file summarized with printable strings only.";
                    return BuildSummary(relativePath, bytes.Length, "binary-strings", true, note, strings, logger);
                }

                return BuildBinarySummary(
                    relativePath,
                    bytes.Length,
                    "binary",
                    false,
                    "Binary file saved but not included in prompt context.", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AnalyzeBytes relativePath: {relativePath.ToString()} bytes: {bytes.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the build context markdown operation.</summary>
        /// <param name="workspaceName">Input value for workspaceName.</param>
        /// <param name="root">Input value for root.</param>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="analyzedFiles">Input value for analyzedFiles.</param>
        /// <param name="warnings">Input value for warnings.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildContextMarkdown(
            string workspaceName,
            string root,
            string prompt,
            IReadOnlyList<AnalyzedUploadFile> analyzedFiles,
            IReadOnlyList<string> warnings, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
              .AppendLine("# LocalGPT Chat Upload Workspace")
              .AppendLine()
              .AppendLine($"Workspace: `{workspaceName}`")
              .AppendLine($"Root path: `{root}`")
              .AppendLine($"Created UTC: {DateTimeOffset.UtcNow:O}")
              .AppendLine()
              .AppendLine("## Prompt")
              .AppendLine(text.TrimForPrompt(prompt, 4_000, logger))
              .AppendLine()
              .AppendLine("## AI workflow instructions")
              .AppendLine("- Use this workspace as uploaded user evidence for the current DXAiChat prompt.")
              .AppendLine("- Read files through chat.upload_workspace_* DXAiFunctions instead of asking for huge pasted context.")
              .AppendLine("- Zips are extracted safely; skipped entries are listed as warnings.")
              .AppendLine("- PDB, DLL, EXE, WASM, and other binaries are never executed; only bounded printable strings are shown.")
              .AppendLine("- Generated or edited code belongs in a council artifact workspace, then a refreshed zip download.")
              .AppendLine();

                if (warnings.Count > 0)
                {
                    builder.AppendLine("## Warnings");
                    foreach (var warning in warnings)
                        builder.AppendLine($"- {warning}");
                    builder.AppendLine();
                }

                builder.AppendLine("## Files");
                foreach (var file in analyzedFiles.Select(file => file.Summary))
                {
                    builder
                        .Append("- ")
                        .Append(file.RelativePath)
                        .Append(" (")
                        .Append(file.Kind)
                        .Append(", ")
                        .Append(file.Length)
                        .Append(" bytes): ")
                        .AppendLine(file.Note);
                }

                builder.AppendLine();
                builder.AppendLine("## Extracted context");

                var remainingCharacters = catalog.MaxContextCharacters - builder.Length;
                foreach (var file in analyzedFiles.Where(file => file.Summary.IncludedInPrompt))
                {
                    if (remainingCharacters <= 0)
                        break;

                    var excerpt = text.TrimForPrompt(file.Excerpt, Math.Min(catalog.MaxExcerptCharactersPerFile, remainingCharacters), logger);
                    if (string.IsNullOrWhiteSpace(excerpt))
                        continue;

                    var section = new StringBuilder()
                        .AppendLine()
                        .AppendLine($"### {file.Summary.RelativePath}")
                        .AppendLine($"Kind: {file.Summary.Kind}. {file.Summary.Note}")
                        .AppendLine()
                        .AppendLine("```text")
                        .AppendLine(excerpt)
                        .AppendLine("```")
                        .ToString();

                    if (section.Length > remainingCharacters)
                        section = text.TrimForPrompt(section, remainingCharacters, logger);

                    builder.Append(section);
                    remainingCharacters -= section.Length;
                }

                return text.TrimForPrompt(builder.ToString(), catalog.MaxContextCharacters, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build upload-workspace context Markdown for {WorkspaceName}.", workspaceName);
                return string.Empty;
            }

        }

        /// <summary>Executes the resolve workspace file operation.</summary>
        /// <param name="workspace">Input value for workspace.</param>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string? ResolveWorkspaceFile(string workspace, string relativePath, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    return null;

                var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(normalized))
                    return null;

                var root = Path.GetFullPath(workspace);
                var file = Path.GetFullPath(Path.Combine(root, normalized));
                return IsInsideRoot(root, file, logger) ? file : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error ResolveWorkspaceFile workspace {workspace} relativePath {relativePath}");
                return null;
            }
        }

        /// <summary>Executes the is inside root operation.</summary>
        /// <param name="root">Input value for root.</param>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsInsideRoot(string root, string path, ILogger logger)
        {
            try
            {
                return platform.IsSameOrDescendantPath(root, path);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsInsideRoot root {root} path {path}");
                return false;
            }
        }

        /// <summary>Executes the build workspace name operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="files">Input value for files.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildWorkspaceName(
            string prompt,
            IReadOnlyList<ChatUploadWorkspaceInputFile> files, ILogger logger)
        {
            try
            {
                var source = files.FirstOrDefault()?.Name;
                if (string.IsNullOrWhiteSpace(source))
                    source = prompt;

                var slug = Regex.Replace(source ?? "prompt", "[^A-Za-z0-9]+", "-")
                    .Trim('-')
                    .ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(slug))
                    slug = "prompt";
                if (slug.Length > 24)
                    slug = slug[..24].Trim('-');

                var suffix = Guid.NewGuid().ToString("N")[..8];
                return $"chat-upload-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{slug}-{suffix}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build an upload-workspace name.");
                return string.Empty;
            }
        }

        /// <summary>Executes the is zip operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsZip(string path, ILogger logger)
        {
            try
            {
                return Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsZip path {path}");
                return false;
            }
        }


        /// <summary>Executes the is text like operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsTextLike(string path, ILogger logger)
        {
            try
            {
                return catalog.TextExtensions.Contains(Path.GetExtension(path));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsTextLike path {path}");
                return false;
            }
        }

        /// <summary>Executes the determine file kind operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string DetermineFileKind(string path, ILogger logger)
        {
            try
            {
                if (IsZip(path, logger))
                    return "zip";
                if (IsTextLike(path, logger))
                    return "text";
                return catalog.BinaryDiagnosticExtensions.Contains(Path.GetExtension(path))
                    ? "binary-diagnostic"
                    : "binary";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error DetermineFileKind path {path}");
                return string.Empty;
            }
        }

        /// <summary>Executes the looks like text operation.</summary>
        /// <param name="bytes">Input value for bytes.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool LooksLikeText(byte[] bytes, ILogger logger)
        {
            try
            {
                if (bytes.Length == 0)
                    return true;

                var sampleLength = Math.Min(bytes.Length, 8192);
                var controlCount = 0;
                for (var i = 0; i < sampleLength; i++)
                {
                    var value = bytes[i];
                    if (value == 0)
                        return false;

                    if (value < 9 || (value > 13 && value < 32))
                        controlCount++;
                }

                return controlCount <= sampleLength / 20;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error LooksLikeText bytes {bytes.ToString()}");
                return false;
            }

        }

    }
}
