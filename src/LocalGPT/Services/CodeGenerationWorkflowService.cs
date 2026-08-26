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
/// Coordinates code generation workflow behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class CodeGenerationWorkflowService : ICodeGenerationWorkflowService
    {
        /// <summary>
        /// Stores the database context factory dependency used by <see cref="CodeGenerationWorkflowService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
        /// <summary>
        /// Stores the council artifact service dependency used by <see cref="CodeGenerationWorkflowService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilArtifactService councilArtifacts;
        /// <summary>
        /// Stores the artifact build executor dependency used by <see cref="CodeGenerationWorkflowService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IArtifactBuildExecutor artifactBuildExecutor;
        /// <summary>
        /// Stores the project maintenance service dependency used by <see cref="CodeGenerationWorkflowService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IProjectMaintenanceService projectMaintenance;
        /// <summary>
        /// Stores the regex pattern service dependency used by <see cref="CodeGenerationWorkflowService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IRegexPatternService regexPatterns;
        /// <summary>Stores host filesystem semantics behind the injected platform boundary.</summary>
        private readonly IPlatformRuntimeService platform;
        /// <summary>
        /// Stores the logger used by <see cref="CodeGenerationWorkflowService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<CodeGenerationWorkflowService> logger;

        /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
        /// <param name="dbContextFactory">Injected dependency used by the CodeGenerationWorkflowService.</param>
        /// <param name="councilArtifacts">Injected dependency used by the CodeGenerationWorkflowService.</param>
        /// <param name="artifactBuildExecutor">Injected dependency used by the CodeGenerationWorkflowService.</param>
        /// <param name="projectMaintenance">Injected dependency used by the CodeGenerationWorkflowService.</param>
        /// <param name="regexPatterns">Injected dependency used by the CodeGenerationWorkflowService.</param>
        /// <param name="logger">Injected dependency used by the CodeGenerationWorkflowService.</param>
        public CodeGenerationWorkflowService(
            IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
            ICouncilArtifactService councilArtifacts,
            IArtifactBuildExecutor artifactBuildExecutor,
            IProjectMaintenanceService projectMaintenance,
            IRegexPatternService regexPatterns,
            IPlatformRuntimeService platform,
            ILogger<CodeGenerationWorkflowService> logger)
        {
            this.dbContextFactory = dbContextFactory;
            this.councilArtifacts = councilArtifacts;
            this.artifactBuildExecutor = artifactBuildExecutor;
            this.projectMaintenance = projectMaintenance;
            this.regexPatterns = regexPatterns;
            this.platform = platform;
            this.logger = logger;
        }


    /// <summary>
    /// Stores the internal JSON options state used by <see cref="CodeGenerationWorkflowService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Creates review as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The code generation review snapshot produced by the operation.</returns>
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

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
    /// Retrieves review as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The code generation review snapshot produced by the operation.</returns>
    public async Task<CodeGenerationReviewSnapshot?> GetReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
    try
    {
            using var scope = BeginReviewScope("GetCodeGenerationReview", reviewId);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
    /// Lists reviews as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="take">Take value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
}
