using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Formatting;

/// <summary>
/// Converts bounded, syntactically valid JSON blocks into a readable Markdown tree while preserving
/// an encoded raw representation. Candidate detection uses the same canonical regex definitions that
/// are seeded into LocalGPT's database-backed regex catalog; JSON syntax is validated by System.Text.Json.
/// </summary>
public sealed class StructuredTextTranslationService : IStructuredTextTranslationService
{
    /// <summary>
    /// Defines the JSON fence pattern name constant used by <see cref="StructuredTextTranslationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string JsonFencePatternName = "builtin.json-fence-pattern";
    /// <summary>
    /// Defines the JSON plain start pattern name constant used by <see cref="StructuredTextTranslationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string JsonPlainStartPatternName = "builtin.json-plain-start-pattern";
    /// <summary>
    /// Defines the JSON protected block pattern name constant used by <see cref="StructuredTextTranslationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string JsonProtectedBlockPatternName = "builtin.json-protected-block-pattern";
    /// <summary>
    /// Defines the JSON key token pattern name constant used by <see cref="StructuredTextTranslationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string JsonKeyTokenPatternName = "builtin.json-key-token-pattern";
    /// <summary>
    /// Defines the JSON scalar pattern name constant used by <see cref="StructuredTextTranslationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string JsonScalarPatternName = "builtin.json-scalar-pattern";
    /// <summary>Legacy catalog identifier retained for stored regex compatibility; fixed assessment protocol tags now use deterministic scanning.</summary>
    public const string SelfAssessmentBlockPatternName = "builtin.localgpt-self-assessment-block-pattern";

