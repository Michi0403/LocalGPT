using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Lists knowledge-backed compiler/runtime discovery profiles.</summary>
/// <param name="knowledge">Toolchain knowledge service dependency used by the list toolchain knowledge profiles function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the list toolchain knowledge profiles function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListToolchainKnowledgeProfilesFunction(IToolchainKnowledgeService knowledge, IDxAiFunctionJsonService json, ILogger<ListToolchainKnowledgeProfilesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list toolchain knowledge profiles function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.knowledge.list", "POST", "/api/dxai/functions/toolchain.knowledge.list/invoke",
        "Lists cross-platform compiler and runtime discovery profiles parsed from the local Knowledge Database.", "No parameters.",
        "Read-only local knowledge. The result contains discovery names and search policy, not environment-variable values or machine-specific paths.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ListToolchainKnowledgeProfilesFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list toolchain knowledge profiles function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var profiles = await knowledge.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
            return json.Success(profiles.Select(item => new { item.Key, item.DisplayName, item.Language, item.Kind, item.ExecutableNames, item.EnvironmentRootVariables, item.ProjectMarkers, item.ContextTags }).ToList());
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing toolchain knowledge profiles was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing toolchain knowledge profiles failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain knowledge profiles could not be listed. Review LocalGPT logs." }; }
    }
}

/// <summary>Lists stored compiler, runtime, SDK, and build-tool installations as structured local toolchain records.</summary>
/// <param name="projectMaintenance">Project maintenance service used to read persisted toolchain installations.</param>
/// <param name="json">DXFunction JSON service used to shape the invocation result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListToolchainInstallationsFunction(IProjectMaintenanceService projectMaintenance, IDxAiFunctionJsonService json, ILogger<ListToolchainInstallationsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the read-only AI capability that lists persisted local toolchain installations without exposing environment-variable values.</summary>
    /// <value>The DXFunction descriptor registered in the shared LocalGPT function registry.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.installation.list", "POST", "/api/dxai/functions/toolchain.installation.list/invoke",
        "Lists stored compiler, runtime, SDK, and build-tool installations with structured environment-variable names and Knowledge Database links.",
        "No parameters.",
        "Read-only local metadata. Environment variable values and full PATH blobs are omitted from the AI-facing result.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>Returns the stored local toolchain inventory without exposing environment-variable values.</summary>
    /// <param name="request">DXFunction invocation request; this read-only operation accepts no parameters.</param>
    /// <param name="cancellationToken">Cancellation token that stops the local database read.</param>
    /// <returns>The structured toolchain inventory returned through the standard DXFunction result envelope.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await projectMaintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false);
            return json.Success(items.Select(item => new
            {
                item.Id, item.Name, item.Language, item.ToolchainKind, item.DetectedPlatform,
                item.ExecutablePath, item.CompilerHomePath, item.Version, item.Architecture, item.DiscoverySource,
                item.ValidationArguments, item.KnowledgeProfileKey, item.KnowledgeEntryId, item.VersionKnowledgeEntryId,
                EnvironmentVariables = item.EnvironmentVariables.Select(value => new { value.Name, value.Source, value.IsEnabled }).ToList(),
                item.IsEnabled, item.IsDefaultForLanguage, item.LastValidatedAtUtc, item.LastValidationSucceeded
            }).ToList());
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Listing toolchain installations was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing toolchain installations failed; executable and environment values were omitted from logs.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain installations could not be listed. Review LocalGPT logs." };
        }
    }
}

