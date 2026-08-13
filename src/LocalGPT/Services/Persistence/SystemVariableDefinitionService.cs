using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Creates the maintained strongly typed system-variable definitions and their deterministic seed values.
/// </summary>
[DocumentationUpdated("2.1.20")]
public sealed class SystemVariableDefinitionService : ISystemVariableDefinitionService
{
    /// <summary>
    /// Stores the logger used by <see cref="SystemVariableDefinitionService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
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

    /// <summary>
    /// Gets the default max output tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default max output tokens value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> DefaultMaxOutputTokens { get; }
    /// <summary>
    /// Gets the default max prompt characters value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default max prompt characters value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> DefaultMaxPromptCharacters { get; }
    /// <summary>
    /// Gets the max bootstrap characters value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max bootstrap characters value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> MaxBootstrapCharacters { get; }
    /// <summary>
    /// Gets the default max parallel models value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default max parallel models value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> DefaultMaxParallelModels { get; }
    /// <summary>
    /// Gets the default heavy model GPU layers value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default heavy model GPU layers value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> DefaultHeavyModelGpuLayers { get; }
    /// <summary>
    /// Gets the default council resource load percent value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default council resource load percent value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> DefaultCouncilResourceLoadPercent { get; }
    /// <summary>
    /// Gets the default council critique rounds value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default council critique rounds value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> DefaultCouncilCritiqueRounds { get; }
    /// <summary>
    /// Gets the min context tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The min context tokens value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> MinContextTokens { get; }
    /// <summary>
    /// Gets the default context tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default context tokens value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> DefaultContextTokens { get; }
    /// <summary>
    /// Gets the max context tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max context tokens value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> MaxContextTokens { get; }
    /// <summary>
    /// Gets the min output tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The min output tokens value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> MinOutputTokens { get; }
    /// <summary>
    /// Gets the max output tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max output tokens value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> MaxOutputTokens { get; }
    /// <summary>
    /// Gets the default Ollama endpoint that identifies the network or application endpoint associated with this system variable definition state.
    /// </summary>
    /// <value>The default Ollama endpoint value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<string> DefaultOllamaEndpoint { get; }
    /// <summary>
    /// Gets the provider selection policy value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider selection policy value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<string> ProviderSelectionPolicy { get; }
    /// <summary>
    /// Gets the repository knowledge seed version value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The repository knowledge seed version value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> RepositoryKnowledgeSeedVersion { get; }
    /// <summary>
    /// Gets the council defaults version value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council defaults version value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> CouncilDefaultsVersion { get; }
    /// <summary>
    /// Gets the regex match timeout milliseconds value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The regex match timeout milliseconds value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> RegexMatchTimeoutMilliseconds { get; }
    /// <summary>
    /// Gets the council DevExpress maximum calls per step value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council DevExpress maximum calls per step value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> CouncilDxMaximumCallsPerStep { get; }
    /// <summary>
    /// Gets the council DevExpress maximum parameter characters value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council DevExpress maximum parameter characters value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> CouncilDxMaximumParameterCharacters { get; }
    /// <summary>
    /// Gets the council DevExpress maximum result characters value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council DevExpress maximum result characters value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<int> CouncilDxMaximumResultCharacters { get; }
    /// <summary>
    /// Gets the first run onboarding completed value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The first run onboarding completed value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public SystemVariableDefinition<bool> FirstRunOnboardingCompleted { get; }
    /// <summary>
    /// Gets the legacy council resource load percent value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The legacy council resource load percent value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
    public int LegacyCouncilResourceLoadPercent { get; }
    /// <summary>
    /// Gets the initial values collection maintained or exposed by this system variable definition instance for downstream processing.
    /// </summary>
    /// <value>The initial values value exposed by <see cref="SystemVariableDefinitionService"/>.</value>
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
