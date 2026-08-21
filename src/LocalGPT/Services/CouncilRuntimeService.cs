using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
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
    /// Owns deterministic Council prompt reconstruction, source/artifact generation helpers and shared runtime formatting operations.
    /// </summary>
    [DocumentationUpdated("2.1.20")]
    public sealed partial class CouncilRuntimeService
    {
        /// <summary>
        /// Stores the council text service dependency used by <see cref="CouncilRuntimeService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly CouncilTextService text;
        /// <summary>
        /// Stores the local GPT catalog service dependency used by <see cref="CouncilRuntimeService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly LocalGptCatalogService catalog;
        /// <summary>
        /// Stores the logger used by <see cref="CouncilRuntimeService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<CouncilRuntimeService> serviceLogger;

        /// <summary>
        /// Initializes the service with its dependency-injected collaborators.
        /// </summary>
        /// <param name="text">Injected dependency used by the service.</param>
        /// <param name="catalog">Injected dependency used by the service.</param>
        /// <param name="serviceLogger">Injected dependency used by the service.</param>
        public CouncilRuntimeService(
            CouncilTextService text,
            LocalGptCatalogService catalog,
            ILogger<CouncilRuntimeService> serviceLogger)
        {
            this.text = text;
            this.catalog = catalog;
            this.serviceLogger = serviceLogger;
        }

        /// <summary>
        /// Stores the in-memory Ollama models without native tool metadata collection maintained internally by <see cref="CouncilRuntimeService"/> for its current workflow state.
        /// </summary>
        private readonly ConcurrentDictionary<string, byte> ollamaModelsWithoutNativeToolMetadata = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Stores provider-qualified Ollama models that rejected the explicit thinking flag during the current LocalGPT process.
        /// </summary>
        private readonly ConcurrentDictionary<string, byte> ollamaModelsWithoutExplicitThinking = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the current provider-qualified Ollama model already rejected the explicit thinking request flag.
        /// </summary>
        /// <param name="endpoint">Ollama endpoint used by the current request.</param>
        /// <param name="modelName">Ollama model name used by the current request.</param>
        /// <param name="logger">Logger used for bounded compatibility diagnostics.</param>
        /// <returns><see langword="true"/> when later requests should omit the explicit thinking flag.</returns>
        public bool OllamaThinkingChatClientShouldSkipExplicitThinking(Uri? endpoint, string modelName, ILogger logger)
        {
            try
            {
                if (endpoint is null || string.IsNullOrWhiteSpace(modelName))
                    return false;
                var key = $"{endpoint.GetLeftPart(UriPartial.Authority).TrimEnd('/')}|{modelName.Trim()}";
                return ollamaModelsWithoutExplicitThinking.ContainsKey(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not inspect the process-local Ollama thinking compatibility cache.");
                return false;
            }
        }

        /// <summary>
        /// Remembers that a provider-qualified Ollama model rejected the explicit thinking request flag.
        /// </summary>
        /// <param name="endpoint">Ollama endpoint used by the rejected request.</param>
        /// <param name="modelName">Ollama model name used by the rejected request.</param>
        /// <param name="logger">Logger used for bounded compatibility diagnostics.</param>
        public void OllamaThinkingChatClientRememberExplicitThinkingRejected(Uri? endpoint, string modelName, ILogger logger)
        {
            try
            {
                if (endpoint is null || string.IsNullOrWhiteSpace(modelName))
                    return;
                var key = $"{endpoint.GetLeftPart(UriPartial.Authority).TrimEnd('/')}|{modelName.Trim()}";
                ollamaModelsWithoutExplicitThinking[key] = 1;
                logger.LogInformation(
                    "Remembered for this LocalGPT process that Ollama model {Model} at {Endpoint} rejects the explicit thinking flag; later requests will keep working without repeating the compatibility probe.",
                    modelName,
                    endpoint.GetLeftPart(UriPartial.Authority));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not update the process-local Ollama thinking compatibility cache for model {Model}.", modelName);
                throw;
            }
        }

        /// <summary>
        /// Projects provider-specific reasoning and function metadata from a streaming update into durable user-visible chat markup.
        /// </summary>
        /// <param name="update">Provider streaming update to inspect without assuming a provider-specific AIContent implementation.</param>
        /// <param name="logger">Logger used for bounded trace-projection diagnostics.</param>
        /// <returns>Supplemental trace fragments that can be streamed and persisted with the normal chat transcript.</returns>
        public IReadOnlyList<string> BuildUserVisibleProviderTrace(ChatResponseUpdate update, ILogger logger)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(update);
                var traces = new List<string>();
                AppendProviderAdditionalPropertyTraces(update, traces);
                var contentsProperty = update.GetType().GetProperty("Contents");
                if (contentsProperty?.GetValue(update) is not System.Collections.IEnumerable contents)
                    return traces;

                foreach (var content in contents)
                {
                    if (content is null || content is TextContent)
                        continue;

                    AppendProviderAdditionalPropertyTraces(content, traces);
                    var typeName = content.GetType().Name;
                    if (typeName.Contains("FunctionCall", StringComparison.OrdinalIgnoreCase) ||
                        typeName.Contains("ToolCall", StringComparison.OrdinalIgnoreCase))
                    {
                        var name = ReadProviderTraceProperty(content, "Name", "FunctionName", "ToolName") ?? typeName;
                        var callId = ReadProviderTraceProperty(content, "CallId", "Id");
                        var arguments = ReadProviderTraceProperty(content, "Arguments", "Parameters", "Input");
                        var callIdMarkup = string.IsNullOrWhiteSpace(callId)
                            ? string.Empty
                            : $"Call id: <code>{WebUtility.HtmlEncode(callId)}</code>\n\n";
                        var payloadMarkup = FormatUserVisibleCodePayload(
                            string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments,
                            logger);
                        traces.Add($"<details class=\"council-step\" open><summary>Function call · {WebUtility.HtmlEncode(name)}</summary>\n\n{callIdMarkup}{payloadMarkup}\n\n</details>\n\n");
                        continue;
                    }

                    if (typeName.Contains("FunctionResult", StringComparison.OrdinalIgnoreCase) ||
                        typeName.Contains("ToolResult", StringComparison.OrdinalIgnoreCase))
                    {
                        var name = ReadProviderTraceProperty(content, "Name", "FunctionName", "ToolName") ?? typeName;
                        var callId = ReadProviderTraceProperty(content, "CallId", "Id");
                        var result = ReadProviderTraceProperty(content, "Result", "Output", "Value", "Content");
                        var callIdMarkup = string.IsNullOrWhiteSpace(callId)
                            ? string.Empty
                            : $"Call id: <code>{WebUtility.HtmlEncode(callId)}</code>\n\n";
                        var payloadMarkup = FormatUserVisibleCodePayload(
                            string.IsNullOrWhiteSpace(result) ? "(no provider result payload)" : result,
                            logger);
                        traces.Add($"<details class=\"council-step\" open><summary>Function result · {WebUtility.HtmlEncode(name)}</summary>\n\n{callIdMarkup}{payloadMarkup}\n\n</details>\n\n");
                        continue;
                    }

                    if (typeName.Contains("Reasoning", StringComparison.OrdinalIgnoreCase) ||
                        typeName.Contains("Thinking", StringComparison.OrdinalIgnoreCase) ||
                        typeName.Contains("Analysis", StringComparison.OrdinalIgnoreCase))
                    {
                        var reasoning = ReadProviderTraceProperty(content, "Text", "Reasoning", "Thinking", "Analysis", "Content", "Value");
                        if (!string.IsNullOrWhiteSpace(reasoning))
                        {
                            traces.Add($"<details class=\"model-thinking open\" open><summary>Model thinking</summary>\n\n{WebUtility.HtmlEncode(reasoning)}\n\n</details>\n\n");
                        }
                    }
                }

                return traces.Distinct(StringComparer.Ordinal).ToList();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not project provider-specific stream metadata into the user-visible chat trace; the original provider update will continue unchanged.");
                return [];
            }
        }

        /// <summary>
        /// Formats a provider/tool payload as an inert user-visible code block, pretty-printing valid JSON while
        /// keeping Unicode characters readable and preventing payload markup from becoming executable HTML.
        /// </summary>
        /// <param name="payload">Provider or tool payload to render.</param>
        /// <param name="logger">Logger used by JSON normalization diagnostics.</param>
        /// <returns>Controlled HTML containing an encoded code block.</returns>
        public string FormatUserVisibleCodePayload(string? payload, ILogger logger)
        {
            try
            {
                var raw = string.IsNullOrWhiteSpace(payload) ? "(empty payload)" : payload;
                var looksJson = raw.TrimStart().StartsWith('{') || raw.TrimStart().StartsWith('[');
                var formatted = looksJson
                    ? FormatJsonForUserVisibleCode(raw)
                    : text.PrettyPrintJson(raw, logger);
                var languageClass = looksJson ? " class=\"language-json\"" : string.Empty;
                return $"<pre><code{languageClass}>{WebUtility.HtmlEncode(formatted)}</code></pre>";
            }
            catch (Exception ex)
            {
                serviceLogger.LogWarning(ex, "Could not format a provider/tool payload for user-visible code rendering; payload content was omitted from logs.");
                return $"<pre><code>{WebUtility.HtmlEncode(payload ?? string.Empty)}</code></pre>";
            }
        }

        /// <summary>Pretty-prints a JSON payload for an inert user-visible code surface while decoding display-only HTML entities inside JSON string values.</summary>
        /// <param name="raw">Raw JSON payload produced by a provider or LocalGPT function.</param>
        /// <returns>Indented JSON whose Unicode and human text are readable before the final HTML encoding boundary.</returns>
        private string FormatJsonForUserVisibleCode(string raw)
        {
            try
            {
                using var document = JsonDocument.Parse(raw);
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(
                    stream,
                    new JsonWriterOptions
                    {
                        Indented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }))
                {
                    static void WriteElement(Utf8JsonWriter target, JsonElement element)
                    {
                        switch (element.ValueKind)
                        {
                            case JsonValueKind.Object:
                                target.WriteStartObject();
                                foreach (var property in element.EnumerateObject())
                                {
                                    target.WritePropertyName(WebUtility.HtmlDecode(property.Name));
                                    WriteElement(target, property.Value);
                                }
                                target.WriteEndObject();
                                break;
                            case JsonValueKind.Array:
                                target.WriteStartArray();
                                foreach (var item in element.EnumerateArray())
                                    WriteElement(target, item);
                                target.WriteEndArray();
                                break;
                            case JsonValueKind.String:
                                target.WriteStringValue(WebUtility.HtmlDecode(element.GetString() ?? string.Empty));
                                break;
                            default:
                                element.WriteTo(target);
                                break;
                        }
                    }

                    WriteElement(writer, document.RootElement);
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (JsonException exception)
            {
                serviceLogger.LogDebug(exception, "Provider/tool payload was not valid JSON during user-visible code formatting; the original payload will be rendered as inert code.");
                return raw;
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "Formatting provider/tool JSON for the user-visible code surface failed; payload content was omitted from diagnostics.");
                return raw;
            }
        }

        /// <summary>
        /// Projects provider-specific additional-property dictionaries when an SDK keeps reasoning or tool metadata outside typed AI content objects.
        /// </summary>
        /// <param name="owner">Streaming update or content object that may expose an AdditionalProperties collection.</param>
        /// <param name="traces">Destination trace collection for controlled user-visible markup.</param>
        private void AppendProviderAdditionalPropertyTraces(object owner, List<string> traces)
        {
            try
            {
                var additionalProperties = owner.GetType().GetProperty("AdditionalProperties")?.GetValue(owner);
                if (additionalProperties is not System.Collections.IEnumerable items)
                    return;

                foreach (var item in items)
                {
                    if (item is null)
                        continue;
                    var key = item.GetType().GetProperty("Key")?.GetValue(item)?.ToString();
                    var value = item.GetType().GetProperty("Value")?.GetValue(item);
                    if (string.IsNullOrWhiteSpace(key) || value is null)
                        continue;

                    string serialized;
                    try
                    {
                        serialized = value is string textValue ? textValue : JsonSerializer.Serialize(value);
                    }
                    catch
                    {
                        serialized = value.ToString() ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(serialized))
                        continue;

                    if (key.Contains("reasoning", StringComparison.OrdinalIgnoreCase) ||
                        key.Contains("thinking", StringComparison.OrdinalIgnoreCase) ||
                        key.Contains("analysis", StringComparison.OrdinalIgnoreCase))
                    {
                        traces.Add($"<details class=\"model-thinking open\" open><summary>Model thinking · {WebUtility.HtmlEncode(key)}</summary>\n\n{WebUtility.HtmlEncode(serialized)}\n\n</details>\n\n");
                    }
                    else if (key.Contains("tool_call", StringComparison.OrdinalIgnoreCase) ||
                             key.Contains("function_call", StringComparison.OrdinalIgnoreCase))
                    {
                        traces.Add($"<details class=\"council-step\" open><summary>Function call metadata · {WebUtility.HtmlEncode(key)}</summary>\n\n{FormatUserVisibleCodePayload(serialized, serviceLogger)}\n\n</details>\n\n");
                    }
                    else if (key.Contains("tool_result", StringComparison.OrdinalIgnoreCase) ||
                             key.Contains("function_result", StringComparison.OrdinalIgnoreCase))
                    {
                        traces.Add($"<details class=\"council-step\" open><summary>Function result metadata · {WebUtility.HtmlEncode(key)}</summary>\n\n{FormatUserVisibleCodePayload(serialized, serviceLogger)}\n\n</details>\n\n");
                    }
                }
            }
            catch (Exception ex)
            {
                serviceLogger.LogWarning(ex, "Could not inspect provider additional properties for user-visible reasoning/function metadata.");
            }
        }

        /// <summary>
        /// Reads the first available provider metadata property from an opaque AI content object.
        /// </summary>
        /// <param name="content">Opaque provider content object.</param>
        /// <param name="propertyNames">Candidate property names ordered by preference.</param>
        /// <returns>A bounded display representation, or <see langword="null"/> when no value is available.</returns>
        private string? ReadProviderTraceProperty(object content, params string[] propertyNames)
        {
            try
            {
                foreach (var propertyName in propertyNames)
                {
                    var property = content.GetType().GetProperty(propertyName);
                    if (property is null)
                        continue;
                    var value = property.GetValue(content);
                    if (value is null)
                        continue;
                    if (value is string textValue)
                        return textValue;
                    try
                    {
                        return JsonSerializer.Serialize(value);
                    }
                    catch
                    {
                        return value.ToString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                serviceLogger.LogWarning(ex, "Could not read provider trace metadata property from content type {ContentType}.", content.GetType().FullName);
                return null;
            }
        }

        /// <summary>
        /// Performs Ollama thinking chat client should skip native tools as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="endpoint">Endpoint value supplied to the council runtime operation and used when producing its result.</param>
        /// <param name="modelName">Model name value supplied to the council runtime operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool OllamaThinkingChatClientShouldSkipNativeTools(Uri? endpoint, string modelName, ILogger logger)
        {
            try
            {
                if (endpoint is null || string.IsNullOrWhiteSpace(modelName))
                    return false;
                var key = $"{endpoint.GetLeftPart(UriPartial.Authority).TrimEnd('/')}|{modelName.Trim()}";
                return ollamaModelsWithoutNativeToolMetadata.ContainsKey(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not inspect the process-local Ollama native-tool compatibility cache.");
                return false;
            }
        }

        /// <summary>
        /// Performs Ollama thinking chat client remember native tools rejected as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="endpoint">Endpoint value supplied to the council runtime operation and used when producing its result.</param>
        /// <param name="modelName">Model name value supplied to the council runtime operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        public void OllamaThinkingChatClientRememberNativeToolsRejected(Uri? endpoint, string modelName, ILogger logger)
        {
            try
            {
                if (endpoint is null || string.IsNullOrWhiteSpace(modelName))
                    return;
                var key = $"{endpoint.GetLeftPart(UriPartial.Authority).TrimEnd('/')}|{modelName.Trim()}";
                ollamaModelsWithoutNativeToolMetadata[key] = 1;
                logger.LogInformation(
                    "Remembered for this LocalGPT process that Ollama model {Model} at {Endpoint} rejects native tool metadata; later requests will skip the known-incompatible metadata instead of repeating HTTP 400/501 probing.",
                    modelName,
                    endpoint.GetLeftPart(UriPartial.Authority));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not update the process-local Ollama native-tool compatibility cache for model {Model}.", modelName);
                throw;
            }
        }
        /// <summary>Executes the find repository root operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        private string? FindRepositoryRoot(ILogger<BuildDebugInventoryService> logger)
        {
            try
            {
                foreach (var start in new[]
 {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
                {
                    var directory = new DirectoryInfo(start);
                    while (directory is not null)
                    {
                        if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                            Directory.Exists(Path.Combine(directory.FullName, ".git")))
                        {
                            return directory.FullName;
                        }

                        directory = directory.Parent;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FindRepositoryRoot");
                return string.Empty;
            }
        }
        /// <summary>Executes the build manual expected lane operation.</summary>
        /// <param name="task">Input value for task.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public EngineeringBenchmarkLaneResult BuildManualExpectedLane(BenchmarkTaskDefinition task, ILogger logger)
        {
            try
            {
                var lane = new EngineeringBenchmarkLaneResult
                {
                    Lane = "D. manual expected output",
                    Status = "Reference",
                    ValidArchitectureScore = 10,
                    BuildabilityScore = 10,
                    MissingFilesScore = 10,
                    WrongPackagesTemplatesScore = 10,
                    TimeToUsableOutputScore = 0,
                    RepairPromptsScore = 10,
                    DownloadableArtifactScore = 0,
                    RepairPromptCount = 0,
                    Notes = task.ManualExpectedOutput
                };
                lane.TotalScore = SumScores(lane, logger);
                return lane;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the manual benchmark reference lane for task {TaskId}.", task?.Id);
                return new EngineeringBenchmarkLaneResult
                {
                    Lane = "D. manual expected output",
                    Status = "Error",
                    Notes = "The manual reference lane could not be prepared. Review LocalGPT logs."
                };
            }
        }
        /// <summary>Executes the not run lane operation.</summary>
        /// <param name="laneName">Input value for laneName.</param>
        /// <param name="notes">Input value for notes.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public EngineeringBenchmarkLaneResult NotRunLane(string laneName, string notes, ILogger logger)
        {
            try
            {
                return new EngineeringBenchmarkLaneResult
                {
                    Lane = laneName,
                    Status = "NotRun",
                    Notes = notes
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create benchmark lane {LaneName}.", laneName);
                return new EngineeringBenchmarkLaneResult
                {
                    Lane = string.IsNullOrWhiteSpace(laneName) ? "Unnamed benchmark lane" : laneName,
                    Status = "Error",
                    Notes = string.IsNullOrWhiteSpace(notes) ? "The benchmark lane could not be prepared." : notes
                };
            }
        }
        /// <summary>Executes the score architecture operation.</summary>
        /// <param name="task">Input value for task.</param>
        /// <param name="zipEntries">Input value for zipEntries.</param>
        /// <param name="artifacts">Input value for artifacts.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int ScoreArchitecture(
    BenchmarkTaskDefinition task,
    HashSet<string> zipEntries,
    IReadOnlyList<CouncilArtifact> artifacts, ILogger logger)
        {
            try
            {
                if (artifacts.Count == 0)
                    return 0;

                var hits = task.ArchitectureEvidence.Count(evidence =>
                    zipEntries.Any(entry => entry.Contains(evidence, StringComparison.OrdinalIgnoreCase)) ||
                    artifacts.Any(artifact => artifact.Summary.Contains(evidence, StringComparison.OrdinalIgnoreCase) ||
                        artifact.Kind.Contains(evidence, StringComparison.OrdinalIgnoreCase)));

                return task.ArchitectureEvidence.Count == 0
                    ? 7
                    : Math.Clamp(4 + hits * 2, 0, 10);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ScoreArchitecture task {task.ToString()} zipEntries {zipEntries.ToString()} artifacts {artifacts.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the score wrong template risk operation.</summary>
        /// <param name="task">Input value for task.</param>
        /// <param name="zipEntries">Input value for zipEntries.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int ScoreWrongTemplateRisk(BenchmarkTaskDefinition task, HashSet<string> zipEntries, ILogger logger)
        {
            try
            {
                if (task.WrongTemplateGuards.Count == 0)
                    return 8;

                var guardHits = task.WrongTemplateGuards.Count(guard => text.ContainsZipEntry(zipEntries, guard, logger));
                return guardHits == task.WrongTemplateGuards.Count ? 10 : Math.Max(0, 10 - (task.WrongTemplateGuards.Count - guardHits) * 3);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ScoreWrongTemplateRisk task {task.ToString()} zipEntries {zipEntries.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the score buildability operation.</summary>
        /// <param name="task">Input value for task.</param>
        /// <param name="artifacts">Input value for artifacts.</param>
        /// <param name="buildChecks">Input value for buildChecks.</param>
        /// <param name="validateBuildableArtifacts">Input value for validateBuildableArtifacts.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int ScoreBuildability(
      BenchmarkTaskDefinition task,
      IReadOnlyList<CouncilArtifact> artifacts,
      IReadOnlyList<EngineeringBenchmarkBuildCheck> buildChecks,
      bool validateBuildableArtifacts, ILogger logger)
        {
            try
            {
                if (artifacts.Count == 0)
                    return 0;

                if (!validateBuildableArtifacts)
                    return task.LocalGptBuildabilityScore;

                var dotnetChecks = buildChecks
                    .Where(check => !check.Status.Equals("NoSolution", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (dotnetChecks.Length == 0)
                    return task.LocalGptBuildabilityScore;

                return dotnetChecks.All(check => check.Status.Equals("BuildPassed", StringComparison.OrdinalIgnoreCase))
                    ? 10
                    : 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ScoreBuildability task {task.ToString()} artifacts {artifacts.ToString()} buildChecks {buildChecks.ToString()} validateBuildableArtifacts {validateBuildableArtifacts.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the read zip entries safe operation.</summary>
        /// <param name="artifact">Input value for artifact.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<string> ReadZipEntriesSafe(CouncilArtifact artifact, ILogger logger)
        {

            if (!File.Exists(artifact.FilePath))
                return [];

            try
            {
                using var archive = ZipFile.OpenRead(artifact.FilePath);
                return archive.Entries
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ReadZipEntriesSafe artifact {artifact.ToString()}");
                return new List<string>();
            }
        }
        /// <summary>Executes the sum scores operation.</summary>
        /// <param name="lane">Input value for lane.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int SumScores(EngineeringBenchmarkLaneResult lane, ILogger logger)
        {
            try
            {
                return lane.ValidArchitectureScore +
                lane.BuildabilityScore +
                lane.MissingFilesScore +
                lane.WrongPackagesTemplatesScore +
                lane.TimeToUsableOutputScore +
                lane.RepairPromptsScore +
                lane.DownloadableArtifactScore;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SumScores lane {lane.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the read small text operation.</summary>
        /// <param name="file">Input value for file.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string ReadSmallText(FileInfo file, ILogger logger)
        {
            try
            {
                return System.IO.File.ReadAllText(file.FullName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadSmallText file {file?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the add if operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="signals">Input value for signals.</param>
        /// <param name="label">Input value for label.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="needles">Input value for needles.</param>
        public void AddIf(string text, List<string> signals, string label, ILogger logger, params string[] needles)
        {
            try
            {
                if (needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase)))
                    signals.Add(label);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddIf text {text?.ToString()} signals {signals?.ToString()} label {label?.ToString()} needles {needles?.ToString()}");

            }
        }
        /// <summary>Executes the build windows dev docs entries operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="markdownFiles">Input value for markdownFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<CouncilKnowledgeEntry> BuildWindowsDevDocsEntries(
            string rootPath,
            IReadOnlyList<FileInfo> markdownFiles, ILogger logger)
        {
            try
            {
                var now = DateTime.UtcNow;
                var docfxSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, logger, "docfx", "metadata", "toc", "index", "authoring");
                var platformSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, logger, "windows-app-sdk", "winui", "webview2", "msix", "desktop");
                var supportSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, logger, "developer-mode", "dev-drive", "winget", "terminal", "arm64");
                var designSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, logger, "design", "accessibility", "navigation", "layout", "typography");
                var frontMatterCount = markdownFiles
                    .Take(800)
                    .Select(filter => ReadSmallText(filter, logger))
                    .Count(text => text.TrimStart().StartsWith("---", StringComparison.Ordinal));

                return StampSourceMetadata(
                    rootPath,
                    markdownFiles,
                    now,
                [
                    new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|docfx|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Windows developer docs DocFX and Microsoft Learn authoring",
                    Scope = "DocFX / developer documentation",
                    Source = "Local learn-base docs corpus: windows-dev-docs-docs",
                    Content = "The local Windows developer docs corpus uses Microsoft Learn/DocFX-style Markdown. " +
                        "Generation should preserve normal physical line breaks, front matter, title/description metadata, ms.topic/ms.date fields, relative links, includes, image references, and table/list readability. " +
                        "For docfx generation, produce docs that can be indexed by topic, source file, service boundary, build command, troubleshooting case, and related API/platform area. " +
                        "Do not paste full docs into prompts; summarize source maps and let LocalGPT retrieve narrow entries. " +
                        $"Sampled {markdownFiles.Count} markdown files; {frontMatterCount} of the first 800 looked like front-matter pages.",
                    HelpfulSources = docfxSamples,
                    Tags = "learn-base; windows-dev-docs; docfx; microsoft-learn; markdown; documentation; source-backed",
                    Confidence = 88,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|platform|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Windows app platform source map for LocalGPT generation",
                    Scope = "Windows app development",
                    Source = "Local learn-base docs corpus: windows-dev-docs-docs",
                    Content = "When generating Windows-capable .NET apps, use the Windows docs corpus as a compact source map for Windows App SDK, WinUI, WebView2, MSIX/package deployment, app lifecycle, desktop integration, and Windows desktop support boundaries. " +
                        "For LocalGPT-style apps, keep WebView2 wrappers thin, own Blazor/ASP.NET Core work in the backend, and document static assets, package/runtime dependencies, and deploy/debug differences. " +
                        "Generated projects should include health routes, package diagnostics, build/run docs, and clear user-facing setup checks.",
                    HelpfulSources = platformSamples,
                    Tags = "learn-base; windows; winui; windowsappsdk; webview2; msix; deployment; desktop; source-backed",
                    Confidence = 88,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|support|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Windows technician workflow for developer support",
                    Scope = "Windows support / operations",
                    Source = "Local learn-base docs corpus: windows-dev-docs-docs",
                    Content = "Use the Windows docs corpus to support developer-machine setup and troubleshooting: Developer Mode, Device Portal/discovery, winget, Windows Terminal, Dev Drive, PowerToys, Visual Studio/SDK/runtime checks, Arm64/Arm64EC/Arm64X compatibility, package logs, event logs, certificates, and deployment diagnostics. " +
                        "LocalGPT should present these as guided checks and repair scripts, not as vague advice. Mark actions that need admin rights, downloads, or package changes before running them.",
                    HelpfulSources = supportSamples,
                    Tags = "learn-base; windows-support; winget; terminal; dev-drive; arm64; diagnostics; technician; source-backed",
                    Confidence = 86,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|design|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Windows design and accessibility guidance for generated Blazor apps",
                    Scope = "Frontend design / accessibility",
                    Source = "Local learn-base docs corpus: windows-dev-docs-docs",
                    Content = "Use Windows design guidance as a source-backed supplement for generated Blazor/DevExpress apps: navigation clarity, command placement, typography, layout, iconography, accessibility, keyboard focus, density, status messages, and responsive behavior. " +
                        "Generated apps should be understandable without long instructional text, while still surfacing setup state, loading state, errors, empty states, and next actions.",
                    HelpfulSources = designSamples,
                    Tags = "learn-base; windows-design; accessibility; blazor; devexpress; ux; source-backed",
                    Confidence = 86,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                }
                ], logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildWindowsDevDocsEntries rootPath {rootPath?.ToString()} markdownFiles {markdownFiles?.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }

        }
        /// <summary>Executes the build dot net docs entries operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="markdownFiles">Input value for markdownFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<CouncilKnowledgeEntry> BuildDotNetDocsEntries(
     string rootPath,
     IReadOnlyList<FileInfo> markdownFiles, ILogger logger)
        {
            try
            {
                var now = DateTime.UtcNow;
                var docfxSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "docfx", "toc", "index", "includes", "samples");
                var architectureSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "architecture", "microservices", "cloud-native", "modern-web-apps");
                var csharpSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "csharp", "language-reference", "compiler", "csharp-12", "language-versioning");
                var webSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "aspnet", "blazor", "web-api", "minimal-api", "dependency-injection");
                var dataSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "entity-framework", "ef-core", "linq", "data", "serialization");
                var frontMatterCount = markdownFiles
                    .Take(1000)
                    .Select(filter => ReadSmallText(filter, logger))
                    .Count(text => text.TrimStart().StartsWith("---", StringComparison.Ordinal));

                return StampSourceMetadata(
                    rootPath,
                    markdownFiles,
                    now,
                [
                    new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|docfx|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Microsoft .NET docs corpus source map",
                    Scope = ".NET / Microsoft Learn / DocFX",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "The local Microsoft .NET docs corpus is a source-backed map for .NET, C#, compiler diagnostics, architecture, libraries, samples, and Microsoft Learn authoring. " +
                        "Do not paste the full corpus into model prompts. Store concise source maps in SQLite, retrieve narrow entries, and inspect exact files only when a generation task needs exact syntax or version detail. " +
                        "Generated docs should preserve front matter, relative links, includes, readable tables/lists, normal physical line breaks, and topic/source metadata. " +
                        $"Sampled {markdownFiles.Count} markdown files; {frontMatterCount} of the first 1000 looked like front-matter pages.",
                    HelpfulSources = docfxSamples,
                    Tags = "learn-base; dotnet-docs; microsoft-learn; docfx; markdown; source-backed",
                    Confidence = 90,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|architecture|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Modern .NET architecture guidance for generated solutions",
                    Scope = ".NET architecture / application generation",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "Use the Microsoft .NET architecture docs as the source map for generated enterprise solutions: layered or modular monoliths, microservices only when useful, cloud-native boundaries, service ownership, DI/options/logging/configuration, background services, API contracts, resiliency, data access, tests, deployment, and documentation. " +
                        "When a prompt asks for a whole app, generate projects, services, models, routes/pages, persistence, tests or smoke paths, README, and downloadable artifacts instead of only pages.",
                    HelpfulSources = architectureSamples,
                    Tags = "learn-base; dotnet; architecture; microservices; modular-monolith; aspnetcore; source-backed",
                    Confidence = 90,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|csharp-compiler|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "C# language and compiler source map for LocalGPT code generation",
                    Scope = "C# / compiler diagnostics / language versions",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "Use the C# language reference, language-version docs, compiler options, compiler messages, nullable reference type guidance, pattern matching, records, required members, primary constructors, collection expressions, interceptors where version-supported, generics, LINQ, async, attributes, XML docs, source generators, and analyzers as source-backed input for code generation and repair. " +
                        "If the requested feature depends on C# 12 or newer syntax, verify the target SDK/langversion and emit buildable code for that version instead of guessing. Compiler diagnostics win over model confidence.",
                    HelpfulSources = csharpSamples,
                    Tags = "learn-base; csharp; csharp12; compiler; diagnostics; langversion; roslyn; source-backed",
                    Confidence = 91,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|web-blazor|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "ASP.NET Core and Blazor source map for generated web apps",
                    Scope = "ASP.NET Core / Blazor",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "Use Microsoft docs as a source map for ASP.NET Core hosting, minimal APIs/controllers, routing, middleware, configuration, DI, authentication/authorization, SignalR where relevant, Blazor component rendering modes, forms, validation, static assets, file uploads/downloads, testing, publishing, and diagnostics. " +
                        "For LocalGPT-style apps, generate backend services and safe HTTP download routes for generated files; Blazor pages should present state, controls, and validation rather than owning privileged execution.",
                    HelpfulSources = webSamples,
                    Tags = "learn-base; aspnetcore; blazor; minimal-api; middleware; static-assets; source-backed",
                    Confidence = 88,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|data|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = ".NET data, LINQ, serialization, and EF source map",
                    Scope = ".NET data access / EF / serialization",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "Use Microsoft docs as a source map for LINQ, serialization, configuration binding, EF-style data modeling when present, migrations/schema evolution, nullable columns for populated databases, DTOs, validation, and database-backed application state. " +
                        "When DevExpress Web API/XAF/OData compatibility is requested, combine this with LocalGPT's DevExpress business-object guidance instead of inventing shadow properties or ambiguous relationships.",
                    HelpfulSources = dataSamples,
                    Tags = "learn-base; dotnet-data; linq; serialization; efcore; database; source-backed",
                    Confidence = 87,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                }
                ], logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDotNetDocsEntries rootPath {rootPath?.ToString()} markdownFiles {markdownFiles?.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }
        }
        /// <summary>Executes the to knowledge entry operation.</summary>
        /// <param name="summary">Input value for summary.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public CouncilKnowledgeEntry? ToKnowledgeEntry(LearnBaseProjectSummary summary, ILogger logger)
        {
            try
            {
                var sanitizedSourcePath = text.RedactSensitiveName(summary.SourcePath, logger);
                var now = DateTime.UtcNow;
                var content = new StringBuilder()
                    .AppendLine("This entry is about reusable architecture and wiring patterns, not about copying names or branding.")
                    .AppendLine("Learn the functionality, protocols, service boundaries, host wiring, and component usage. Treat source labels as evidence labels, not as target product names.")
                    .AppendLine($"Architecture fingerprint source label: {summary.Name}")
                    .AppendLine($"Sanitized source path label: {sanitizedSourcePath}")
                    .AppendLine($"Architecture signals: {summary.Architecture}")
                    .AppendLine($"Protocols/components: {summary.ProtocolsAndComponents}")
                    .AppendLine($"Target frameworks: {text.Fallback(summary.TargetFrameworks, "none detected", logger)}")
                    .AppendLine($"Package references: {text.Fallback(summary.PackageReferences, "none detected", logger)}")
                    .AppendLine($"Important files: {summary.ImportantFiles}")
                    .AppendLine($"Source files counted: {summary.SourceFileCount}; binary/build artifacts counted but not stored: {summary.BinaryFileCount}.")
                    .AppendLine("Generation guidance: learn host shapes, protocols, libraries, service boundaries, and solution setup. Do not preserve project names unless the user explicitly asks.")
                    .AppendLine("Ask for a poll when the user has not selected monolith vs microservice, Blazor vs non-Blazor frontend, DevExpress Web API/security, Python interop, or data persistence style.")
                    .AppendLine("Legacy offensive names are sanitized in knowledge records; preserve the technical pattern, not the wording.")
                    .ToString();

                return new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"learn-base|{summary.SourcePath}", logger),
                    Topic = $"Selected learn-base architecture fingerprint: {summary.Architecture}",
                    Scope = "Selected local project learn-base",
                    Source = $"Local learn-base scan: {sanitizedSourcePath}",
                    Content = content,
                    HelpfulSources = "The user-selected local learn-base source folder. Import stores compact fingerprints only; inspect the selected source directly before copying exact code. Legacy offensive names are sanitized before teaching.",
                    Tags = BuildTags(summary, logger),
                    Confidence = 78,
                    VerificationStatus = "SourceBacked",
                    ReviewStatus = "NeedsUserReview",
                    ExpiresAtUtc = now.AddDays(180),
                    LastVerifiedAtUtc = now,
                    SourceDateUtc = TryGetLatestSourceDateUtc(summary.SourcePath, logger),
                    SourceHash = ComputeSummaryHash(summary, logger),
                    StalenessReason = "Local project fingerprints should be approved or corrected by the user before being treated as durable generation guidance.",
                    StalenessDetectedBy = "Learn-base importer",
                    IsUserApproved = false,
                    IsPinned = summary.Name.Contains("Tacos", StringComparison.OrdinalIgnoreCase) ||
                        summary.Name.Contains("DevExpress", StringComparison.OrdinalIgnoreCase) ||
                        summary.Name.Contains("Jezzifa", StringComparison.OrdinalIgnoreCase)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToKnowledgeEntry summary {summary?.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the build windows docs path samples operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="markdownFiles">Input value for markdownFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="needles">Input value for needles.</param>
        /// <returns>The operation result.</returns>
        public string BuildWindowsDocsPathSamples(
            string rootPath,
            IReadOnlyList<FileInfo> markdownFiles,
            ILogger logger,
            params string[] needles)
        {
            try
            {
                var samples = BuildDocsPathSamples(rootPath, markdownFiles, logger, needles);
                if (!samples.StartsWith("No direct sample paths matched", StringComparison.OrdinalIgnoreCase))
                    return samples;

                return string.Join(
                    "\n",
                    markdownFiles
                        .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                        .Take(16)
                        .Select(file => "- " + text.RedactSensitiveName(Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/'), logger)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildWindowsDocsPathSamples rootPath {rootPath?.ToString()} markdownFiles {markdownFiles?.ToString()} needles {needles?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the stamp source metadata operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="files">Input value for files.</param>
        /// <param name="now">Input value for now.</param>
        /// <param name="entries">Input value for entries.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<CouncilKnowledgeEntry> StampSourceMetadata(
    string rootPath,
    IReadOnlyList<FileInfo> files,
    DateTime now,
    IReadOnlyList<CouncilKnowledgeEntry> entries, ILogger logger)
        {
            try
            {
                var sourceDateUtc = files.Count == 0
               ? Directory.GetLastWriteTimeUtc(rootPath)
               : files.Max(file => file.LastWriteTimeUtc);
                var corpusHash = ComputeCorpusHash(rootPath, files, logger);
                foreach (var entry in entries)
                {
                    entry.ReviewStatus = "Current";
                    entry.LastVerifiedAtUtc = now;
                    entry.SourceDateUtc = sourceDateUtc;
                    entry.SourceHash = corpusHash;
                }

                return entries;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in StampSourceMetadata rootPath {rootPath?.ToString()} files {files?.ToString()} now {now.ToString()} entries {entries?.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }
        }
        /// <summary>Executes the enumerate documentation corpus candidates operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
        public IEnumerable<string> EnumerateDocumentationCorpusCandidates(string rootPath, ILogger logger)
        {
            try
            {
                yield return rootPath;

                foreach (var child in SafeEnumerateDirectories(rootPath, logger))
                {
                    yield return child;

                    foreach (var grandChild in SafeEnumerateDirectories(child, logger))
                    {
                        var name = Path.GetFileName(grandChild);
                        if (name.Contains("docs", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("learn", StringComparison.OrdinalIgnoreCase))
                        {
                            yield return grandChild;
                        }
                    }
                }
            }
            finally
            {
                logger.LogInformation($"Ended EnumerateDocumentationCorpusCandidates rootPath {rootPath?.ToString()}");

            }
        }
}
}
