using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Lets Council members report a concrete stale-knowledge observation without mutating external sources.</summary>
public sealed class ReportKnowledgeFreshnessFunction(
    IKnowledgeFreshnessReviewService freshness,
    IDxAiFunctionJsonService json,
    ILogger<ReportKnowledgeFreshnessFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.freshness.report", "POST", "/api/dxai/functions/localgpt.knowledge.freshness.report/invoke",
        "Reports that one persisted Council knowledge entry appears outdated and queues a durable local-human freshness review.",
        "knowledgeId and reason are required. detectedBy, sourceUrl and councilRunId are optional. Use the stable knowledgeId shown in knowledge briefings.",
        "Does not fetch a URL or overwrite knowledge. It marks the entry for review and creates visible review buttons in Chat/ASCII/Approvals & team.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["knowledgeId","reason"],"properties":{"knowledgeId":{"type":"string","format":"uuid"},"reason":{"type":"string","maxLength":500},"detectedBy":{"type":"string","maxLength":160},"sourceUrl":{"type":"string","maxLength":2048},"councilRunId":{"type":["string","null"],"format":"uuid"}},"additionalProperties":false}""");

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
public sealed class RefreshKnowledgeSourceFunction(
    IKnowledgeFreshnessReviewService freshness,
    IDxAiFunctionJsonService json,
    ILogger<RefreshKnowledgeSourceFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.source.refresh", "POST", "/api/dxai/functions/localgpt.knowledge.source.refresh/invoke",
        "Refreshes one stale knowledge entry from one exact reviewed external http/https source through LocalGPT's bounded remote importer.",
        "knowledgeId and sourceUrl are required. Approval is bound to the complete parameters, so permission for one URL does not approve a different URL.",
        "Consequential network/database operation. Requires fresh human confirmation and rejects local/private-network sources so they remain on LocalGPT's local/LAN path.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["knowledgeId","sourceUrl"],"properties":{"knowledgeId":{"type":"string","format":"uuid"},"sourceUrl":{"type":"string","maxLength":2048}},"additionalProperties":false}""");

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
