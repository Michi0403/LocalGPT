using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Lists visible toolchain environment names and scope metadata without exposing values to AI callers.</summary>
public sealed class ListToolchainEnvironmentFunction(IToolchainEnvironmentService environment, IDxAiFunctionJsonService json, ILogger<ListToolchainEnvironmentFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the read-only environment inventory function.</summary>
    /// <value>The registered DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.environment.list", "POST", "/api/dxai/functions/toolchain.environment.list/invoke",
        "Lists environment-variable names, scopes, override state and writability used by the Toolchains workbench.",
        "No parameters.", "Read-only local metadata. Environment-variable values are deliberately omitted from the AI-facing result.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""", IsCoordinationOnly: true);

    /// <summary>Returns the redacted environment inventory metadata.</summary>
    /// <param name="request">DXFunction request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The standard invocation result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await environment.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return json.Success(new
            {
                snapshot.Platform,
                snapshot.SupportsPersistentOperatingSystemScopes,
                Entries = snapshot.Entries.Select(item => new { item.Name, item.Scope, item.Source, item.IsSensitive, item.IsWritable, item.IsOverridden }).ToList()
            });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing toolchain environment metadata was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing toolchain environment metadata failed; names and values were omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain environment metadata could not be listed. Review LocalGPT logs." }; }
    }
}

/// <summary>Changes one toolchain environment value after the existing DXFunction human-approval flow completes.</summary>
public sealed class ChangeToolchainEnvironmentFunction(IToolchainEnvironmentService environment, IDxAiFunctionJsonService json, ILogger<ChangeToolchainEnvironmentFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the confirmation-gated environment mutation function.</summary>
    /// <value>The registered DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.environment.change", "POST", "/api/dxai/functions/toolchain.environment.change/invoke",
        "Changes or removes one Process, Application, User, or Machine environment variable through the toolchain environment service.",
        "name, value, scope and remove are supported. Effective is read-only.",
        "Mutates runtime or persistent environment state and requires exact human confirmation. Credential-like values are not returned afterward.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["name","scope"],"properties":{"name":{"type":"string","minLength":1,"maxLength":256},"value":{"type":"string","maxLength":32768},"scope":{"type":"integer","minimum":1,"maximum":4},"remove":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>Applies one approved environment-variable change.</summary>
    /// <param name="request">DXFunction request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The standard invocation result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ToolchainEnvironmentChangeRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            var snapshot = await environment.ChangeAsync(binding.Value, cancellationToken).ConfigureAwait(false);
            return json.Success(new { Changed = true, snapshot.Platform, EntryCount = snapshot.Entries.Count });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Changing a toolchain environment value was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Changing a toolchain environment value failed; name and value were omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Environment-variable change failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Lists LocalGPT's reviewed direct toolchain download catalog.</summary>
public sealed class ListToolchainAcquisitionCatalogFunction(IToolchainAcquisitionService acquisition, IDxAiFunctionJsonService json, ILogger<ListToolchainAcquisitionCatalogFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the read-only toolchain acquisition catalog function.</summary>
    /// <value>The registered DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.acquisition.catalog", "POST", "/api/dxai/functions/toolchain.acquisition.catalog/invoke",
        "Lists reviewed manufacturer/GitHub direct-download entries for Python, .NET, Node.js and other toolchains.",
        "No parameters.", "Read-only embedded catalog. No network request occurs until a human approves a download.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""", IsCoordinationOnly: true);

    /// <summary>Returns the reviewed toolchain acquisition catalog.</summary>
    /// <param name="request">DXFunction request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The standard invocation result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return json.Success(await acquisition.GetCatalogAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing toolchain acquisition entries was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing toolchain acquisition entries failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain acquisition catalog could not be listed." }; }
    }
}

