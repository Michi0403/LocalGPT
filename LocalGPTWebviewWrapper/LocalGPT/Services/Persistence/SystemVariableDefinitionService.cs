using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Creates the maintained strongly typed system-variable definitions and their deterministic seed values.
/// </summary>
[DocumentationUpdated("2.1.20")]
public sealed class SystemVariableDefinitionService : ISystemVariableDefinitionService
{
    private readonly ILogger<SystemVariableDefinitionService> _logger;

    /// <summary>
    /// Initializes every maintained system-variable definition.
    /// </summary>
    /// <param name="logger">Writes bounded initialization diagnostics.</param>
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
            CouncilDxMaximumCallsPerStep = new SystemVariableDefinition<int>("CouncilDxMaximumCallsPerStep", 3);
            CouncilDxMaximumParameterCharacters = new SystemVariableDefinition<int>("CouncilDxMaximumParameterCharacters", 24000);
            CouncilDxMaximumResultCharacters = new SystemVariableDefinition<int>("CouncilDxMaximumResultCharacters", 32000);
            FirstRunOnboardingCompleted = new SystemVariableDefinition<bool>("FirstRunOnboardingCompleted", false);
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
                ToInitialVariable(RegexMatchTimeoutMilliseconds),
                ToInitialVariable(CouncilDxMaximumCallsPerStep),
                ToInitialVariable(CouncilDxMaximumParameterCharacters),
                ToInitialVariable(CouncilDxMaximumResultCharacters),
                ToInitialVariable(FirstRunOnboardingCompleted)
            ];
            _logger.LogInformation($"Initialized {InitialValues.Count} LocalGPT system-variable definitions and seed defaults.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"LocalGPT system-variable definition initialization failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>Gets the default maximum output-token definition.</summary>
    public SystemVariableDefinition<int> DefaultMaxOutputTokens { get; }
    /// <summary>Gets the default maximum prompt-character definition.</summary>
    public SystemVariableDefinition<int> DefaultMaxPromptCharacters { get; }
    /// <summary>Gets the maximum bootstrap-character definition.</summary>
    public SystemVariableDefinition<int> MaxBootstrapCharacters { get; }
    /// <summary>Gets the default maximum parallel-model definition.</summary>
    public SystemVariableDefinition<int> DefaultMaxParallelModels { get; }
    /// <summary>Gets the legacy heavy-model GPU-layer default.</summary>
    public SystemVariableDefinition<int> DefaultHeavyModelGpuLayers { get; }
    /// <summary>Gets the default Council resource-load percentage definition.</summary>
    public SystemVariableDefinition<int> DefaultCouncilResourceLoadPercent { get; }
    /// <summary>Gets the default Council critique-round definition.</summary>
    public SystemVariableDefinition<int> DefaultCouncilCritiqueRounds { get; }
    /// <summary>Gets the minimum context-token definition.</summary>
    public SystemVariableDefinition<int> MinContextTokens { get; }
    /// <summary>Gets the default context-token definition.</summary>
    public SystemVariableDefinition<int> DefaultContextTokens { get; }
    /// <summary>Gets the maximum context-token definition.</summary>
    public SystemVariableDefinition<int> MaxContextTokens { get; }
    /// <summary>Gets the minimum output-token definition.</summary>
    public SystemVariableDefinition<int> MinOutputTokens { get; }
    /// <summary>Gets the maximum output-token definition.</summary>
    public SystemVariableDefinition<int> MaxOutputTokens { get; }
    /// <summary>Gets the default loopback Ollama endpoint definition.</summary>
    public SystemVariableDefinition<string> DefaultOllamaEndpoint { get; }
    /// <summary>Gets the provider-selection policy definition.</summary>
    public SystemVariableDefinition<string> ProviderSelectionPolicy { get; }
    /// <summary>Gets the repository knowledge seed-version definition.</summary>
    public SystemVariableDefinition<int> RepositoryKnowledgeSeedVersion { get; }
    /// <summary>Gets the Council defaults migration-version definition.</summary>
    public SystemVariableDefinition<int> CouncilDefaultsVersion { get; }
    /// <summary>Gets the regular-expression timeout definition.</summary>
    public SystemVariableDefinition<int> RegexMatchTimeoutMilliseconds { get; }
    /// <summary>Gets the maximum DXFunction calls per Council step definition.</summary>
    public SystemVariableDefinition<int> CouncilDxMaximumCallsPerStep { get; }
    /// <summary>Gets the maximum DXFunction parameter-character definition.</summary>
    public SystemVariableDefinition<int> CouncilDxMaximumParameterCharacters { get; }
    /// <summary>Gets the maximum DXFunction result-character definition.</summary>
    public SystemVariableDefinition<int> CouncilDxMaximumResultCharacters { get; }
    /// <summary>Gets the persisted first-run onboarding completion flag definition.</summary>
    public SystemVariableDefinition<bool> FirstRunOnboardingCompleted { get; }
    /// <summary>Gets the former hard-coded Council resource-load percentage.</summary>
    public int LegacyCouncilResourceLoadPercent { get; }
    /// <summary>Gets the deterministic initial variable list.</summary>
    public IReadOnlyList<InitialVariable> InitialValues { get; }

    /// <summary>
    /// Converts one strongly typed definition into the persistence seed representation.
    /// </summary>
    /// <typeparam name="T">Definition value type.</typeparam>
    /// <param name="definition">Definition to convert.</param>
    /// <returns>The persistence-ready initial variable.</returns>
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
