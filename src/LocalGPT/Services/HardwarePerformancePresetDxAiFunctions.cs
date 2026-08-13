using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Owns the shared read-model and identity-resolution behavior used by hardware-performance DXFunctions so
/// handlers remain thin and the service-layer diagnostics policy is preserved.
/// </summary>
/// <param name="presets">Performance-profile persistence service.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
public sealed class HardwarePerformancePresetDxAiSupport(
    IHardwarePerformancePresetService presets,
    ILogger<HardwarePerformancePresetDxAiSupport> logger)
{
    /// <summary>Builds a Council-facing profile view with parsed provider-qualified routes.</summary>
    /// <param name="preset">Stored profile to project.</param>
    /// <returns>A bounded anonymous read model suitable for DXFunction results.</returns>
    public object BuildView(HardwarePerformancePreset preset)
    {
        try
        {
            List<OneWireCouncilModelRoute> routes;
            try
            {
                routes = JsonSerializer.Deserialize<List<OneWireCouncilModelRoute>>(preset.ModelRoutesJson) ?? [];
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Hardware performance preset {PresetId} contains unreadable route JSON; returning an empty route list to the DXFunction caller.", preset.Id);
                routes = [];
            }

            return new
            {
                preset.Id,
                preset.Name,
                preset.Description,
                preset.ResourceLoadPercent,
                preset.SourceRunId,
                preset.SourceKind,
                preset.IsDefault,
                preset.IsArchived,
                preset.IsUserApproved,
                preset.CreatedAtUtc,
                preset.UpdatedAtUtc,
                ModelRoutes = routes
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a hardware performance preset DXFunction view failed for preset {PresetId}.", preset.Id);
            throw;
        }
    }

    /// <summary>Resolves one exact profile by stable identifier first, then by case-insensitive user-visible name.</summary>
    /// <param name="presetId">Optional stable preset identifier.</param>
    /// <param name="name">Optional exact user-visible preset name.</param>
    /// <param name="cancellationToken">Cancellation token for database access.</param>
    /// <returns>The matching profile, or <see langword="null"/> when no exact identity exists.</returns>
    public async Task<HardwarePerformancePreset?> ResolveAsync(
        Guid? presetId,
        string? name,
        CancellationToken cancellationToken)
    {
        try
        {
            if (presetId is Guid id && id != Guid.Empty)
                return await presets.GetPresetAsync(id, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var rows = await presets.GetPresetsAsync(includeArchived: true, cancellationToken).ConfigureAwait(false);
            return rows.FirstOrDefault(item => item.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Resolving hardware performance preset DXFunction identity was cancelled.");
            else
                logger.LogError(exception, "Resolving hardware performance preset DXFunction identity failed.");
            throw;
        }
    }
}

/// <summary>Lets AI Councils discover the durable hardware-spooler performance profiles exposed to users in Chat configuration.</summary>
/// <param name="presets">Performance-profile service that owns persistence and normalization.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
/// <param name="support">Support value supplied to the list hardware performance presets function operation and used when producing its result.</param>
public sealed class ListHardwarePerformancePresetsFunction(
    IHardwarePerformancePresetService presets,
    HardwarePerformancePresetDxAiSupport support,
    ILogger<ListHardwarePerformancePresetsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the preseeded function descriptor synchronized into the DXFunction catalog at startup.</summary>
    /// <value>The descriptor value exposed by <see cref="ListHardwarePerformancePresetsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.hardware.performance.presets.list",
        "POST",
        "/api/dxai/functions/localgpt.hardware.performance.presets.list/invoke",
        "Lists durable Hardware spooler performance profiles created manually or by approved provider benchmarks. The profiles are independent from Council membership presets.",
        "Optional includeArchived boolean. Returned routes keep exact provider/endpoint/model identity and their min/max context, min/max output, GPU road, load override and lane concurrency.",
        "Read-only. It does not benchmark models or change current/prepared Council settings.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "HardwarePerformancePresetDxAiFunctions",
        ParameterSchemaJson: """
        {
          "type":"object",
          "properties":{"includeArchived":{"type":"boolean"}},
          "additionalProperties":false
        }
        """);

    /// <summary>Returns the current performance-profile catalog in a Council-friendly shape.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var includeArchived = request.Parameters.ValueKind == JsonValueKind.Object
                && request.Parameters.TryGetProperty("includeArchived", out var includeArchivedElement)
                && includeArchivedElement.ValueKind == JsonValueKind.True;
            var rows = await presets.GetPresetsAsync(includeArchived, cancellationToken).ConfigureAwait(false);
            var value = rows.Select(support.BuildView).ToList();
            logger.LogInformation("Listed {PresetCount} hardware performance preset(s) for a DXFunction caller.", value.Count);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = value };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing hardware performance presets through DXFunction failed.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "Hardware performance presets could not be listed. Review LocalGPT logs."
            };
        }
    }


}

