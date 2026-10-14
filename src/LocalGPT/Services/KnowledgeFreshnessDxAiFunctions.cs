using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Lets Council members report a concrete stale-knowledge observation without mutating external sources.</summary>
/// <param name="freshness">Knowledge freshness review service dependency used by the report knowledge freshness function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the report knowledge freshness function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ReportKnowledgeFreshnessFunction(
    IKnowledgeFreshnessReviewService freshness,
    IDxAiFunctionJsonService json,
    ILogger<ReportKnowledgeFreshnessFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the report knowledge freshness function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ReportKnowledgeFreshnessFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.freshness.report", "POST", "/api/dxai/functions/localgpt.knowledge.freshness.report/invoke",
        "Reports that one persisted Council knowledge entry appears outdated and queues a durable local-human freshness review.",
        "knowledgeId and reason are required. detectedBy, sourceUrl and councilRunId are optional. Use the stable knowledgeId shown in knowledge briefings.",
        "Does not fetch a URL or overwrite knowledge. It marks the entry for review and creates visible review buttons in Chat/ASCII/Approvals & team.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["knowledgeId","reason"],"properties":{"knowledgeId":{"type":"string","format":"uuid"},"reason":{"type":"string","maxLength":500},"detectedBy":{"type":"string","maxLength":160},"sourceUrl":{"type":"string","maxLength":2048},"councilRunId":{"type":["string","null"],"format":"uuid"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ReportKnowledgeFreshnessFunction"/>, keeping the operation consistent with the state and invariants of the surrounding report knowledge freshness function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<KnowledgeFreshnessReportRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await freshness.ReportAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Knowledge freshness report DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogWarning(exception, "Knowledge freshness report DXFunction was rejected; content was omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Rejected", Error = exception.Message }; }
    }
}

/// <summary>Refreshes one exact external source only after the standard DXFunction approval gate confirms the complete parameter set.</summary>
/// <param name="freshness">Knowledge freshness review service dependency used by the refresh knowledge source function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the refresh knowledge source function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RefreshKnowledgeSourceFunction(
    IKnowledgeFreshnessReviewService freshness,
    IDxAiFunctionJsonService json,
    ILogger<RefreshKnowledgeSourceFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the refresh knowledge source function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="RefreshKnowledgeSourceFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.source.refresh", "POST", "/api/dxai/functions/localgpt.knowledge.source.refresh/invoke",
        "Refreshes one stale knowledge entry from one exact reviewed external http/https source through LocalGPT's bounded remote importer.",
        "knowledgeId and sourceUrl are required. Approval is bound to the complete parameters, so permission for one URL does not approve a different URL.",
        "Consequential network/database operation. Requires fresh human confirmation and rejects local/private-network sources so they remain on LocalGPT's local/LAN path.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["knowledgeId","sourceUrl"],"properties":{"knowledgeId":{"type":"string","format":"uuid"},"sourceUrl":{"type":"string","maxLength":2048}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="RefreshKnowledgeSourceFunction"/>, keeping the operation consistent with the state and invariants of the surrounding refresh knowledge source function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<KnowledgeSourceRefreshRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = request.UserConfirmed;
            return json.Success(await freshness.RefreshApprovedSourceAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Exact-source knowledge refresh DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogWarning(exception, "Exact-source knowledge refresh DXFunction was rejected; URL content was omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Rejected", Error = exception.Message }; }
    }
}
