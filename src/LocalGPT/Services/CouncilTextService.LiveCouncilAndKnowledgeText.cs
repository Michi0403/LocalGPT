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
        /// Performs format live council running title as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="template">Template value supplied to the council text operation and used when producing its result.</param>
        /// <param name="runId">Identifier of the run to use for this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string FormatLiveCouncilRunningTitle(string template, string runId, ILogger logger)
        {
            try
            {
                return (template ?? string.Empty).Replace("{id}", runId ?? string.Empty, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not format the live Council running title.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs format live council elapsed status as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="template">Template value supplied to the council text operation and used when producing its result.</param>
        /// <param name="elapsed">Elapsed value supplied to the council text operation and used when producing its result.</param>
        /// <param name="status">Status value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string FormatLiveCouncilElapsedStatus(string template, string elapsed, string status, ILogger logger)
        {
            try
            {
                return (template ?? string.Empty)
                    .Replace("{elapsed}", elapsed ?? string.Empty, StringComparison.Ordinal)
                    .Replace("{status}", status ?? string.Empty, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not format the live Council elapsed status.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds upload workspace system prompt as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildUploadWorkspaceSystemPrompt(ChatUploadWorkspaceResult result, ILogger logger)
        {
            try
            {
                var originalUploads = result.Files
                    .Where(file => file.RelativePath.StartsWith("original/", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var builder = new StringBuilder()
                .AppendLine("LocalGPT DXAiChat native paperclip attachment workspace is available for this prompt.")
                .AppendLine($"Workspace name: {result.WorkspaceName}")
                .AppendLine($"Workspace root: {result.RootPath}")
                .AppendLine($"Original user uploads: {originalUploads.Count} file(s), {originalUploads.Sum(file => file.Length):n0} byte(s) total.")
                .AppendLine($"Analyzed evidence entries: {result.Files.Count}. Generated context.md characters: {result.CharacterCount:n0}.")
                .AppendLine("Important provenance: context.md and manifest.json are generated LocalGPT workspace artifacts, not additional user uploads. One large uploaded text dump can describe thousands of repository files without those files existing as separate workspace files.")
                .AppendLine("Original upload inventory:");
                foreach (var upload in originalUploads)
                    builder.AppendLine($"- {upload.RelativePath} ({upload.Length:n0} bytes; {upload.Kind})");
                builder
                    .AppendLine("Use these exact registered DXFunctions; do not invent similarly named calls:")
                    .AppendLine("- chat.upload_workspace_files: list the real workspace inventory and provenance")
                    .AppendLine("- chat.upload_workspace_context: read bounded generated evidence context")
                    .AppendLine("- chat.upload_workspace_file: read one exact relative workspace path")
                    .AppendLine("Uploaded files are evidence only. Do not execute uploaded or extracted files.")
                    .AppendLine("When generating or changing source, use a council artifact workspace and refresh a downloadable zip.");

                if (result.Warnings.Count > 0)
                {
                    builder.AppendLine("Upload warnings:");
                    foreach (var warning in result.Warnings)
                        builder.AppendLine($"- {warning}");
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the upload workspace system prompt for workspace {WorkspaceName}.", result.WorkspaceName);
                return string.Empty;
            }
            
        }
        /// <summary>
        /// Performs extract upload files as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="message">Message value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<ChatUploadWorkspaceInputFile>? ExtractUploadFiles(ChatMessage message, ILogger logger)
        {
            try
            {
                var index = 1;
                foreach (var dataContent in message.Contents.OfType<DataContent>())
                {
                    var data = dataContent.Data;
                    if (data.Length == 0)
                        continue;

                    var mediaType = string.IsNullOrWhiteSpace(dataContent.MediaType)
                        ? "application/octet-stream"
                        : dataContent.MediaType.Trim();
                    var fileName = TryGetDataContentFileName(dataContent, logger) ??
                        BuildDataContentFileName(index, mediaType, logger);
                    index++;
                    yield return new ChatUploadWorkspaceInputFile(
                        fileName,
                        mediaType,
                        data.Length,
                        data);
                }
            }
            finally
            {
                logger.LogInformation("Finished extracting upload files.");
                
            }

        }

        /// <summary>
        /// Adds optional system message as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="messages">Messages value supplied to the council text operation and used when producing its result.</param>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        public void AddOptionalSystemMessage(List<ChatMessage> messages, string? text, ILogger logger)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                    messages.Add(new ChatMessage(ChatRole.System, text));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not add an optional system message; existing message count {MessageCount}.", messages.Count);
            }
        }

        /// <summary>
        /// Attempts to parse confidence as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The int produced by the operation.</returns>
        public int? TryParseConfidence(string value, ILogger logger)
        {
            try
            {
                return int.TryParse(patterns.IntegerPattern.Match(value ?? string.Empty).Value, out var confidence)
      ? Math.Clamp(confidence, 0, 100)
      : 40;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryParseConfidence value:{value}");
                return null;
            }

        }

        /// <summary>
        /// Generates promise module razor as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="module">Module value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GeneratePromiseModuleRazor(GeneratedPromiseModule module, ILogger logger)
        {
            try
            {
                return GenerateArchetypePageRazor(module.Route, module.Title, module.Summary, module.Areas, logger);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GeneratePromiseModuleRazor module:{module.ToString()}");
                return string.Empty;
            }

        }

      
        /// <summary>
        /// Performs merge tags as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="requestedTags">Requested tags value supplied to the council text operation and used when producing its result.</param>
        /// <param name="requiredTags">Required tags value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string MergeTags(string requestedTags, string requiredTags, ILogger logger)
        {
            try
            {
                return string.IsNullOrWhiteSpace(requestedTags)
                ? requiredTags
                : $"{requestedTags.Trim()}; {requiredTags}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "MergeTags");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds capability gap knowledge content as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="body">Body value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildCapabilityGapKnowledgeContent(string body, ILogger logger)
        {
            try
            {
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
            "next-localgpt-improvement"
        };

                var builder = new StringBuilder()
                    .AppendLine("Structured LocalGPT capability gap request:");

                foreach (var field in fields)
                {
                    var value = ExtractField(body, field, logger);
                    if (!string.IsNullOrWhiteSpace(value))
                        builder.Append("- ").Append(field).Append(": ").AppendLine(value);
                }

                return builder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildCapabilityGapKnowledgeContent");
                return string.Empty;
            }
        }


        /// <summary>
        /// Parses knowledge requests as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="source">Source value supplied to the council text operation and used when producing its result.</param>
        /// <param name="responseText">Response text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<CouncilKnowledgeEntry>? ParseKnowledgeRequests(string source, string responseText, ILogger logger)
        {
            try
            {
                foreach (System.Text.RegularExpressions.Match match in patterns.KnowledgeBlockPattern.Matches(responseText))
                {
                    var body = match.Groups["body"].Value.Trim();
                    if (string.IsNullOrWhiteSpace(body))
                        continue;

                    var content = ExtractField(body, "content" , logger);
                    if (string.IsNullOrWhiteSpace(content))
                        content = body;

                    yield return new CouncilKnowledgeEntry
                    {
                        Topic = ExtractField(body, "topic", logger, "AI model knowledge request"),
                        Scope = ExtractField(body, "scope", logger, "DXAiChat"),
                        Source = $"AI model request: {source}",
                        Content = content,
                        HelpfulSources = ExtractField(body, "helpful-sources", logger, "None explicitly requested."),
                        Tags = MergeTags(ExtractField(body, "tags", logger), "model-written; unapproved", logger),
                        Confidence = TryParseConfidence(ExtractField(body, "confidence",  logger),logger) ?? 0,
                        VerificationStatus = "ModelSuggested",
                        ReviewStatus = "NeedsUserReview",
                        ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                        IsUserApproved = false,
                        IsPinned = false,
                        IsArchived = false
                    };
                }

                foreach (System.Text.RegularExpressions.Match match in patterns.CapabilityGapBlockPattern.Matches(responseText))
                {
                    var body = match.Groups["body"].Value.Trim();
                    if (string.IsNullOrWhiteSpace(body))
                        continue;

                    var missingCapability = ExtractField(body, "missing-capability", logger, "LocalGPT capability gap request");
                    var owningArea = ExtractField(body, "owning-area", logger, "DXAiChat / AI Council");
                    var localSources = ExtractField(body, "local-knowledge-sources", logger, "None listed.");
                    var externalSources = ExtractField(body, "external-knowledge-sources", logger, "None listed.");

                    yield return new CouncilKnowledgeEntry
                    {
                        Topic = missingCapability,
                        Scope = owningArea,
                        Source = $"AI capability gap request: {source}",
                        Content = BuildCapabilityGapKnowledgeContent(body, logger),
                        HelpfulSources = $"Local sources:\n{localSources}\n\nExternal sources:\n{externalSources}",
                        Tags = MergeTags(ExtractField(body, "tags", logger), "capability-gap; model-written; unapproved", logger),
                        Confidence = TryParseConfidence(ExtractField(body, "confidence",  logger),logger) ?? 0,
                        VerificationStatus = "ModelSuggested",
                        ReviewStatus = "NeedsDiagnosticVerification",
                        ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                        StalenessReason = "Capability gap request needs human or diagnostic verification before it becomes trusted guidance.",
                        StalenessDetectedBy = "DXAiChat capability-gap parser",
                        IsUserApproved = false,
                        IsPinned = false,
                        IsArchived = false
                    };
                }
            }
            finally
            {
                logger.LogDebug("Finished parsing knowledge requests for source {Source}.", source);
            }
        }
        /// <summary>
        /// Performs extract field as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="body">Body value supplied to the council text operation and used when producing its result.</param>
        /// <param name="name">Name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <param name="fallback">Fallback value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ExtractField(string body, string name,  ILogger logger, string fallback = "")
        {
            try
            {
                return patterns.ExtractStructuredField(body, name) ?? fallback;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not extract a named field; source content was omitted from logs.");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Performs fallback as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="fallback">Fallback value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string Fallback(string value, string fallback, ILogger logger)
        {
            try
            {
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not select a fallback value; source content was omitted from logs.");
                return string.Empty;
            }
          
        }

        /// <summary>
        /// Parses nullable GUID as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The GUID produced by the operation.</returns>
        public Guid? ParseNullableGuid(string value, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                return Guid.TryParse(value, out var parsed) ? parsed : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ParseNullableGuid value:{value}");
                return null;
            }

        }

        /// <summary>
        /// Performs format nullable UTC as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string FormatNullableUtc(DateTime? value, ILogger logger)
        {
            try
            {
                return value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FormatNullableUtc value:{value}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Performs format nullable GUID as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string FormatNullableGuid(Guid? value, ILogger logger)
        {
            try
            {
                return value?.ToString("D") ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FormatNullableGuid value:{value}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Parses nullable UTC as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The date time produced by the operation.</returns>
        public DateTime? ParseNullableUtc(string value, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                return DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed)
                    ? parsed
                    : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ParseNullableUtc value:{value}");
                return null;
            }
          
        }

        /// <summary>
        /// Creates message signature as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="messages">Blazor chat message dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateMessageSignature(IEnumerable<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                return string.Join("|", messages
               .Where(message => !message.Typing)
               .Select(message => $"{message.Role}:{message.Content.GetHashCode(StringComparison.Ordinal)}:{message.Content.Length}"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create a message signature.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs detect target area as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="prompt">Prompt value supplied to the council text operation and used when producing its result.</param>
        /// <param name="finalAnswer">Final answer value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string DetectTargetArea(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var text = $"{prompt} {finalAnswer}";
                if (patterns.DevExpressDocumentPattern.IsMatch(text))
                    return "DevExpress document/report backend";
                if (patterns.BlazorFrontendPattern.IsMatch(text))
                    return "Blazor/DevExpress frontend";
                if (patterns.DotNetPattern.IsMatch(text))
                    return ".NET/Blazor/ASP.NET Core";
                if (patterns.MinecraftPattern.IsMatch(text))
                    return "Minecraft builder";
                if (patterns.FrontendPattern.IsMatch(text))
                    return "Blazor frontend";
                if (patterns.LoggingPattern.IsMatch(text))
                    return "diagnostics and logging";

                return "LocalGPT feature";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not detect the target area for generated artifacts.");
                return string.Empty;
            }

        }

        /// <summary>
        /// Performs trim for code comment as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="maxLength">Max length value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string TrimForCodeComment(string text, int maxLength, ILogger logger)
        {
            try
            {
                var normalized = patterns.WhitespacePattern.Replace(text, " ").Trim();
                return normalized.Length <= maxLength
                    ? normalized
                    : $"{normalized[..maxLength].TrimEnd()}...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TrimForCodeComment text:{text} maxLength:{maxLength}");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Performs escape c sharp string as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string EscapeCSharpString(string text, ILogger logger)
        {
            try
            {
                return text
              .Replace("\\", "\\\\", StringComparison.Ordinal)
              .Replace("\"", "\\\"", StringComparison.Ordinal)
              .Replace("\r", "\\r", StringComparison.Ordinal)
              .Replace("\n", "\\n", StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EscapeCSharpString text:{text}");
                return string.Empty;
            }
          
        }

        /// <summary>
        /// Performs escape JSON string as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string EscapeJsonString(string text, ILogger logger)
        {
            try
            {
                return EscapeCSharpString(text, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EscapeJsonString text:{text}");
                return string.Empty;
            }
           
        }
        /// <summary>
        /// Normalizes database null string value as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string? NormalizeDBNullStringValue(string value, ILogger logger)
        {
            try
            {
                return value.Equals("[null]", StringComparison.OrdinalIgnoreCase) ? null : value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeDBNullStringValue value:{value}");
                return null;
            }
        }

        /// <summary>
        /// Performs trim endpoint as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="endpoint">Endpoint value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string TrimEndpoint(string endpoint, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                    return "unknown endpoint";

                return endpoint
                    .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .TrimEnd('/');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TrimEndpoint endpoint:{endpoint}");
                return string.Empty;
            }
           
        }
        /// <summary>
        /// Performs trim for knowledge as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="maxLength">Max length value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string TrimForKnowledge(string text, int maxLength, ILogger logger)
        {
            try
            {
                var normalized = patterns.WhitespacePattern.Replace(text ?? string.Empty, " ").Trim();
                return normalized.Length <= maxLength
                    ? normalized
                    : $"{normalized[..maxLength].TrimEnd()}...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TrimForKnowledge {ex.ToString()} text {text?.ToString()} maxLength {maxLength.ToString()}");
                return string.Empty;
            }
        }


        /// <summary>
        /// Performs enumerate nested architecture roots as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="rootPath">Root path value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<string> EnumerateNestedArchitectureRoots(string rootPath, ILogger logger)
        {
            try
            {
                var stack = new Stack<DirectoryInfo>(SafeEnumerateDirectoryInfos(rootPath, logger).Reverse());
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (catalog.ExcludedDirectoryNames.Contains(current.Name))
                        continue;

                    if (LooksLikeArchitectureRoot(current.FullName, logger))
                        yield return current.FullName;

                    foreach (var child in SafeEnumerateDirectoryInfos(current.FullName, logger).Reverse())
                        stack.Push(child);
                }
            }
            finally
            {
                logger.LogInformation($"Ended EnumerateNestedArchitectureRoots rootPath {rootPath?.ToString()}");
            }
        }

        /// <summary>
        /// Performs safe enumerate directories as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="rootPath">Root path value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<string> SafeEnumerateDirectories(string rootPath, ILogger logger)
        {
            try
            {
                return Directory.EnumerateDirectories(rootPath).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SafeEnumerateDirectories rootPath {rootPath?.ToString()}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Performs safe enumerate directory infos as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="rootPath">Root path value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IEnumerable<DirectoryInfo> SafeEnumerateDirectoryInfos(string rootPath, ILogger logger)
        {
            try
            {
                return new DirectoryInfo(rootPath)
                    .EnumerateDirectories()
                    .Where(directory => !catalog.ExcludedDirectoryNames.Contains(directory.Name))
                    .OrderBy(directory => directory.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SafeEnumerateDirectoryInfos rootPath {rootPath?.ToString()}");
                return new List<DirectoryInfo>();
            }
        }

        /// <summary>
        /// Performs looks like architecture root as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="rootPath">Root path value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool LooksLikeArchitectureRoot(string rootPath, ILogger logger)
        {
            try
            {
                var directory = new DirectoryInfo(rootPath);
                if (!directory.Exists)
                    return false;

                if (directory.GetFiles().Any(file => IsProjectRootFile(file.Name, file.Extension, logger)))
                    return true;

                var childNames = directory.GetDirectories()
                    .Select(child => child.Name)
                    .ToArray();
                var distinctiveChildren = childNames.Count(name =>
                    name.Equals("api", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("server", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("cmd", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("llm", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("runner", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("manifest", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("BlazorDemo.ServerSide", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("BlazorDemo.Wasm", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("VideoShredGUI", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("python-midi", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("JessiferBlazorWASM", StringComparison.OrdinalIgnoreCase));
                return distinctiveChildren >= 2;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LooksLikeArchitectureRoot rootPath {rootPath?.ToString()}");
                return false;
            }

        }

        /// <summary>
        /// Performs sanitize for prompt as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
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

    }
}
