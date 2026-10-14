using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Exposes the durable per-file learning work queue without treating inventory as review.</summary>
/// <param name="learning">DI-owned learning service.</param>
/// <param name="logger">Typed service diagnostics.</param>
public sealed class GetLearningFileChecklistFunction(ILearningRoundService learning, ILogger<GetLearningFileChecklistFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Policy-approved read-only function contract.</summary>
    /// <value>The descriptor value exposed by <see cref="GetLearningFileChecklistFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.learning.files", "POST", "/api/dxai/functions/localgpt.learning.files/invoke",
        "Lists a bounded page of source-revision files, persisted read/review status, blockers and exact unfinished counts. A successful hash alone does not mean learned.",
        "JSON parameters: projectId (or active project), optional revisionId, offset, take (1-200).",
        "Read-only. All project-file learning states are persisted, not model self-reported.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"},"revisionId":{"type":"string","format":"uuid"},"offset":{"type":"integer","minimum":0},"take":{"type":"integer","minimum":1,"maximum":200}},"additionalProperties":false}
        """);

    /// <summary>Returns one bounded file page with revision-wide counts.</summary>
    /// <param name="request">Function arguments and active project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Checklist result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = request.Parameters.ValueKind == JsonValueKind.Object
                ? request.Parameters.Deserialize<LearningFileCursorRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new LearningFileCursorRequest()
                : new LearningFileCursorRequest();
            var projectId = arguments.ProjectId ?? request.ProjectId ?? throw new ArgumentException("A selected projectId is required to list file learning tasks.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true, Status = "Completed",
                Value = await learning.GetFileChecklistAsync(projectId, arguments.RevisionId, arguments.Offset, arguments.Take, cancellationToken).ConfigureAwait(false)
            };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Listing learned-file progress was canceled.");
            else
                logger.LogError(exception, "Listing learned-file progress failed.");
            throw;
        }
    }
}

/// <summary>Streams one source-backed file chunk and records exact continuation progress.</summary>
/// <param name="learning">DI-owned learning service.</param>
/// <param name="logger">Typed service diagnostics.</param>
public sealed class ReadLearningFileChunkFunction(ILearningRoundService learning, ILogger<ReadLearningFileChunkFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Read-only source-backed function contract.</summary>
    /// <value>The descriptor value exposed by <see cref="ReadLearningFileChunkFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.learning.file.read", "POST", "/api/dxai/functions/localgpt.learning.file.read/invoke",
        "Reads the next chunk of an approved project's tracked text file. Stores the actually read cursor, checks source hash, and returns HasMore; never marks semantic review complete.",
        "JSON parameters: projectId (or active project), optional revisionId and fileId, maximumCharacters (1-16000).",
        "Source files remain read-only; per-file review progress metadata is persisted as coordination state. Binary files and changed hashes become visible blockers.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"},"revisionId":{"type":"string","format":"uuid"},"fileId":{"type":"string","format":"uuid"},"maximumCharacters":{"type":"integer","minimum":1,"maximum":16000}},"additionalProperties":false}
        """, IsCoordinationOnly: true);

    /// <summary>
    /// Performs invoke for <see cref="ReadLearningFileChunkFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read learning file chunk function workflow.
    /// </summary>
    /// <param name="request">Function arguments and active project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Content and continuation cursor.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = request.Parameters.ValueKind == JsonValueKind.Object
                ? request.Parameters.Deserialize<LearningFileCursorRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new LearningFileCursorRequest()
                : new LearningFileCursorRequest();
            var projectId = arguments.ProjectId ?? request.ProjectId ?? throw new ArgumentException("A selected projectId is required to read a tracked learning file.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true, Status = "Completed",
                Value = await learning.ReadNextFileChunkAsync(projectId, arguments.RevisionId, arguments.FileId, arguments.MaximumCharacters, cancellationToken).ConfigureAwait(false)
            };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading a tracked learning file was canceled.");
            else
                logger.LogError(exception, "Reading a tracked learning file failed; content omitted from diagnostics.");
            throw;
        }
    }
}

/// <summary>Persists source-backed model review evidence or an explicit unresolved blocker.</summary>
/// <param name="learning">DI-owned learning service.</param>
/// <param name="logger">Typed service diagnostics.</param>
public sealed class ReviewLearningFileFunction(ILearningRoundService learning, ILogger<ReviewLearningFileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Coordination-only model-suggested review function contract.</summary>
    /// <value>The descriptor value exposed by <see cref="ReviewLearningFileFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.learning.file.review", "POST", "/api/dxai/functions/localgpt.learning.file.review/invoke",
        "Records source-backed model findings after a complete tracked-file read, or an explicit blocker when source cannot be reviewed.",
        "JSON parameters: fileId UUID, evidence summary, optional blocked boolean.",
        "Only a complete source hash can be marked reviewed; findings remain model-suggested and never grant user approval or execution permission.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["fileId","evidence"],"properties":{"fileId":{"type":"string","format":"uuid"},"evidence":{"type":"string","minLength":1,"maxLength":4000},"blocked":{"type":"boolean"}},"additionalProperties":false}
        """, IsCoordinationOnly: true);

    /// <summary>Records a review only when the reader actually reached EOF.</summary>
    /// <param name="request">Model findings and review scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Persisted review status.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = request.Parameters.Deserialize<LearningFileReviewRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new ArgumentException("Review parameters are required.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true, Status = "Completed",
                Value = await learning.ReviewFileAsync(arguments.FileId, arguments.Evidence, arguments.Blocked, cancellationToken).ConfigureAwait(false)
            };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Saving a file review was canceled.");
            else
                logger.LogError(exception, "Saving a file review failed; finding content omitted from diagnostics.");
            throw;
        }
    }
}
