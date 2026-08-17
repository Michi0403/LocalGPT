using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Returns the current non-mutating AI-guided setup snapshot.</summary>
/// <param name="setup">Initial setup orchestration service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class InitialSetupStatusFunction(IInitialSetupAssistantService setup, IDxAiFunctionJsonService json, ILogger<InitialSetupStatusFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the read-only initial-setup snapshot capability.</summary>
    /// <value>The descriptor value exposed by <see cref="InitialSetupStatusFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.status", "POST", "/api/dxai/functions/initial.setup.status/invoke",
        "Returns detected/configured GPU devices, knowledge-backed provider profiles, installed provider-qualified models, recommended strong curator models, platform and onboarding state. During AI-guided setup use human.collaboration.request with suggestedResponses for user choices instead of inventing a second prompt mechanism.",
        "No parameters. The normal human.collaboration.request DXFunction is the maintained menu/guidance channel for hardware source, optional web opt-in, provider choice and benchmark choices.", "Read-only local discovery; does not install or download anything.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>Returns the setup snapshot.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return json.Success(await setup.GetSnapshotAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Initial setup status DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Initial setup status DXFunction failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Initial setup status could not be loaded. Review LocalGPT logs." }; }
    }
}

/// <summary>Fetches CanIRun.ai recommendations only after the normal human approval gate authorizes the exact device lookup.</summary>
/// <param name="recommendations">CanIRun.ai recommendation service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class CanIRunRecommendationsFunction(ICanIRunHardwareRecommendationService recommendations, IDxAiFunctionJsonService json, ILogger<CanIRunRecommendationsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the optional attributed web lookup capability.</summary>
    /// <value>The descriptor value exposed by <see cref="CanIRunRecommendationsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.canirun.recommendations", "POST", "/api/dxai/functions/initial.setup.canirun.recommendations/invoke",
        "Fetches public CanIRun.ai model compatibility cards for one user-selected GPU slug and returns them with source attribution.",
        "deviceSlug is required.", "Optional external web access to canirun.ai only. Requires fresh human confirmation and is never automatic.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["deviceSlug"],"properties":{"deviceSlug":{"type":"string","maxLength":120}},"additionalProperties":false}""");

    /// <summary>Runs the explicitly approved CanIRun.ai lookup.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<CanIRunLookupRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await recommendations.GetRecommendationsAsync(binding.Value.DeviceSlug, userConfirmedWebLookup: true, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "CanIRun.ai DXFunction lookup was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "CanIRun.ai DXFunction lookup failed; response content omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "CanIRun.ai recommendations could not be loaded. Review LocalGPT logs." }; }
    }
}

/// <summary>Lists current-platform provider bootstrap profiles from the local Knowledge Database.</summary>
/// <param name="providers">Provider bootstrap service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class ProviderBootstrapProfilesFunction(IAiProviderBootstrapService providers, IDxAiFunctionJsonService json, ILogger<ProviderBootstrapProfilesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the read-only provider profile list.</summary>
    /// <value>The descriptor value exposed by <see cref="ProviderBootstrapProfilesFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.provider.list", "POST", "/api/dxai/functions/initial.setup.provider.list/invoke",
        "Lists user-maintainable local Knowledge Database profiles for installing and controlling supported AI providers on the current OS.",
        "No parameters.", "Read-only local knowledge. Command text is returned for human review but nothing is executed.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>Returns provider profiles.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return json.Success(await providers.GetProfilesAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing provider setup profiles was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing provider setup profiles failed."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Provider setup profiles could not be loaded. Review LocalGPT logs." }; }
    }
}

