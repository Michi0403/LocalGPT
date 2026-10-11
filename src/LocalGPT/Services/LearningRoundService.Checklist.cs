using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LocalGPT.Services;

/// <summary>Persistent per-file progress for the existing database-first project structure.</summary>
public sealed partial class LearningRoundService
{
    /// <summary>Lists a bounded file window and accurate revision-wide learning counters.</summary>
    /// <param name="projectId">Selected database-first project.</param>
    /// <param name="revisionId">Selected source revision, or the base project.</param>
    /// <param name="offset">Zero-based page offset.</param>
    /// <param name="take">Maximum result rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Learning checklist with source-backed rows and totals.</returns>
    public async Task<LearningFileChecklistSnapshot> GetFileChecklistAsync(Guid projectId, Guid? revisionId = null, int offset = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.LocalGptProjectTrackedFiles.AsNoTracking().Where(file => file.ProjectId == projectId && file.RevisionId == revisionId);
            var counts = await query.GroupBy(file => file.LearningStatus)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var counter = counts.ToDictionary(item => item.Status, item => item.Count, StringComparer.OrdinalIgnoreCase);
            int Count(string status) => counter.GetValueOrDefault(status);
            var files = await query.OrderBy(file => file.ProjectRelativePath).Skip(Math.Max(0, offset)).Take(Math.Clamp(take, 1, 200))
                .Select(file => new LearningFileChecklistItem(file.Id, file.ProjectRelativePath, file.ContentHash, file.LearningStatus,
                    file.LearningReadCharacters, file.LearningReadComplete, file.LearningEvidence, file.LearningBlockReason, file.LearningUpdatedAtUtc))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return new LearningFileChecklistSnapshot(projectId, revisionId, counts.Sum(item => item.Count), Count("Pending"), Count("Reading"),
                Count("NeedsReview"), Count("Blocked"), Count("Completed"), Count("SkippedWithReason"), Math.Max(0, offset), files);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Learning file-checklist snapshot was canceled.");
            else
                logger.LogError(exception, "Learning file-checklist snapshot failed.");
            throw;
        }
    }

    /// <summary>Reads one bounded source page, persisting the cursor only for verified source bytes.</summary>
    /// <param name="projectId">Project containing the tracked file.</param>
    /// <param name="revisionId">Revision or base project to read.</param>
    /// <param name="fileId">Optional exact file; otherwise the next pending/in-progress row.</param>
    /// <param name="maximumCharacters">Bounded text response size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exact source-backed content segment and durable continuation token.</returns>
    public async Task<LearningFileReadResult> ReadNextFileChunkAsync(Guid projectId, Guid? revisionId = null, Guid? fileId = null, int maximumCharacters = 6000, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.LocalGptProjectTrackedFiles.Where(file => file.ProjectId == projectId && file.RevisionId == revisionId && file.Exists);
            var file = fileId is Guid requestedId
                ? await query.SingleOrDefaultAsync(item => item.Id == requestedId, cancellationToken).ConfigureAwait(false)
                : await query.Where(item => item.LearningStatus == "Pending" || item.LearningStatus == "Reading")
                    .OrderBy(item => item.ProjectRelativePath).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (file is null)
            {
                if (fileId is not null)
                    throw new KeyNotFoundException("The selected source file is not tracked in this project revision.");
                return new LearningFileReadResult(Guid.Empty, string.Empty, string.Empty, 0, 0, false, string.Empty, "NoPendingFiles", string.Empty);
            }
            if (file.LearningStatus == "Completed")
                throw new InvalidOperationException("This source hash is already reviewed; changed revisions are reopened by the project synchronizer.");

            var project = await db.LocalGptProjects.AsNoTracking().SingleAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false);
            var revision = revisionId is Guid id
                ? await db.LocalGptProjectRevisions.AsNoTracking().SingleAsync(item => item.Id == id && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                : null;
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(!string.IsNullOrWhiteSpace(revision?.SourceRootPath) ? revision.SourceRootPath : project.RootPath));
            var path = Path.GetFullPath(file.AbsolutePath);
            var relative = Path.GetRelativePath(root, path);
            if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                HasUnsafeReparsePoint(root, path))
                throw new UnauthorizedAccessException("The tracked source file is outside its approved project root or follows a link.");

            if (!File.Exists(path))
                return await BlockFileAsync(db, file, "The tracked source file is missing; rescan the project before learning.", cancellationToken).ConfigureAwait(false);
            if (file.EncodingName == "binary")
                return await BlockFileAsync(db, file, "Binary source requires a file-type-specific review; metadata hashing is not semantic learning.", cancellationToken).ConfigureAwait(false);

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
            var currentHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
            if (!currentHash.Equals(file.ContentHash, StringComparison.OrdinalIgnoreCase))
                return await BlockFileAsync(db, file, "Source hash changed since tracking; resynchronize before reviewing.", cancellationToken).ConfigureAwait(false);
            stream.Position = 0;

            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var position = Math.Max(0, file.LearningReadCharacters);
            try
            {
                var remaining = position;
                var skipBuffer = new char[4096];
                while (remaining > 0)
                {
                    var skipped = await reader.ReadAsync(skipBuffer.AsMemory(0, (int)Math.Min(skipBuffer.Length, remaining)), cancellationToken).ConfigureAwait(false);
                    if (skipped == 0)
                        return await BlockFileAsync(db, file, "Stored reading cursor exceeds the current file; rescan before continuing.", cancellationToken).ConfigureAwait(false);
                    remaining -= skipped;
                }
                var contentBuffer = new char[Math.Clamp(maximumCharacters, 1, 16000)];
                var read = await reader.ReadAsync(contentBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                var hasMore = reader.Peek() >= 0;
                file.LearningReadCharacters = position + read;
                file.LearningReadComplete = !hasMore;
                file.LearningStatus = hasMore ? "Reading" : "NeedsReview";
                file.LearningBlockReason = string.Empty;
                file.LearningUpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return new LearningFileReadResult(file.Id, file.ProjectRelativePath, file.ContentHash, position, file.LearningReadCharacters,
                    hasMore, new string(contentBuffer, 0, read), file.LearningStatus, string.Empty);
            }
            catch (DecoderFallbackException)
            {
                return await BlockFileAsync(db, file, "Source encoding cannot be decoded as UTF-8 or a supported Unicode BOM; manual file-type review is required.", cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Learning source-file read was canceled.");
            else
                logger.LogError(exception, "Learning source-file read failed; source content omitted from diagnostics.");
            throw;
        }
    }

    /// <summary>Completes an evidence-backed file review or records an explicit blocking reason.</summary>
    /// <param name="fileId">Existing tracked file identifier.</param>
    /// <param name="evidence">Concise findings or exact blocking reason.</param>
    /// <param name="blocked">Whether a source limitation prevented completion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Persisted revision/file review status.</returns>
    public async Task<LearningFileReviewResult> ReviewFileAsync(Guid fileId, string evidence, bool blocked = false, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(evidence) || evidence.Length > 4000)
                throw new ArgumentException("A nonempty, at most 4000-character source-backed finding or blocking reason is required.", nameof(evidence));
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var file = await db.LocalGptProjectTrackedFiles.SingleAsync(item => item.Id == fileId, cancellationToken).ConfigureAwait(false);
            if (!blocked && (!file.Exists || !file.LearningReadComplete || file.LearningStatus != "NeedsReview"))
                throw new InvalidOperationException("A complete, current source-backed read is required before a file can be marked reviewed.");
            if (!blocked)
            {
                if (!File.Exists(file.AbsolutePath))
                    throw new FileNotFoundException("The reviewed source file has been removed; rescan before committing the review.");
                var stream = new FileStream(file.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous);
                await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
                var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
                if (!actualHash.Equals(file.ContentHash, StringComparison.OrdinalIgnoreCase))
                {
                    file.LearningStatus = "Blocked";
                    file.LearningBlockReason = "Source changed after the complete read; rescan before review.";
                    file.LearningCompletedAtUtc = null;
                    file.LearningUpdatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return new LearningFileReviewResult(file.Id, file.ContentHash, file.LearningStatus, false);
                }
            }
            file.LearningStatus = blocked ? "Blocked" : "Completed";
            file.LearningEvidence = blocked ? string.Empty : evidence.Trim();
            file.LearningBlockReason = blocked ? evidence.Trim() : string.Empty;
            file.LearningReviewedHash = blocked ? string.Empty : file.ContentHash;
            file.LearningCompletedAtUtc = blocked ? null : DateTime.UtcNow;
            file.LearningUpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new LearningFileReviewResult(file.Id, file.ContentHash, file.LearningStatus, file.LearningReadComplete);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Learning file review was canceled.");
            else
                logger.LogError(exception, "Learning file review failed; evidence content omitted from diagnostics.");
            throw;
        }
    }

    /// <summary>Marks an inaccessible file blocked without masquerading its metadata as reviewed content.</summary>
    /// <param name="db">Current database context.</param>
    /// <param name="file">Tracked file being processed.</param>
    /// <param name="reason">Short diagnostic reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new blocked file state.</returns>
    private async Task<LearningFileReadResult> BlockFileAsync(LocalGptMemoryDbContext db, LocalGptProjectTrackedFile file, string reason, CancellationToken cancellationToken)
    {
        try
        {
            file.LearningStatus = "Blocked";
            file.LearningBlockReason = reason;
            file.LearningCompletedAtUtc = null;
            file.LearningUpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new LearningFileReadResult(file.Id, file.ProjectRelativePath, file.ContentHash, file.LearningReadCharacters,
                file.LearningReadCharacters, false, string.Empty, "Blocked", reason);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Blocking a learning file was canceled.");
            else
                logger.LogError(exception, "Could not persist the learning-file blocker.");
            throw;
        }
    }

    /// <summary>Rejects symlink and junction traversal in the otherwise bounded project source path.</summary>
    /// <param name="root">Approved project root.</param>
    /// <param name="path">Tracked file path.</param>
    /// <returns>Whether an ancestor crosses a filesystem reparse point.</returns>
    private bool HasUnsafeReparsePoint(string root, string path)
    {
        try
        {
            var current = path;
            while (!string.Equals(current, root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                if (File.Exists(current) || Directory.Exists(current))
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        return true;
                }
                var parent = Path.GetDirectoryName(current);
                if (string.IsNullOrWhiteSpace(parent) || parent == current)
                    return true;
                current = parent;
            }
            return false;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not validate tracked source-file link ownership.");
            return true;
        }
    }
}
