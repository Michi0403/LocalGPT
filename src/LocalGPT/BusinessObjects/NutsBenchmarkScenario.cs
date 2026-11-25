using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>A user-authored role assessment across provider-qualified models.</summary>
public sealed class NutsBenchmarkScenario
{
    /// <summary>The scenario name retained with its benchmark report.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Ordered cases used for every selected model and profile.</summary>
    public List<NutsBenchmarkCase> Cases { get; init; } = [];
}

/// <summary>One role exercise with an independently supplied answer key.</summary>
public sealed class NutsBenchmarkCase
{
    /// <summary>A unique label within this scenario.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The configured Council team key.</summary>
    public string TeamKey { get; init; } = string.Empty;

    /// <summary>The configured role name within the team.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>The exercise and requested JSON response shape.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Expected JSON properties and values, kept out of the exercise prompt.</summary>
    public Dictionary<string, JsonElement> Expected { get; init; } = [];
}

/// <summary>Observed case results for one model, profile and team role.</summary>
public sealed record NutsRoleEvidence(
    string ModelSelectionKey,
    string ModelDisplayName,
    string ProfileName,
    string TeamKey,
    string Role,
    int Passed,
    IReadOnlyList<string> CapabilityGaps,
    IReadOnlyList<string> UnmeasuredCases);