/// <summary>Runs one knowledge-backed provider detection command after user approval of the mutable knowledge-defined command.</summary>
/// <param name="providers">Provider bootstrap service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class DetectProviderBootstrapFunction(IAiProviderBootstrapService providers, IDxAiFunctionJsonService json, ILogger<DetectProviderBootstrapFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes provider detection through the bounded console engine.</summary>
    /// <value>The descriptor value exposed by <see cref="DetectProviderBootstrapFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.provider.detect", "POST", "/api/dxai/functions/initial.setup.provider.detect/invoke",
        "Runs the selected provider profile's read-only detection command through LocalGPT's bounded shared console.",
        "profileKey is required.", "The command comes from user-maintainable knowledge, therefore AI invocation still requires human confirmation. No automatic invocation.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey"],"properties":{"profileKey":{"type":"string","maxLength":160}},"additionalProperties":false}""");

    /// <summary>Runs provider detection.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProviderProfileActionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await providers.DetectAsync(binding.Value.ProfileKey, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider detection DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider detection DXFunction failed; command content omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Provider detection failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Lists a provider's local model store through its knowledge-backed read-only command after user review.</summary>
/// <param name="providers">Provider bootstrap service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class ListProviderBootstrapModelsFunction(IAiProviderBootstrapService providers, IDxAiFunctionJsonService json, ILogger<ListProviderBootstrapModelsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes provider model-store listing through the bounded console engine.</summary>
    /// <value>The descriptor value exposed by <see cref="ListProviderBootstrapModelsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.provider.models.list", "POST", "/api/dxai/functions/initial.setup.provider.models.list/invoke",
        "Runs the selected provider profile's read-only local model-listing command through LocalGPT's bounded shared console.",
        "profileKey is required.", "The command comes from user-maintainable Knowledge. Human confirmation is required for AI invocation; no automatic shell execution.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey"],"properties":{"profileKey":{"type":"string","maxLength":160}},"additionalProperties":false}""");

    /// <summary>Runs the reviewed provider model listing.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProviderProfileActionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await providers.ListModelsAsync(binding.Value.ProfileKey, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider model listing DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider model listing DXFunction failed; command/output content omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Provider model listing failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Installs a provider from a local knowledge profile after fresh human confirmation.</summary>
/// <param name="providers">Provider bootstrap service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class InstallProviderBootstrapFunction(IAiProviderBootstrapService providers, IDxAiFunctionJsonService json, ILogger<InstallProviderBootstrapFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes provider installation.</summary>
    /// <value>The descriptor value exposed by <see cref="InstallProviderBootstrapFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.provider.install", "POST", "/api/dxai/functions/initial.setup.provider.install/invoke",
        "Installs the selected AI provider using the exact command stored in the local Knowledge Database.", "profileKey is required.",
        "Consequential local-machine change from user-maintainable knowledge. Requires fresh human confirmation; never automatic.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey"],"properties":{"profileKey":{"type":"string","maxLength":160}},"additionalProperties":false}""");

    /// <summary>Runs provider installation.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var binding = json.Bind<ProviderProfileActionRequest>(request.Parameters); if (!binding.Succeeded) return json.InvalidParameters(binding.Error); return json.Success(await providers.InstallAsync(binding.Value.ProfileKey, true, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider installation DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider installation DXFunction failed; command content omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Provider installation failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Starts a provider runtime from a knowledge profile after fresh human confirmation.</summary>
/// <param name="providers">Provider bootstrap service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class StartProviderBootstrapFunction(IAiProviderBootstrapService providers, IDxAiFunctionJsonService json, ILogger<StartProviderBootstrapFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes provider startup.</summary>
    /// <value>The descriptor value exposed by <see cref="StartProviderBootstrapFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.provider.start", "POST", "/api/dxai/functions/initial.setup.provider.start/invoke",
        "Starts the selected AI provider using the exact command stored in the local Knowledge Database.", "profileKey is required.",
        "Consequential local-machine change from user-maintainable knowledge. Requires fresh human confirmation; never automatic.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey"],"properties":{"profileKey":{"type":"string","maxLength":160}},"additionalProperties":false}""");

    /// <summary>Runs provider startup.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var binding = json.Bind<ProviderProfileActionRequest>(request.Parameters); if (!binding.Succeeded) return json.InvalidParameters(binding.Error); return json.Success(await providers.StartAsync(binding.Value.ProfileKey, true, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider startup DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider startup DXFunction failed; command content omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Provider startup failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Downloads one provider model using the knowledge profile's install template after fresh human confirmation.</summary>
