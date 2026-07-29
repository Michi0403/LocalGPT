using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services;

public sealed class DeferredDxAiInvocationService(ILocalGptVocabularyService vocabulary,
    
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<DeferredDxAiInvocationService> logger) : IDeferredDxAiInvocationService
{
    private const int MaxResultCharacters = 8_000;
    private readonly SemaphoreSlim databaseGate = new(1, 1);

    public async Task QueueAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        Guid approvalRequestId,
        string correlationId,
        Guid? councilRunId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentNullException.ThrowIfNull(request);
        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            if (await db.DeferredDxAiInvocations.AnyAsync(
                    item => item.ApprovalRequestId == approvalRequestId,
                    cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var parametersJson = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? "{}"
                : request.Parameters.GetRawText();
            if (parametersJson.Length > 64_000)
                throw new InvalidOperationException("The exact deferred function parameters exceed the 64,000-character local safety limit.");

            db.DeferredDxAiInvocations.Add(new DeferredDxAiInvocation
            {
                ApprovalRequestId = approvalRequestId,
                CouncilRunId = councilRunId,
                OperationId = request.OperationId ?? Guid.NewGuid(),
                CorrelationId = Limit(correlationId, 180),
                FunctionName = Limit(functionName, 180),
                ParametersJson = parametersJson,
                ConfirmationSummaryHash = Limit(request.ConfirmationSummaryHash, 180),
                RequestedBy = Limit(request.RequestedBy, 160),
                ConversationId = request.ConversationId,
                ProjectId = request.ProjectId,
                ProjectVersionId = request.ProjectVersionId,
                ApplicationVersion = Limit(request.ApplicationVersion, 80),
                Status = vocabulary.Get().DeferredPendingApproval,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Queued deferred DXAI invocation for function {FunctionName} under approval request {ApprovalRequestId}; exact parameters were persisted locally and omitted from logs.",
                functionName,
                approvalRequestId);
        }
        finally
        {
            databaseGate.Release();
        }
    }

    public async Task<IReadOnlyList<DeferredDxAiExecutionOutcome>> ExecuteApprovedForHeartbeatAsync(
        Guid councilRunId,
        int councilRound,
        CancellationToken cancellationToken = default)
    {
        var candidates = await ClaimCandidatesAsync(councilRunId, cancellationToken).ConfigureAwait(false);
        if (candidates.Count == 0)
            return [];

        var outcomes = new List<DeferredDxAiExecutionOutcome>(candidates.Count);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            outcomes.Add(await ExecuteCandidateAsync(candidate, councilRound, cancellationToken).ConfigureAwait(false));
        }
        return outcomes;
    }

    private async Task<List<DeferredDxAiInvocation>> ClaimCandidatesAsync(
        Guid councilRunId,
        CancellationToken cancellationToken)
    {
        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var candidates = await db.DeferredDxAiInvocations
                .Where(item => item.CouncilRunId == councilRunId &&
                    item.Status == vocabulary.Get().DeferredPendingApproval)
                .OrderBy(item => item.CreatedAtUtc)
                .Take(8)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (candidates.Count == 0)
                return [];

            var approvalIds = candidates.Select(item => item.ApprovalRequestId).ToList();
            var approvalStatuses = await db.HumanCollaborationRequests.AsNoTracking()
                .Where(item => approvalIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Status, cancellationToken)
                .ConfigureAwait(false);

            var claimed = new List<DeferredDxAiInvocation>();
            foreach (var candidate in candidates)
            {
                if (!approvalStatuses.TryGetValue(candidate.ApprovalRequestId, out var approvalStatus))
                    continue;
                if (approvalStatus == vocabulary.Get().HumanStatusDeclined)
                {
                    candidate.Status = vocabulary.Get().DeferredDeclined;
                    candidate.ResultStatus = vocabulary.Get().HumanStatusDeclined;
                    candidate.ResultSummary = "The local human declined this exact invocation.";
                    candidate.CompletedAtUtc = DateTime.UtcNow;
                    candidate.UpdatedAtUtc = DateTime.UtcNow;
                    continue;
                }
                if (approvalStatus == vocabulary.Get().HumanStatusConsumed)
                {
                    candidate.Status = vocabulary.Get().DeferredCompletedElsewhere;
                    candidate.ResultStatus = vocabulary.Get().HumanStatusConsumed;
                    candidate.ResultSummary = "The exact approval was consumed by another retry path.";
                    candidate.CompletedAtUtc = DateTime.UtcNow;
                    candidate.UpdatedAtUtc = DateTime.UtcNow;
                    continue;
                }
                if (approvalStatus != vocabulary.Get().HumanStatusApproved)
                    continue;

                candidate.Status = vocabulary.Get().DeferredExecuting;
                candidate.AttemptCount++;
                candidate.LastAttemptAtUtc = DateTime.UtcNow;
                candidate.UpdatedAtUtc = DateTime.UtcNow;
                claimed.Add(candidate);
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return claimed.Select(Clone).ToList();
        }
        finally
        {
            databaseGate.Release();
        }
    }

    private async Task<DeferredDxAiExecutionOutcome> ExecuteCandidateAsync(
        DeferredDxAiInvocation candidate,
        int councilRound,
        CancellationToken cancellationToken)
    {
        DxAiFunctionInvocationResult result;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var registry = scope.ServiceProvider.GetRequiredService<IDxAiFunctionRegistry>();
            using var document = JsonDocument.Parse(candidate.ParametersJson);
            result = await registry.InvokeAsync(
                candidate.FunctionName,
                new DxAiFunctionInvocationRequest
                {
                    OperationId = candidate.OperationId,
                    Parameters = document.RootElement.Clone(),
                    UserConfirmed = false,
                    AutomaticInvocation = false,
                    ConfirmationSummaryHash = string.IsNullOrWhiteSpace(candidate.ConfirmationSummaryHash)
                        ? null
                        : candidate.ConfirmationSummaryHash,
                    RequestedBy = $"DeferredCouncilHeartbeat:{councilRound}",
                    ConversationId = candidate.ConversationId,
                    ProjectId = candidate.ProjectId,
                    ProjectVersionId = candidate.ProjectVersionId,
                    ApplicationVersion = candidate.ApplicationVersion
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Deferred DXAI invocation {DeferredInvocationId} for function {FunctionName} failed; parameters and returned content were omitted from logs.",
                candidate.Id,
                candidate.FunctionName);
            result = new DxAiFunctionInvocationResult
            {
                FunctionName = candidate.FunctionName,
                OperationId = candidate.OperationId,
                Status = "Failed",
                Succeeded = false,
                Error = "The deferred invocation failed. Review LocalGPT application logs."
            };
        }

        var summary = BuildResultSummary(result);
        await CompleteAsync(candidate.Id, result, summary, cancellationToken).ConfigureAwait(false);
        return new DeferredDxAiExecutionOutcome(
            candidate.Id,
            candidate.ApprovalRequestId,
            candidate.FunctionName,
            result.Succeeded ? vocabulary.Get().DeferredCompleted : vocabulary.Get().DeferredFailed,
            result.Status,
            summary);
    }

    private async Task CompleteAsync(
        Guid deferredInvocationId,
        DxAiFunctionInvocationResult result,
        string summary,
        CancellationToken cancellationToken)
    {
        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entity = await db.DeferredDxAiInvocations
                .SingleAsync(item => item.Id == deferredInvocationId, cancellationToken)
                .ConfigureAwait(false);
            entity.Status = result.Succeeded
                ? vocabulary.Get().DeferredCompleted
                : vocabulary.Get().DeferredFailed;
            entity.ResultStatus = Limit(result.Status, 80);
            entity.ResultSummary = summary;
            entity.CompletedAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Deferred DXAI invocation {DeferredInvocationId} for function {FunctionName} completed with status {ResultStatus} and success={Succeeded}; returned content was omitted from logs.",
                entity.Id,
                entity.FunctionName,
                entity.ResultStatus,
                result.Succeeded);
        }
        finally
        {
            databaseGate.Release();
        }
    }

    private string BuildResultSummary(DxAiFunctionInvocationResult result)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                result.Status,
                result.Succeeded,
                result.Value,
                result.Error
            });
            return Limit(payload, MaxResultCharacters);
        }
        catch
        {
            return Limit(
                JsonSerializer.Serialize(new
                {
                    result.Status,
                    result.Succeeded,
                    Error = result.Error,
                    ValueSerialization = "The returned value could not be serialized for council context."
                }),
                MaxResultCharacters);
        }
    }

    private DeferredDxAiInvocation Clone(DeferredDxAiInvocation value) => new()
    {
        Id = value.Id,
        ApprovalRequestId = value.ApprovalRequestId,
        CouncilRunId = value.CouncilRunId,
        OperationId = value.OperationId,
        CorrelationId = value.CorrelationId,
        FunctionName = value.FunctionName,
        ParametersJson = value.ParametersJson,
        ConfirmationSummaryHash = value.ConfirmationSummaryHash,
        RequestedBy = value.RequestedBy,
        ConversationId = value.ConversationId,
        ProjectId = value.ProjectId,
        ProjectVersionId = value.ProjectVersionId,
        ApplicationVersion = value.ApplicationVersion,
        Status = value.Status,
        AttemptCount = value.AttemptCount,
        CreatedAtUtc = value.CreatedAtUtc,
        UpdatedAtUtc = value.UpdatedAtUtc,
        LastAttemptAtUtc = value.LastAttemptAtUtc
    };

    private string Limit(string? value, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }
}