/// <summary>Lets AI Councils inspect one exact durable hardware-spooler performance profile by identifier or name.</summary>
/// <param name="presets">Performance-profile service that owns persistence and normalization.</param>
/// <param name="json">DXFunction JSON binder.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
/// <param name="support">Support value supplied to the get hardware performance preset function operation and used when producing its result.</param>
public sealed class GetHardwarePerformancePresetFunction(
    HardwarePerformancePresetDxAiSupport support,
    IDxAiFunctionJsonService json,
    ILogger<GetHardwarePerformancePresetFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the preseeded function descriptor synchronized into the DXFunction catalog at startup.</summary>
    /// <value>The descriptor value exposed by <see cref="GetHardwarePerformancePresetFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.hardware.performance.presets.get",
        "POST",
        "/api/dxai/functions/localgpt.hardware.performance.presets.get/invoke",
        "Reads one durable Hardware spooler performance profile by presetId or exact name, including provider-qualified token and hardware roads.",
        "Provide either presetId or name. Use the list function first when the exact identity is unknown.",
        "Read-only. It never applies or edits the profile.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "HardwarePerformancePresetDxAiFunctions",
        ParameterSchemaJson: """
        {
          "type":"object",
          "properties":{
            "presetId":{"type":"string","format":"uuid"},
            "name":{"type":"string","minLength":1,"maxLength":160}
          },
          "additionalProperties":false
        }
        """);

    /// <summary>Reads one exact profile without changing application state.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<PresetIdentityParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var preset = await support.ResolveAsync(binding.Value.PresetId, binding.Value.Name, cancellationToken).ConfigureAwait(false);
            if (preset is null)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "NotFound",
                    Error = "The requested hardware performance preset was not found."
                };
            }

            return json.Success(support.BuildView(preset));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a hardware performance preset through DXFunction failed.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "The hardware performance preset could not be read. Review LocalGPT logs."
            };
        }
    }

    /// <summary>Input contract shared by the preset read/delete/apply DXFunctions.</summary>
    internal sealed class PresetIdentityParameters
    {
        /// <summary>Initializes an empty preset identity contract for JSON binding.</summary>
        public PresetIdentityParameters() { }

        /// <summary>
        /// Gets or sets the stable preset identifier used to identify or correlate this preset identity parameters instance with related application state.
        /// </summary>
        /// <value>The preset identifier value exposed by <see cref="PresetIdentityParameters"/>.</value>
        public Guid? PresetId { get; set; }
        /// <summary>
        /// Gets or sets the name value that forms part of the preset identity parameters state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="PresetIdentityParameters"/>.</value>
        public string Name { get; set; } = string.Empty;
    }
}

