using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates code generation workflow behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CodeGenerationWorkflowService
    {
    /// <summary>
    /// Executes review as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The code generation execution result produced by the operation.</returns>
    public async Task<CodeGenerationExecutionResult> ExecuteReviewAsync(
        Guid reviewId,
        ExecuteCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var scope = BeginReviewScope("ExecuteCodeGenerationReview", reviewId);

        var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
        var entity = await db.CodeGenerationChangeReviews
            .SingleOrDefaultAsync(review => review.Id == reviewId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Code-generation review {reviewId} was not found.");

        if (!request.UserConfirmed)
            throw new InvalidOperationException("Fresh human confirmation is required for this exact reviewed generation operation.");
        if (entity.ApprovalConsumed)
            throw new InvalidOperationException("The approval for this review was already consumed. Create a new review for another generation attempt.");
        if (!string.Equals(entity.Status, CodeGenerationReviewStatuses.AwaitingUserDecision, StringComparison.Ordinal))
            throw new InvalidOperationException($"Review {reviewId} cannot be executed from status {entity.Status}.");
        if (string.IsNullOrWhiteSpace(request.ExpectedReviewHash) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(entity.ReviewHash),
                Encoding.UTF8.GetBytes(request.ExpectedReviewHash.Trim())))
        {
            throw new InvalidOperationException("The reviewed content changed or the confirmation hash does not match. Re-open the review before approving generation.");
        }
        if (request.BuildAfterGeneration && !request.UserConfirmedBuild)
            throw new InvalidOperationException("A separate current human confirmation is required before invoking the bounded .NET build.");

        // Even a review created by an earlier binary must not consume approval or write bytes
        // unless the complete project and the current run's resolved human decisions agree.
        var reviewedPayload = DeserializePayload(entity.PayloadJson);
        if (reviewedPayload.DraftManifest is not null)
        {
            var runId = entity.CouncilRunId ?? throw new InvalidOperationException("A reviewed Council source draft must retain its run identity.");
            var decisions = await humanCollaboration.GetSourcePlanningDecisionsAsync(runId, cancellationToken).ConfigureAwait(false);
            var diagnostic = ValidateCompletedSourceCoverage(reviewedPayload, entity.Goal, decisions);
            if (!string.IsNullOrWhiteSpace(diagnostic))
                throw new InvalidOperationException("Approval has not been consumed. Source structure needs a new reviewed repair: " + diagnostic);
            foreach (var source in reviewedPayload.Files)
            {
                diagnostic = ValidateDraftProjectTarget(entity.Goal, source.RelativePath, source.Content);
                if (!string.IsNullOrWhiteSpace(diagnostic))
                    throw new InvalidOperationException("Approval has not been consumed. " + source.RelativePath + ": " + diagnostic);
            }
        }

        entity.Status = CodeGenerationReviewStatuses.Generating;
        entity.DecisionNote = ValueOrFallback(request.DecisionNote, "Approved by the current user.");
        entity.DecidedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.ApprovalConsumed = true;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new CodeGenerationExecutionResult
        {
            ReviewId = entity.Id,
            ReviewHash = entity.ReviewHash,
            Status = CodeGenerationReviewStatuses.Generating
        };

        try
        {
            var payload = DeserializePayload(entity.PayloadJson);
            var workspaceName = $"review-{entity.Id:N}";
            var workspaceBase = councilArtifacts.ArtifactRoot;
            if (entity.ProjectId is Guid workspaceProjectId)
            {
                var resolution = await projectMaintenance.ResolveWorkspaceAsync(workspaceProjectId, cancellationToken).ConfigureAwait(false);
                workspaceBase = resolution.RootPath;
            }
            var workspaceRoot = Path.Combine(workspaceBase, "LocalGPT-CodeGeneration", workspaceName);
            if (Directory.Exists(workspaceRoot))
                Directory.Delete(workspaceRoot, recursive: true);
            Directory.CreateDirectory(workspaceRoot);

            entity.WorkspaceName = workspaceName;
            result.WorkspaceName = workspaceName;
            result.WorkspacePath = workspaceRoot;

            string clonedSolutionPath = string.Empty;
            if (payload.DraftManifest is null && entity.ProjectId is Guid projectId)
                clonedSolutionPath = await CopyTrackedProjectIntoWorkspaceAsync(workspaceRoot, projectId, entity.ProjectRevisionId, result, cancellationToken).ConfigureAwait(false);

            await WriteReviewDocumentAsync(workspaceRoot, entity, payload, cancellationToken).ConfigureAwait(false);
            result.WrittenFiles.Add("CHANGE_REVIEW.md");
            var reviewedSources = new List<ReviewedSourceArtifact>();

            foreach (var file in payload.Files)
            {
                var relativePath = NormalizeRelativePath(file.RelativePath);
                var fullPath = ResolveInsideRoot(workspaceRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? workspaceRoot);
                await File.WriteAllTextAsync(fullPath, file.Content, cancellationToken).ConfigureAwait(false);
                result.WrittenFiles.Add(relativePath.Replace('\\', '/'));
                reviewedSources.Add(new ReviewedSourceArtifact(relativePath, fullPath));
                logger.LogDebug("Wrote reviewed source file {RelativePath} for review {ReviewId}.", relativePath, entity.Id);
            }

            foreach (var typeSpec in payload.CodeDomTypes)
            {
                var relativePath = NormalizeRelativePath(typeSpec.RelativePath);
                var fullPath = ResolveInsideRoot(workspaceRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? workspaceRoot);
                try
                {
                    var source = GenerateCodeDomSource(typeSpec);
                    await File.WriteAllTextAsync(fullPath, source, cancellationToken).ConfigureAwait(false);
                    if (!result.WrittenFiles.Contains(relativePath.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase))
                        result.WrittenFiles.Add(relativePath.Replace('\\', '/'));
                    if (!reviewedSources.Any(sourceArtifact => string.Equals(sourceArtifact.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase)))
                        reviewedSources.Add(new ReviewedSourceArtifact(relativePath, fullPath));
                    logger.LogDebug("Generated reviewed CodeDOM source file {RelativePath} for review {ReviewId}.", relativePath, entity.Id);
                }
                catch (Exception codeDomException) when (codeDomException is not OperationCanceledException)
                {
                    // CodeDOM is optional. A reviewed explicit source file with the same path wins; otherwise
                    // generate the equivalent minimal C# source with the plain-text fallback writer.
                    if (!File.Exists(fullPath))
                    {
                        var fallbackSource = GeneratePlainCSharpFallbackSource(typeSpec);
                        await File.WriteAllTextAsync(fullPath, fallbackSource, cancellationToken).ConfigureAwait(false);
                    }
                    if (!result.WrittenFiles.Contains(relativePath.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase))
                        result.WrittenFiles.Add(relativePath.Replace('\\', '/'));
                    if (!reviewedSources.Any(sourceArtifact => string.Equals(sourceArtifact.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase)))
                        reviewedSources.Add(new ReviewedSourceArtifact(relativePath, fullPath));
                    result.Warnings.Add($"CodeDOM generation failed for {relativePath}; LocalGPT used the reviewed/plain-file fallback route instead. Review application logs for the CodeDOM exception.");
                    logger.LogWarning(codeDomException, "CodeDOM generation failed for reviewed file {RelativePath}; plain-file fallback was used.", relativePath);
                }
            }

            var buildTargets = new List<string>();
            if (payload.DraftManifest is null)
            {
                foreach (var output in payload.Outputs)
                    await ScaffoldOutputAsync(workspaceRoot, output, reviewedSources, result.WrittenFiles, buildTargets, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Every authored source path is already explicit. Scaffolding would introduce unreviewed duplicate project files.
                var solutions = reviewedSources.Where(source =>
                    IsSourceSolutionPath(source.RelativePath)).ToList();
                buildTargets.AddRange((solutions.Count > 0 ? solutions : reviewedSources.Where(source =>
                    IsSourceProjectPath(source.RelativePath)).ToList())
                    .Select(source => ResolveInsideRoot(workspaceRoot, source.RelativePath)));
            }

            var registeredSolutionPath = string.Empty;
            if (entity.ProjectId is Guid registeredProjectId && entity.ProjectRevisionId is Guid baseRevisionId)
            {
                // The generated workspace becomes a NEW child revision. The reviewed base revision keeps its source root
                // and snapshot hash, so the original version stays recoverable and every generation is its own version.
                registeredSolutionPath = reviewedSources
                    .Where(source => IsSourceSolutionPath(source.RelativePath))
                    .Select(source => ResolveInsideRoot(workspaceRoot, source.RelativePath))
                    .FirstOrDefault(File.Exists) ?? FindPreferredSolutionPath(workspaceRoot, clonedSolutionPath);
                var baseRevision = await db.LocalGptProjectRevisions.AsNoTracking()
                    .SingleOrDefaultAsync(revision => revision.Id == baseRevisionId && revision.ProjectId == registeredProjectId, cancellationToken)
                    .ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Base revision {baseRevisionId} of project {registeredProjectId} was not found.");
                var generationSuffix = $" · generated {entity.Id.ToString("N")[..8]}";
                var baseName = string.IsNullOrWhiteSpace(baseRevision.RevisionName) ? "revision" : baseRevision.RevisionName.Trim();
                var childRevision = await projectArchitecture.SaveRevisionAsync(
                    registeredProjectId,
                    new SaveProjectRevisionRequest
                    {
                        ParentRevisionId = baseRevisionId,
                        BranchName = string.IsNullOrWhiteSpace(baseRevision.BranchName) ? "main" : baseRevision.BranchName,
                        RevisionName = baseName[..Math.Min(baseName.Length, 160 - generationSuffix.Length)] + generationSuffix,
                        Summary = $"Generated by approved code-generation review {entity.Id} from revision {baseRevisionId}.",
                        ProjectStructureJson = string.IsNullOrWhiteSpace(baseRevision.ProjectStructureJson) ? "{}" : baseRevision.ProjectStructureJson,
                        IsCurrent = true,
                        UserConfirmed = true
                    },
                    cancellationToken).ConfigureAwait(false);
                await projectMaintenance.RegisterRevisionWorkspaceAsync(
                    registeredProjectId,
                    childRevision.Id,
                    workspaceRoot,
                    registeredSolutionPath,
                    userConfirmed: true,
                    cancellationToken).ConfigureAwait(false);
                await projectMaintenance.ScanProjectFilesAsync(
                    registeredProjectId,
                    new ScanProjectFilesRequest
                    {
                        RevisionId = childRevision.Id,
                        UserConfirmed = true
                    },
                    cancellationToken).ConfigureAwait(false);
                result.GeneratedRevisionId = childRevision.Id;
                logger.LogInformation(
                    "Registered and scanned generated workspace of review {ReviewId} as child revision {ChildRevisionId} of base revision {BaseRevisionId} in project {ProjectId}.",
                    entity.Id, childRevision.Id, baseRevisionId, registeredProjectId);
            }

            if (request.BuildAfterGeneration)
            {
                // SourceFiles reviews may already contain a complete project without scaffolded output targets.
                // Use only the exact approved materialized source paths, never discover unrelated host files.
                if (buildTargets.Count == 0)
                {
                    var explicitSolutions = reviewedSources.Where(source =>
                        IsSourceSolutionPath(source.RelativePath)).ToList();
                    var explicitTargets = explicitSolutions.Count > 0 ? explicitSolutions : reviewedSources.Where(source =>
                        IsSourceProjectPath(source.RelativePath)).ToList();
                    buildTargets.AddRange(explicitTargets.Select(source => ResolveInsideRoot(workspaceRoot, source.RelativePath)).Where(File.Exists));
                }
                if (buildTargets.Count == 0)
                {
                    // Plain-file reviews are not limited to CodeDOM or MSBuild. Recognize
                    // configured project targets through the persisted workspace regex,
                    // but only among explicitly reviewed/materialized source files.
                    var configuredRoots = await projectMaintenance.GetWorkspaceRootsAsync(entity.ProjectId, cancellationToken).ConfigureAwait(false);
                    var registeredRoots = configuredRoots.Where(item => item.IsEnabled &&
                            item.PreferredCompilerInstallationId.HasValue && !string.IsNullOrWhiteSpace(item.BuildArguments) &&
                            ((entity.ProjectId.HasValue && item.ProjectId == entity.ProjectId) ||
                             (!string.IsNullOrWhiteSpace(item.RootPath) && platform.IsSameOrDescendantPath(item.RootPath, workspaceRoot))))
                        .OrderByDescending(item => entity.ProjectId.HasValue && item.ProjectId == entity.ProjectId)
                        .ThenBy(item => item.Priority);
                    foreach (var configuredRoot in registeredRoots)
                    {
                        try
                        {
                            var matches = reviewedSources.Where(source =>
                                System.Text.RegularExpressions.Regex.IsMatch(source.RelativePath.Replace('\\', '/'), configuredRoot.SolutionPattern,
                                    System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                                    runtimePolicy.RegexTimeout)).ToList();
                            if (matches.Count == 0) continue;
                            buildTargets.AddRange(matches.Select(source => ResolveInsideRoot(workspaceRoot, source.RelativePath)).Where(File.Exists));
                            break;
                        }
                        catch (Exception exception) when (exception is ArgumentException or System.Text.RegularExpressions.RegexMatchTimeoutException)
                        {
                            logger.LogWarning(exception, "Configured workspace build-target regex was invalid; no source build target was selected from it.");
                        }
                    }
                }
                if (buildTargets.Count == 0 && !string.IsNullOrWhiteSpace(registeredSolutionPath) && File.Exists(registeredSolutionPath))
                    buildTargets.Add(registeredSolutionPath);
                if (buildTargets.Count == 0)
                {
                    result.Warnings.Add("Build was requested, but the reviewed source contains no configured solution file or compiler project target.");
                    entity.BuildStatus = "NoBuildTarget";
                }
                else
                {
                    var buildStatuses = new List<string>();
                    var failedDiagnostics = new System.Text.StringBuilder();
                    foreach (var buildTarget in buildTargets.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        ArtifactBuildExecutionResult buildResult;
                        try
                        {
                            buildResult = await artifactBuildExecutor.BuildAsync(
                                buildTarget,
                                workspaceRoot,
                                "Release",
                                outputDirectory: null,
                                requestedTimeout: TimeSpan.FromMinutes(5),
                                cancellationToken,
                                userConfirmed: request.UserConfirmedBuild,
                                projectId: entity.ProjectId).ConfigureAwait(false);
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            // A toolchain/preflight exception cannot attest a build, but must not discard the approved source archive.
                            logger.LogWarning(exception, "Build check failed for review {ReviewId} target {TargetName}; preserving written source.", entity.Id, Path.GetFileName(buildTarget));
                            buildStatuses.Add($"{Path.GetFileName(buildTarget)}:BuildCheckError");
                            failedDiagnostics.AppendLine($"{Path.GetFileName(buildTarget)}: BuildCheckError ({exception.GetType().Name})");
                            continue;
                        }
                        buildStatuses.Add($"{Path.GetFileName(buildTarget)}:{buildResult.Status}");
                        if (!buildResult.Succeeded)
                        {
                            var errorLines = buildDiagnostics.Summarize(buildResult.StandardOutput, buildResult.StandardError);
                            if (!string.IsNullOrWhiteSpace(errorLines))
                                failedDiagnostics.AppendLine($"{Path.GetFileName(buildTarget)}:").AppendLine(errorLines);
                        }
                        logger.LogInformation(
                            "Bounded build for review {ReviewId} target {TargetName} completed with status {BuildStatus} and exit code {ExitCode}.",
                            entity.Id,
                            Path.GetFileName(buildTarget),
                            buildResult.Status,
                            buildResult.ExitCode);
                    }

                    entity.BuildStatus = string.Join(";", buildStatuses);
                    result.BuildStatus = entity.BuildStatus;
                    result.BuildDiagnostics = failedDiagnostics.ToString().TrimEnd();
                }
            }

            if (!request.BuildAfterGeneration)
                entity.BuildStatus = "Disabled";

            var zipFileName = $"localgpt-change-review-{entity.Id:N}.zip";
            var zipPath = Path.Combine(councilArtifacts.ArtifactRoot, zipFileName);
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            ZipFile.CreateFromDirectory(workspaceRoot, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            entity.ZipFileName = zipFileName;
            // A disabled or unavailable toolchain is not a compiler failure. The approved source
            // remains downloadable, while verified delivery still requires an actual successful build.
            entity.Status = !request.BuildAfterGeneration ? CodeGenerationReviewStatuses.Generated
                : BuildCompletedSuccessfully(entity.BuildStatus) ? CodeGenerationReviewStatuses.BuildPassed
                : entity.BuildStatus.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(item => item.EndsWith(":BuildFailed", StringComparison.OrdinalIgnoreCase))
                    ? CodeGenerationReviewStatuses.BuildFailed
                : entity.BuildStatus.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(item => item.EndsWith(":Disabled", StringComparison.OrdinalIgnoreCase))
                    ? CodeGenerationReviewStatuses.BuildDisabled
                : CodeGenerationReviewStatuses.BuildNotAttempted;
            entity.CompletedAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            result.Status = entity.Status;
            result.ZipFileName = zipFileName;
            result.DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(zipFileName)}";
            result.BuildStatus = entity.BuildStatus;

            logger.LogInformation(
                "Completed code-generation review {ReviewId} with status {Status}, {WrittenFileCount} written file(s), workspace {WorkspaceName}, and zip {ZipFileName}.",
                entity.Id,
                entity.Status,
                result.WrittenFiles.Count,
                workspaceName,
                zipFileName);
            return result;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            entity.Status = CodeGenerationReviewStatuses.Failed;
            entity.DecisionNote = ValueOrFallback($"{entity.DecisionNote} Generation was cancelled.", "Generation was cancelled.");
            entity.UpdatedAtUtc = DateTime.UtcNow;
            entity.CompletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            logger.LogInformation(exception, "Code-generation review {ReviewId} was cancelled after approval was consumed.", reviewId);
            throw;
        }
        catch (Exception ex)
        {
            entity.Status = CodeGenerationReviewStatuses.Failed;
            entity.BuildStatus = "GenerationFailed";
            entity.UpdatedAtUtc = DateTime.UtcNow;
            entity.CompletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            logger.LogError(ex, "Code-generation review {ReviewId} failed after approval was consumed; generated payload content was omitted from logs.", reviewId);
            throw;
        }
    }

    /// <summary>
    /// Rejects review as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The code generation review snapshot produced by the operation.</returns>
    public async Task<CodeGenerationReviewSnapshot> RejectReviewAsync(
        Guid reviewId,
        RejectCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            using var scope = BeginReviewScope("RejectCodeGenerationReview", reviewId);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required to reject this exact review.");

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var entity = await db.CodeGenerationChangeReviews
                .SingleOrDefaultAsync(review => review.Id == reviewId && review.Status != DraftStatus, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Code-generation review {reviewId} was not found.");
            if (entity.ApprovalConsumed)
                throw new InvalidOperationException("This review has already been consumed and cannot be rejected retroactively.");
            if (!string.Equals(entity.ReviewHash, request.ExpectedReviewHash?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The review hash does not match the review currently stored in LocalGPT.");

            entity.Status = CodeGenerationReviewStatuses.Rejected;
            entity.DecisionNote = ValueOrFallback(request.DecisionNote, "Rejected by the current user.");
            entity.DecidedAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Rejected code-generation review {ReviewId}.", reviewId);
            return ToSnapshot(entity, DeserializePayload(entity.PayloadJson));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(RejectReviewAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(RejectReviewAsync)} failed.");
        throw;
    }
}

    }
}
