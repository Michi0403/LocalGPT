using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Binds a discovered Python installation to the LocalGPT runtime through the existing runtime/toolchain services.</summary>
/// <param name="runtime">Local ai runtime service dependency used by the configure local AI python function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the configure local AI python function workflow to provide the corresponding application capability.</param>
/// <param name="platform">Platform runtime service that owns host filesystem path comparison semantics.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ConfigureLocalAiPythonFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, IPlatformRuntimeService platform, ILogger<ConfigureLocalAiPythonFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the configure local AI python function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ConfigureLocalAiPythonFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.python.configure", "POST", "/api/dxai/functions/localai.python.configure/invoke",
        "Selects one already-installed Python runtime for LocalGPT local-AI use and stores it through the generic toolchain/configuration services.",
        "executablePath is required and must match a currently discovered Python candidate.",
        "Persistent runtime/toolchain mutation. Invoke with the exact selected path to queue Human Collaboration approval; no Python package or model is installed by this call.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["executablePath"],"properties":{"executablePath":{"type":"string","minLength":1,"maxLength":4096}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ConfigureLocalAiPythonFunction"/>, keeping the operation consistent with the state and invariants of the surrounding configure local AI python function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiPythonConfigureRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var requested = Path.GetFullPath(binding.Value.ExecutablePath);
            var candidates = await runtime.DiscoverPythonAsync(cancellationToken).ConfigureAwait(false);
            var candidate = candidates.FirstOrDefault(item => platform.PathComparer.Equals(Path.GetFullPath(item.ExecutablePath), requested));
            if (candidate is null)
                return new() { Succeeded = false, Status = "NotFound", Error = "The requested executable is not a currently discovered Python runtime candidate." };
            await runtime.ConfigurePythonAsync(candidate, cancellationToken).ConfigureAwait(false);
            return json.Success(new { Configured = true, candidate.Version, candidate.DiscoverySource, candidate.RuntimeLibraryDetected });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError("Configuring LocalGPT Python failed with {ExceptionType}; runtime paths were omitted.", exception.GetType().Name);
            return new() { Succeeded = false, Status = "Failed", Error = "Python runtime configuration failed. Review LocalGPT logs." };
        }
    }
}

/// <summary>Creates the managed Python environment after the generic DX approval dispatcher authorizes the exact action.</summary>
/// <param name="runtime">Local ai runtime service dependency used by the create local AI python environment function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the create local AI python environment function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CreateLocalAiPythonEnvironmentFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<CreateLocalAiPythonEnvironmentFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the create local AI python environment function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="CreateLocalAiPythonEnvironmentFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.python.environment.create", "POST", "/api/dxai/functions/localai.python.environment.create/invoke",
        "Creates or repairs LocalGPT's managed Python virtual environment and baseline package profile.",
        "No parameters.", "Filesystem/process/package mutation requiring exact Human Collaboration approval.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="CreateLocalAiPythonEnvironmentFunction"/>, keeping the operation consistent with the state and invariants of the surrounding create local AI python environment function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await runtime.CreateManagedEnvironmentAsync(userConfirmed: true, cancellationToken).ConfigureAwait(false);
            return json.Success(new { Ready = true });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError("Creating the managed Python environment failed with {ExceptionType}; paths and package output were omitted.", exception.GetType().Name);
            return new() { Succeeded = false, Status = "Failed", Error = "Managed Python environment creation failed. Review LocalGPT logs." };
        }
    }
}

/// <summary>Installs one named package profile in the managed Python environment through the existing runtime service.</summary>
/// <param name="runtime">Local ai runtime service dependency used by the install local AI python package profile function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the install local AI python package profile function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class InstallLocalAiPythonPackageProfileFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<InstallLocalAiPythonPackageProfileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the install local AI python package profile function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="InstallLocalAiPythonPackageProfileFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.python.package.install", "POST", "/api/dxai/functions/localai.python.package.install/invoke",
        "Installs one configured LocalGPT Python package profile such as PyTorch, Whisper support or dynamic web-content support.",
        "profileKey is required and must exist in the configured package-profile catalog.",
        "Python package mutation requiring exact Human Collaboration approval. Package output and local paths are not returned to AI callers.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey"],"properties":{"profileKey":{"type":"string","minLength":1,"maxLength":96}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="InstallLocalAiPythonPackageProfileFunction"/>, keeping the operation consistent with the state and invariants of the surrounding install local AI python package profile function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiPythonPackageInstallRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            await runtime.InstallPackageProfileAsync(binding.Value.ProfileKey, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            return json.Success(new { Installed = true, ProfileKey = binding.Value.ProfileKey });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError("Installing a managed Python package profile failed with {ExceptionType}; package output and paths were omitted.", exception.GetType().Name);
            return new() { Succeeded = false, Status = "Failed", Error = "Python package-profile installation failed. Review LocalGPT logs." };
        }
    }
}
