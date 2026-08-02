using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Describes one strongly typed database-backed system-variable key and its seed default.
/// </summary>
/// <typeparam name="T">Value type stored by the variable.</typeparam>
/// <param name="Name">Stable database key.</param>
/// <param name="DefaultValue">Value used only when no durable user value exists.</param>
public sealed record SystemVariableDefinition<T>(string Name, T DefaultValue);

/// <summary>
/// Owns maintained LocalGPT system-variable keys so services never depend on ad-hoc string literals.
/// </summary>
[DocumentationUpdated("2.1.20")]
public interface ISystemVariableDefinitionService
{
    /// <summary>Gets the default maximum output-token definition.</summary>
    SystemVariableDefinition<int> DefaultMaxOutputTokens { get; }
    /// <summary>Gets the default maximum prompt-character definition.</summary>
    SystemVariableDefinition<int> DefaultMaxPromptCharacters { get; }
    /// <summary>Gets the maximum bootstrap-character definition.</summary>
    SystemVariableDefinition<int> MaxBootstrapCharacters { get; }
    /// <summary>Gets the default maximum parallel-model definition.</summary>
    SystemVariableDefinition<int> DefaultMaxParallelModels { get; }
    /// <summary>Gets the legacy heavy-model GPU-layer default.</summary>
    SystemVariableDefinition<int> DefaultHeavyModelGpuLayers { get; }
    /// <summary>Gets the default Council resource-load percentage definition.</summary>
    SystemVariableDefinition<int> DefaultCouncilResourceLoadPercent { get; }
    /// <summary>Gets the default Council critique-round count definition.</summary>
    SystemVariableDefinition<int> DefaultCouncilCritiqueRounds { get; }
    /// <summary>Gets the minimum context-token definition.</summary>
    SystemVariableDefinition<int> MinContextTokens { get; }
    /// <summary>Gets the default context-token definition.</summary>
    SystemVariableDefinition<int> DefaultContextTokens { get; }
    /// <summary>Gets the maximum context-token definition.</summary>
    SystemVariableDefinition<int> MaxContextTokens { get; }
    /// <summary>Gets the minimum output-token definition.</summary>
    SystemVariableDefinition<int> MinOutputTokens { get; }
    /// <summary>Gets the maximum output-token definition.</summary>
    SystemVariableDefinition<int> MaxOutputTokens { get; }
    /// <summary>Gets the default loopback Ollama endpoint definition.</summary>
    SystemVariableDefinition<string> DefaultOllamaEndpoint { get; }
    /// <summary>Gets the provider-selection policy definition.</summary>
    SystemVariableDefinition<string> ProviderSelectionPolicy { get; }
    /// <summary>Gets the repository knowledge seed-version definition.</summary>
    SystemVariableDefinition<int> RepositoryKnowledgeSeedVersion { get; }
    /// <summary>Gets the Council defaults migration-version definition.</summary>
    SystemVariableDefinition<int> CouncilDefaultsVersion { get; }
    /// <summary>Gets the regular-expression timeout definition.</summary>
    SystemVariableDefinition<int> RegexMatchTimeoutMilliseconds { get; }
    /// <summary>Gets the maximum DXFunction calls per Council step definition.</summary>
    SystemVariableDefinition<int> CouncilDxMaximumCallsPerStep { get; }
    /// <summary>Gets the maximum DXFunction parameter-character definition.</summary>
    SystemVariableDefinition<int> CouncilDxMaximumParameterCharacters { get; }
    /// <summary>Gets the maximum DXFunction result-character definition.</summary>
    SystemVariableDefinition<int> CouncilDxMaximumResultCharacters { get; }
    /// <summary>Gets the persisted first-run onboarding completion flag definition.</summary>
    SystemVariableDefinition<bool> FirstRunOnboardingCompleted { get; }
    /// <summary>Gets the former hard-coded Council resource-load percentage for migration diagnostics.</summary>
    int LegacyCouncilResourceLoadPercent { get; }
    /// <summary>Gets all maintained initial variables in deterministic seed order.</summary>
    IReadOnlyList<InitialVariable> InitialValues { get; }
}
