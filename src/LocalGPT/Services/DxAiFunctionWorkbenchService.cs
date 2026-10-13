using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Service boundary used by the DevExpress workbench; the registry remains the only executor.</summary>
public sealed class DxAiFunctionWorkbenchService(
    IDxAiFunctionServiceClient client,
    IDxAiFunctionInvocationAuditService audit,
    ILogger<DxAiFunctionWorkbenchService> logger) : IDxAiFunctionWorkbenchService
{
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