/// <summary>Lets an AI Council create or update a user-visible hardware performance profile after explicit approval.</summary>
/// <param name="presets">Performance-profile service that owns persistence and normalization.</param>
/// <param name="json">DXFunction JSON binder.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
/// <param name="support">Support value supplied to the save hardware performance preset function operation and used when producing its result.</param>
public sealed class SaveHardwarePerformancePresetFunction(
    IHardwarePerformancePresetService presets,
    HardwarePerformancePresetDxAiSupport support,
    IDxAiFunctionJsonService json,
    ILogger<SaveHardwarePerformancePresetFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the preseeded function descriptor synchronized into the DXFunction catalog at startup.</summary>
    /// <value>The descriptor value exposed by <see cref="SaveHardwarePerformancePresetFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.hardware.performance.presets.save",
        "POST",
        "/api/dxai/functions/localgpt.hardware.performance.presets.save/invoke",
        "Creates or updates a durable Hardware spooler performance profile containing provider-qualified CPU/GPU roads and token ranges. Use this only for reviewed profile synthesis; normal provider benchmarks already persist their measured result automatically.",
        "Required: name and modelRoutes. Optional: presetId to update an exact profile, description, resourceLoadPercent and isDefault. Each model route must use the exact provider-qualified ModelName/selection key.",
        "Requires fresh human approval because it persists AI performance configuration. It never changes Council membership, downloads models, or modifies provider-global settings.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "HardwarePerformancePresetDxAiFunctions",
        ParameterSchemaJson: """
        {
          "type":"object",
          "required":["name","modelRoutes"],
          "properties":{
            "presetId":{"type":"string","format":"uuid"},
            "name":{"type":"string","minLength":1,"maxLength":160},
            "description":{"type":"string","maxLength":1000},
            "resourceLoadPercent":{"type":"integer","minimum":0,"maximum":100},
            "isDefault":{"type":"boolean"},
            "modelRoutes":{"type":"array","minItems":1,"items":{"type":"object"}}
          },
          "additionalProperties":false
        }
        """,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>Persists a Council-synthesized profile only after the human approval gate has completed.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!request.UserConfirmed)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "HumanConfirmationRequired",
                    Error = "Fresh human confirmation is required before saving a hardware performance preset."
                };
            }

            var binding = json.Bind<SavePresetParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            if (string.IsNullOrWhiteSpace(parameters.Name))
                return json.InvalidParameters("Parameter 'name' is required.");
            if (parameters.ModelRoutes.Count == 0)
                return json.InvalidParameters("Parameter 'modelRoutes' must contain at least one provider-qualified route.");
            if (parameters.ModelRoutes.Any(route => string.IsNullOrWhiteSpace(route.ModelName)))
                return json.InvalidParameters("Every modelRoutes item must contain the exact provider-qualified modelName/selection key.");

            var preset = new HardwarePerformancePreset
            {
                Id = parameters.PresetId ?? Guid.NewGuid(),
                Name = parameters.Name,
                Description = parameters.Description,
                ModelRoutesJson = JsonSerializer.Serialize(parameters.ModelRoutes, json.Options),
                ResourceLoadPercent = parameters.ResourceLoadPercent,
                SourceKind = "Council",
                IsDefault = parameters.IsDefault,
                IsUserApproved = true
            };
            var saved = await presets.SavePresetAsync(preset, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Council DXFunction saved hardware performance preset {PresetId} with {RouteCount} route(s).", saved.Id, parameters.ModelRoutes.Count);
            return json.Success(support.BuildView(saved));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving a hardware performance preset through DXFunction failed.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "The hardware performance preset could not be saved. Review LocalGPT logs."
            };
        }
    }

    /// <summary>Input contract for Council-owned hardware performance profile persistence.</summary>
    private sealed class SavePresetParameters
    {
        /// <summary>Initializes an empty save contract for JSON binding.</summary>
        public SavePresetParameters() { }

        /// <summary>Gets or sets the optional stable identifier of an existing profile to update.</summary>
        /// <value>The preset identifier value exposed by <see cref="SavePresetParameters"/>.</value>
        public Guid? PresetId { get; set; }
        /// <summary>
        /// Gets or sets the name value that forms part of the save preset parameters state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="SavePresetParameters"/>.</value>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description value that forms part of the save preset parameters state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The description value exposed by <see cref="SavePresetParameters"/>.</value>
        public string Description { get; set; } = string.Empty;
        /// <summary>Gets or sets the session-wide load percentage used when routes do not override it.</summary>
        /// <value>The resource load percent value exposed by <see cref="SavePresetParameters"/>.</value>
        public int ResourceLoadPercent { get; set; } = 100;
        /// <summary>Gets or sets whether the saved profile becomes the preferred default.</summary>
        /// <value>The is default value exposed by <see cref="SavePresetParameters"/>.</value>
        public bool IsDefault { get; set; }
        /// <summary>Gets or sets the exact provider-qualified hardware/token roads to persist.</summary>
        /// <value>The model routes value exposed by <see cref="SavePresetParameters"/>.</value>
        public List<OneWireCouncilModelRoute> ModelRoutes { get; set; } = [];
    }
}

