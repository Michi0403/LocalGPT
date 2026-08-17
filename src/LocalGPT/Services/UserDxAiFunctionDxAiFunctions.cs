using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Lists user-created composable DXFunctions.</summary>
/// <param name="userFunctions">User devexpress ai function service dependency used by the list user DevExpress AI functions function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the list user DevExpress AI functions function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListUserDxAiFunctionsFunction(IUserDxAiFunctionService userFunctions, IDxAiFunctionJsonService json, ILogger<ListUserDxAiFunctionsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list user DevExpress AI functions function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.user_dxfunction.list", "POST", "/api/dxai/functions/localgpt.user_dxfunction.list/invoke",
        "Lists user-created DXFunctions and the Remote Control pipeline each one wraps.", "No parameters.", "Read-only local configuration metadata.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");
    /// <summary>
    /// Performs invoke for <see cref="ListUserDxAiFunctionsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list user DevExpress AI functions function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return json.Success(await userFunctions.ListAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing user DXFunctions was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing user DXFunctions failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "User DXFunctions could not be listed. Review LocalGPT logs." }; }
    }
}

/// <summary>Creates or updates a user-defined DXFunction backed by a Remote Control pipeline.</summary>
/// <param name="userFunctions">User devexpress ai function service dependency used by the save user DevExpress AI function function workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Devexpress ai function catalog service dependency used by the save user DevExpress AI function function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the save user DevExpress AI function function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SaveUserDxAiFunctionFunction(IUserDxAiFunctionService userFunctions, IDxAiFunctionCatalogService catalog, IDxAiFunctionJsonService json, ILogger<SaveUserDxAiFunctionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the save user DevExpress AI function function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.user_dxfunction.save", "POST", "/api/dxai/functions/localgpt.user_dxfunction.save/invoke",
        "Creates or updates a user.* DXFunction whose implementation is a persisted Remote Control pipeline.",
        "functionName, pipelineKey, purpose, parameterSchemaJson and policy flags define the function.",
        "Capability mutation requiring human confirmation. The resulting function still uses the normal DXFunction registry, schema validation and approval policy.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, Source: "DIHandler", SupportsDeferredApprovalRequest: true,
        ParameterSchemaJson: """{"type":"object","required":["functionName","pipelineKey"],"properties":{"id":{"type":["string","null"]},"functionName":{"type":"string"},"displayName":{"type":"string"},"purpose":{"type":"string"},"safetyNotes":{"type":"string"},"parameterSchemaJson":{"type":"string"},"pipelineKey":{"type":"string"},"isEnabled":{"type":"boolean"},"availableToAi":{"type":"boolean"},"isReadOnly":{"type":"boolean"},"requiresHumanConfirmation":{"type":"boolean"},"supportsAutomaticInvocation":{"type":"boolean"}},"additionalProperties":false}""");
    /// <summary>
    /// Performs invoke for <see cref="SaveUserDxAiFunctionFunction"/>, keeping the operation consistent with the state and invariants of the surrounding save user DevExpress AI function function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<SaveUserDxAiFunctionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            var row = await userFunctions.SaveAsync(binding.Value, cancellationToken).ConfigureAwait(false);
            await catalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            return json.Success(row);
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Saving user DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Saving user DXFunction failed; schema content was omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "User DXFunction could not be saved. Review LocalGPT logs." }; }
    }
}

/// <summary>Deletes a user-defined DXFunction while preserving its pipeline.</summary>
/// <param name="userFunctions">User devexpress ai function service dependency used by the delete user DevExpress AI function function workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Devexpress ai function catalog service dependency used by the delete user DevExpress AI function function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the delete user DevExpress AI function function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DeleteUserDxAiFunctionFunction(IUserDxAiFunctionService userFunctions, IDxAiFunctionCatalogService catalog, IDxAiFunctionJsonService json, ILogger<DeleteUserDxAiFunctionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the delete user DevExpress AI function function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.user_dxfunction.delete", "POST", "/api/dxai/functions/localgpt.user_dxfunction.delete/invoke",
        "Deletes one user.* DXFunction definition without deleting its underlying Remote Control pipeline.", "functionName is required.",
        "Destructive capability mutation requiring human confirmation. DI/system functions cannot be deleted by this function.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, Source: "DIHandler", SupportsDeferredApprovalRequest: true,
        ParameterSchemaJson: """{"type":"object","required":["functionName"],"properties":{"functionName":{"type":"string"}},"additionalProperties":false}""");
    /// <summary>
    /// Performs invoke for <see cref="DeleteUserDxAiFunctionFunction"/>, keeping the operation consistent with the state and invariants of the surrounding delete user DevExpress AI function function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<DeleteUserDxAiFunctionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var removed = await userFunctions.DeleteAsync(binding.Value.FunctionName, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            await catalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            return json.Success(new { removed });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Deleting user DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Deleting user DXFunction failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "User DXFunction could not be deleted. Review LocalGPT logs." }; }
    }
}
