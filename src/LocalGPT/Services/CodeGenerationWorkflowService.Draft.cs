using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Owns the existing durable draft lifecycle and review promotion.</summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <summary>Internal review status for a draft that cannot be executed or downloaded.</summary>
    private const string DraftStatus = "SourceDraft";

    /// <inheritdoc />
    public async Task<(object Progress, CodeGenerationReviewSnapshot? Review)> ProcessDraftAsync(
        CreateCodeGenerationReviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var runId = request.CouncilRunId ?? throw new ArgumentException("An active Council run is required.");
            var operation = request.DraftOperation;
            if (operation is not ("begin" or "next" or "complete" or "submit" or "repair"))
                throw new ArgumentException("Select begin, next, complete, submit or repair.");

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var database = db.ConfigureAwait(false);
            var draft = await FindCouncilDraftAsync(db, runId, cancellationToken).ConfigureAwait(false);
            if (operation == "repair")
            {
                var repaired = await BeginRepairDraftAsync(db, runId, draft, request, cancellationToken).ConfigureAwait(false);
                return (new { ReviewId = repaired.Id, repaired.ProjectId, repaired.ProjectRevisionId, repaired.Status }, null);
            }
            if (draft is null)
            {
                if (operation != "begin")
                    throw new ArgumentException("Begin the Council source draft before taking a file.");
                draft = await BeginDraftAsync(db, runId, request, cancellationToken).ConfigureAwait(false);
            }

            if (draft.Status != DraftStatus)
            {
                var review = ToSnapshot(draft, DeserializePayload(draft.PayloadJson));
                return (new { ReviewId = review.Id, review.ReviewHash, review.Status }, operation == "submit" ? review : null);
            }

            var plan = ReadDraftPlan(draft);
            var manifest = plan.DraftManifest ?? throw new InvalidOperationException("The saved source draft has no manifest.");
            var nextFile = manifest.FirstOrDefault(item => !plan.Files.Any(file =>
                string.Equals(file.RelativePath, item.Key, StringComparison.OrdinalIgnoreCase)));

            if (operation == "submit")
            {
                if (plan.Files.Count != manifest.Count || manifest.Keys.Any(path => !plan.Files.Any(file =>
                    string.Equals(file.RelativePath, path, StringComparison.OrdinalIgnoreCase))))
                    throw new ArgumentException($"Complete the remaining {manifest.Count - plan.Files.Count} workspace file(s) before submitting.");

                foreach (var candidate in plan.Files)
                {
                    var diagnostic = ValidateDraftProjectTarget(plan.Goal, candidate.RelativePath, candidate.Content);
                    if (!string.IsNullOrWhiteSpace(diagnostic))
                        throw new ArgumentException($"{candidate.RelativePath}: {diagnostic}");
                }
                var review = await CreateReviewCoreAsync(plan, draft, cancellationToken).ConfigureAwait(false);
                return (new { ReviewId = review.Id, review.ReviewHash, review.Status, FileCount = manifest.Count }, review);
            }

            if (operation == "complete")
            {
                if (plan.DraftAssignments.Count > 0 || plan.DraftWorkspaceMode == "HyperlightSourceDraft")
                    throw new ArgumentException("This draft uses Council-assigned file work; only its assigned worker receipts may complete files.");
                if (request.Files is not [{ Content: { } } submittedFile])
                    throw new ArgumentException("Complete accepts exactly one file with its full content.");
                var file = NormalizeFile(submittedFile);
                var previous = plan.Files.Find(item => string.Equals(item.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase));
                if (previous is not null && previous.Content != file.Content)
                    throw new ArgumentException("That workspace file was already completed with different content.");
                if (previous is null)
                {
                    if (!string.Equals(nextFile.Key, file.RelativePath, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("Complete the workspace file returned by next.");
                    var length = Encoding.UTF8.GetByteCount(file.Content);
                    var maximum = runtimePolicy.GetLong("MaxSingleFileBytes");
                    if (maximum > 0 && length > maximum)
                        throw new ArgumentException($"The file exceeds the configured maximum of {maximum} bytes.");
                    file.RelativePath = nextFile.Key;
                    file.Purpose = nextFile.Value;
                    var targetDiagnostic = ValidateDraftProjectTarget(plan.Goal, file.RelativePath, file.Content);
                    if (!string.IsNullOrWhiteSpace(targetDiagnostic)) throw new ArgumentException(targetDiagnostic);
                    plan.Files.Add(file);
                    var payload = JsonSerializer.Serialize(plan, JsonOptions);
                    var changed = await db.CodeGenerationChangeReviews
                        .Where(row => row.Id == draft.Id && row.Status == DraftStatus && row.PayloadJson == draft.PayloadJson)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(row => row.PayloadJson, payload)
                            .SetProperty(row => row.UpdatedAtUtc, DateTime.UtcNow), cancellationToken).ConfigureAwait(false);
                    if (changed == 0)
                        throw new ArgumentException("The workspace changed during completion. Ask for the next file again.");
                }
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content)));
                logger.LogInformation("Council source draft {RunId} completed {CompletedFiles}/{FileCount} files.", runId, plan.Files.Count, manifest.Count);
                return (new { CompletedPath = previous?.RelativePath ?? file.RelativePath, Sha256 = hash,
                    CompletedFiles = plan.Files.Count, FileCount = manifest.Count, ReadyToSubmit = plan.Files.Count == manifest.Count }, null);
            }

            return (new { ReviewId = draft.Id, draft.ProjectId, draft.ProjectRevisionId, RelativePath = nextFile.Key, WorkOrder = nextFile.Value,
                CompletedFiles = plan.Files.Count, FileCount = manifest.Count, ReadyToSubmit = plan.Files.Count == manifest.Count }, null);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Council source draft operation canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council source draft operation failed.");
            throw;
        }
    }

    /// <summary>Persists the ordered work manifest before authoring any file.</summary>
    /// <param name="db">Database used to persist the draft.</param>
    /// <param name="runId">Originating Council run identifier.</param>
    /// <param name="request">Manifest and output intent.</param>
    /// <param name="cancellationToken">Cancels the draft insertion.</param>
    /// <returns>The durable draft row.</returns>
    private async Task<CodeGenerationChangeReview> BeginDraftAsync(
        LocalGPT.BusinessObjects.EFCore.LocalGptMemoryDbContext db,
        Guid runId, CreateCodeGenerationReviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Files is { Count: > 0 })
                throw new ArgumentException("draftOperation=begin forbids files. Send each assigned file with complete after the manifest is persisted.");
            if (request.CodeDomTypes is { Count: > 0 })
                throw new ArgumentException("draftOperation=begin forbids codeDomTypes. Supply source work orders in draftManifest.");
            if (string.IsNullOrWhiteSpace(request.Goal))
                throw new ArgumentException("draftOperation=begin requires a nonempty goal.");
            if (request.DraftManifest is not { Count: > 0 } paths)
                throw new ArgumentException("draftOperation=begin requires a nonempty draftManifest mapping relative paths to work orders.");

            var manifest = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hasDotNetProject = paths.Keys.Any(path => string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetExtension(path), ".fsproj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetExtension(path), ".vbproj", StringComparison.OrdinalIgnoreCase));
            foreach (var (path, workOrder) in paths)
            {
                if (path.TrimEnd().EndsWith('/') || path.TrimEnd().EndsWith('\\'))
                    throw new ArgumentException("Manifest entries must name files, not directories.");
                var relativePath = NormalizeRelativePath(path);
                var segments = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (hasDotNetProject && segments.Any(segment => string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"Manifest path '{relativePath}' is a compiler-output directory, not authored project source.");
                if (relativePath == "." || string.Equals(relativePath, "CHANGE_REVIEW.md", StringComparison.OrdinalIgnoreCase) || !manifest.TryAdd(relativePath, workOrder))
                    throw new ArgumentException("The file manifest must contain distinct relative file paths.");
            }

            foreach (var path in manifest.Keys)
            {
                for (var parent = Path.GetDirectoryName(path); !string.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent))
                {
                    if (manifest.ContainsKey(parent))
                        throw new ArgumentException($"Manifest path '{parent}' is both a file and a directory.");
                }
            }

            if (request.DraftWorkspaceMode is not ("ReviewedSourceDraft" or "HyperlightSourceDraft"))
                throw new ArgumentException("Select one supported source-draft workspace mode.");
            if (request.DraftWorkspaceMode == "HyperlightSourceDraft" && string.IsNullOrWhiteSpace(request.DraftGuestLanguageKey))
                throw new ArgumentException("Isolated script generation requires one selected guest language.");
            if (request.DraftWorkspaceMode == "ReviewedSourceDraft") request.DraftGuestLanguageKey = string.Empty;
            var policy = runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson");
            if (manifest.Count > Math.Max(1, policy.MaximumManifestFiles) || manifest.Values.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("The draft manifest exceeds the configured file count or contains an empty work order.");
            // Validate the registered output contract before creating the run-owned project or draft.
            foreach (var output in request.Outputs ?? [])
            {
                if (output is null) throw new ArgumentException("Manifest output entries must be objects.");
                output.Kind = NormalizeOutputKind(output.Kind);
            }
            request.DraftInspections = [];
            request.DraftManifest = manifest;
            request.DraftDependencies = NormalizeDraftDependencies(manifest, request.DraftDependencies);
            request.DraftAssignments = [];
            request.DraftRepairSources = new(StringComparer.OrdinalIgnoreCase);
            request.DraftRepairReviewId = null;
            request.DraftRepairReviewHash = string.Empty;
            request.Files = [];
            // Greenfield draft authoring always gets its own stable database project. Never
            // inherit an unrelated selected repository as the base of new source generation.
            var project = await projectArchitecture.EnsureCouncilRunProjectAsync(runId, request.Title,
                request.Goal, cancellationToken).ConfigureAwait(false);
            request.ProjectId = project.Project.Id;
            request.ProjectRevisionId = project.Revision.Id;
            request.ProjectTopicId = null;
            var draft = new CodeGenerationChangeReview
            {
                Id = runId,
                CouncilRunId = runId,
                ProjectId = request.ProjectId,
                ProjectRevisionId = request.ProjectRevisionId,
                Title = request.Title,
                Goal = request.Goal,
                Status = DraftStatus,
                PayloadJson = JsonSerializer.Serialize(request, JsonOptions)
            };
            db.CodeGenerationChangeReviews.Add(draft);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return draft;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Council source draft initialization canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council source draft initialization failed.");
            throw;
        }
    }
}