/// <param name="providers">Provider bootstrap service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class InstallProviderModelBootstrapFunction(IAiProviderBootstrapService providers, IDxAiFunctionJsonService json, ILogger<InstallProviderModelBootstrapFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes provider model installation.</summary>
    /// <value>The descriptor value exposed by <see cref="InstallProviderModelBootstrapFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.provider.model.install", "POST", "/api/dxai/functions/initial.setup.provider.model.install/invoke",
        "Installs one selected model through the provider-specific Knowledge Database command template.", "profileKey and modelId are required.",
        "Consequential local-machine/model-store change from user-maintainable knowledge. Requires fresh human confirmation; never automatic.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey","modelId"],"properties":{"profileKey":{"type":"string","maxLength":160},"modelId":{"type":"string","maxLength":240}},"additionalProperties":false}""");

    /// <summary>Runs model installation.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var binding = json.Bind<ProviderModelInstallRequest>(request.Parameters); if (!binding.Succeeded) return json.InvalidParameters(binding.Error); return json.Success(await providers.InstallModelAsync(binding.Value.ProfileKey, binding.Value.ModelId, true, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider model installation DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider model installation DXFunction failed; command/model values omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Provider model installation failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Creates a user-owned hardware-curated benchmark Council team after human confirmation.</summary>
/// <param name="setup">Initial setup orchestration service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class CreateInitialBenchmarkTeamFunction(IInitialSetupAssistantService setup, IDxAiFunctionJsonService json, ILogger<CreateInitialBenchmarkTeamFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the user-owned benchmark-team creation capability.</summary>
    /// <value>The descriptor value exposed by <see cref="CreateInitialBenchmarkTeamFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.benchmark.team.create", "POST", "/api/dxai/functions/initial.setup.benchmark.team.create/invoke",
        "Creates or refreshes a user-owned adaptive benchmark Council team. Broad benchmark roles use all selected models while curator/director/reviewer roles use the preferred stronger model pool.",
        "modelSelectionKeys is required; preferredCuratorModelKeys and displayName are optional.",
        "Persists Council team configuration and requires fresh human confirmation.", IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelSelectionKeys"],"properties":{"modelSelectionKeys":{"type":"array","minItems":1,"maxItems":128,"items":{"type":"string","maxLength":512}},"preferredCuratorModelKeys":{"type":"array","maxItems":4,"items":{"type":"string","maxLength":512}},"displayName":{"type":"string","maxLength":200}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="CreateInitialBenchmarkTeamFunction"/>, keeping the operation consistent with the state and invariants of the surrounding create initial benchmark team function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<CreateInitialBenchmarkTeamRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            return json.Success(await setup.CreateBenchmarkTeamAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Initial benchmark team DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Initial benchmark team DXFunction failed; model values omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "The initial benchmark team could not be created. Review LocalGPT logs." }; }
    }
}

/// <summary>Parameter object for one optional CanIRun.ai device lookup.</summary>
public sealed class CanIRunLookupRequest { /// <summary>Gets or sets the CanIRun.ai device slug.</summary>
    public string DeviceSlug { get; set; } = string.Empty; }
/// <summary>Parameter object for one provider profile operation.</summary>
public sealed class ProviderProfileActionRequest { /// <summary>Gets or sets the provider profile key.</summary>
    public string ProfileKey { get; set; } = string.Empty; }
/// <summary>Parameter object for one provider model installation.</summary>
public sealed class ProviderModelInstallRequest { /// <summary>Gets or sets the provider profile key.</summary>
    /// <summary>
    /// Gets or sets the stable profile key used to identify or correlate this provider model install instance with related application state.
    /// </summary>
    /// <value>The profile key value exposed by <see cref="ProviderModelInstallRequest"/>.</value>
    public string ProfileKey { get; set; } = string.Empty; /// <summary>Gets or sets the model identifier.</summary>
    public string ModelId { get; set; } = string.Empty; }

