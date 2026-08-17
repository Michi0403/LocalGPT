using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates remote knowledge import behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class RemoteKnowledgeImportService
    {
    /// <summary>
    /// Applies role and topic tags as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ApplyRoleAndTopicTagsAsync(RemoteKnowledgeImportResult result, CancellationToken cancellationToken)
    {
    try
    {
            if (result.AppliedTags.Count == 0 || result.LearnBaseResult is null) return;
            var ids = result.LearnBaseResult.Projects
                .Where(item => item.KnowledgeEntryId.HasValue)
                .Select(item => item.KnowledgeEntryId!.Value)
                .ToHashSet();
            if (ids.Count == 0) return;
            var entries = await knowledge.GetEntriesAsync(includeArchived: true, take: 500, cancellationToken).ConfigureAwait(false);
            foreach (var entry in entries.Where(item => ids.Contains(item.Id)))
            {
                entry.Tags = string.Join("; ", (entry.Tags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Concat(result.AppliedTags)).Distinct(StringComparer.OrdinalIgnoreCase));
                entry.HelpfulSources = string.IsNullOrWhiteSpace(entry.HelpfulSources)
                    ? result.SourceUrl
                    : entry.HelpfulSources + Environment.NewLine + result.SourceUrl;
                entry.Source = result.SourceUrl;
                entry.IsUserApproved = true;
                entry.VerificationStatus = "SourceBacked";
                entry.LastVerifiedAtUtc = DateTime.UtcNow;
                await knowledge.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ApplyRoleAndTopicTagsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ApplyRoleAndTopicTagsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds tags as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> BuildTags(RemoteKnowledgeImportRequest request) {
    try
    {
        return (request.RoleKeys ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => "role:" + item.Trim())
            .Concat((request.Topics ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => "topic:" + item.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(128)
            .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildTags)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildTags)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves kind as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requested">Requested value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="sourceUri">Source uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveKind(string? requested, Uri sourceUri)
    {
    try
    {
            if (string.Equals(requested, "GitHub", StringComparison.OrdinalIgnoreCase)) return "GitHub";
            if (string.Equals(requested, "Web", StringComparison.OrdinalIgnoreCase) || string.Equals(requested, "Website", StringComparison.OrdinalIgnoreCase)) return "Web";
            return sourceUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ? "GitHub" : "Web";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ResolveKind)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ResolveKind)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves branch as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="segments">Segments value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="requested">Requested value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveBranch(string[] segments, string? requested)
    {
    try
    {
            var treeIndex = Array.FindIndex(segments, item => item.Equals("tree", StringComparison.OrdinalIgnoreCase));
            if (treeIndex >= 0 && treeIndex + 1 < segments.Length) return SafeSegment(segments[treeIndex + 1]);
            return SafeSegment(string.IsNullOrWhiteSpace(requested) ? "main" : requested.Trim());
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ResolveBranch)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ResolveBranch)} failed.");
        throw;
    }
}

    }
}
