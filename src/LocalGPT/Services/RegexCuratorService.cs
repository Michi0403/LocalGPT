using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Persists regex provenance and review state separately from the database-backed pattern text.</summary>
/// <param name="regexPatterns">Regex pattern service dependency used by the regex curator workflow to provide the corresponding application capability.</param>
/// <param name="ambientContext">Ambient local gpt context dependency used by the regex curator workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the regex curator workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RegexCuratorService(
    IRegexPatternService regexPatterns,
    IAmbientLocalGptContext ambientContext,
    LocalGptCatalogService catalog,
    ILogger<RegexCuratorService> logger) : IRegexCuratorService
{
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="RegexCuratorService"/>.
    /// </summary>
    private readonly SemaphoreSlim Gate = new(1, 1);
    /// <summary>
    /// Stores the internal catalog path state used by <see cref="RegexCuratorService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string CatalogPath = Path.Combine(LocalGptApplicationDataPaths.ResolveUserPath("RegexCurator"), "curator.json");

    /// <summary>
    /// Performs mark suggested as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="provenance">Provenance value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="usageScope">Usage scope value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task MarkSuggestedAsync(string name, string provenance, string usageScope, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var entries = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
                var entry = entries.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                {
                    entry = new RegexCuratorEntry { Name = name.Trim() };
                    entries.Add(entry);
                }
                entry.Provenance = Bound(provenance, 240, "AI suggestion");
                entry.UsageScope = Bound(usageScope, 160, "General");
                entry.ReviewStatus = "NeedsReview";
                entry.UserApproved = false;
                entry.Reviews.Clear();
                entry.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await SaveUnsafeAsync(entries, cancellationToken).ConfigureAwait(false);
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Marking regex {RegexName} as curator-pending failed.", name);
            throw;
        }
    }

    /// <summary>
    /// Performs review as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The regex curator entry produced by the operation.</returns>
    public async Task<RegexCuratorEntry> ReviewAsync(RegexCuratorReviewRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
            var reviewer = ReviewerIdentity();
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var entries = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
                var entry = entries.FirstOrDefault(item => string.Equals(item.Name, request.Name, StringComparison.OrdinalIgnoreCase))
                    ?? new RegexCuratorEntry { Name = request.Name.Trim(), ReviewStatus = "NeedsReview", UserApproved = false, Provenance = "Review-created" };
                if (!entries.Contains(entry)) entries.Add(entry);
                entry.Reviews.RemoveAll(item => string.Equals(item.ReviewerIdentity, reviewer.Identity, StringComparison.OrdinalIgnoreCase));
                entry.Reviews.Add(new RegexCuratorReview
                {
                    ReviewerIdentity = reviewer.Identity,
                    ReviewerKind = reviewer.Kind,
                    Approved = request.Approved,
                    Notes = Bound(request.Notes, 2000, string.Empty),
                    ReviewedAtUtc = DateTimeOffset.UtcNow
                });
                var independentApprovals = entry.Reviews.Where(item => item.Approved).Select(item => item.ReviewerIdentity).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                entry.ReviewStatus = entry.Reviews.Any(item => !item.Approved)
                    ? "ChangesRequested"
                    : independentApprovals >= Math.Max(1, entry.RequiredIndependentApprovals) && entry.UserApproved ? "Approved" : "NeedsReview";
                entry.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await SaveUnsafeAsync(entries, cancellationToken).ConfigureAwait(false);
                return entry;
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reviewing regex curator entry {RegexName} failed.", request?.Name);
            throw;
        }
    }

    /// <summary>
    /// Sets user approval as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="approved">Value indicating whether approved should apply to this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The regex curator entry produced by the operation.</returns>
    public async Task<RegexCuratorEntry> SetUserApprovalAsync(string name, bool approved, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (!userConfirmed)
                throw new InvalidOperationException("Regex curator approval requires explicit user confirmation.");
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var entries = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
                var entry = entries.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    ?? new RegexCuratorEntry { Name = name.Trim(), ReviewStatus = "NeedsReview", UserApproved = false, Provenance = "User-reviewed" };
                if (!entries.Contains(entry)) entries.Add(entry);
                entry.UserApproved = approved;
                var approvals = entry.Reviews.Where(item => item.Approved).Select(item => item.ReviewerIdentity).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                entry.ReviewStatus = !approved ? "NeedsReview" : approvals >= Math.Max(1, entry.RequiredIndependentApprovals) ? "Approved" : "NeedsReview";
                entry.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await SaveUnsafeAsync(entries, cancellationToken).ConfigureAwait(false);
                return entry;
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Changing user approval for regex {RegexName} failed.", name);
            throw;
        }
    }

    /// <summary>
    /// Performs list as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<RegexCuratorEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var entries = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
                await EnsureLegacyEntriesUnsafeAsync(entries, cancellationToken).ConfigureAwait(false);
                return entries.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex) { logger.LogError(ex, "Listing regex curator entries failed."); throw; }
    }

    /// <summary>
    /// Lists approved patterns as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<RegexPattern>> ListApprovedPatternsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var states = await ListAsync(cancellationToken).ConfigureAwait(false);
            var approvedNames = states.Where(item => item.UserApproved && string.Equals(item.ReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return (await regexPatterns.ListAllAsync().ConfigureAwait(false)).Where(item => approvedNames.Contains(item.Name)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Listing curator-approved regex patterns failed.");
            throw;
        }
    }

    /// <summary>
    /// Ensures legacy entries unsafe as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="entries">Entries value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnsureLegacyEntriesUnsafeAsync(List<RegexCuratorEntry> entries, CancellationToken cancellationToken)
    {
        try
        {
            var changed = false;
            foreach (var pattern in await regexPatterns.ListAllAsync().ConfigureAwait(false))
            {
                if (entries.Any(item => string.Equals(item.Name, pattern.Name, StringComparison.OrdinalIgnoreCase))) continue;
                entries.Add(new RegexCuratorEntry { Name = pattern.Name, Provenance = "SeedOrPreCuratorPattern", UsageScope = "General", ReviewStatus = "Approved", UserApproved = true, RequiredIndependentApprovals = 0, UpdatedAtUtc = DateTimeOffset.UtcNow });
                changed = true;
            }
            if (changed) await SaveUnsafeAsync(entries, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("Ensured curator metadata for legacy/seed regex patterns.");
        }
        catch (Exception ex) { logger.LogError(ex, "Ensuring legacy regex curator entries failed."); throw; }
    }

    /// <summary>
    /// Loads unsafe as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<List<RegexCuratorEntry>> LoadUnsafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(CatalogPath)) return [];
            var json = await File.ReadAllTextAsync(CatalogPath, cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<List<RegexCuratorEntry>>(json, catalog.JsonOptions) ?? [];
            logger.LogDebug("Loaded {RegexCount} regex curator metadata entries.", result.Count);
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Loading regex curator metadata failed."); throw; }
    }

    /// <summary>
    /// Persists unsafe as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="entries">Entries value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveUnsafeAsync(List<RegexCuratorEntry> entries, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
            var temporary = CatalogPath + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(entries, catalog.JsonOptions), cancellationToken).ConfigureAwait(false);
            File.Move(temporary, CatalogPath, true);
            logger.LogDebug("Saved {RegexCount} regex curator metadata entries.", entries.Count);
        }
        catch (Exception ex) { logger.LogError(ex, "Saving regex curator metadata failed."); throw; }
    }

    /// <summary>
    /// Performs reviewer identity as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string identity string kind produced by the operation.</returns>
    private (string Identity, string Kind) ReviewerIdentity()
    {
        var current = ambientContext.Current;
        var identity = string.Join(":", new[]
        {
            current.ActorKind,
            current.ActorDisplayName,
            current.CouncilRunId?.ToString("N") ?? current.CorrelationId,
            current.Phase
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return (Bound(identity, 300, "LocalGPT:System"), Bound(current.ActorKind, 80, "System"));
    }

    /// <summary>
    /// Performs bound as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="maxLength">Max length value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the regex curator operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Bound(string? value, int maxLength, string fallback)
    {
        try
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            var result = normalized[..Math.Min(normalized.Length, maxLength)];
            logger.LogDebug("Bounded one regex curator metadata value.");
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Bounding regex curator metadata failed."); throw; }
    }
}
