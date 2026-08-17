using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council team configuration behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilTeamConfigurationService
    {
    /// <summary>Normalizes and validates one workflow execution-mode string.</summary>
    /// <param name="value">Requested execution mode.</param>
    /// <returns>The canonical supported execution mode.</returns>
    private string NormalizeExecutionMode(string? value)
    {
    try
    {
            var candidate = string.IsNullOrWhiteSpace(value) ? "AllMembersSequentialOnEachAIHostParallel" : value.Trim();
            if (candidate.Equals("AllMembers", StringComparison.OrdinalIgnoreCase) || candidate.Equals("Parallel", StringComparison.OrdinalIgnoreCase))
                candidate = "AllMembersParallel";
            else if (candidate.Equals("SequentialPerHost", StringComparison.OrdinalIgnoreCase) || candidate.Equals("HostSequential", StringComparison.OrdinalIgnoreCase))
                candidate = "AllMembersSequentialOnEachAIHostParallel";
            else if (candidate.Equals("Sequential", StringComparison.OrdinalIgnoreCase))
                candidate = "AllMembersSequential";
            else if (candidate.Equals("Single", StringComparison.OrdinalIgnoreCase))
                candidate = "LeaderSingle";

            var normalized = SupportedExecutionModes.FirstOrDefault(mode => mode.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            return normalized ?? throw new InvalidOperationException(
                $"Execution mode '{candidate}' is not supported. Use {string.Join(", ", SupportedExecutionModes.OrderBy(mode => mode, StringComparer.OrdinalIgnoreCase))}.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(NormalizeExecutionMode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(NormalizeExecutionMode)} failed.");
        throw;
    }
}

    /// <summary>Normalizes user-edited registered-function names without applying a hidden runtime allow-list.</summary>
    /// <param name="values">Function names persisted by the user-edited team or workflow configuration.</param>
    /// <returns>A trimmed, case-insensitively distinct and deterministically ordered list.</returns>
    private List<string> NormalizeFunctionNames(IEnumerable<string>? values)
    {
        try
        {
            return (values ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing Council automatic-function names failed.");
            throw;
        }
    }

    /// <summary>Converts legacy saved tool fields into one explicit persisted automatic-function policy mode.</summary>
    /// <param name="step">Persisted workflow step whose legacy fields are being normalized.</param>
    /// <returns>The explicit policy mode that preserves the saved step's intended function exposure.</returns>
    private CouncilAutomaticFunctionPolicyMode NormalizeAutomaticFunctionPolicy(CouncilWorkflowStepDefinition step)
    {
        try
        {
            if (!step.CanUseOrganicFunctions)
                return CouncilAutomaticFunctionPolicyMode.Disabled;
            if (step.AutomaticFunctionPolicyMode != CouncilAutomaticFunctionPolicyMode.Legacy)
                return step.AutomaticFunctionPolicyMode;
            return step.AllowedAutomaticFunctions is { Count: > 0 }
                ? CouncilAutomaticFunctionPolicyMode.ExactAllowList
                : CouncilAutomaticFunctionPolicyMode.AllPolicyApproved;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing Council automatic-function policy failed for step {StepKey}.", step?.Key);
            throw;
        }
    }

    /// <summary>Creates a unique user-owned key derived from a supplied system-seed key.</summary>
    /// <param name="seedKey">Stable supplied seed key being preserved.</param>
    /// <param name="existingKeys">Keys already present in the configuration store.</param>
    /// <returns>A normalized key that does not collide with an existing team.</returns>
    private string CreateUniqueUserCopyKey(string seedKey, IReadOnlyCollection<string> existingKeys)
    {
        try
        {
            var baseKey = $"{seedKey.Trim().ToLowerInvariant()}-custom";
            var existing = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
            if (!existing.Contains(baseKey))
                return baseKey;
            for (var suffix = 2; suffix <= 10000; suffix++)
            {
                var candidate = $"{baseKey}-{suffix}";
                if (!existing.Contains(candidate))
                    return candidate;
            }
            throw new InvalidOperationException($"Could not allocate a unique user-owned Council team key for supplied seed '{seedKey}'.");
        }
        catch (Exception __serviceMethodException)
        {
            logger.LogError(__serviceMethodException, "Allocating a user-owned Council team key for seed {SeedKey} failed.", seedKey);
            throw;
        }
    }

    /// <summary>Clones a supplied or edited definition as an explicit user-owned literal workflow.</summary>
    /// <param name="source">Definition whose content should be preserved.</param>
    /// <param name="customKey">Unique custom key allocated for the user-owned copy.</param>
    /// <returns>A deep-cloned user-owned team definition.</returns>
    private OrganicCouncilTeamDefinition CloneAsUserOwnedDefinition(OrganicCouncilTeamDefinition source, string customKey)
    {
        try
        {
            var json = JsonSerializer.Serialize(source, JsonOptions);
            var clone = JsonSerializer.Deserialize<OrganicCouncilTeamDefinition>(json, JsonOptions)
                ?? throw new InvalidOperationException("Council team cloning returned no definition.");
            clone.Key = customKey;
            if (!clone.DisplayName.Contains("custom", StringComparison.OrdinalIgnoreCase))
                clone.DisplayName = $"{clone.DisplayName} custom";
            clone.IsSystemSeed = false;
            clone.IsUserModified = true;
            foreach (var step in clone.WorkflowSteps)
                step.UseBuiltInBehavior = false;
            return clone;
        }
        catch (Exception __serviceMethodException)
        {
            logger.LogError(__serviceMethodException, "Cloning supplied Council team {TeamKey} into user-owned configuration {CustomKey} failed.", source.Key, customKey);
            throw;
        }
    }

    /// <summary>Copies a normalized definition into its persistence row.</summary>
    /// <param name="row">Target persistence row.</param>
    /// <param name="definition">Normalized source definition.</param>
    private void ApplyDefinition(CouncilTeamConfiguration row, OrganicCouncilTeamDefinition definition)
    {
    try
    {
            row.Key = definition.Key.Trim().ToLowerInvariant();
            row.DisplayName = definition.DisplayName.Trim();
            row.Purpose = definition.Purpose.Trim();
            row.RolesJson = Serialize(definition.Roles);
            row.PreferredCapabilitiesJson = Serialize(definition.PreferredCapabilities);
            row.AllowedAutomaticFunctionsJson = Serialize(definition.AllowedAutomaticFunctions);
            row.ArchitectureContractsJson = Serialize(definition.ArchitectureContracts);
            row.WorkflowStepsJson = Serialize(definition.WorkflowSteps);
            row.ExpertPreparationPromptTemplate = definition.ExpertPreparationPromptTemplate;
            row.LeaderSynthesisPromptTemplate = definition.LeaderSynthesisPromptTemplate;
            row.MainRoundInstructionTemplate = definition.MainRoundInstructionTemplate;
            row.AllMembersReadinessPreflightMode = definition.AllMembersReadinessPreflightMode;
            row.IncludeAllMembersReadinessPreflightInWorkflowContext = definition.IncludeAllMembersReadinessPreflightInWorkflowContext;
            row.AllMembersReadinessPreflightMaxOutputTokens = definition.AllMembersReadinessPreflightMaxOutputTokens;
            row.AllMembersReadinessPreflightPromptTemplate = definition.AllMembersReadinessPreflightPromptTemplate ?? string.Empty;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ApplyDefinition)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ApplyDefinition)} failed.");
        throw;
    }
}

    /// <summary>Converts one persistence row into a runtime team definition.</summary>
    /// <param name="row">Source persistence row.</param>
    /// <returns>The runtime definition.</returns>
    private OrganicCouncilTeamDefinition ToDefinition(CouncilTeamConfiguration row) {
    try
    {
        return new()
    {
        Key = row.Key,
        DisplayName = row.DisplayName,
        Purpose = row.Purpose,
        Roles = Deserialize<List<OrganicCouncilRoleDefinition>>(row.RolesJson) ?? [],
        PreferredCapabilities = Deserialize<List<string>>(row.PreferredCapabilitiesJson) ?? [],
        AllowedAutomaticFunctions = Deserialize<List<string>>(row.AllowedAutomaticFunctionsJson) ?? [],
        ArchitectureContracts = Deserialize<List<string>>(row.ArchitectureContractsJson) ?? [],
        WorkflowSteps = Deserialize<List<CouncilWorkflowStepDefinition>>(row.WorkflowStepsJson) ?? [],
        ExpertPreparationPromptTemplate = row.ExpertPreparationPromptTemplate,
        LeaderSynthesisPromptTemplate = row.LeaderSynthesisPromptTemplate,
        MainRoundInstructionTemplate = row.MainRoundInstructionTemplate,
        AllMembersReadinessPreflightMode = row.AllMembersReadinessPreflightMode,
        IncludeAllMembersReadinessPreflightInWorkflowContext = row.IncludeAllMembersReadinessPreflightInWorkflowContext,
        AllMembersReadinessPreflightMaxOutputTokens = row.AllMembersReadinessPreflightMaxOutputTokens,
        AllMembersReadinessPreflightPromptTemplate = row.AllMembersReadinessPreflightPromptTemplate,
        IsEnabled = row.IsEnabled,
        IsDeleted = row.IsDeleted,
        IsSystemSeed = row.IsSystemSeed,
        IsUserModified = row.IsUserModified
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ToDefinition)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ToDefinition)} failed.");
        throw;
    }
}

    /// <summary>Serializes one bounded configuration value with the maintained web defaults.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">Value to serialize.</param>
    /// <returns>JSON text.</returns>
    private string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
    /// <summary>Deserializes one optional configuration value.</summary>
    /// <typeparam name="T">Requested result type.</typeparam>
    /// <param name="json">Stored JSON text.</param>
    /// <returns>The deserialized value, or default when the payload is blank or invalid.</returns>
    private T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    }
}
