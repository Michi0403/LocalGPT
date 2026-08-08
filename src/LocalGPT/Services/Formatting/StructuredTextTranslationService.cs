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
    public const string JsonFencePatternName = "builtin.json-fence-pattern";
    public const string JsonPlainStartPatternName = "builtin.json-plain-start-pattern";
    public const string JsonProtectedBlockPatternName = "builtin.json-protected-block-pattern";
    public const string JsonKeyTokenPatternName = "builtin.json-key-token-pattern";
    public const string JsonScalarPatternName = "builtin.json-scalar-pattern";

    private const int MaximumInputLength = 1_000_000;
    private const int MaximumJsonDocumentLength = 200_000;
    private const int MaximumDepth = 32;
    private readonly Regex fencedBlockRegex;
    private readonly Regex plainStartRegex;
    private readonly Regex protectedBlockRegex;
    private readonly Regex keyTokenRegex;
    private readonly ILogger<StructuredTextTranslationService> logger;
    private readonly IRegexPatternService regexPatternService;
    private readonly JsonDocumentOptions documentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = MaximumDepth
    };

    public StructuredTextTranslationService(
        IInitialDataCatalog initialDataCatalog,
        ILocalGptRuntimePolicyDataService runtimePolicy,
        IRegexPatternService regexPatterns,
        ILogger<StructuredTextTranslationService> logger)
    {
        this.logger = logger;
        regexPatternService = regexPatterns;
        fencedBlockRegex = CreateCatalogRegex(
            initialDataCatalog.RegexPatterns,
            JsonFencePatternName,
            "```(?:json)?\\s*(?<json>[\\[{].*?[\\]}])\\s*```",
            "IgnoreCase|Singleline|Compiled|CultureInvariant",
            runtimePolicy.RegexTimeout);
        plainStartRegex = CreateCatalogRegex(
            initialDataCatalog.RegexPatterns,
            JsonPlainStartPatternName,
            "(?m)^\\s*(?<jsonStart>[\\[{])",
            "Multiline|Compiled|CultureInvariant",
            runtimePolicy.RegexTimeout);
        protectedBlockRegex = CreateCatalogRegex(
            initialDataCatalog.RegexPatterns,
            JsonProtectedBlockPatternName,
            @"(?:```.*?(?:```|$)|<pre\b[^>]*>.*?(?:</pre>|$)|<code\b[^>]*>.*?(?:</code>|$)|<localgpt-dx-call>.*?(?:</localgpt-dx-call>|$))",
            "IgnoreCase|Singleline|Compiled|CultureInvariant",
            runtimePolicy.RegexTimeout);
        keyTokenRegex = CreateCatalogRegex(
            initialDataCatalog.RegexPatterns,
            JsonKeyTokenPatternName,
            "(?<=[a-z0-9])(?=[A-Z])|[_\\-.]+",
            "CultureInvariant|Compiled",
            runtimePolicy.RegexTimeout);
    }

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

        if (text.Length > MaximumInputLength)
        {
            result.Warnings.Add($"Input was truncated to {MaximumInputLength:n0} characters before structured JSON inspection.");
            text = text[..MaximumInputLength];
        }

        var maximumDocuments = Math.Clamp(request.MaximumDocuments, 1, 100);
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
            if (candidate.Length > MaximumJsonDocumentLength)
            {
                result.Warnings.Add($"Skipped a JSON candidate longer than {MaximumJsonDocumentLength:n0} characters.");
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(candidate.Json, documentOptions);
                if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                    continue;

                var markdown = RenderElement(document.RootElement, 0, null);
                var normalizedJson = JsonSerializer.Serialize(
                    document.RootElement,
                    new JsonSerializerOptions { WriteIndented = true });
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
