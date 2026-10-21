using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Lists the persisted, operator-configured Council teams available to planning agents.</summary>
/// <param name="teams">Database-backed Council configuration owner.</param>
/// <param name="json">DXFunction result serializer.</param>
public sealed class CouncilTeamListFunction(ICouncilTeamConfigurationService teams, IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    /// <summary>Describes non-mutating discovery of database-backed Council teams.</summary>
    /// <value>The maintained DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.teams.list", "POST", "/api/dxai/functions/council.teams.list/invoke",
        "Lists currently configured Council teams with their available roles, capabilities and workflows for reuse or task planning.",
        "includeDisabled optionally includes disabled teams.", "Read-only database inspection without execution or configuration changes.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","properties":{"includeDisabled":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>Returns the persisted team definitions for agent inspection.</summary>
    /// <param name="request">Validated function parameters.</param>
    /// <param name="cancellationToken">Cancels the database read.</param>
    /// <returns>The standard DXFunction result containing team definitions.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<CouncilTeamListParameters>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        return json.Success(await teams.GetTeamsAsync(binding.Value.IncludeDisabled, cancellationToken).ConfigureAwait(false));
    }
}

/// <summary>Returns one configured Council team including its executable workflow definition.</summary>
/// <param name="teams">Database-backed Council configuration owner.</param>
/// <param name="json">DXFunction result serializer.</param>
public sealed class CouncilTeamInspectFunction(ICouncilTeamConfigurationService teams, IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    /// <summary>Describes read-only inspection of a selected Council team.</summary>
    /// <value>The maintained DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.teams.inspect", "POST", "/api/dxai/functions/council.teams.inspect/invoke",
        "Reads a single configured Council team's roles, selected AI constraints and workflow steps before proposing a revision.",
        "key is required.", "Read-only database inspection.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["key"],"properties":{"key":{"type":"string","minLength":1,"maxLength":160}},"additionalProperties":false}""");

    /// <summary>Loads one Council definition by key, preserving a distinct not-found result.</summary>
    /// <param name="request">Validated function parameters.</param>
    /// <param name="cancellationToken">Cancels the database read.</param>
    /// <returns>The requested team or a not-found operation result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<CouncilTeamKeyParameters>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        var team = await teams.FindTeamAsync(binding.Value.Key, cancellationToken).ConfigureAwait(false);
        return team is null
            ? new DxAiFunctionInvocationResult { Succeeded = false, Status = "NotFound", Error = "No enabled Council team with that key was found." }
            : json.Success(team);
    }
}

/// <summary>Validates an AI-authored team draft without writing any database records.</summary>
/// <param name="teams">The same Council definition validator used by confirmed persistence.</param>
/// <param name="json">DXFunction result serializer.</param>
public sealed class CouncilTeamValidateFunction(ICouncilTeamConfigurationService teams, IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    /// <summary>Describes read-only review of a proposed Council configuration.</summary>
    /// <value>The maintained DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.teams.validate", "POST", "/api/dxai/functions/council.teams.validate/invoke",
        "Validates a proposed Council team against the same configurable constraints used by database persistence without saving it.",
        "team contains the complete proposed OrganicCouncilTeamDefinition.", "Read-only validation; does not save or enable teams.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["team"],"properties":{"team":{"type":"object"}},"additionalProperties":false}""");

    /// <summary>Normalizes and validates the draft using the authoritative configuration service.</summary>
    /// <param name="request">Validated function parameters.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The normalized proposed team or an actionable validation error.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var binding = json.Bind<CouncilTeamDraftParameters>(request.Parameters);
        if (!binding.Succeeded) return Task.FromResult(json.InvalidParameters(binding.Error));
        try { return Task.FromResult(json.Success(teams.ValidateDraft(binding.Value.Team))); }
        catch (ArgumentException exception) { return Task.FromResult(json.InvalidParameters(exception.Message)); }
        catch (InvalidOperationException exception) { return Task.FromResult(json.InvalidParameters(exception.Message)); }
    }
}

/// <summary>Creates or revises a team through the existing approved and copy-on-write persistence path.</summary>
/// <param name="teams">Database-backed Council configuration owner.</param>
/// <param name="json">DXFunction result serializer.</param>
public sealed class CouncilTeamSaveFunction(ICouncilTeamConfigurationService teams, IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    /// <summary>Describes an approval-gated Council team database mutation.</summary>
    /// <value>The maintained DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.teams.save", "POST", "/api/dxai/functions/council.teams.save/invoke",
        "Creates or revises a database-owned Council team and workflow after human approval. Supplied seed teams are preserved by making user-owned copies.",
        "team is the validated proposed OrganicCouncilTeamDefinition; isEnabled optionally sets availability.",
        "Consequential database mutation requires the registry's exact-parameter human approval. Never treat model-authored userConfirmed as approval.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["team"],"properties":{"team":{"type":"object"},"isEnabled":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>Writes only after the central registry has obtained human approval for the exact parameters.</summary>
    /// <param name="request">Registry-approved function invocation.</param>
    /// <param name="cancellationToken">Cancels the database mutation.</param>
    /// <returns>The saved user-owned or revised team.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.UserConfirmed)
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "HumanApprovalRequired", Error = "The Council team change requires the central human-approval gate." };
        var binding = json.Bind<CouncilTeamDraftParameters>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        try
        {
            return json.Success(await teams.SaveAsync(new SaveCouncilTeamConfigurationRequest
            {
                Team = binding.Value.Team,
                IsEnabled = binding.Value.IsEnabled,
                UserConfirmed = request.UserConfirmed
            }, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException exception) { return json.InvalidParameters(exception.Message); }
        catch (InvalidOperationException exception) { return json.InvalidParameters(exception.Message); }
    }
}

