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

public sealed class CodeGenerationWorkflowService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ICouncilArtifactService councilArtifacts,
    IArtifactBuildExecutor artifactBuildExecutor,
    IProjectMaintenanceService projectMaintenance,
    ILogger<CodeGenerationWorkflowService> logger) : ICodeGenerationWorkflowService
{
    private const int MaxPayloadCharacters = 4_000_000;
    private const int MaxFileCount = 512;
    private const int MaxReviewTake = 100;

    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public async Task<CodeGenerationReviewSnapshot> CreateReviewAsync(
        CreateCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default)
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
        if (payloadJson.Length > MaxPayloadCharacters)
            throw new InvalidOperationException($"The proposed generation payload exceeds the {MaxPayloadCharacters:n0}-character review limit.");

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await ValidateProjectReferencesAsync(db, request.ProjectId, request.ProjectRevisionId, request.ProjectTopicId, cancellationToken).ConfigureAwait(false);

        var entity = new CodeGenerationChangeReview
        {
            ProjectId = request.ProjectId,
            ProjectRevisionId = request.ProjectRevisionId,
            ProjectTopicId = request.ProjectTopicId,
            CouncilRunId = request.CouncilRunId,
            Title = Limit(request.Title, 240, "Code generation change review"),
            Goal = Limit(request.Goal, 12_000, "Generate a reviewed LocalGPT artifact."),
            CurrentProjectState = Limit(request.CurrentProjectState, 20_000, "Current project state was not supplied."),
            CouncilSummary = Limit(request.CouncilSummary, 24_000, "Council summary was not supplied."),
            ChangeSummary = Limit(request.ChangeSummary, 20_000, BuildDefaultChangeSummary(payload)),
            SafetySummary = Limit(request.SafetySummary, 8_000,
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

    public async Task<CodeGenerationReviewSnapshot?> GetReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<CodeGenerationReviewSnapshot>> ListReviewsAsync(
        Guid? projectId = null,
        int take = 20,
        CancellationToken cancellationToken = default)
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
            .Take(Math.Clamp(take, 1, MaxReviewTake))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug("Listed {ReviewCount} code-generation review(s).", entities.Count);
        return entities.Select(entity => ToSnapshot(entity, DeserializePayload(entity.PayloadJson))).ToList();
    }

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
        entity.DecisionNote = Limit(request.DecisionNote, 2_000, "Approved by the current user.");
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
                var source = GenerateCodeDomSource(typeSpec);
                await File.WriteAllTextAsync(fullPath, source, cancellationToken).ConfigureAwait(false);
                result.WrittenFiles.Add(relativePath.Replace('\\', '/'));
                reviewedSources.Add(new ReviewedSourceArtifact(relativePath, fullPath));
                logger.LogDebug("Generated reviewed CodeDOM source file {RelativePath} for review {ReviewId}.", relativePath, entity.Id);
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
                        MaximumFiles = 100000,
                        MaximumFileBytes = 4L * 1024 * 1024 * 1024,
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
            entity.DecisionNote = Limit($"{entity.DecisionNote} Generation was cancelled.", 2_000, "Generation was cancelled.");
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

    public async Task<CodeGenerationReviewSnapshot> RejectReviewAsync(
        Guid reviewId,
        RejectCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default)
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
        entity.DecisionNote = Limit(request.DecisionNote, 2_000, "Rejected by the current user.");
        entity.DecidedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Rejected code-generation review {ReviewId}.", reviewId);
        return ToSnapshot(entity, DeserializePayload(entity.PayloadJson));
    }

    private IDisposable? BeginReviewScope(string operation, Guid reviewId) =>
        logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = Guid.NewGuid(),
            ["Operation"] = operation,
            ["ReviewId"] = reviewId
        });

    private void ValidateReviewRequest(CreateCodeGenerationReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Goal))
            throw new ArgumentException("A concrete generation goal is required.", nameof(request));
        if (request.Files.Count > MaxFileCount)
            throw new ArgumentException($"A review may contain at most {MaxFileCount} explicit source files.", nameof(request));
        if (request.CodeDomTypes.Count > MaxFileCount)
            throw new ArgumentException($"A review may contain at most {MaxFileCount} CodeDOM source types.", nameof(request));
        foreach (var file in request.Files)
            _ = NormalizeRelativePath(file.RelativePath);
        foreach (var type in request.CodeDomTypes)
            _ = NormalizeRelativePath(type.RelativePath);
        foreach (var output in request.Outputs)
            _ = NormalizeRelativePath(string.IsNullOrWhiteSpace(output.RelativeDirectory) ? "." : output.RelativeDirectory);
    }

    private async Task ValidateProjectReferencesAsync(
        LocalGptMemoryDbContext db,
        Guid? projectId,
        Guid? projectRevisionId,
        Guid? projectTopicId,
        CancellationToken cancellationToken)
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

    private CodeGenerationFileSpec NormalizeFile(CodeGenerationFileSpec file) => new()
    {
        RelativePath = NormalizeRelativePath(file.RelativePath),
        Content = file.Content ?? string.Empty,
        Purpose = Limit(file.Purpose, 1_000, "Reviewed source file")
    };

    private CodeDomTypeSpec NormalizeCodeDomType(CodeDomTypeSpec type) => new()
    {
        RelativePath = NormalizeRelativePath(type.RelativePath),
        Namespace = NormalizeIdentifierPath(type.Namespace, "LocalGPT.Generated"),
        TypeName = NormalizeIdentifier(type.TypeName, "GeneratedFeature"),
        MethodName = NormalizeIdentifier(type.MethodName, "Describe"),
        MethodResult = Limit(type.MethodResult, 8_000, "Generated with LocalGPT after human review."),
        Summary = Limit(type.Summary, 2_000, "Reviewed CodeDOM source type")
    };

    private CodeGenerationOutputSpec NormalizeOutput(CodeGenerationOutputSpec output) => new()
    {
        Kind = NormalizeOutputKind(output.Kind),
        Name = NormalizeIdentifier(output.Name, "LocalGptGeneratedFeature"),
        RelativeDirectory = NormalizeRelativePath(string.IsNullOrWhiteSpace(output.RelativeDirectory) ? "." : output.RelativeDirectory),
        TargetFramework = NormalizeTargetFramework(output.TargetFramework),
        RootNamespace = NormalizeIdentifierPath(output.RootNamespace, "LocalGPT.Generated"),
        Description = Limit(output.Description, 2_000, "Reviewed LocalGPT output")
    };

    private string NormalizeOutputKind(string? kind)
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
            CodeGenerationOutputKinds.JavaScriptModule => CodeGenerationOutputKinds.JavaScriptModule,
            _ => throw new ArgumentException($"Unsupported reviewed output kind '{value}'.")
        };
    }

    private string NormalizeTargetFramework(string? value)
    {
        var framework = string.IsNullOrWhiteSpace(value) ? "net10.0" : value.Trim();
        if (!framework.StartsWith("net", StringComparison.OrdinalIgnoreCase) || framework.Any(character => !(char.IsLetterOrDigit(character) || character is '.' or '-')))
            throw new ArgumentException("Target framework contains unsupported characters.");
        return framework;
    }

    private string BuildDefaultChangeSummary(CodeGenerationReviewPayload payload) =>
        $"Create {payload.Files.Count} explicit source file(s), {payload.CodeDomTypes.Count} CodeDOM-generated type(s), and {payload.Outputs.Count} output target(s) in an isolated LocalGPT workspace; when a project revision is linked, preserve every unchanged approved tracked file byte-for-byte.";

    private string ComputeReviewHash(CodeGenerationChangeReview entity)
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

    private CodeGenerationReviewPayload DeserializePayload(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<CodeGenerationReviewPayload>(payloadJson, JsonOptions) ?? new CodeGenerationReviewPayload();
        payload.Files ??= [];
        payload.CodeDomTypes ??= [];
        payload.Outputs ??= [];
        return payload;
    }

    private CodeGenerationReviewSnapshot ToSnapshot(
        CodeGenerationChangeReview entity,
        CodeGenerationReviewPayload payload) => new()
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

    private async Task WriteReviewDocumentAsync(
        string workspaceRoot,
        CodeGenerationChangeReview review,
        CodeGenerationReviewPayload payload,
        CancellationToken cancellationToken)
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

    private async Task<string> CopyTrackedProjectIntoWorkspaceAsync(
        string workspaceRoot,
        Guid projectId,
        Guid? revisionId,
        CodeGenerationExecutionResult result,
        CancellationToken cancellationToken)
    {
        var tracked = await projectMaintenance.GetTrackedFilesAsync(projectId, revisionId, cancellationToken).ConfigureAwait(false);
        var approved = tracked.Where(item => item.Exists && item.IsUserApproved && !item.IsGenerated).OrderBy(item => item.ProjectRelativePath, StringComparer.Ordinal).ToList();
        if (approved.Count == 0)
        {
            result.Warnings.Add("No approved tracked project files were available to clone. Scan the selected project revision before executing a maintenance review.");
            return string.Empty;
        }

        string solutionPath = string.Empty;
        var recorded = 0;
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
            if (recorded++ < 5000)
                result.WrittenFiles.Add(relativePath.Replace('\\', '/'));
            if (Path.GetExtension(relativePath).Equals(".sln", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(relativePath).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
                solutionPath = destination;
        }
        if (approved.Count > 5000)
            result.Warnings.Add($"The complete approved project tree with {approved.Count:n0} files was cloned; the response lists only the first 5,000 paths.");
        logger.LogInformation("Cloned {FileCount} approved tracked file(s) into isolated maintenance workspace for project {ProjectId} revision {RevisionId}; paths omitted from logs.", approved.Count, projectId, revisionId);
        return solutionPath;
    }


    private async Task<string> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    }

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

    private string GenerateCodeDomSource(CodeDomTypeSpec spec)
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

    private async Task ScaffoldOutputAsync(
        string workspaceRoot,
        CodeGenerationOutputSpec output,
        IReadOnlyList<ReviewedSourceArtifact> reviewedSources,
        List<string> writtenFiles,
        List<string> buildTargets,
        CancellationToken cancellationToken)
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

    private async Task<bool> CopyReviewedSourcesAsync(
        string workspaceRoot,
        string destinationRoot,
        IEnumerable<ReviewedSourceArtifact> sources,
        List<string> writtenFiles,
        CancellationToken cancellationToken)
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

    private string BuildLibrarySource(CodeGenerationOutputSpec output) => $$"""
    namespace {{output.RootNamespace}};

    public sealed class GeneratedFeature
    {
        public string Describe() => "{{EscapeCSharp(output.Description)}}";
    }
    """;

    private string BuildAddonSource(CodeGenerationOutputSpec output) => $$"""
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

    private string BuildSolutionFile(string name, string relativeProjectPath)
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

    private bool BuildCompletedSuccessfully(string buildStatus)
    {
        var statuses = buildStatus.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return statuses.Length > 0 && statuses.All(status => status.EndsWith(":BuildPassed", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsInsideDirectory(string path, string directory)
    {
        var normalizedDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var normalizedPath = Path.GetFullPath(path);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(normalizedPath, normalizedDirectory, comparison) ||
               normalizedPath.StartsWith(normalizedDirectory + Path.DirectorySeparatorChar, comparison);
    }

    private string ResolveInsideRoot(string root, string relativePath)
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

    private string NormalizeRelativePath(string? value)
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

    private bool IsWindowsReservedName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return name.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
               Regex.IsMatch(name, "^(COM|LPT)[1-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private string NormalizeIdentifier(string? value, string fallback)
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

    private string NormalizeIdentifierPath(string? value, string fallback) =>
        string.Join('.', (string.IsNullOrWhiteSpace(value) ? fallback : value)
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => NormalizeIdentifier(part, "Generated")));

    private string EscapeCSharp(string? value) =>
        (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private string Limit(string? value, int maxLength, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private string HashPrefix(string hash) => hash.Length <= 12 ? hash : hash[..12];
}
