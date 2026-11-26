using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Adds measured NUTS recommendations to a Council preset through the Human Collaboration Inbox.</summary>
/// <param name="benchmarks">Durable benchmark evidence store.</param>
/// <param name="assessment">NUTS recommendation and role assignment service.</param>
/// <param name="json">DXAI function payload binding and results.</param>
public sealed class NutsRoleAssignmentFunction(
    IProviderModelBenchmarkService benchmarks, NutsBenchmarkAssessment assessment, IDxAiFunctionJsonService json)
    : IDxAiFunctionHandler
{
    /// <summary>Identifies the approval operation.</summary>
    internal const string FunctionName = "nuts.preset.roles.add-models";

    /// <summary>Describes the approval shown for the requested preset, roles, and models.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        FunctionName, "POST", "/api/dxai/functions/" + FunctionName + "/invoke",
        "Add the benchmark-recommended models below to the named Council preset roles.",
        "Supply the completed benchmark ID, preset, and a map from role name to exact provider-qualified model.",
        "Existing assignments, role selection policies and learned knowledge are preserved. A supplied preset creates a selectable user-owned copy.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, SupportsDeferredApprovalRequest: true,
        Source: "DIHandler", ParameterSchemaJson: """
        {"type":"object","required":["benchmarkId","preset","values"],"properties":{"benchmarkId":{"type":"string"},"preset":{"type":"string","minLength":1},"values":{"type":"object","minProperties":1,"additionalProperties":{"type":"string","minLength":1}}},"additionalProperties":false}
        """);

    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.UserConfirmed)
            return json.InvalidParameters("This change requires approval in the Human Collaboration Inbox.");
        var binding = json.Bind<NutsRoleAssignmentParameters>(request.Parameters);
        if (!binding.Succeeded || string.IsNullOrWhiteSpace(binding.Value.Preset) || binding.Value.Values is null)
            return json.InvalidParameters(binding.Succeeded ? "Choose a Council preset." : binding.Error);

        var report = await benchmarks.LoadStoredEvidenceAsync(binding.Value.BenchmarkId, cancellationToken).ConfigureAwait(false);
        if (report is null)
            return json.InvalidParameters("The completed benchmark evidence could not be loaded.");
        try
        {
            var savedPreset = await assessment.AddApprovedModelsAsync(report, binding.Value.Preset, binding.Value.Values, cancellationToken).ConfigureAwait(false);
            return json.Success(new { Preset = savedPreset, Message = $"Approved model additions saved to {savedPreset}. Select that preset in Chat." });
        }
        catch (InvalidOperationException exception)
        {
            return json.InvalidParameters(exception.Message);
        }
    }
}

/// <summary>Stores the exact measured model additions submitted for one preset approval.</summary>
public sealed class NutsRoleAssignmentParameters
{
    /// <summary>Identifies the completed benchmark supporting the assignments.</summary>
    public Guid BenchmarkId { get; set; }
    /// <summary>Identifies the Council preset to update or copy.</summary>
    public string Preset { get; set; } = string.Empty;
    /// <summary>Maps Council role names to exact provider-qualified model identities.</summary>
    public Dictionary<string, string> Values { get; set; } = [];
}
