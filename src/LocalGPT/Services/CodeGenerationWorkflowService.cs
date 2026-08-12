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

namespace LocalGPT.Services;

/// <summary>
/// Provides code generation workflow service operations.
/// </summary>
public sealed class CodeGenerationWorkflowService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ICouncilArtifactService councilArtifacts,
    IArtifactBuildExecutor artifactBuildExecutor,
    IProjectMaintenanceService projectMaintenance,
    IRegexPatternService regexPatterns,
    ILogger<CodeGenerationWorkflowService> logger) : ICodeGenerationWorkflowService
{

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Creates review async.
    /// </summary>
    public async Task<CodeGenerationReviewSnapshot> CreateReviewAsync(
        CreateCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            var operationId = Guid.NewGuid();
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["OperationId"] = operationId,
                ["Operation"] = "CreateCodeGenerationReview",
                ["ProjectId"] = request.ProjectId,
                ["ProjectRevisionId"] = request.ProjectRevisionId,
                ["CouncilRunId"] = request.CouncilRunId
            });

            request.Files ??= [];
            request.CodeDomTypes ??= [];
            request.Outputs ??= [];
            await EnrichOutputIntentAsync(request).ConfigureAwait(false);
            ValidateReviewRequest(request);
            var payload = new CodeGenerationReviewPayload
            {
                Files = request.Files.Select(NormalizeFile).ToList(),
                CodeDomTypes = request.CodeDomTypes.Select(NormalizeCodeDomType).ToList(),
                Outputs = request.Outputs.Count == 0
                    ? [new CodeGenerationOutputSpec()]
                    : request.Outputs.Select(NormalizeOutput).ToList()
            };
            var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await ValidateProjectReferencesAsync(db, request.ProjectId, request.ProjectRevisionId, request.ProjectTopicId, cancellationToken).ConfigureAwait(false);

            var entity = new CodeGenerationChangeReview
            {
                ProjectId = request.ProjectId,
                ProjectRevisionId = request.ProjectRevisionId,
                ProjectTopicId = request.ProjectTopicId,
                CouncilRunId = request.CouncilRunId,
                Title = ValueOrFallback(request.Title, "Code generation change review"),
                Goal = ValueOrFallback(request.Goal, "Generate a reviewed LocalGPT artifact."),
                CurrentProjectState = ValueOrFallback(request.CurrentProjectState, "Current project state was not supplied."),
                CouncilSummary = ValueOrFallback(request.CouncilSummary, "Council summary was not supplied."),
                ChangeSummary = ValueOrFallback(request.ChangeSummary, BuildDefaultChangeSummary(payload)),
                SafetySummary = ValueOrFallback(request.SafetySummary,
                    "Generation is restricted to an isolated LocalGPT workspace. When a project revision is selected, approved tracked files are cloned byte-for-byte before reviewed changes are applied. No generated program or script is executed automatically. Builds require a separate current human confirmation."),
                PayloadJson = payloadJson,
                Status = CodeGenerationReviewStatuses.AwaitingUserDecision,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            entity.ReviewHash = ComputeReviewHash(entity);

            db.CodeGenerationChangeReviews.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Created code-generation review {ReviewId} with {FileCount} source file(s), {CodeDomCount} CodeDOM type(s), {OutputCount} output target(s), and review hash prefix {ReviewHashPrefix}.",
                entity.Id,
                payload.Files.Count,
                payload.CodeDomTypes.Count,
                payload.Outputs.Count,
                HashPrefix(entity.ReviewHash));

            return ToSnapshot(entity, payload);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CreateReviewAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CreateReviewAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Gets review async.
    /// </summary>
    public async Task<CodeGenerationReviewSnapshot?> GetReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
    try
    {
            using var scope = BeginReviewScope("GetCodeGenerationReview", reviewId);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entity = await db.CodeGenerationChangeReviews
                .AsNoTracking()
                .SingleOrDefaultAsync(review => review.Id == reviewId, cancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
            {
                logger.LogInformation("Code-generation review {ReviewId} was not found.", reviewId);
                return null;
            }

            logger.LogDebug("Loaded code-generation review {ReviewId} with status {Status}.", reviewId, entity.Status);
            return ToSnapshot(entity, DeserializePayload(entity.PayloadJson));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(GetReviewAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(GetReviewAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the list reviews async operation.
    /// </summary>
    public async Task<IReadOnlyList<CodeGenerationReviewSnapshot>> ListReviewsAsync(
        Guid? projectId = null,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
    try
    {
            var operationId = Guid.NewGuid();
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["OperationId"] = operationId,
                ["Operation"] = "ListCodeGenerationReviews",
                ["ProjectId"] = projectId
            });

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.CodeGenerationChangeReviews.AsNoTracking();
            if (projectId is Guid selectedProjectId)
                query = query.Where(review => review.ProjectId == selectedProjectId);

            var entities = await query
                .OrderByDescending(review => review.UpdatedAtUtc)
                .Take(Math.Max(1, take))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Listed {ReviewCount} code-generation review(s).", entities.Count);
            return entities.Select(entity => ToSnapshot(entity, DeserializePayload(entity.PayloadJson))).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ListReviewsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ListReviewsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the execute review async operation.
    /// </summary>
    public async Task<CodeGenerationExecutionResult> ExecuteReviewAsync(
        Guid reviewId,
        ExecuteCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var scope = BeginReviewScope("ExecuteCodeGenerationReview", reviewId);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
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
    /// Runs the reject review async operation.
    /// </summary>
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

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Runs the begin review scope operation.
    /// </summary>
    private IDisposable? BeginReviewScope(string operation, Guid reviewId) {
    try
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = Guid.NewGuid(),
            ["Operation"] = operation,
            ["ReviewId"] = reviewId
        });
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BeginReviewScope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BeginReviewScope)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the enrich output intent async operation.
    /// </summary>
    private async Task EnrichOutputIntentAsync(CreateCodeGenerationReviewRequest request)
    {
    try
    {
            if (request.Outputs.Count > 0 || request.Files.Count > 0 || request.CodeDomTypes.Count > 0)
                return;

            var evidence = string.Join(" ", new[] { request.Title, request.Goal, request.ChangeSummary }.Where(value => !string.IsNullOrWhiteSpace(value)));
            var kind = await MatchIntentAsync("builtin.codegen-powershell-script-pattern", evidence).ConfigureAwait(false)
                ? CodeGenerationOutputKinds.PowerShellScript
                : await MatchIntentAsync("builtin.codegen-addon-pattern", evidence).ConfigureAwait(false)
                    ? CodeGenerationOutputKinds.LocalGptAddon
                    : await MatchIntentAsync("builtin.codegen-solution-pattern", evidence).ConfigureAwait(false)
                    ? CodeGenerationOutputKinds.Solution
                    : await MatchIntentAsync("builtin.codegen-console-application-pattern", evidence).ConfigureAwait(false)
                        ? CodeGenerationOutputKinds.ConsoleApplication
                        : await MatchIntentAsync("builtin.codegen-class-library-pattern", evidence).ConfigureAwait(false)
                            ? CodeGenerationOutputKinds.ClassLibrary
                            : string.Empty;

            if (string.IsNullOrWhiteSpace(kind))
                return;

            var quotedLiteral = await ExtractQuotedLiteralAsync(evidence).ConfigureAwait(false);
            var name = BuildGeneratedOutputName(request.Title, quotedLiteral ?? request.Goal);
            var description = kind == CodeGenerationOutputKinds.ConsoleApplication && !string.IsNullOrWhiteSpace(quotedLiteral)
                ? quotedLiteral
                : string.IsNullOrWhiteSpace(request.Goal)
                    ? "Generated with LocalGPT after human review."
                    : request.Goal.Trim();
            request.Outputs.Add(new CodeGenerationOutputSpec
            {
                Kind = kind,
                Name = name,
                RelativeDirectory = ".",
                TargetFramework = "net10.0",
                RootNamespace = $"LocalGPT.Generated.{name}",
                Description = description
            });
            logger.LogInformation("Resolved incomplete code-generation review into database-regex-selected output kind {OutputKind}; no source text was logged.", kind);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(EnrichOutputIntentAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(EnrichOutputIntentAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the extract quoted literal async operation.
    /// </summary>
    private async Task<string?> ExtractQuotedLiteralAsync(string text)
    {
        try
        {
            var pattern = await regexPatterns.GetRegexAsync("builtin.codegen-quoted-literal-pattern").ConfigureAwait(false);
            var match = pattern?.Match(text);
            var value = match?.Groups["text"].Success == true ? match.Groups["text"].Value.Trim() : string.Empty;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not evaluate the database-backed quoted-literal pattern for code generation.");
            return null;
        }
    }

    /// <summary>
    /// Runs the match intent async operation.
    /// </summary>
    private async Task<bool> MatchIntentAsync(string patternName, string text)
    {
        try
        {
            var pattern = await regexPatterns.GetRegexAsync(patternName).ConfigureAwait(false);
            return pattern?.IsMatch(text) == true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not evaluate database-backed code-generation intent pattern {PatternName}.", patternName);
            return false;
        }
    }

    /// <summary>
    /// Builds generated output name.
    /// </summary>
    private string BuildGeneratedOutputName(string? title, string? goal)
    {
    try
    {
            var source = string.IsNullOrWhiteSpace(title) ? goal : title;
            var words = Regex.Matches(source ?? string.Empty, "[A-Za-z0-9]+")
                .Select(match => match.Value)
                .Where(word => !word.Equals("create", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("generate", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("build", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("a", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("an", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("the", StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..])
                .ToList();
            var name = string.Concat(words);
            if (string.IsNullOrWhiteSpace(name))
                name = "GeneratedFeature";
            if (char.IsDigit(name[0]))
                name = "Generated" + name;
            return name[..Math.Min(name.Length, 80)];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildGeneratedOutputName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildGeneratedOutputName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates review request.
    /// </summary>
    private void ValidateReviewRequest(CreateCodeGenerationReviewRequest request)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(request.Goal))
                throw new ArgumentException("A concrete generation goal is required.", nameof(request));
            if (request.Files.Count == 0 && request.CodeDomTypes.Count == 0 && request.Outputs.Count == 0)
                throw new ArgumentException("The generation request needs reviewed files, CodeDOM types, or a concrete output target. LocalGPT could not infer one from the current database-backed code-generation regex catalog.", nameof(request));
            foreach (var file in request.Files)
                _ = NormalizeRelativePath(file.RelativePath);
            foreach (var type in request.CodeDomTypes)
                _ = NormalizeRelativePath(type.RelativePath);
            foreach (var output in request.Outputs)
                _ = NormalizeRelativePath(string.IsNullOrWhiteSpace(output.RelativeDirectory) ? "." : output.RelativeDirectory);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValidateReviewRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValidateReviewRequest)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates project references async.
    /// </summary>
    private async Task ValidateProjectReferencesAsync(
        LocalGptMemoryDbContext db,
        Guid? projectId,
        Guid? projectRevisionId,
        Guid? projectTopicId,
        CancellationToken cancellationToken)
    {
    try
    {
            if (projectId is Guid selectedProjectId)
            {
                var projectExists = await db.LocalGptProjects
                    .AnyAsync(project => project.Id == selectedProjectId && !project.IsArchived, cancellationToken)
                    .ConfigureAwait(false);
                if (!projectExists)
                    throw new InvalidOperationException("The selected LocalGPT project does not exist or is archived.");
            }

            if (projectRevisionId is Guid selectedRevisionId)
            {
                if (projectId is not Guid selectedProjectIdInner)
                    throw new InvalidOperationException("A project revision can only be selected together with its project.");
                var revisionExists = await db.LocalGptProjectRevisions
                    .AnyAsync(revision => revision.Id == selectedRevisionId && revision.ProjectId == selectedProjectIdInner && revision.IsUserApproved, cancellationToken)
                    .ConfigureAwait(false);
                if (!revisionExists)
                    throw new InvalidOperationException("The selected project revision does not exist, is not user-approved, or belongs to another project.");
            }

            if (projectTopicId is Guid selectedTopicId)
            {
                var topicExists = await db.LocalGptProjectTopics
                    .AnyAsync(topic => topic.Id == selectedTopicId && topic.IsUserApproved &&
                        (!projectId.HasValue || topic.ProjectId == projectId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (!topicExists)
                    throw new InvalidOperationException("The selected project topic does not exist, is not user-approved, or belongs to another project.");
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValidateProjectReferencesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValidateProjectReferencesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes file.
    /// </summary>
    private CodeGenerationFileSpec NormalizeFile(CodeGenerationFileSpec file) {
    try
    {
        return new()
    {
        RelativePath = NormalizeRelativePath(file.RelativePath),
        Content = file.Content ?? string.Empty,
        Purpose = ValueOrFallback(file.Purpose, "Reviewed source file")
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeFile)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeFile)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes code dom type.
    /// </summary>
    private CodeDomTypeSpec NormalizeCodeDomType(CodeDomTypeSpec type) {
    try
    {
        return new()
    {
        RelativePath = NormalizeRelativePath(type.RelativePath),
        Namespace = NormalizeIdentifierPath(type.Namespace, "LocalGPT.Generated"),
        TypeName = NormalizeIdentifier(type.TypeName, "GeneratedFeature"),
        MethodName = NormalizeIdentifier(type.MethodName, "Describe"),
        MethodResult = ValueOrFallback(type.MethodResult, "Generated with LocalGPT after human review."),
        Summary = ValueOrFallback(type.Summary, "Reviewed CodeDOM source type")
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeCodeDomType)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeCodeDomType)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes output.
    /// </summary>
    private CodeGenerationOutputSpec NormalizeOutput(CodeGenerationOutputSpec output) {
    try
    {
        return new()
    {
        Kind = NormalizeOutputKind(output.Kind),
        Name = NormalizeIdentifier(output.Name, "LocalGptGeneratedFeature"),
        RelativeDirectory = NormalizeRelativePath(string.IsNullOrWhiteSpace(output.RelativeDirectory) ? "." : output.RelativeDirectory),
        TargetFramework = NormalizeTargetFramework(output.TargetFramework),
        RootNamespace = NormalizeIdentifierPath(output.RootNamespace, "LocalGPT.Generated"),
        Description = ValueOrFallback(output.Description, "Reviewed LocalGPT output")
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeOutput)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeOutput)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes output kind.
    /// </summary>
    private string NormalizeOutputKind(string? kind)
    {
    try
    {
            var value = kind?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return CodeGenerationOutputKinds.SourceFiles;

            return value switch
            {
                CodeGenerationOutputKinds.SourceFiles => CodeGenerationOutputKinds.SourceFiles,
                CodeGenerationOutputKinds.ClassLibrary => CodeGenerationOutputKinds.ClassLibrary,
                CodeGenerationOutputKinds.ConsoleApplication => CodeGenerationOutputKinds.ConsoleApplication,
                CodeGenerationOutputKinds.Solution => CodeGenerationOutputKinds.Solution,
                CodeGenerationOutputKinds.LocalGptAddon => CodeGenerationOutputKinds.LocalGptAddon,
                CodeGenerationOutputKinds.CSharpScript => CodeGenerationOutputKinds.CSharpScript,
                CodeGenerationOutputKinds.PowerShellScript => CodeGenerationOutputKinds.PowerShellScript,
                CodeGenerationOutputKinds.JavaScriptModule => CodeGenerationOutputKinds.JavaScriptModule,
                _ => throw new ArgumentException($"Unsupported reviewed output kind '{value}'.")
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeOutputKind)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeOutputKind)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes target framework.
    /// </summary>
    private string NormalizeTargetFramework(string? value)
    {
    try
    {
            var framework = string.IsNullOrWhiteSpace(value) ? "net10.0" : value.Trim();
            if (!framework.StartsWith("net", StringComparison.OrdinalIgnoreCase) || framework.Any(character => !(char.IsLetterOrDigit(character) || character is '.' or '-')))
                throw new ArgumentException("Target framework contains unsupported characters.");
            return framework;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeTargetFramework)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeTargetFramework)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds default change summary.
    /// </summary>
    private string BuildDefaultChangeSummary(CodeGenerationReviewPayload payload) {
    try
    {
        return $"Create {payload.Files.Count} explicit source file(s), {payload.CodeDomTypes.Count} CodeDOM-generated type(s), and {payload.Outputs.Count} output target(s) in an isolated LocalGPT workspace; when a project revision is linked, preserve every unchanged approved tracked file byte-for-byte.";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildDefaultChangeSummary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildDefaultChangeSummary)} failed.");
        throw;
    }
}

    /// <summary>
    /// Computes review hash.
    /// </summary>
    private string ComputeReviewHash(CodeGenerationChangeReview entity)
    {
    try
    {
            var canonical = string.Join("\n",
                entity.ProjectId?.ToString("D") ?? string.Empty,
                entity.ProjectRevisionId?.ToString("D") ?? string.Empty,
                entity.ProjectTopicId?.ToString("D") ?? string.Empty,
                entity.CouncilRunId?.ToString("D") ?? string.Empty,
                entity.Title,
                entity.Goal,
                entity.CurrentProjectState,
                entity.CouncilSummary,
                entity.ChangeSummary,
                entity.SafetySummary,
                entity.PayloadJson);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ComputeReviewHash)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ComputeReviewHash)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the deserialize payload operation.
    /// </summary>
    private CodeGenerationReviewPayload DeserializePayload(string payloadJson)
    {
    try
    {
            var payload = JsonSerializer.Deserialize<CodeGenerationReviewPayload>(payloadJson, JsonOptions) ?? new CodeGenerationReviewPayload();
            payload.Files ??= [];
            payload.CodeDomTypes ??= [];
            payload.Outputs ??= [];
            return payload;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(DeserializePayload)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(DeserializePayload)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the to snapshot operation.
    /// </summary>
    private CodeGenerationReviewSnapshot ToSnapshot(
        CodeGenerationChangeReview entity,
        CodeGenerationReviewPayload payload) {
    try
    {
        return new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        ProjectRevisionId = entity.ProjectRevisionId,
        ProjectTopicId = entity.ProjectTopicId,
        CouncilRunId = entity.CouncilRunId,
        Title = entity.Title,
        Goal = entity.Goal,
        CurrentProjectState = entity.CurrentProjectState,
        CouncilSummary = entity.CouncilSummary,
        ChangeSummary = entity.ChangeSummary,
        SafetySummary = entity.SafetySummary,
        ReviewHash = entity.ReviewHash,
        Status = entity.Status,
        DecisionNote = entity.DecisionNote,
        WorkspaceName = entity.WorkspaceName,
        ZipFileName = entity.ZipFileName,
        BuildStatus = entity.BuildStatus,
        ApprovalConsumed = entity.ApprovalConsumed,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc,
        DecidedAtUtc = entity.DecidedAtUtc,
        CompletedAtUtc = entity.CompletedAtUtc,
        Files = payload.Files.Select(file => new CodeGenerationFileReview
        {
            RelativePath = file.RelativePath,
            Purpose = file.Purpose,
            CharacterCount = file.Content.Length,
            ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content)))
        }).ToList(),
        CodeDomTypes = payload.CodeDomTypes,
        Outputs = payload.Outputs
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ToSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ToSnapshot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Writes review document async.
    /// </summary>
    private async Task WriteReviewDocumentAsync(
        string workspaceRoot,
        CodeGenerationChangeReview review,
        CodeGenerationReviewPayload payload,
        CancellationToken cancellationToken)
    {
    try
    {
            var builder = new StringBuilder()
                .AppendLine("# LocalGPT Change Review")
                .AppendLine()
                .AppendLine($"- Review ID: `{review.Id}`")
                .AppendLine($"- Review hash: `{review.ReviewHash}`")
                .AppendLine($"- Project ID: `{review.ProjectId?.ToString() ?? "not linked"}`")
                .AppendLine($"- Project revision ID: `{review.ProjectRevisionId?.ToString() ?? "not linked"}`")
                .AppendLine($"- Council run ID: `{review.CouncilRunId?.ToString() ?? "not linked"}`")
                .AppendLine()
                .AppendLine("## Goal")
                .AppendLine(review.Goal)
                .AppendLine()
                .AppendLine("## Current project state")
                .AppendLine(review.CurrentProjectState)
                .AppendLine()
                .AppendLine("## Council summary")
                .AppendLine(review.CouncilSummary)
                .AppendLine()
                .AppendLine("## Approved change set")
                .AppendLine(review.ChangeSummary)
                .AppendLine()
                .AppendLine("## Safety and execution boundary")
                .AppendLine(review.SafetySummary)
                .AppendLine()
                .AppendLine("## Files")
                .AppendLine();

            foreach (var file in payload.Files)
                builder.AppendLine($"- `{file.RelativePath}` — {file.Purpose} ({file.Content.Length:n0} characters)");
            foreach (var type in payload.CodeDomTypes)
                builder.AppendLine($"- `{type.RelativePath}` — CodeDOM type `{type.Namespace}.{type.TypeName}`");

            builder.AppendLine().AppendLine("## Outputs").AppendLine();
            foreach (var output in payload.Outputs)
                builder.AppendLine($"- `{output.Kind}` — `{output.RelativeDirectory}` / `{output.Name}` targeting `{output.TargetFramework}`");

            builder.AppendLine()
                .AppendLine("Generated source and scripts are not executed automatically. A bounded .NET build occurs only when separately enabled and confirmed by the current human for this exact review hash.");

            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "CHANGE_REVIEW.md"), builder.ToString(), cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(WriteReviewDocumentAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(WriteReviewDocumentAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the copy tracked project into workspace async operation.
    /// </summary>
    private async Task<string> CopyTrackedProjectIntoWorkspaceAsync(
        string workspaceRoot,
        Guid projectId,
        Guid? revisionId,
        CodeGenerationExecutionResult result,
        CancellationToken cancellationToken)
    {
    try
    {
            var tracked = await projectMaintenance.GetTrackedFilesAsync(projectId, revisionId, cancellationToken).ConfigureAwait(false);
            var approved = tracked.Where(item => item.Exists && item.IsUserApproved && !item.IsGenerated).OrderBy(item => item.ProjectRelativePath, StringComparer.Ordinal).ToList();
            if (approved.Count == 0)
            {
                result.Warnings.Add("No approved tracked project files were available to clone. Scan the selected project revision before executing a maintenance review.");
                return string.Empty;
            }

            string solutionPath = string.Empty;
            foreach (var file in approved)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(file.AbsolutePath))
                    throw new FileNotFoundException("A tracked project file disappeared before the approved maintenance workspace was created.", file.AbsolutePath);
                var sourceHash = await ComputeFileHashAsync(file.AbsolutePath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(sourceHash, file.ContentHash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Tracked file '{file.ProjectRelativePath}' changed after the approved scan. Rescan the revision before creating a maintenance workspace.");
                var relativePath = NormalizeRelativePath(file.ProjectRelativePath);
                var destination = ResolveInsideRoot(workspaceRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? workspaceRoot);
                await using (var source = new FileStream(file.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
                await using (var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
                    await target.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                var destinationHash = await ComputeFileHashAsync(destination, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(sourceHash, destinationHash, StringComparison.Ordinal))
                    throw new IOException($"The isolated copy of '{file.ProjectRelativePath}' did not preserve the approved file bytes.");
                result.WrittenFiles.Add(relativePath.Replace('\\', '/'));
                if (Path.GetExtension(relativePath).Equals(".sln", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(relativePath).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
                    solutionPath = destination;
            }
            logger.LogInformation("Cloned {FileCount} approved tracked file(s) into isolated maintenance workspace for project {ProjectId} revision {RevisionId}; paths omitted from logs.", approved.Count, projectId, revisionId);
            return solutionPath;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CopyTrackedProjectIntoWorkspaceAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CopyTrackedProjectIntoWorkspaceAsync)} failed.");
        throw;
    }
}


    /// <summary>
    /// Computes file hash async.
    /// </summary>
    private async Task<string> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
    try
    {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            return Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ComputeFileHashAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ComputeFileHashAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Finds preferred solution path.
    /// </summary>
    private string FindPreferredSolutionPath(string workspaceRoot, string clonedSolutionPath)
    {
        if (!string.IsNullOrWhiteSpace(clonedSolutionPath) && File.Exists(clonedSolutionPath))
            return clonedSolutionPath;
        try
        {
            return Directory.EnumerateFiles(workspaceRoot, "*.sln", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(workspaceRoot, "*.slnx", SearchOption.AllDirectories))
                .OrderBy(path => path.Count(character => character == Path.DirectorySeparatorChar))
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Could not enumerate solution files under workspace {WorkspaceRoot}.", workspaceRoot);
            return string.Empty;
        }
    }

    /// <summary>
    /// Runs the generate code dom source operation.
    /// </summary>
    private string GenerateCodeDomSource(CodeDomTypeSpec spec)
    {
    try
    {
            var unit = new CodeCompileUnit();
            var ns = new CodeNamespace(spec.Namespace);
            ns.Imports.Add(new CodeNamespaceImport("System"));
            unit.Namespaces.Add(ns);

            var type = new CodeTypeDeclaration(spec.TypeName)
            {
                IsClass = true,
                TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed
            };
            if (!string.IsNullOrWhiteSpace(spec.Summary))
                type.Comments.Add(new CodeCommentStatement(spec.Summary));
            ns.Types.Add(type);

            var method = new CodeMemberMethod
            {
                Name = spec.MethodName,
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference(typeof(string))
            };
            method.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(spec.MethodResult)));
            type.Members.Add(method);

            using var writer = new StringWriter();
            writer.WriteLine("// <auto-generated>");
            writer.WriteLine("// Generated from a user-approved LocalGPT change review.");
            writer.WriteLine("// </auto-generated>");
            writer.WriteLine();
            using var provider = new CSharpCodeProvider();
            provider.GenerateCodeFromCompileUnit(unit, writer, new CodeGeneratorOptions
            {
                BracingStyle = "C",
                BlankLinesBetweenMembers = true
            });
            return writer.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(GenerateCodeDomSource)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(GenerateCodeDomSource)} failed.");
        throw;
    }
}

    /// <summary>
    /// Generates the deterministic plain-text C# fallback used when the platform CodeDOM provider is unavailable.
    /// </summary>
    private string GeneratePlainCSharpFallbackSource(CodeDomTypeSpec spec)
    {
        try
        {
            var summary = System.Security.SecurityElement.Escape(ValueOrFallback(spec.Summary, "Reviewed generated type")) ?? "Reviewed generated type";
            return $$"""
            namespace {{spec.Namespace}};

            /// <summary>{{summary}}</summary>
            public sealed class {{spec.TypeName}}
            {
                /// <summary>Returns the reviewed generated result.</summary>
                public string {{spec.MethodName}}() => "{{EscapeCSharp(spec.MethodResult)}}";
            }
            """;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Plain C# fallback generation failed; reviewed source content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Runs the scaffold output async operation.
    /// </summary>
    private async Task ScaffoldOutputAsync(
        string workspaceRoot,
        CodeGenerationOutputSpec output,
        IReadOnlyList<ReviewedSourceArtifact> reviewedSources,
        List<string> writtenFiles,
        List<string> buildTargets,
        CancellationToken cancellationToken)
    {
    try
    {
            var outputRoot = ResolveInsideRoot(workspaceRoot, output.RelativeDirectory);
            Directory.CreateDirectory(outputRoot);

            switch (output.Kind)
            {
                case CodeGenerationOutputKinds.SourceFiles:
                    return;

                case CodeGenerationOutputKinds.CSharpScript:
                {
                    var copied = await CopyReviewedSourcesAsync(
                        workspaceRoot,
                        outputRoot,
                        reviewedSources.Where(source => Path.GetExtension(source.RelativePath).Equals(".csx", StringComparison.OrdinalIgnoreCase)),
                        writtenFiles,
                        cancellationToken).ConfigureAwait(false);
                    if (!copied)
                    {
                        var fileName = $"{output.Name}.csx";
                        var path = Path.Combine(outputRoot, fileName);
                        if (!File.Exists(path))
                        {
                            await File.WriteAllTextAsync(path,
                                $"// Reviewed C# script source. LocalGPT does not execute this file automatically.{Environment.NewLine}Console.WriteLine(\"{EscapeCSharp(output.Description)}\");{Environment.NewLine}",
                                cancellationToken).ConfigureAwait(false);
                            writtenFiles.Add(Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/'));
                        }
                    }
                    return;
                }

                case CodeGenerationOutputKinds.PowerShellScript:
                {
                    var copied = await CopyReviewedSourcesAsync(
                        workspaceRoot,
                        outputRoot,
                        reviewedSources.Where(source => Path.GetExtension(source.RelativePath).Equals(".ps1", StringComparison.OrdinalIgnoreCase)),
                        writtenFiles,
                        cancellationToken).ConfigureAwait(false);
                    if (!copied)
                    {
                        var fileName = $"{output.Name}.ps1";
                        var path = Path.Combine(outputRoot, fileName);
                        if (!File.Exists(path))
                        {
                            await File.WriteAllTextAsync(path,
                                $"# Reviewed PowerShell source. LocalGPT writes this file but never executes it automatically.{Environment.NewLine}Write-Output {JsonSerializer.Serialize(output.Description)}{Environment.NewLine}",
                                cancellationToken).ConfigureAwait(false);
                            writtenFiles.Add(Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/'));
                        }
                    }
                    return;
                }

                case CodeGenerationOutputKinds.JavaScriptModule:
                {
                    var copied = await CopyReviewedSourcesAsync(
                        workspaceRoot,
                        outputRoot,
                        reviewedSources.Where(source => Path.GetExtension(source.RelativePath).Equals(".js", StringComparison.OrdinalIgnoreCase)),
                        writtenFiles,
                        cancellationToken).ConfigureAwait(false);
                    if (!copied)
                    {
                        var fileName = $"{output.Name}.js";
                        var path = Path.Combine(outputRoot, fileName);
                        if (!File.Exists(path))
                        {
                            await File.WriteAllTextAsync(path,
                                $"// Reviewed JavaScript module source. LocalGPT does not execute this file automatically.{Environment.NewLine}export function describe() {{ return {JsonSerializer.Serialize(output.Description)}; }}{Environment.NewLine}",
                                cancellationToken).ConfigureAwait(false);
                            writtenFiles.Add(Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/'));
                        }
                    }
                    return;
                }

                case CodeGenerationOutputKinds.ClassLibrary:
                case CodeGenerationOutputKinds.ConsoleApplication:
                case CodeGenerationOutputKinds.LocalGptAddon:
                case CodeGenerationOutputKinds.Solution:
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported output kind {output.Kind}.");
            }

            var projectRoot = output.Kind == CodeGenerationOutputKinds.Solution
                ? Path.Combine(outputRoot, output.Name)
                : outputRoot;
            Directory.CreateDirectory(projectRoot);
            var projectPath = Path.Combine(projectRoot, $"{output.Name}.csproj");
            var outputType = output.Kind == CodeGenerationOutputKinds.ConsoleApplication ? "Exe" : "Library";
            var projectXml = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{output.TargetFramework}}</TargetFramework>
                <OutputType>{{outputType}}</OutputType>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RootNamespace>{{output.RootNamespace}}</RootNamespace>
                <AssemblyName>{{output.Name}}</AssemblyName>
                <Deterministic>true</Deterministic>
                <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
              </PropertyGroup>
            </Project>
            """;
            await File.WriteAllTextAsync(projectPath, projectXml, cancellationToken).ConfigureAwait(false);
            writtenFiles.Add(Path.GetRelativePath(workspaceRoot, projectPath).Replace('\\', '/'));
            buildTargets.Add(projectPath);

            await CopyReviewedSourcesAsync(
                workspaceRoot,
                Path.Combine(projectRoot, "ReviewedSources"),
                reviewedSources.Where(source =>
                    Path.GetExtension(source.RelativePath).Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
                    !IsInsideDirectory(source.FullPath, projectRoot)),
                writtenFiles,
                cancellationToken).ConfigureAwait(false);

            var existingCs = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories).Any();
            if (!existingCs)
            {
                var sourcePath = Path.Combine(projectRoot, output.Kind == CodeGenerationOutputKinds.ConsoleApplication ? "Program.cs" : "GeneratedFeature.cs");
                var source = output.Kind switch
                {
                    CodeGenerationOutputKinds.ConsoleApplication =>
                        $"Console.WriteLine(\"{EscapeCSharp(output.Description)}\");{Environment.NewLine}",
                    CodeGenerationOutputKinds.LocalGptAddon => BuildAddonSource(output),
                    _ => BuildLibrarySource(output)
                };
                await File.WriteAllTextAsync(sourcePath, source, cancellationToken).ConfigureAwait(false);
                writtenFiles.Add(Path.GetRelativePath(workspaceRoot, sourcePath).Replace('\\', '/'));
            }

            if (output.Kind == CodeGenerationOutputKinds.LocalGptAddon)
            {
                var manifestPath = Path.Combine(projectRoot, "localgpt-addon.json");
                await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(new
                {
                    id = output.Name,
                    displayName = output.Name,
                    version = "0.1.0",
                    entryType = $"{output.RootNamespace}.LocalGptAddon",
                    approved = false,
                    autoLoad = false,
                    description = output.Description,
                    safety = "Generated addon binaries are never loaded automatically. Review and approve the exact assembly before registration."
                }, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);
                writtenFiles.Add(Path.GetRelativePath(workspaceRoot, manifestPath).Replace('\\', '/'));
            }

            if (output.Kind == CodeGenerationOutputKinds.Solution)
            {
                var solutionPath = Path.Combine(outputRoot, $"{output.Name}.sln");
                await File.WriteAllTextAsync(solutionPath, BuildSolutionFile(output.Name, Path.GetRelativePath(outputRoot, projectPath)), cancellationToken).ConfigureAwait(false);
                writtenFiles.Add(Path.GetRelativePath(workspaceRoot, solutionPath).Replace('\\', '/'));
                buildTargets.Remove(projectPath);
                buildTargets.Add(solutionPath);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ScaffoldOutputAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ScaffoldOutputAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the copy reviewed sources async operation.
    /// </summary>
    private async Task<bool> CopyReviewedSourcesAsync(
        string workspaceRoot,
        string destinationRoot,
        IEnumerable<ReviewedSourceArtifact> sources,
        List<string> writtenFiles,
        CancellationToken cancellationToken)
    {
    try
    {
            var copiedAny = false;
            foreach (var source in sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = NormalizeRelativePath(source.RelativePath);
                var destinationPath = ResolveInsideRoot(destinationRoot, relativePath);
                if (IsInsideDirectory(source.FullPath, destinationRoot))
                {
                    copiedAny = true;
                    continue;
                }

                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (Path.GetFullPath(source.FullPath).Equals(Path.GetFullPath(destinationPath), comparison))
                {
                    copiedAny = true;
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationRoot);
                await using var sourceStream = new FileStream(source.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
                await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                var packagedRelativePath = Path.GetRelativePath(workspaceRoot, destinationPath).Replace('\\', '/');
                if (!writtenFiles.Contains(packagedRelativePath, StringComparer.OrdinalIgnoreCase))
                    writtenFiles.Add(packagedRelativePath);
                copiedAny = true;
                logger.LogDebug("Copied reviewed source {SourcePath} into generated output path {OutputPath}.", relativePath, packagedRelativePath);
            }

            return copiedAny;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CopyReviewedSourcesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CopyReviewedSourcesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds library source.
    /// </summary>
    private string BuildLibrarySource(CodeGenerationOutputSpec output) {
    try
    {
        return $$"""
    namespace {{output.RootNamespace}};

    public sealed class GeneratedFeature
    {
        public string Describe() => "{{EscapeCSharp(output.Description)}}";
    }
    """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildLibrarySource)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildLibrarySource)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds addon source.
    /// </summary>
    private string BuildAddonSource(CodeGenerationOutputSpec output) {
    try
    {
        return $$"""
    namespace {{output.RootNamespace}};

    public interface ILocalGptAddon
    {
        string Id { get; }
        string Describe();
    }

    public sealed class LocalGptAddon : ILocalGptAddon
    {
        public string Id => "{{EscapeCSharp(output.Name)}}";
        public string Describe() => "{{EscapeCSharp(output.Description)}}";
    }
    """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildAddonSource)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildAddonSource)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds solution file.
    /// </summary>
    private string BuildSolutionFile(string name, string relativeProjectPath)
    {
    try
    {
            var projectGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var solutionGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var normalizedProjectPath = relativeProjectPath.Replace('/', '\\');
            var builder = new StringBuilder();

            builder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            builder.AppendLine("# Visual Studio Version 17");
            builder.AppendLine("VisualStudioVersion = 17.0.31903.59");
            builder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
            builder.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{name}\", \"{normalizedProjectPath}\", \"{projectGuid}\"");
            builder.AppendLine("EndProject");
            builder.AppendLine("Global");
            builder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            builder.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
            builder.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            builder.AppendLine($"\t\t{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            builder.AppendLine($"\t\t{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            builder.AppendLine($"\t\t{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            builder.AppendLine($"\t\t{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU");
            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
            builder.AppendLine($"\t\tSolutionGuid = {solutionGuid}");
            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("EndGlobal");
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildSolutionFile)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildSolutionFile)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds completed successfully.
    /// </summary>
    private bool BuildCompletedSuccessfully(string buildStatus)
    {
    try
    {
            var statuses = buildStatus.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return statuses.Length > 0 && statuses.All(status => status.EndsWith(":BuildPassed", StringComparison.OrdinalIgnoreCase));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildCompletedSuccessfully)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildCompletedSuccessfully)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether inside directory.
    /// </summary>
    private bool IsInsideDirectory(string path, string directory)
    {
    try
    {
            var normalizedDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
            var normalizedPath = Path.GetFullPath(path);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(normalizedPath, normalizedDirectory, comparison) ||
                   normalizedPath.StartsWith(normalizedDirectory + Path.DirectorySeparatorChar, comparison);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(IsInsideDirectory)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(IsInsideDirectory)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves inside root.
    /// </summary>
    private string ResolveInsideRoot(string root, string relativePath)
    {
    try
    {
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var candidate = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!string.Equals(candidate, normalizedRoot, comparison) &&
                !candidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison))
            {
                throw new InvalidOperationException("The requested output path escapes the reviewed artifact workspace.");
            }
            return candidate;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ResolveInsideRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ResolveInsideRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes relative path.
    /// </summary>
    private string NormalizeRelativePath(string? value)
    {
    try
    {
            var path = string.IsNullOrWhiteSpace(value) ? "." : value.Trim().Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(path))
                throw new ArgumentException("Only relative paths are allowed in reviewed generation payloads.");
            var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Any(part => part is "." or ".."))
            {
                if (path == ".")
                    return ".";
                throw new ArgumentException("Relative paths may not contain traversal segments.");
            }
            foreach (var part in parts)
            {
                if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.IndexOfAny(['<', '>', ':', '"', '|', '?', '*', '\0']) >= 0)
                    throw new ArgumentException("A reviewed output path contains invalid path characters.");

                var baseName = Path.GetFileNameWithoutExtension(part).TrimEnd('.', ' ');
                if (IsWindowsReservedName(baseName))
                    throw new ArgumentException($"A reviewed output path uses the reserved Windows name '{baseName}'.");
            }

            return parts.Length == 0 ? "." : Path.Combine(parts);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeRelativePath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeRelativePath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether windows reserved name.
    /// </summary>
    private bool IsWindowsReservedName(string name)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return name.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(name, "^(COM|LPT)[1-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(IsWindowsReservedName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(IsWindowsReservedName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes identifier.
    /// </summary>
    private string NormalizeIdentifier(string? value, string fallback)
    {
    try
    {
            var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            var builder = new StringBuilder();
            foreach (var character in source)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                    builder.Append(character);
            }
            if (builder.Length == 0)
                builder.Append(fallback);
            if (!char.IsLetter(builder[0]) && builder[0] != '_')
                builder.Insert(0, '_');
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeIdentifier)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeIdentifier)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes identifier path.
    /// </summary>
    private string NormalizeIdentifierPath(string? value, string fallback) {
    try
    {
        return string.Join('.', (string.IsNullOrWhiteSpace(value) ? fallback : value)
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => NormalizeIdentifier(part, "Generated")));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeIdentifierPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeIdentifierPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the escape csharp operation.
    /// </summary>
    private string EscapeCSharp(string? value) {
    try
    {
        return (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(EscapeCSharp)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(EscapeCSharp)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the value or fallback operation.
    /// </summary>
    private string ValueOrFallback(string? value, string fallback)
    {
    try
    {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValueOrFallback)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValueOrFallback)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether h prefix.
    /// </summary>
    private string HashPrefix(string hash) {
    try
    {
        return hash.Length <= 12 ? hash : hash[..12];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(HashPrefix)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(HashPrefix)} failed.");
        throw;
    }
}
}