/// <summary>Downloads one reviewed toolchain artifact after DXFunction human approval.</summary>
public sealed class DownloadToolchainAcquisitionFunction(IToolchainAcquisitionService acquisition, IDxAiFunctionJsonService json, ILogger<DownloadToolchainAcquisitionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the confirmation-gated direct toolchain download function.</summary>
    /// <value>The registered DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.acquisition.download", "POST", "/api/dxai/functions/toolchain.acquisition.download/invoke",
        "Downloads one reviewed toolchain artifact directly over HTTPS without GitHub CLI and without executing it.",
        "sourceKey is required.", "Network and filesystem mutation requiring human confirmation. Downloaded scripts/installers are never executed automatically.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sourceKey"],"properties":{"sourceKey":{"type":"string","minLength":1,"maxLength":160}},"additionalProperties":false}""");

    /// <summary>Downloads one approved toolchain catalog entry.</summary>
    /// <param name="request">DXFunction request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The standard invocation result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ToolchainAcquisitionDownloadRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            var result = await acquisition.DownloadAsync(binding.Value, cancellationToken).ConfigureAwait(false);
            return json.Success(new { result.SourceKey, result.LocalPath, result.Bytes, result.Sha256, result.DownloadedAtUtc, result.InstallHint });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Downloading a reviewed toolchain artifact was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Downloading a reviewed toolchain artifact failed; URL and path were omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain download failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Lists reusable database-backed toolchain process profiles so low-B models can select a known workflow instead of reconstructing command lines.</summary>
public sealed class ListToolchainExecutionProfilesFunction(IToolchainExecutionProfileService profiles, IDxAiFunctionJsonService json, ILogger<ListToolchainExecutionProfilesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the read-only profile inventory.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.profile.list", "POST", "/api/dxai/functions/toolchain.profile.list/invoke",
        "Lists reusable database-backed toolchain process profiles and capability bindings.",
        "Optional capabilityKey filters profiles.", "Read-only database metadata. Environment values and process output are not exposed.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, IsCoordinationOnly: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"capabilityKey":{"type":"string","maxLength":160}},"additionalProperties":false}""");

    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            string? capability = null;
            if (request.Parameters.ValueKind == System.Text.Json.JsonValueKind.Object && request.Parameters.TryGetProperty("capabilityKey", out var element) && element.ValueKind == System.Text.Json.JsonValueKind.String)
                capability = element.GetString();
            var items = await profiles.GetProfilesAsync(capability, cancellationToken).ConfigureAwait(false);
            return json.Success(items.Select(item => new { item.ProfileKey, item.Name, item.ToolchainInstallationId, item.CapabilityKey, item.ExecutionKind, item.EntryPoint, item.IsDefaultForCapability, item.RequiresApproval }).ToList());
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing toolchain execution profiles was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing toolchain execution profiles failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain profiles could not be listed." }; }
    }
}

/// <summary>Saves a reusable toolchain process profile through the standard LocalGPT human approval inbox.</summary>
public sealed class SaveToolchainExecutionProfileFunction(IToolchainExecutionProfileService profiles, IDxAiFunctionJsonService json, ILogger<SaveToolchainExecutionProfileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the profile mutation function.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.profile.save", "POST", "/api/dxai/functions/toolchain.profile.save/invoke",
        "Creates or updates a reusable database-backed toolchain process profile for Python, .NET, Java, Node.js, C/C++, Go or other registered executables.",
        "profile contains profileKey, name, toolchainInstallationId, capabilityKey, executionKind, entryPoint, argumentsJson, workingDirectory, environmentVariablesJson and configurationJson.",
        "Changes executable behavior and therefore enters the Human Collaboration approval inbox unless the user has pre-authorized this function in permissions.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profile"],"properties":{"profile":{"type":"object"}},"additionalProperties":false}""");

    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<SaveToolchainExecutionProfileRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            return json.Success(await profiles.SaveProfileAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Saving a toolchain execution profile was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Saving a toolchain execution profile failed; command details were omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain profile save failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Runs a persisted toolchain process profile after approval so models do not need shell access or bespoke runtime code.</summary>
public sealed class ExecuteToolchainProfileFunction(IToolchainExecutionProfileService profiles, IDxAiFunctionJsonService json, ILogger<ExecuteToolchainProfileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the profile execution function.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.profile.execute", "POST", "/api/dxai/functions/toolchain.profile.execute/invoke",
        "Runs one database-backed toolchain process profile without a shell and returns bounded stdout/stderr.",
        "profileKey is required; arguments is an optional string array appended to persisted arguments.",
        "Starts a local process and requires Human Collaboration approval unless the user has pre-authorized the function in permissions.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey"],"properties":{"profileKey":{"type":"string","minLength":1,"maxLength":96},"arguments":{"type":"array","maxItems":256,"items":{"type":"string","maxLength":8192}}},"additionalProperties":false}""");

    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ToolchainProcessExecutionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            var result = await profiles.ExecuteAsync(binding.Value, cancellationToken).ConfigureAwait(false);
            return json.Success(new { result.ProfileKey, result.ExitCode, result.StandardOutput, result.StandardError, result.CompletedAtUtc });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Running a toolchain execution profile was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Running a toolchain execution profile failed; command details and output were omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain profile execution failed. Review LocalGPT logs." }; }
    }
}