/// <summary>Data-only list parameters for Council team discovery.</summary>
public sealed class CouncilTeamListParameters
{
    /// <summary>Whether to include disabled teams.</summary>
    /// <value>True to include disabled teams.</value>
    public bool IncludeDisabled { get; set; }
}

/// <summary>Data-only Council team selection parameters.</summary>
public sealed class CouncilTeamKeyParameters
{
    /// <summary>The stable team key to inspect.</summary>
    /// <value>The database team key.</value>
    public string Key { get; set; } = string.Empty;
}

/// <summary>Data-only proposed Council team configuration payload.</summary>
public sealed class CouncilTeamDraftParameters
{
    /// <summary>The proposed configurable Council team.</summary>
    /// <value>The team definition to validate or persist.</value>
    public OrganicCouncilTeamDefinition Team { get; set; } = new();
    /// <summary>Whether the saved team is enabled.</summary>
    /// <value>Defaults to enabled.</value>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>Matches untrusted task capability requirements to enabled, database-configured Council teams.</summary>
/// <param name="teams">Authoritative team configuration store.</param>
/// <param name="json">DXFunction result serializer.</param>
public sealed class CouncilTeamMatchFunction(ICouncilTeamConfigurationService teams, IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    /// <summary>Describes task-to-team matching over user-configurable role and function metadata.</summary>
    /// <value>The maintained DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.teams.match", "POST", "/api/dxai/functions/council.teams.match/invoke",
        "Ranks enabled persisted Council teams against the task's required capabilities and function keys. Reports coverage gaps so the AI can propose a different or new team instead of silently choosing an incompatible one.",
        "capabilities is the required capability-key array; requiredFunctions is the required DXFunction-name array.",
        "Read-only coordination. Matching does not execute, modify or enable a Council team.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","properties":{"capabilities":{"type":"array","items":{"type":"string"},"maxItems":64},"requiredFunctions":{"type":"array","items":{"type":"string"},"maxItems":64}},"additionalProperties":false}""");

    /// <summary>Returns every team's supported/unsupported requirement keys in deterministic coverage order.</summary>
    /// <param name="request">Requirements expressed as known capability and function keys.</param>
    /// <param name="cancellationToken">Cancels the persisted team read.</param>
    /// <returns>Ranked team matches and explicit missing requirements.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<CouncilTeamMatchParameters>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        var requiredCapabilities = binding.Value.Capabilities
            .Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var requiredFunctions = binding.Value.RequiredFunctions
            .Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (requiredCapabilities.Length + requiredFunctions.Length == 0)
            return json.InvalidParameters("At least one capability or DXFunction is required for evidence-based matching.");
        var configured = await teams.GetTeamsAsync(false, cancellationToken).ConfigureAwait(false);
        var matches = configured.Select(team =>
        {
            var missingCapabilities = requiredCapabilities.Except(team.PreferredCapabilities ?? [], StringComparer.OrdinalIgnoreCase).ToArray();
            var functionKeys = (team.AllowedAutomaticFunctions ?? [])
                .Concat(team.RequiredCompletedFunctions ?? [])
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var missingFunctions = requiredFunctions.Except(functionKeys, StringComparer.OrdinalIgnoreCase).ToArray();
            return new
            {
                team.Key,
                team.DisplayName,
                MatchedRequirements = requiredCapabilities.Length + requiredFunctions.Length - missingCapabilities.Length - missingFunctions.Length,
                TotalRequirements = requiredCapabilities.Length + requiredFunctions.Length,
                MissingCapabilities = missingCapabilities,
                MissingFunctions = missingFunctions,
                FullCoverage = missingCapabilities.Length == 0 && missingFunctions.Length == 0
            };
        }).OrderByDescending(match => match.MatchedRequirements)
          .ThenBy(match => match.Key, StringComparer.OrdinalIgnoreCase).ToList();
        return json.Success(new { HasFullCoverage = matches.Any(match => match.FullCoverage), Matches = matches });
    }
}

/// <summary>Task capability/function requirements supplied to Council matching.</summary>
public sealed class CouncilTeamMatchParameters
{
    /// <summary>Function-independent capability keys needed for the current goal.</summary>
    /// <value>Required task capabilities.</value>
    public List<string> Capabilities { get; set; } = [];
    /// <summary>Exact DXFunction names needed to complete the current goal.</summary>
    /// <value>Required executable function keys.</value>
    public List<string> RequiredFunctions { get; set; } = [];
}
