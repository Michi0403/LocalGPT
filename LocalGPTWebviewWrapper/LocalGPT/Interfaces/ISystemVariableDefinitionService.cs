namespace LocalGPT.Interfaces;

public sealed record SystemVariableDefinition<T>(string Name, T DefaultValue);

public interface ISystemVariableDefinitionService
{
    SystemVariableDefinition<int> DefaultMaxOutputTokens { get; }
    SystemVariableDefinition<int> DefaultMaxPromptCharacters { get; }
    SystemVariableDefinition<int> MaxBootstrapCharacters { get; }
    SystemVariableDefinition<int> DefaultMaxParallelModels { get; }
    SystemVariableDefinition<int> DefaultHeavyModelGpuLayers { get; }
    SystemVariableDefinition<int> DefaultCouncilResourceLoadPercent { get; }
    SystemVariableDefinition<int> DefaultCouncilCritiqueRounds { get; }
    SystemVariableDefinition<int> MinContextTokens { get; }
    SystemVariableDefinition<int> DefaultContextTokens { get; }
    SystemVariableDefinition<int> MaxContextTokens { get; }
    SystemVariableDefinition<int> MinOutputTokens { get; }
    SystemVariableDefinition<int> MaxOutputTokens { get; }
    SystemVariableDefinition<string> DefaultOllamaEndpoint { get; }
    SystemVariableDefinition<string> ProviderSelectionPolicy { get; }
    SystemVariableDefinition<int> RepositoryKnowledgeSeedVersion { get; }
    SystemVariableDefinition<int> CouncilDefaultsVersion { get; }
    SystemVariableDefinition<int> RegexMatchTimeoutMilliseconds { get; }
    int LegacyCouncilResourceLoadPercent { get; }
    IReadOnlyList<InitialVariable> InitialValues { get; }
}
