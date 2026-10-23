using LocalGPT.BusinessObjects;
using System.Net;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Verifies actual distinct completed workspace-file reads before allowing configured Council learning goals to advance.</summary>
public sealed partial class MultiModelCouncilService
{
    /// <summary>Computes service-observed source-reading progress, excluding inventory/hash evidence, uncompleted pages and duplicate rereads.</summary>
    /// <param name="result">Immutable Council transcript and registered-function result traces.</param>
    /// <param name="request">Attached workspaces associated with the run.</param>
    /// <param name="definition">Workflow step whose source-inspection goal is being measured.</param>
    /// <param name="bootstrap">Source-authoritative bootstrap used to resolve attached workspaces.</param>
    /// <param name="cancellationToken">Cancels workspace enumeration.</param>
    /// <returns>Distinct fully read files and archive roots represented by those reads.</returns>
    private (int CompletedFiles, int CoveredRoots, int RequiredRoots, long ReviewedCharacters, int TruncatedReadAttempts) GetVerifiedWorkspaceReadProgress(
        MultiModelCouncilResult result, MultiModelCouncilRequest request,
        CouncilWorkflowStepDefinition definition, string bootstrap, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = runtimePolicy.GetJson<ChatUploadWorkspaceRuntimeParameters>(
                LocalGptRuntimeValue.ChatUploadWorkspaceRuntimeParametersJson);
            var referenced = uploadWorkspaces.ListWorkspaces(parameters.DefaultWorkspaceListCount)
                .Where(workspace => request.AttachedWorkspaceNames.Contains(workspace.WorkspaceName, StringComparer.OrdinalIgnoreCase) ||
                    bootstrap.Contains(workspace.WorkspaceName, StringComparison.OrdinalIgnoreCase))
                .Select(workspace => workspace.WorkspaceName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var workspaceName in referenced)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // The independent archive-root inventory is not paginated along with Files.
                var page = uploadWorkspaces.QueryFiles(workspaceName, 1, 0);
                foreach (var root in page.ArchiveRoots)
                    roots.Add($"{workspaceName}|{root.TrimEnd('/')}/");
            }

            var intervals = new Dictionary<string, List<(long Start, long End, bool Final)>>(StringComparer.OrdinalIgnoreCase);
            var truncatedAttempts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var phasePrefix = string.IsNullOrWhiteSpace(definition.Phase) ? definition.DisplayName : definition.Phase;
            foreach (var step in result.Steps.Where(item => string.IsNullOrWhiteSpace(item.Error) &&
                (string.Equals(item.WorkflowStepKey, definition.Key, StringComparison.OrdinalIgnoreCase) ||
                 (string.Equals(item.ModelName, "LocalGPT DXFunction gateway", StringComparison.Ordinal) &&
                  item.Phase.StartsWith(phasePrefix, StringComparison.OrdinalIgnoreCase)))))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var payloads = new List<string>();
                if (string.Equals(step.ModelName, "LocalGPT DXFunction gateway", StringComparison.Ordinal))
                {
                    if (step.VisibleContent.StartsWith("chat.upload_workspace_file -> Completed", StringComparison.OrdinalIgnoreCase))
                        payloads.Add(string.IsNullOrWhiteSpace(step.VerifiedWorkspaceReadMetadataJson)
                            ? step.Content : step.VerifiedWorkspaceReadMetadataJson);
                }
                else
                {
                    // Native tool calls are emitted as HTML-escaped JSON code blocks by the
                    // Ollama registry result formatter, not as model assertions.
                    // Ollama's transport function name uses underscores; the DX registry
                    // and older sessions may store the canonical dotted name instead.
                    var markers = new[]
                    {
                        "<summary>Function result · chat.upload_workspace_file · Completed</summary>",
                        "<summary>Function result · chat_upload_workspace_file · Completed</summary>"
                    };
                    var cursor = 0;
                    while (true)
                    {
                        var located = markers
                            .Select(marker => (Position: step.Content.IndexOf(marker, cursor, StringComparison.OrdinalIgnoreCase), Length: marker.Length))
                            .Where(match => match.Position >= 0)
                            .OrderBy(match => match.Position)
                            .FirstOrDefault();
                        if (located.Length == 0) break;
                        cursor = located.Position;
                        var start = step.Content.IndexOf("<pre><code", cursor + located.Length, StringComparison.OrdinalIgnoreCase);
                        var nextResult = step.Content.IndexOf("<summary>Function result", cursor + located.Length, StringComparison.OrdinalIgnoreCase);
                        if (start < 0 || (nextResult >= 0 && nextResult < start)) break;
                        var codeStart = step.Content.IndexOf('>', start);
                        if (codeStart < 0) break;
                        var end = step.Content.IndexOf("</code></pre>", codeStart + 1, StringComparison.OrdinalIgnoreCase);
                        if (end < 0) break;
                        payloads.Add(WebUtility.HtmlDecode(step.Content[(codeStart + 1)..end]));
                        cursor = end + 13;
                    }
                }

