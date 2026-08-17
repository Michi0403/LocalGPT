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
            if (entity.ProjectId is Guid projectId)
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
            foreach (var output in payload.Outputs)
                await ScaffoldOutputAsync(workspaceRoot, output, reviewedSources, result.WrittenFiles, buildTargets, cancellationToken).ConfigureAwait(false);

            var registeredSolutionPath = string.Empty;
            if (entity.ProjectId is Guid registeredProjectId && entity.ProjectRevisionId is Guid registeredRevisionId)
            {
                registeredSolutionPath = FindPreferredSolutionPath(workspaceRoot, clonedSolutionPath);
                await projectMaintenance.RegisterRevisionWorkspaceAsync(
                    registeredProjectId,
                    registeredRevisionId,
                    workspaceRoot,
                    registeredSolutionPath,
                    userConfirmed: true,
                    cancellationToken).ConfigureAwait(false);
                await projectMaintenance.ScanProjectFilesAsync(
                    registeredProjectId,
                    new ScanProjectFilesRequest
                    {
                        RevisionId = registeredRevisionId,
                        UserConfirmed = true
                    },
                    cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Registered and scanned generated workspace for project {ProjectId} revision {RevisionId} after approved code generation.", registeredProjectId, registeredRevisionId);
            }

            if (request.BuildAfterGeneration)
            {
                if (buildTargets.Count == 0 && !string.IsNullOrWhiteSpace(registeredSolutionPath) && File.Exists(registeredSolutionPath))
                    buildTargets.Add(registeredSolutionPath);
                if (buildTargets.Count == 0)
                {
                    result.Warnings.Add("Build was requested, but the review contains no .sln or .csproj output target.");
                    entity.BuildStatus = "NoBuildTarget";
                }
                else
                {
                    var buildStatuses = new List<string>();
                    foreach (var buildTarget in buildTargets.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        var buildResult = await artifactBuildExecutor.BuildAsync(
                            buildTarget,
                            workspaceRoot,
                            "Release",
                            outputDirectory: null,
                            requestedTimeout: TimeSpan.FromMinutes(5),
                            cancellationToken,
                            userConfirmed: request.UserConfirmedBuild).ConfigureAwait(false);
                        buildStatuses.Add($"{Path.GetFileName(buildTarget)}:{buildResult.Status}");
                        logger.LogInformation(
                            "Bounded build for review {ReviewId} target {TargetName} completed with status {BuildStatus} and exit code {ExitCode}.",
                            entity.Id,
                            Path.GetFileName(buildTarget),
                            buildResult.Status,
                            buildResult.ExitCode);
                    }

                    entity.BuildStatus = string.Join(";", buildStatuses);
                    result.BuildStatus = entity.BuildStatus;
                }
            }

            var zipFileName = $"localgpt-change-review-{entity.Id:N}.zip";
            var zipPath = Path.Combine(councilArtifacts.ArtifactRoot, zipFileName);
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            ZipFile.CreateFromDirectory(workspaceRoot, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            entity.ZipFileName = zipFileName;
            entity.Status = request.BuildAfterGeneration
                ? BuildCompletedSuccessfully(entity.BuildStatus)
                    ? CodeGenerationReviewStatuses.BuildPassed
                    : CodeGenerationReviewStatuses.BuildFailed
                : CodeGenerationReviewStatuses.Generated;
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
                .SingleOrDefaultAsync(review => review.Id == reviewId, cancellationToken)
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
