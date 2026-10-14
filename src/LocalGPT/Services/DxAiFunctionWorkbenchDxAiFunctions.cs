using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Searches user-approved DX function descriptors in small pages, with schemas only when requested.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the discover DevExpress AI functions function workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Devexpress ai function catalog service dependency used by the discover DevExpress AI functions function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DiscoverDxAiFunctionsFunction(
    IDxAiFunctionJsonService json,
    IDxAiFunctionCatalogService catalog,
    ILogger<DiscoverDxAiFunctionsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the discover DevExpress AI functions function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="DiscoverDxAiFunctionsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "dxai.tools.discover", "POST", "/api/dxai/functions/dxai.tools.discover/invoke",
        "Search currently enabled and AI-exposed LocalGPT functions by purpose or name. First discover names, then request exact parameter schemas.",
        "query optional task keywords, skip optional page offset, take optional page size, includeSchemas true only for selected functions.",
        "Read-only descriptor discovery. Disabled functions and public service reflections do not become callable.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true, Source: "DxFunctionBroker",
        ParameterSchemaJson: """
        {"type":"object","properties":{"query":{"type":"string","maxLength":500},"skip":{"type":"integer","minimum":0},"take":{"type":"integer","minimum":1,"maximum":50},"includeSchemas":{"type":"boolean"}},"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="DiscoverDxAiFunctionsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding discover DevExpress AI functions function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var parsed = json.Bind<DxAiFunctionDiscoveryRequest>(request.Parameters);
            if (!parsed.Succeeded) return json.InvalidParameters(parsed.Error);
            var result = await catalog.DiscoverForAiAsync(parsed.Value, cancellationToken).ConfigureAwait(false);
            return json.Success(result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Discovering permitted DX functions for an AI participant failed.");
            throw;
        }
    }
}

/// <summary>Allows small Council models to validate one proposed tool call without creating an approval or executing it.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the preview DevExpress AI function function workflow to provide the corresponding application capability.</param>
/// <param name="registry">Devexpress ai function registry dependency used by the preview DevExpress AI function function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PreviewDxAiFunctionFunction(
    IDxAiFunctionJsonService json,
    IDxAiFunctionRegistry registry,
    ILogger<PreviewDxAiFunctionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the preview DevExpress AI function function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="PreviewDxAiFunctionFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "dxai.tools.preview", "POST", "/api/dxai/functions/dxai.tools.preview/invoke",
        "Validate one exact DXFunction call against the current schema and saved policy without executing it, and return a preview fingerprint.",
        "functionName required exact name from dxai.tools.discover; parameters required JSON object matching its registered schema.",
        "Only previews. Preview cannot grant permission, execute a mutation, or bypass Human Collaboration approval.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true, Source: "DxFunctionBroker",
        ParameterSchemaJson: """
        {"type":"object","properties":{"functionName":{"type":"string","minLength":1},"parameters":{"type":"object"}},"required":["functionName","parameters"],"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="PreviewDxAiFunctionFunction"/>, keeping the operation consistent with the state and invariants of the surrounding preview DevExpress AI function function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Parameters.ValueKind != JsonValueKind.Object ||
                !request.Parameters.TryGetProperty("functionName", out var nameElement) ||
                nameElement.ValueKind != JsonValueKind.String ||
                !request.Parameters.TryGetProperty("parameters", out var parameters))
                return json.InvalidParameters("Provide functionName and a parameters JSON object.");
            var name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name)) return json.InvalidParameters("functionName must be non-empty.");
            var preview = await registry.InvokeAsync(name, new DxAiFunctionInvocationRequest
            {
                Parameters = parameters.Clone(),
                PreviewOnly = true,
                AutomaticInvocation = true,
                UserConfirmed = false,
                ConversationId = request.ConversationId,
                ProjectId = request.ProjectId,
                ProjectVersionId = request.ProjectVersionId,
                ApplicationVersion = request.ApplicationVersion,
                RequestedBy = request.RequestedBy
            }, cancellationToken).ConfigureAwait(false);
            return json.Success(preview);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Previewing a requested Council DX function failed; arguments were not logged.");
            throw;
        }
    }
}

/// <summary>AI broker for an exact previously previewed function, using the unchanged registry and approval boundary.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the invoke discovered DevExpress AI function workflow to provide the corresponding application capability.</param>
/// <param name="registry">Devexpress ai function registry dependency used by the invoke discovered DevExpress AI function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class InvokeDiscoveredDxAiFunction(
    IDxAiFunctionJsonService json,
    IDxAiFunctionRegistry registry,
    ILogger<InvokeDiscoveredDxAiFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the invoke discovered DevExpress AI function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="InvokeDiscoveredDxAiFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "dxai.tools.invoke", "POST", "/api/dxai/functions/dxai.tools.invoke/invoke",
        "Request execution of one exact function previously discovered and previewed. The underlying function keeps its own saved policy and human approval requirements.",
        "functionName exact discovered name, parameters exact previewed object, previewFingerprint required token from dxai.tools.preview.",
        "Coordination-only broker. Never self-confirms. Mutations are routed to the target's existing Human Collaboration approval gate; blind retries are forbidden.",
        IsReadOnly: false, IsCoordinationOnly: true, AvailableToAi: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DxFunctionBroker",
        ParameterSchemaJson: """
        {"type":"object","properties":{"functionName":{"type":"string","minLength":1},"parameters":{"type":"object"},"previewFingerprint":{"type":"string","minLength":64,"maxLength":64}},"required":["functionName","parameters","previewFingerprint"],"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="InvokeDiscoveredDxAiFunction"/>, keeping the operation consistent with the state and invariants of the surrounding invoke discovered DevExpress AI function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Parameters.ValueKind != JsonValueKind.Object ||
                !request.Parameters.TryGetProperty("functionName", out var nameValue) ||
                nameValue.ValueKind != JsonValueKind.String ||
                !request.Parameters.TryGetProperty("parameters", out var parameters) ||
                parameters.ValueKind != JsonValueKind.Object ||
                !request.Parameters.TryGetProperty("previewFingerprint", out var fingerprintValue) ||
                fingerprintValue.ValueKind != JsonValueKind.String)
                return json.InvalidParameters("functionName, parameters and a prior previewFingerprint are required.");
            var name = nameValue.GetString();
            var fingerprint = fingerprintValue.GetString();
            if (string.IsNullOrWhiteSpace(name) || name.StartsWith("dxai.tools.", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(fingerprint))
                return json.InvalidParameters("Select a non-broker DX function from discovery and preview it before invoking.");
            // The tool broker never manufactures a human approval, nor does it bypass target function validation.
            var result = await registry.InvokeAsync(name, new DxAiFunctionInvocationRequest
            {
                Parameters = parameters.Clone(),
                ExpectedPreviewFingerprint = fingerprint,
                PreviewOnly = false,
                UserConfirmed = false,
                AutomaticInvocation = request.AutomaticInvocation,
                RequestedBy = request.RequestedBy,
                ConversationId = request.ConversationId,
                ProjectId = request.ProjectId,
                ProjectVersionId = request.ProjectVersionId,
                ApplicationVersion = request.ApplicationVersion
            }, cancellationToken).ConfigureAwait(false);
            return json.Success(new { Target = name, Result = result });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Forwarding a preview-bound DX function request through Human Collaboration failed; parameters were omitted.");
            throw;
        }
    }
}