                foreach (var payload in payloads)
                {
                    try
                    {
                        using var parsed = JsonDocument.Parse(payload);
                        var document = parsed.RootElement;
                        if (document.ValueKind != JsonValueKind.Object ||
                            !document.TryGetProperty("succeeded", out var succeeded) ||
                            succeeded.ValueKind != JsonValueKind.True)
                            continue;
                        var source = document.TryGetProperty("readProgress", out var progress) &&
                            progress.ValueKind == JsonValueKind.Object
                                ? progress
                                : document.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Object
                                    ? value : default;
                        if (source.ValueKind != JsonValueKind.Object ||
                            !source.TryGetProperty("workspaceName", out var workspace) ||
                            !source.TryGetProperty("relativePath", out var path) ||
                            !source.TryGetProperty("kind", out var kind) ||
                            !source.TryGetProperty("characterOffset", out var offset) ||
                            !source.TryGetProperty("charactersReturned", out var count) ||
                            !source.TryGetProperty("hasMore", out var hasMore) ||
                            workspace.ValueKind != JsonValueKind.String || path.ValueKind != JsonValueKind.String ||
                            kind.ValueKind != JsonValueKind.String ||
                            !string.Equals(kind.GetString(), "text", StringComparison.OrdinalIgnoreCase) ||
                            offset.ValueKind != JsonValueKind.Number || count.ValueKind != JsonValueKind.Number ||
                            (hasMore.ValueKind != JsonValueKind.True && hasMore.ValueKind != JsonValueKind.False))
                            continue;
                        var name = workspace.GetString() ?? string.Empty;
                        var relative = (path.GetString() ?? string.Empty).Replace('\\', '/');
                        if (!offset.TryGetInt64(out var start) || !count.TryGetInt64(out var characters))
                            continue;
                        if (!referenced.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                            relative.Length == 0 || start < 0 || characters <= 0 ||
                            start > long.MaxValue - characters)
                            continue;
                        var key = $"{name}|{relative}";
                        if (document.TryGetProperty("truncated", out var truncated) &&
                            truncated.ValueKind == JsonValueKind.True)
                        {
                            // Transport truncated a registered read. This is evidence of an
                            // attempt and can justify corrective smaller-page retry, but is
                            // NOT proof that the model received the complete file contents.
                            truncatedAttempts.Add($"{key}|{start}|{characters}");
                            continue;
                        }
                        if (!intervals.TryGetValue(key, out var segments))
                        {
                            segments = [];
                            intervals.Add(key, segments);
                        }
                        segments.Add((start, start + characters, hasMore.ValueKind == JsonValueKind.False));
                    }
                    catch (JsonException)
                    {
                        logger.LogDebug("Skipped a non-JSON or incomplete Council function-result display trace while measuring source review progress.");
                    }
                }
            }

            var completePaths = new List<string>();
            long reviewedCharacters = 0;
            foreach (var entry in intervals)
            {
                var continuousCursor = 0L;
                var countedCursor = 0L;
                var completed = false;
                foreach (var segment in entry.Value.OrderBy(item => item.Start).ThenBy(item => item.End))
                {
                    // Any new non-overlapping interval is useful progress, even when a large
                    // source has not reached the last page. Overlaps/repeated reads earn nothing.
                    if (segment.End > countedCursor)
                    {
                        reviewedCharacters = checked(reviewedCharacters + segment.End - Math.Max(countedCursor, segment.Start));
                        countedCursor = segment.End;
                    }
                    // Completion additionally requires an uninterrupted interval from zero
                    // through a tool-confirmed last page; a guessed final offset is not enough.
                    if (segment.Start <= continuousCursor)
                    {
                        continuousCursor = Math.Max(continuousCursor, segment.End);
                        if (segment.Final && segment.End <= continuousCursor)
                            completed = true;
                    }
                }
                if (completed) completePaths.Add(entry.Key);
            }
            var covered = roots.Count(root => completePaths.Any(path => path.StartsWith(root, StringComparison.OrdinalIgnoreCase)));
            return (completePaths.Count, covered, roots.Count, reviewedCharacters, truncatedAttempts.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to determine verified workspace source-review progress for workflow step {StepKey}.", definition.Key);
            throw;
        }
    }
}