/// <summary>Discovers local compiler/runtime installations through PATH and local knowledge-defined roots.</summary>
/// <param name="projectMaintenance">Project maintenance service dependency used by the discover toolchains function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the discover toolchains function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DiscoverToolchainsFunction(IProjectMaintenanceService projectMaintenance, IDxAiFunctionJsonService json, ILogger<DiscoverToolchainsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the discover toolchains function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.discover", "POST", "/api/dxai/functions/toolchain.discover/invoke",
        "Discovers local compiler and runtime installations using PATH first, then Knowledge Database environment roots and Windows/Linux/macOS roots.",
        "Optional customSearchRoots/customSearchRootsText and saveDiscovered. Local confirmation is required before the bounded filesystem scan.",
        "Local filesystem discovery only; no network lookup is performed and full PATH/environment values are never returned as one blob.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler", SupportsDeferredApprovalRequest: true,
        ParameterSchemaJson: """{"type":"object","properties":{"customSearchRoots":{"type":"array","items":{"type":"string"}},"customSearchRootsText":{"type":"string"},"saveDiscovered":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="DiscoverToolchainsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding discover toolchains function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<DiscoverProjectCompilersRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            return json.Success(await projectMaintenance.DiscoverCompilerInstallationsAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Toolchain discovery DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Toolchain discovery DXFunction failed; paths were omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain discovery failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Saves a compiler/runtime installation through the existing Project Maintenance service.</summary>
/// <param name="projectMaintenance">Project maintenance service dependency used by the save toolchain installation function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the save toolchain installation function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SaveToolchainInstallationFunction(IProjectMaintenanceService projectMaintenance, IDxAiFunctionJsonService json, ILogger<SaveToolchainInstallationFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the save toolchain installation function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.installation.save", "POST", "/api/dxai/functions/toolchain.installation.save/invoke",
        "Creates or updates one local compiler/runtime installation, including structured environment variables and its Knowledge Database profile link.",
        "Parameters follow SaveProjectCompilerInstallationRequest.", "Mutates local toolchain configuration and requires exact human confirmation.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, Source: "DIHandler", SupportsDeferredApprovalRequest: true,
        ParameterSchemaJson: """{"type":"object","required":["name","language","executablePath"],"properties":{"id":{"type":["string","null"]},"name":{"type":"string"},"language":{"type":"string"},"executablePath":{"type":"string"},"compilerHomePath":{"type":"string"},"version":{"type":"string"},"architecture":{"type":"string"},"discoverySource":{"type":"string"},"toolchainKind":{"type":"string"},"detectedPlatform":{"type":"string"},"validationArguments":{"type":"string"},"environmentVariables":{"type":"array"},"knowledgeProfileKey":{"type":"string"},"knowledgeEntryId":{"type":["string","null"]},"versionKnowledgeEntryId":{"type":["string","null"]},"isEnabled":{"type":"boolean"},"isDefaultForLanguage":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="SaveToolchainInstallationFunction"/>, keeping the operation consistent with the state and invariants of the surrounding save toolchain installation function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<SaveProjectCompilerInstallationRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            return json.Success(await projectMaintenance.SaveCompilerInstallationAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Saving toolchain installation was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Saving toolchain installation failed; local paths and environment values were omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain installation could not be saved. Review LocalGPT logs." }; }
    }
}

/// <summary>Runs the stored bounded version probe and links/request exact version knowledge.</summary>
/// <param name="projectMaintenance">Project maintenance service dependency used by the validate toolchain installation function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the validate toolchain installation function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ValidateToolchainInstallationFunction(IProjectMaintenanceService projectMaintenance, IDxAiFunctionJsonService json, ILogger<ValidateToolchainInstallationFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the validate toolchain installation function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.installation.validate", "POST", "/api/dxai/functions/toolchain.installation.validate/invoke",
        "Validates one stored compiler/runtime installation with its bounded local version probe, then checks local Knowledge Database context for the detected version.",
        "compilerId is required.", "Executes one local tool with its stored validation arguments. Human confirmation is required; no network lookup occurs.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, Source: "DIHandler", SupportsDeferredApprovalRequest: true,
        ParameterSchemaJson: """{"type":"object","required":["compilerId"],"properties":{"compilerId":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ValidateToolchainInstallationFunction"/>, keeping the operation consistent with the state and invariants of the surrounding validate toolchain installation function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ToolchainInstallationActionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await projectMaintenance.ValidateCompilerInstallationAsync(binding.Value.CompilerId, userConfirmed: true, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Validating toolchain installation was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Validating toolchain installation failed; probe output was omitted from logs."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain validation failed. Review LocalGPT logs." }; }
    }
}

/// <summary>
/// Represents a delete toolchain installation function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="projectMaintenance">Project maintenance service dependency used by the delete toolchain installation function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the delete toolchain installation function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DeleteToolchainInstallationFunction(IProjectMaintenanceService projectMaintenance, IDxAiFunctionJsonService json, ILogger<DeleteToolchainInstallationFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the delete toolchain installation function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.installation.delete", "POST", "/api/dxai/functions/toolchain.installation.delete/invoke",
        "Deletes one stored compiler/runtime installation when it is not protected by existing project references.", "compilerId is required.",
        "Destructive local configuration mutation requiring human confirmation.", IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, Source: "DIHandler", SupportsDeferredApprovalRequest: true,
        ParameterSchemaJson: """{"type":"object","required":["compilerId"],"properties":{"compilerId":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="DeleteToolchainInstallationFunction"/>, keeping the operation consistent with the state and invariants of the surrounding delete toolchain installation function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ToolchainInstallationActionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(new { removed = await projectMaintenance.DeleteCompilerInstallationAsync(binding.Value.CompilerId, userConfirmed: true, cancellationToken).ConfigureAwait(false) });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Deleting toolchain installation was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Deleting toolchain installation failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Toolchain installation could not be deleted. Review LocalGPT logs." }; }
    }
}

/// <summary>Requests missing exact-version compiler/runtime knowledge from the local user.</summary>
/// <param name="knowledge">Toolchain knowledge service dependency used by the request toolchain knowledge function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the request toolchain knowledge function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RequestToolchainKnowledgeFunction(IToolchainKnowledgeService knowledge, IDxAiFunctionJsonService json, ILogger<RequestToolchainKnowledgeFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the request toolchain knowledge function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "toolchain.knowledge.request", "POST", "/api/dxai/functions/toolchain.knowledge.request/invoke",
        "Asks the local user for Markdown, Knowledge Database, or text context when an exact compiler/runtime version is missing from local knowledge.",
        "profileKey and version are required; context is optional.", "Creates a non-blocking local Human Collaboration question and performs no online lookup.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, IsCoordinationOnly: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey","version"],"properties":{"profileKey":{"type":"string"},"version":{"type":"string"},"context":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="RequestToolchainKnowledgeFunction"/>, keeping the operation consistent with the state and invariants of the surrounding request toolchain knowledge function workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ToolchainKnowledgeGapRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await knowledge.RequestMissingVersionKnowledgeAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Requesting toolchain knowledge was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Requesting toolchain knowledge failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "The toolchain knowledge request could not be created. Review LocalGPT logs." }; }
    }
}