    /// <summary>
    /// Defines the maximum depth constant used by <see cref="StructuredTextTranslationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaximumDepth = 32;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to fenced block regex state owned by <see cref="StructuredTextTranslationService"/>.
    /// </summary>
    private readonly Regex fencedBlockRegex;
    /// <summary>
    /// Stores the internal plain start regex state used by <see cref="StructuredTextTranslationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex plainStartRegex;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to protected block regex state owned by <see cref="StructuredTextTranslationService"/>.
    /// </summary>
    private readonly Regex protectedBlockRegex;
    /// <summary>
    /// Stores the internal key token regex state used by <see cref="StructuredTextTranslationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex keyTokenRegex;
    /// <summary>Number of invalid assessment payloads observed; diagnostics are sampled so streaming snapshots cannot flood logs.</summary>
    private long invalidAssessmentCount;
    /// <summary>Only fingerprints, never private payload text, are retained to suppress repeat parse exceptions across streamed snapshots.</summary>
    private readonly HashSet<(ulong Fingerprint, int Length)> rejectedAssessmentJson = new();
    /// <summary>Protects the bounded negative cache from concurrent model/rendering threads.</summary>
    private readonly object rejectedAssessmentJsonGate = new();
    /// <summary>
    /// Stores the logger used by <see cref="StructuredTextTranslationService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<StructuredTextTranslationService> logger;
    /// <summary>
    /// Stores the regex pattern service dependency used by <see cref="StructuredTextTranslationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IRegexPatternService regexPatternService;
    /// <summary>Database-backed operator runtime policy for structured-text capacity.</summary>
    private readonly ILocalGptRuntimePolicyDataService runtimePolicy;
    /// <summary>
    /// Stores the internal document options state used by <see cref="StructuredTextTranslationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonDocumentOptions documentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = MaximumDepth
    };

    /// <summary>
    /// Initializes a new <see cref="StructuredTextTranslationService"/> instance and captures the dependencies or initial state required by its structured text translation workflow.
    /// </summary>
    /// <param name="initialDataCatalog">Initial data catalog dependency used by the structured text translation workflow to provide the corresponding application capability.</param>
    /// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the structured text translation workflow to provide the corresponding application capability.</param>
    /// <param name="regexPatterns">Regex pattern service dependency used by the structured text translation workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public StructuredTextTranslationService(
        IInitialDataCatalog initialDataCatalog,
        ILocalGptRuntimePolicyDataService runtimePolicy,
        IRegexPatternService regexPatterns,
        ILogger<StructuredTextTranslationService> logger)
    {
        this.logger = logger;
        this.runtimePolicy = runtimePolicy;
        regexPatternService = regexPatterns;
        fencedBlockRegex = CreateCatalogRegex(
            initialDataCatalog.RegexPatterns,
            JsonFencePatternName,
            runtimePolicy.GetPattern(LocalGptRuntimePattern.StructuredJsonFence).ToString(),
            "IgnoreCase|Singleline|Compiled|CultureInvariant",
            runtimePolicy.RegexTimeout);
        plainStartRegex = CreateCatalogRegex(
            initialDataCatalog.RegexPatterns,
            JsonPlainStartPatternName,
            runtimePolicy.GetPattern(LocalGptRuntimePattern.StructuredJsonStart).ToString(),
            "Multiline|Compiled|CultureInvariant",
            runtimePolicy.RegexTimeout);
        protectedBlockRegex = CreateCatalogRegex(
            initialDataCatalog.RegexPatterns,
            JsonProtectedBlockPatternName,
            runtimePolicy.GetPattern(LocalGptRuntimePattern.StructuredProtectedMarkup).ToString(),
            "IgnoreCase|Singleline|Compiled|CultureInvariant",
            runtimePolicy.RegexTimeout);
        keyTokenRegex = CreateCatalogRegex(
            initialDataCatalog.RegexPatterns,
            JsonKeyTokenPatternName,
            runtimePolicy.GetPattern(LocalGptRuntimePattern.StructuredIdentifierWordBoundary).ToString(),
            "CultureInvariant|Compiled",
            runtimePolicy.RegexTimeout);

        // Assessment envelopes are protocol tags, not an unbounded regex over a live stream.
        // The legacy catalog key stays available for persisted data compatibility, but the
        // streaming renderer resolves these fixed tags with a linear scan below.
    }

    /// <summary>
    /// Performs translate JSON as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The structured JSON translation result produced by the operation.</returns>
    public StructuredJsonTranslationResult TranslateJson(StructuredJsonTranslationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var text = request.Text ?? string.Empty;
        var result = new StructuredJsonTranslationResult
        {
            Status = "NoJson",
            TranslatedText = text
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            result.Warnings.Add("No text was supplied.");
            return result;
        }

        if (text.Length > Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.StructuredTextMaximumInputCharacters)))
        {
            result.Warnings.Add($"Input was truncated to {Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.StructuredTextMaximumInputCharacters)):n0} characters before structured JSON inspection.");
            text = text[..Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.StructuredTextMaximumInputCharacters))];
        }

        var maximumDocuments = Math.Max(1, request.MaximumDocuments);
        var candidates = FindJsonCandidates(text, maximumDocuments);
        if (candidates.Count == 0)
        {
            result.TranslatedText = text;
            result.Warnings.Add("No complete standalone JSON object or array was detected.");
            return result;
        }

        var replacements = new List<(int Start, int Length, string Replacement)>();
        foreach (var candidate in candidates)
        {
            if (candidate.Length > Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.StructuredTextMaximumJsonDocumentCharacters)))
            {
                result.Warnings.Add($"Skipped a JSON candidate longer than {Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.StructuredTextMaximumJsonDocumentCharacters)):n0} characters.");
                continue;
            }

            if (!LooksLikeCompleteAssessmentJson(candidate.Json))
                continue;
            try
            {
                using var document = JsonDocument.Parse(candidate.Json, documentOptions);
                if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                    continue;

                var markdown = RenderElement(document.RootElement, 0, null);
                var normalizedJson = JsonSerializer.Serialize(
                    document.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                var translatedBlock = BuildTranslatedBlock(markdown, normalizedJson, request.IncludeRawJson);
                var index = result.Documents.Count + 1;
                result.Documents.Add(new StructuredJsonDocument
                {
                    Index = index,
                    RootKind = document.RootElement.ValueKind.ToString(),
                    Markdown = markdown,
                    NormalizedJson = normalizedJson,
                    StartIndex = candidate.Start,
                    Length = candidate.Length
                });
                replacements.Add((candidate.Start, candidate.Length, translatedBlock));
            }
            catch (JsonException)
            {
                // A balanced brace sequence may still be ordinary prose or an incomplete model frame.
                // It is deliberately left untouched and is not logged with content.
            }
        }

        if (replacements.Count == 0)
        {
            result.TranslatedText = text;
            result.Warnings.Add("Candidate blocks were found, but none contained valid JSON objects or arrays.");
            return result;
        }

        var builder = new StringBuilder(text);
        foreach (var replacement in replacements.OrderByDescending(item => item.Start))
        {
            builder.Remove(replacement.Start, replacement.Length);
            builder.Insert(replacement.Start, replacement.Replacement);
        }

        result.Succeeded = true;
        result.Status = "Translated";
        result.TranslatedText = builder.ToString();
        logger.LogDebug("Translated {DocumentCount} standalone JSON document(s) into readable chat structure.", result.Documents.Count);
        return result;
    }

    /// <summary>
    /// Converts LocalGPT self-assessment tagged JSON into controlled structured/code markup before Markdig
    /// can interpret URLs or tag-like payload text. This is display normalization only; it does not trust or
    /// approve the self-assessment contents.
    /// </summary>
    /// <param name="text">Chat text that may contain encoded or literal LocalGPT self-assessment blocks.</param>
    /// <returns>The text with recognized self-assessment blocks rendered as inert structured data.</returns>
    public string TranslateSelfAssessmentBlocksToMarkdown(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        // Most Council snapshots have no assessment envelope at all. Do not run a multi-line
        // wildcard regex over the entire (growing) transcript on every Blazor render.
        if (text.IndexOf("<localgpt-self-", StringComparison.OrdinalIgnoreCase) < 0 &&
            text.IndexOf("&lt;localgpt-self-", StringComparison.OrdinalIgnoreCase) < 0)
            return text;

        try
        {
            StringBuilder? output = null;
            var cursor = 0;
            while (TryFindAssessmentTag(text, cursor, closing: false, out var openStart, out var openEnd, out var tag))
            {
                if (!TryFindAssessmentTag(text, openEnd, closing: true, out var closeStart, out var closeEnd, out _))
                    break; // Incomplete live stream: keep the original text until the tag closes.

                // The original protocol accepts either recognized closing spelling for both
                // assessment kinds. HTML-encoded and literal brackets are equally recognized.
                output ??= new StringBuilder(text.Length);
                var replaceStart = GetAssessmentHeadingStart(text, cursor, openStart);
                output.Append(text, cursor, replaceStart - cursor);
                var payload = System.Net.WebUtility.HtmlDecode(text[openEnd..closeStart]).Trim();
                if (payload.Length > 1 && payload[^1] == '\\' && payload[^2] is '}' or ']')
                    payload = payload[..^1].TrimEnd();

                if (payload.Length == 0 ||
                    payload.Length > Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.StructuredTextMaximumJsonDocumentCharacters)))
                {
                    ReportInvalidAssessment("EmptyOrOverPolicyLimit", payload.Length, text.Length, null);
                    output.Append(BuildTaggedPayloadCodeBlock(tag, payload, isValidJson: false));
                }
                else if (!LooksLikeCompleteAssessmentJson(payload))
                {
                    ReportInvalidAssessment("NotACompleteJsonContainer", payload.Length, text.Length, null);
                    output.Append(BuildTaggedPayloadCodeBlock(tag, payload, isValidJson: false));
                }
                else if (HasRejectedAssessmentJson(payload))
                {
                    ReportInvalidAssessment("PreviouslyRejectedJsonSyntax", payload.Length, text.Length, null);
                    output.Append(BuildTaggedPayloadCodeBlock(tag, payload, isValidJson: false));
                }
                else
                {
                    try
                    {
                        using var document = JsonDocument.Parse(payload, documentOptions);
                        if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                        {
                            ReportInvalidAssessment("UnexpectedJsonRoot", payload.Length, text.Length, null);
                            output.Append(BuildTaggedPayloadCodeBlock(tag, payload, isValidJson: false));
                        }
                        else
                        {
                            var markdown = RenderElement(document.RootElement, 0, null);
                            var normalizedJson = JsonSerializer.Serialize(
                                document.RootElement,
                                new JsonSerializerOptions
                                {
                                    WriteIndented = true,
                                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                                });
                            var label = tag.Contains("annotated", StringComparison.OrdinalIgnoreCase)
                                ? "Self-annotated assessment"
                                : "Self-assessment";
                            output.AppendLine("<details class=\"localgpt-json-translation\" open>")
                                .Append("<summary>").Append(label).AppendLine(" · structured data</summary>")
                                .AppendLine()
                                .AppendLine(markdown)
                                .AppendLine()
                                .AppendLine("<details class=\"localgpt-json-source\">")
                                .AppendLine("<summary>Raw JSON</summary>")
                                .Append("<pre><code class=\"language-json\">")
                                .Append(Encode(normalizedJson))
                                .AppendLine("</code></pre>")
                                .AppendLine("</details>")
                                .AppendLine("</details>");
                        }
                    }
                    catch (JsonException ex)
                    {
                        RememberRejectedAssessmentJson(payload);
                        ReportInvalidAssessment("JsonSyntax", payload.Length, text.Length, ex);
                        output.Append(BuildTaggedPayloadCodeBlock(tag, payload, isValidJson: false));
                    }
                }
                cursor = closeEnd;
            }

            if (output is null)
                return text;
            output.Append(text, cursor, text.Length - cursor);
            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT self-assessment display normalization failed; the original text remains visible and payload content was omitted from logs.");
            return text;
        }
    }

    /// <summary>Consumes an optional Markdown heading prefix immediately before an assessment tag, preserving legacy formatting.</summary>
    private int GetAssessmentHeadingStart(string text, int cursor, int openStart)
    {
        var lineStart = text.LastIndexOf('\n', Math.Max(0, openStart - 1)) + 1;
        if (lineStart < cursor || openStart - lineStart > 8)
            return openStart;
        var pos = lineStart;
        while (pos < openStart && pos - lineStart < 6 && text[pos] == '#') pos++;
        if (pos == lineStart || pos == openStart || text[pos] is not (' ' or '\t'))
            return openStart;
        while (pos < openStart && text[pos] is ' ' or '\t') pos++;
        return pos == openStart ? lineStart : openStart;
    }

    /// <summary>
    /// Locates a recognized assessment opening or closing tag in a single forward pass.
    /// Fixed protocol delimiters do not require a regex or a matching timeout.
    /// </summary>
    private bool TryFindAssessmentTag(string text, int from, bool closing, out int start, out int end, out string tag)
    {
        var literalPrefix = closing ? "</localgpt-self-" : "<localgpt-self-";
        var encodedPrefix = closing ? "&lt;/localgpt-self-" : "&lt;localgpt-self-";
        start = -1;
        end = -1;
        tag = string.Empty;
        while (from < text.Length)
        {
            var literal = text.IndexOf(literalPrefix, from, StringComparison.OrdinalIgnoreCase);
            var encoded = text.IndexOf(encodedPrefix, from, StringComparison.OrdinalIgnoreCase);
            if (literal < 0 && encoded < 0)
                return false;

            var isEncoded = literal < 0 || (encoded >= 0 && encoded < literal);
            start = isEncoded ? encoded : literal;
            var nameStart = start + (isEncoded ? encodedPrefix.Length : literalPrefix.Length);
            var suffix = isEncoded ? "&gt;" : ">";
            const string shortName = "assessment";
            const string annotatedName = "annotated-assessment";
            var name = text.AsSpan(nameStart).StartsWith(annotatedName, StringComparison.OrdinalIgnoreCase)
                ? annotatedName
                : text.AsSpan(nameStart).StartsWith(shortName, StringComparison.OrdinalIgnoreCase)
                    ? shortName
                    : string.Empty;
            end = nameStart + name.Length;
            if (name.Length > 0 && text.AsSpan(end).StartsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                end += suffix.Length;
                tag = "localgpt-self-" + name;
                return true;
            }
            from = start + 1;
        }
        return false;
    }

    /// <summary>
    /// Rejects ordinary prose and incomplete JSON without allocating parser exceptions.
    /// JsonDocument remains the authoritative grammar validator for complete candidates.
    /// </summary>
    private bool LooksLikeCompleteAssessmentJson(string json)
    {
        if (json.Length < 2 || (json[0] != '{' && json[0] != '[') ||
            (json[^1] != '}' && json[^1] != ']'))
            return false;

        Span<char> openings = stackalloc char[MaximumDepth];
        var depth = 0;
        var quoted = false;
        var escaped = false;
        var lineComment = false;
        var blockComment = false;
        for (var i = 0; i < json.Length; i++)
        {
            var ch = json[i];
            if (lineComment)
            {
                if (ch is '\r' or '\n') lineComment = false;
                continue;
            }
            if (blockComment)
            {
                if (ch == '*' && i + 1 < json.Length && json[i + 1] == '/')
                {
                    blockComment = false;
                    i++;
                }
                continue;
            }
            if (quoted)
            {
                if (escaped) escaped = false;
                else if (ch == '\\') escaped = true;
                else if (ch == '"') quoted = false;
                continue;
            }
            if (ch == '"') { quoted = true; continue; }
            if (ch == '/' && i + 1 < json.Length && json[i + 1] == '/')
            {
                lineComment = true;
                i++;
                continue;
            }
            if (ch == '/' && i + 1 < json.Length && json[i + 1] == '*')
            {
                blockComment = true;
                i++;
                continue;
            }
            if (ch is '{' or '[')
            {
                if (depth == MaximumDepth) return false;
                openings[depth++] = ch;
            }
            else if (ch is '}' or ']')
            {
                if (depth == 0 || (openings[--depth] == '{' ? ch != '}' : ch != ']'))
                    return false;
                if (depth == 0 && i < json.Length - 1)
                    return false;
            }
        }
        return depth == 0 && !quoted && !blockComment;
    }

    /// <summary>Checks a bounded negative cache without persisting uploaded or model-owned content.</summary>
    private bool HasRejectedAssessmentJson(string json)
    {
        var key = GetAssessmentJsonFingerprint(json);
        lock (rejectedAssessmentJsonGate)
            return rejectedAssessmentJson.Contains(key);
    }

    /// <summary>Stores a bounded fingerprint only after System.Text.Json confirms invalid syntax.</summary>
    private void RememberRejectedAssessmentJson(string json)
    {
        var key = GetAssessmentJsonFingerprint(json);
        lock (rejectedAssessmentJsonGate)
        {
            if (rejectedAssessmentJson.Count >= 512)
                rejectedAssessmentJson.Clear();
            rejectedAssessmentJson.Add(key);
        }
    }

    /// <summary>Calculates a compact content fingerprint for repeated invalid display-only frames.</summary>
    private (ulong Fingerprint, int Length) GetAssessmentJsonFingerprint(string json)
    {
        // Non-cryptographic FNV-1a is sufficient for a bounded display-only negative cache.
        // It is never used for trust decisions, identities, file integrity, or authorization.
        ulong hash = 14695981039346656037UL;
        foreach (var ch in json)
        {
            hash = unchecked((hash ^ ch) * 1099511628211UL);
        }
        return (hash, json.Length);
    }

    /// <summary>Samples invalid assessment diagnostics without emitting private model text or recurring exceptions.</summary>
    private void ReportInvalidAssessment(string reason, int payloadLength, int sourceLength, JsonException? exception)
    {
        var count = Interlocked.Increment(ref invalidAssessmentCount);
        if (count != 1 && count % 256 != 0)
            return;

        logger.LogWarning(
            "LocalGPT assessment display received an unparseable payload: reason={Reason}, occurrence={Occurrence}, payloadCharacters={PayloadLength}, transcriptCharacters={SourceLength}, jsonLine={JsonLine}, jsonBytePosition={JsonBytePosition}. Content omitted; rendered as inert source.",
            reason, count, payloadLength, sourceLength, exception?.LineNumber, exception?.BytePositionInLine);
    }

    /// <summary>
    /// Builds an inert code disclosure for a tagged assessment that could not be parsed as valid JSON.
    /// </summary>
    /// <param name="tag">Recognized LocalGPT assessment tag name.</param>
    /// <param name="payload">Decoded payload text.</param>
    /// <param name="isValidJson">Whether the payload was validated as JSON; retained for explicit display semantics.</param>
    /// <returns>Controlled markup that cannot activate payload HTML.</returns>
    private string BuildTaggedPayloadCodeBlock(string tag, string payload, bool isValidJson)
    {
        try
        {
            var label = tag.Contains("annotated", StringComparison.OrdinalIgnoreCase)
                ? "Self-annotated assessment"
                : "Self-assessment";
            var status = isValidJson ? "JSON" : "unparsed payload";
            return $"<details class=\"localgpt-json-source\"><summary>{label} · {status}</summary>\n<pre><code>{Encode(payload)}</code></pre>\n</details>";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not build the inert self-assessment code disclosure; payload content was omitted from logs.");
            return Encode(payload);
        }
    }

    /// <summary>
    /// Performs translate plain JSON blocks to markdown as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string TranslatePlainJsonBlocksToMarkdown(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        try
        {
            return TranslateJson(new StructuredJsonTranslationRequest
            {
                Text = text,
                IncludeRawJson = true,
                MaximumDocuments = 20
            }).TranslatedText;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Structured JSON translation failed; chat content was omitted from logs and will remain unchanged.");
            return text;
        }
    }

    /// <summary>
    /// Finds JSON candidates as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="maximumDocuments">Maximum documents value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<StructuredJsonCandidate> FindJsonCandidates(string text, int maximumDocuments)
    {
    try
    {
            var excluded = fencedBlockRegex.Matches(text)
                .Concat(protectedBlockRegex.Matches(text).Cast<Match>())
                .Select(match => (Start: match.Index, End: match.Index + match.Length))
                .OrderBy(range => range.Start)
                .ToList();
            var candidates = new List<StructuredJsonCandidate>();

            foreach (Match startMatch in plainStartRegex.Matches(text))
            {
                if (candidates.Count >= maximumDocuments)
                    break;
                var startGroup = startMatch.Groups["jsonStart"];
                var index = startGroup.Success ? startGroup.Index : startMatch.Index;
                if (index < 0 || index >= text.Length || IsInsideExcludedRange(index, excluded) || !StartsStandaloneBlock(text, index))
                    continue;
                if (!TryFindBalancedJsonEnd(text, index, out var end) || !EndsStandaloneBlock(text, end))
                    continue;

                var length = end - index + 1;
                candidates.Add(new StructuredJsonCandidate(index, length, text.Substring(index, length)));
            }

            return candidates;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(FindJsonCandidates)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(FindJsonCandidates)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether inside excluded range as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="index">Index value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="excluded">End) dependency used by the structured text translation workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsInsideExcludedRange(int index, IReadOnlyList<(int Start, int End)> excluded) {
    try
    {
        return excluded.Any(range => index >= range.Start && index < range.End);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(IsInsideExcludedRange)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(IsInsideExcludedRange)} failed.");
        throw;
    }
}

    /// <summary>
    /// Starts s standalone block as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="index">Index value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool StartsStandaloneBlock(string text, int index)
    {
    try
    {
            var lineStart = text.LastIndexOf('\n', Math.Max(0, index - 1));
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            for (var cursor = lineStart; cursor < index; cursor++)
            {
                if (!char.IsWhiteSpace(text[cursor]))
                    return false;
            }
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(StartsStandaloneBlock)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(StartsStandaloneBlock)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs ends standalone block as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="end">End value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool EndsStandaloneBlock(string text, int end)
    {
    try
    {
            for (var cursor = end + 1; cursor < text.Length && text[cursor] is not ('\r' or '\n'); cursor++)
            {
                if (!char.IsWhiteSpace(text[cursor]))
                    return false;
            }
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(EndsStandaloneBlock)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(EndsStandaloneBlock)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to find balanced JSON end as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="start">Start value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="end">End value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryFindBalancedJsonEnd(string text, int start, out int end)
    {
    try
    {
            var stack = new Stack<char>();
            var inString = false;
            var escaping = false;
            for (var index = start; index < text.Length; index++)
            {
                var current = text[index];
                if (inString)
                {
                    if (escaping)
                    {
                        escaping = false;
                        continue;
                    }
                    if (current == '\\')
                    {
                        escaping = true;
                        continue;
                    }
                    if (current == '"')
                        inString = false;
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    continue;
                }
                if (current is '{' or '[')
                {
                    stack.Push(current);
                    continue;
                }
                if (current is '}' or ']')
                {
                    if (stack.Count == 0)
                        break;
                    var opening = stack.Pop();
                    if ((opening == '{' && current != '}') || (opening == '[' && current != ']'))
                        break;
                    if (stack.Count == 0)
                    {
                        end = index;
                        return true;
                    }
                }
            }

            end = -1;
            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(TryFindBalancedJsonEnd)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(TryFindBalancedJsonEnd)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs render element as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="depth">Depth value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderElement(JsonElement element, int depth, string? name)
    {
    try
    {
            if (depth > MaximumDepth)
                return "- … depth limit reached";

            var indent = new string(' ', depth * 2);
            var label = string.IsNullOrWhiteSpace(name) ? string.Empty : $"**{Encode(BeautifyKey(name))}**: ";
            return element.ValueKind switch
            {
                JsonValueKind.Object => RenderObject(element, depth, label),
                JsonValueKind.Array => RenderArray(element, depth, label),
                _ => $"{indent}- {label}{RenderScalar(element)}"
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(RenderElement)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(RenderElement)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs render object as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="depth">Depth value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="label">Label value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderObject(JsonElement element, int depth, string label)
    {
    try
    {
            var lines = new List<string>();
            var indent = new string(' ', depth * 2);
            if (!string.IsNullOrEmpty(label))
                lines.Add($"{indent}- {label}");
            if (!element.EnumerateObject().Any())
            {
                lines.Add($"{indent}  - _(empty object)_");
                return string.Join(Environment.NewLine, lines);
            }

            var childDepth = string.IsNullOrEmpty(label) ? depth : depth + 1;
            foreach (var property in element.EnumerateObject())
                lines.Add(RenderElement(property.Value, childDepth, property.Name));
            return string.Join(Environment.NewLine, lines);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(RenderObject)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(RenderObject)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs render array as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="depth">Depth value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="label">Label value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderArray(JsonElement element, int depth, string label)
    {
    try
    {
            var lines = new List<string>();
            var indent = new string(' ', depth * 2);
            if (!string.IsNullOrEmpty(label))
                lines.Add($"{indent}- {label}");
            var items = element.EnumerateArray().ToList();
            if (items.Count == 0)
            {
                lines.Add($"{indent}  - _(empty list)_");
                return string.Join(Environment.NewLine, lines);
            }

            var childDepth = string.IsNullOrEmpty(label) ? depth : depth + 1;
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var itemName = $"Item {index + 1}";
                lines.Add(RenderElement(item, childDepth, itemName));
            }
            return string.Join(Environment.NewLine, lines);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(RenderArray)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(RenderArray)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs render scalar as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderScalar(JsonElement element) {
    try
    {
        return element.ValueKind switch
    {
        JsonValueKind.String => Encode(element.GetString() ?? string.Empty),
        JsonValueKind.Number => $"`{Encode(element.GetRawText())}`",
        JsonValueKind.True => "`true`",
        JsonValueKind.False => "`false`",
        JsonValueKind.Null => "_(null)_",
        _ => Encode(element.GetRawText())
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(RenderScalar)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(RenderScalar)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs beautify key as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BeautifyKey(string key)
    {
    try
    {
            var parts = keyTokenRegex.Split(key)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();
            if (parts.Length == 0)
                return key;
            return string.Join(" ", parts.Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(BeautifyKey)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(BeautifyKey)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds translated block as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="markdown">Markdown value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="normalizedJson">Normalized json value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="includeRawJson">Value indicating whether include raw JSON should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildTranslatedBlock(string markdown, string normalizedJson, bool includeRawJson)
    {
    try
    {
            var builder = new StringBuilder()
                .AppendLine("<details class=\"localgpt-json-translation\" open>")
                .AppendLine("<summary>Structured data</summary>")
                .AppendLine()
                .AppendLine(markdown);
            if (includeRawJson)
            {
                builder.AppendLine()
                    .AppendLine("<details class=\"localgpt-json-source\">")
                    .AppendLine("<summary>Raw JSON</summary>")
                    .Append("<pre><code class=\"language-json\">")
                    .Append(Encode(normalizedJson))
                    .AppendLine("</code></pre>")
                    .AppendLine("</details>");
            }
            builder.AppendLine("</details>");
            return builder.ToString().TrimEnd();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(BuildTranslatedBlock)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(BuildTranslatedBlock)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates catalog regex as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="patterns">Regex pattern dto dependency used by the structured text translation workflow to provide the corresponding application capability.</param>
    /// <param name="name">Name value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="fallbackFlags">Fallback flags value supplied to the structured text translation operation and used when producing its result.</param>
    /// <param name="timeout">Timeout value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    private Regex CreateCatalogRegex(
        IReadOnlyList<RegexPatternDto> patterns,
        string name,
        string fallback,
        string fallbackFlags,
        TimeSpan timeout)
    {
    try
    {
            var definition = patterns.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            var flags = string.IsNullOrWhiteSpace(definition?.Flags)
                ? fallbackFlags
                : $"{fallbackFlags}|{definition.Flags}";
            return regexPatternService.Compile(definition?.Pattern ?? fallback, flags, timeout);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(CreateCatalogRegex)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(CreateCatalogRegex)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs encode as part of the structured text translation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the structured text translation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Encode(string value) {
    try
    {
        return HtmlEncoder.Default.Encode(value);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(Encode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StructuredTextTranslationService)}.{nameof(Encode)} failed.");
        throw;
    }
}

}
