using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

public sealed class SystemVariableDefinitionService : ISystemVariableDefinitionService
{
    private readonly ILogger<SystemVariableDefinitionService> _logger;

    public SystemVariableDefinitionService(ILogger<SystemVariableDefinitionService> logger)
    {
        _logger = logger;
        try
        {
            DefaultMaxOutputTokens = new SystemVariableDefinition<int>("DefaultMaxOutputTokens", 262144);
            DefaultMaxPromptCharacters = new SystemVariableDefinition<int>("DefaultMaxPromptCharacters", int.MaxValue);
            MaxBootstrapCharacters = new SystemVariableDefinition<int>("MaxBootstrapCharacters", 6000);
            DefaultMaxParallelModels = new SystemVariableDefinition<int>("DefaultMaxParallelModels", 1);
            DefaultHeavyModelGpuLayers = new SystemVariableDefinition<int>("DefaultHeavyModelGpuLayers", 20);
            DefaultCouncilResourceLoadPercent = new SystemVariableDefinition<int>("DefaultCouncilResourceLoadPercent", 100);
            DefaultCouncilCritiqueRounds = new SystemVariableDefinition<int>("DefaultCouncilCritiqueRounds", 1);
            MinContextTokens = new SystemVariableDefinition<int>("MinContextTokens", 2048);
            DefaultContextTokens = new SystemVariableDefinition<int>("DefaultContextTokens", 262144);
            MaxContextTokens = new SystemVariableDefinition<int>("MaxContextTokens", 262144);
            MinOutputTokens = new SystemVariableDefinition<int>("MinOutputTokens", 64);
            MaxOutputTokens = new SystemVariableDefinition<int>("MaxOutputTokens", 262144);
            DefaultOllamaEndpoint = new SystemVariableDefinition<string>("DefaultOllamaEndpoint", "http://127.0.0.1:11434");
            ProviderSelectionPolicy = new SystemVariableDefinition<string>("ProviderSelectionPolicy", "CapabilityBased");
            RepositoryKnowledgeSeedVersion = new SystemVariableDefinition<int>("RepositoryKnowledgeSeedVersion", 6);
            CouncilDefaultsVersion = new SystemVariableDefinition<int>("CouncilDefaultsVersion", 2);
            RegexMatchTimeoutMilliseconds = new SystemVariableDefinition<int>("RegexMatchTimeoutMilliseconds", 2000);
            LegacyCouncilResourceLoadPercent = 30;
            InitialValues =
            [
                ToInitialVariable(DefaultMaxOutputTokens),
                ToInitialVariable(DefaultMaxPromptCharacters),
                ToInitialVariable(MaxBootstrapCharacters),
                ToInitialVariable(DefaultMaxParallelModels),
                ToInitialVariable(DefaultHeavyModelGpuLayers),
                ToInitialVariable(DefaultCouncilResourceLoadPercent),
                ToInitialVariable(DefaultCouncilCritiqueRounds),
                ToInitialVariable(MinContextTokens),
                ToInitialVariable(DefaultContextTokens),
                ToInitialVariable(MaxContextTokens),
                ToInitialVariable(MinOutputTokens),
                ToInitialVariable(MaxOutputTokens),
                ToInitialVariable(DefaultOllamaEndpoint),
                ToInitialVariable(ProviderSelectionPolicy),
                ToInitialVariable(RepositoryKnowledgeSeedVersion),
                ToInitialVariable(CouncilDefaultsVersion),
                ToInitialVariable(RegexMatchTimeoutMilliseconds)
            ];
            _logger.LogInformation($"Initialized {InitialValues.Count} LocalGPT system-variable definitions and seed defaults.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"LocalGPT system-variable definition initialization failed: {exception.Message}");
            throw;
        }
    }

    public SystemVariableDefinition<int> DefaultMaxOutputTokens { get; }
    public SystemVariableDefinition<int> DefaultMaxPromptCharacters { get; }
    public SystemVariableDefinition<int> MaxBootstrapCharacters { get; }
    public SystemVariableDefinition<int> DefaultMaxParallelModels { get; }
    public SystemVariableDefinition<int> DefaultHeavyModelGpuLayers { get; }
    public SystemVariableDefinition<int> DefaultCouncilResourceLoadPercent { get; }
    public SystemVariableDefinition<int> DefaultCouncilCritiqueRounds { get; }
    public SystemVariableDefinition<int> MinContextTokens { get; }
    public SystemVariableDefinition<int> DefaultContextTokens { get; }
    public SystemVariableDefinition<int> MaxContextTokens { get; }
    public SystemVariableDefinition<int> MinOutputTokens { get; }
    public SystemVariableDefinition<int> MaxOutputTokens { get; }
    public SystemVariableDefinition<string> DefaultOllamaEndpoint { get; }
    public SystemVariableDefinition<string> ProviderSelectionPolicy { get; }
    public SystemVariableDefinition<int> RepositoryKnowledgeSeedVersion { get; }
    public SystemVariableDefinition<int> CouncilDefaultsVersion { get; }
    public SystemVariableDefinition<int> RegexMatchTimeoutMilliseconds { get; }
    public int LegacyCouncilResourceLoadPercent { get; }
    public IReadOnlyList<InitialVariable> InitialValues { get; }

    private InitialVariable ToInitialVariable<T>(SystemVariableDefinition<T> definition)
    {
        try
        {
            var value = Convert.ToString(definition.DefaultValue, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            _logger.LogDebug($"Prepared seed metadata for system variable {definition.Name}.");
            return new InitialVariable(definition.Name, value, typeof(T).FullName ?? typeof(T).Name);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Could not convert the system-variable seed {definition.Name}: {exception.Message}");
            throw new InvalidOperationException($"Could not convert the system-variable seed {definition.Name}.", exception);
        }
    }
}
