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
    /// <summary>
    /// Gets the default max output tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default max output tokens value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> DefaultMaxOutputTokens { get; }
    /// <summary>
    /// Gets the default max prompt characters value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default max prompt characters value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> DefaultMaxPromptCharacters { get; }
    /// <summary>
    /// Gets the max bootstrap characters value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max bootstrap characters value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> MaxBootstrapCharacters { get; }
    /// <summary>
    /// Gets the default max parallel models value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default max parallel models value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> DefaultMaxParallelModels { get; }
    /// <summary>
    /// Gets the default heavy model GPU layers value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default heavy model GPU layers value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> DefaultHeavyModelGpuLayers { get; }
    /// <summary>
    /// Gets the default council resource load percent value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default council resource load percent value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> DefaultCouncilResourceLoadPercent { get; }
    /// <summary>
    /// Gets the default council critique rounds value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default council critique rounds value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> DefaultCouncilCritiqueRounds { get; }
    /// <summary>
    /// Gets the min context tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The min context tokens value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> MinContextTokens { get; }
    /// <summary>
    /// Gets the default context tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default context tokens value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> DefaultContextTokens { get; }
    /// <summary>
    /// Gets the max context tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max context tokens value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> MaxContextTokens { get; }
    /// <summary>
    /// Gets the min output tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The min output tokens value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> MinOutputTokens { get; }
    /// <summary>
    /// Gets the max output tokens value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max output tokens value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> MaxOutputTokens { get; }
    /// <summary>
    /// Gets the default Ollama endpoint that identifies the network or application endpoint associated with this system variable definition state.
    /// </summary>
    /// <value>The default Ollama endpoint value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<string> DefaultOllamaEndpoint { get; }
    /// <summary>
    /// Gets the provider selection policy value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider selection policy value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<string> ProviderSelectionPolicy { get; }
    /// <summary>
    /// Gets the repository knowledge seed version value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The repository knowledge seed version value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> RepositoryKnowledgeSeedVersion { get; }
    /// <summary>
    /// Gets the council defaults version value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council defaults version value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> CouncilDefaultsVersion { get; }
    /// <summary>
    /// Gets the regex match timeout milliseconds value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The regex match timeout milliseconds value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> RegexMatchTimeoutMilliseconds { get; }
    /// <summary>
    /// Gets the council DevExpress maximum calls per step value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council DevExpress maximum calls per step value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> CouncilDxMaximumCallsPerStep { get; }
    /// <summary>
    /// Gets the council DevExpress maximum parameter characters value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council DevExpress maximum parameter characters value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> CouncilDxMaximumParameterCharacters { get; }
    /// <summary>
    /// Gets the council DevExpress maximum result characters value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council DevExpress maximum result characters value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<int> CouncilDxMaximumResultCharacters { get; }
    /// <summary>
    /// Gets the first run onboarding completed value that forms part of the system variable definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The first run onboarding completed value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    SystemVariableDefinition<bool> FirstRunOnboardingCompleted { get; }
    /// <summary>Gets the former hard-coded Council resource-load percentage for migration diagnostics.</summary>
    /// <value>The legacy council resource load percent value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    int LegacyCouncilResourceLoadPercent { get; }
    /// <summary>
    /// Gets the initial values collection maintained or exposed by this system variable definition instance for downstream processing.
    /// </summary>
    /// <value>The initial values value exposed by <see cref="ISystemVariableDefinitionService"/>.</value>
    IReadOnlyList<InitialVariable> InitialValues { get; }
}