/// <summary>Lets an AI Council delete one stored performance profile after explicit user approval.</summary>
/// <param name="presets">Performance-profile service that owns persistence and normalization.</param>
/// <param name="json">DXFunction JSON binder.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
/// <param name="support">Support value supplied to the delete hardware performance preset function operation and used when producing its result.</param>
public sealed class DeleteHardwarePerformancePresetFunction(
    IHardwarePerformancePresetService presets,
    HardwarePerformancePresetDxAiSupport support,
    IDxAiFunctionJsonService json,
    ILogger<DeleteHardwarePerformancePresetFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the preseeded function descriptor synchronized into the DXFunction catalog at startup.</summary>
    /// <value>The descriptor value exposed by <see cref="DeleteHardwarePerformancePresetFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.hardware.performance.presets.delete",
        "POST",
        "/api/dxai/functions/localgpt.hardware.performance.presets.delete/invoke",
        "Deletes one durable Hardware spooler performance profile by presetId or exact name.",
        "Provide presetId or exact name. List profiles first when identity is uncertain.",
        "Requires fresh human approval. Deleting a stored profile does not reset hardware roads already copied into a prepared or running Council.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "HardwarePerformancePresetDxAiFunctions",
        ParameterSchemaJson: """
        {
          "type":"object",
          "properties":{
            "presetId":{"type":"string","format":"uuid"},
            "name":{"type":"string","minLength":1,"maxLength":160}
          },
          "additionalProperties":false
        }
        """,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>Deletes the resolved profile only after the human approval gate has completed.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!request.UserConfirmed)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "HumanConfirmationRequired",
                    Error = "Fresh human confirmation is required before deleting a hardware performance preset."
                };
            }

            var binding = json.Bind<GetHardwarePerformancePresetFunction.PresetIdentityParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var preset = await support.ResolveAsync(binding.Value.PresetId, binding.Value.Name, cancellationToken).ConfigureAwait(false);
            if (preset is null)
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "NotFound", Error = "The requested hardware performance preset was not found." };

            await presets.DeletePresetAsync(preset.Id, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Council DXFunction deleted hardware performance preset {PresetId}.", preset.Id);
            return json.Success(new { preset.Id, preset.Name }, "Deleted");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deleting a hardware performance preset through DXFunction failed.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "The hardware performance preset could not be deleted. Review LocalGPT logs."
            };
        }
    }
}

