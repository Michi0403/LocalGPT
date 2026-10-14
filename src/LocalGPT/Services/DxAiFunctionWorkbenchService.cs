using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Service boundary used by the DevExpress workbench; the registry remains the only executor.</summary>
/// <param name="client">Devexpress ai function service client dependency used by the DevExpress AI function workbench workflow to provide the corresponding application capability.</param>
/// <param name="audit">Devexpress ai function invocation audit service dependency used by the DevExpress AI function workbench workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DxAiFunctionWorkbenchService(
    IDxAiFunctionServiceClient client,
    IDxAiFunctionInvocationAuditService audit,
    ILogger<DxAiFunctionWorkbenchService> logger) : IDxAiFunctionWorkbenchService
{
    /// <summary>
    /// Performs preview as part of the DevExpress AI function workbench service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="parametersJson">Parameters json value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> PreviewAsync(string functionName, string parametersJson, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryParameters(parametersJson, out var parameters, out var error))
                return Invalid(functionName, error);
            return await client.CallAsync(functionName, new DxAiFunctionInvocationRequest
            {
                Parameters = parameters,
                PreviewOnly = true,
                AutomaticInvocation = false,
                UserConfirmed = false
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "DX function workbench preview failed for {FunctionName}; parameters were not logged.", functionName);
            throw;
        }
    }

    /// <summary>
    /// Performs execute as part of the DevExpress AI function workbench service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="parametersJson">Parameters json value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="previewFingerprint">Preview fingerprint value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> ExecuteAsync(string functionName, string parametersJson,
        string previewFingerprint, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(previewFingerprint))
                return Invalid(functionName, "Preview this exact call before attempting execution.");
            if (!TryParameters(parametersJson, out var parameters, out var error))
                return Invalid(functionName, error);
            return await client.CallAsync(functionName, new DxAiFunctionInvocationRequest
            {
                Parameters = parameters,
                PreviewOnly = false,
                ExpectedPreviewFingerprint = previewFingerprint,
                UserConfirmed = false,
                AutomaticInvocation = false
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "DX function workbench execution failed for {FunctionName}; parameters were not logged.", functionName);
            throw;
        }
    }

    /// <summary>
    /// Retrieves recent as part of the DevExpress AI function workbench service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<DxAiFunctionInvocationAuditEntry>> GetRecentAsync(int take = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            return await audit.GetRecentAsync(take, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "DX function workbench history load failed.");
            throw;
        }
    }

    /// <summary>
    /// Attempts to parameters as part of the DevExpress AI function workbench service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryParameters(string json, out JsonElement parameters, out string error)
    {
        parameters = default;
        error = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Function parameters must be a JSON object.";
                return false;
            }
            parameters = document.RootElement.Clone();
            return true;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Workbench function parameter JSON was rejected without recording the payload.");
            error = "Function parameters are not valid JSON. Correct them and preview again.";
            return false;
        }
    }

    /// <summary>
    /// Performs invalid as part of the DevExpress AI function workbench service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <param name="message">Message value supplied to the DevExpress AI function workbench operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    private DxAiFunctionInvocationResult Invalid(string functionName, string message)
    {
        try
        {
            return new DxAiFunctionInvocationResult
            {
                FunctionName = functionName,
                Status = "InvalidParameters",
                Error = message,
                ValidationIssues = [new DxAiFunctionValidationIssue { Code = "InvalidJson", Path = "parameters", Message = message }]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a DX function input diagnostic failed.");
            throw;
        }
    }
}