/// <summary>Lets an approved AI Council apply a stored performance profile to matching prepared or running provider-qualified model roads.</summary>
/// <param name="presets">Performance-profile service that owns persistence and run/preparation application.</param>
/// <param name="json">DXFunction JSON binder.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
/// <param name="support">Support value supplied to the apply hardware performance preset function operation and used when producing its result.</param>
public sealed class ApplyHardwarePerformancePresetFunction(
    IHardwarePerformancePresetService presets,
    HardwarePerformancePresetDxAiSupport support,
    IDxAiFunctionJsonService json,
    ILogger<ApplyHardwarePerformancePresetFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the preseeded function descriptor synchronized into the DXFunction catalog at startup.</summary>
    /// <value>The descriptor value exposed by <see cref="ApplyHardwarePerformancePresetFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.hardware.performance.presets.apply",
        "POST",
        "/api/dxai/functions/localgpt.hardware.performance.presets.apply/invoke",
        "Applies one stored Hardware spooler performance profile to matching provider-qualified routes without changing Council membership. With runId it updates that running Council; without runId it updates the saved preparation for the next run.",
        "Provide presetId or exact name. Optional runId targets one running Council. Only routes whose exact provider-qualified modelName matches are changed; unrelated routes and all participant selection remain intact.",
        "Requires fresh human approval because it changes prepared or live Council hardware/token settings. It never downloads models or changes provider-global configuration.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "HardwarePerformancePresetDxAiFunctions",
        ParameterSchemaJson: """
        {
          "type":"object",
          "properties":{
            "presetId":{"type":"string","format":"uuid"},
            "name":{"type":"string","minLength":1,"maxLength":160},
            "runId":{"type":"string","format":"uuid"}
          },
          "additionalProperties":false
        }
        """,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>Applies the resolved profile after approval and reports how many exact provider-qualified routes changed.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!request.UserConfirmed)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "HumanConfirmationRequired",
                    Error = "Fresh human confirmation is required before applying a hardware performance preset."
                };
            }

            var binding = json.Bind<ApplyPresetParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var preset = await support.ResolveAsync(binding.Value.PresetId, binding.Value.Name, cancellationToken).ConfigureAwait(false);
            if (preset is null)
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "NotFound", Error = "The requested hardware performance preset was not found." };

            int appliedRoutes;
            string target;
            if (binding.Value.RunId is Guid runId && runId != Guid.Empty)
            {
                appliedRoutes = await presets.ApplyPresetToRunAsync(preset.Id, runId, userConfirmed: true, cancellationToken).ConfigureAwait(false);
                target = $"RunningCouncil:{runId:D}";
            }
            else
            {
                appliedRoutes = await presets.ApplyPresetToPreparationAsync(preset.Id, userConfirmed: true, cancellationToken).ConfigureAwait(false);
                target = "Preparation";
            }

            logger.LogInformation("Council DXFunction applied hardware performance preset {PresetId} to {AppliedRouteCount} route(s).", preset.Id, appliedRoutes);
            return json.Success(new { preset.Id, preset.Name, AppliedRoutes = appliedRoutes, Target = target }, "Applied");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (KeyNotFoundException exception)
        {
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "NotFound", Error = exception.Message };
        }
        catch (InvalidOperationException exception)
        {
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "InvalidState", Error = exception.Message };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying a hardware performance preset through DXFunction failed.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "The hardware performance preset could not be applied. Review LocalGPT logs."
            };
        }
    }

    /// <summary>Input contract for applying a stored profile to preparation or one running Council.</summary>
    private sealed class ApplyPresetParameters
    {
        /// <summary>Initializes an empty apply contract for JSON binding.</summary>
        public ApplyPresetParameters() { }

        /// <summary>
        /// Gets or sets the stable preset identifier used to identify or correlate this apply preset parameters instance with related application state.
        /// </summary>
        /// <value>The preset identifier value exposed by <see cref="ApplyPresetParameters"/>.</value>
        public Guid? PresetId { get; set; }
        /// <summary>
        /// Gets or sets the name value that forms part of the apply preset parameters state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="ApplyPresetParameters"/>.</value>
        public string Name { get; set; } = string.Empty;
        /// <summary>Gets or sets the optional running Council identifier; null targets preparation.</summary>
        /// <value>The run identifier value exposed by <see cref="ApplyPresetParameters"/>.</value>
        public Guid? RunId { get; set; }
    }
}
