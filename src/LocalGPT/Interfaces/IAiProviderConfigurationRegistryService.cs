using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Owns detached editing and durable merge semantics for configured AI provider hosts.
/// </summary>
public interface IAiProviderConfigurationRegistryService
{
    /// <summary>
    /// Creates detached draft as part of the AI provider configuration registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the AI provider configuration registry operation and used when producing its result.</param>
    /// <returns>The AI core options produced by the operation.</returns>
    AICoreOptions CreateDetachedDraft(AICoreOptions? source);

    /// <summary>
    /// Applies detached draft as part of the AI provider configuration registry service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="target">Target value supplied to the AI provider configuration registry operation and used when producing its result.</param>
    /// <param name="draft">Draft value supplied to the AI provider configuration registry operation and used when producing its result.</param>
    /// <param name="removedOllamaEndpoints">String dependency used by the AI provider configuration registry workflow to provide the corresponding application capability.</param>
    /// <param name="explicitOllamaPrimaryEndpoint">Explicit ollama primary endpoint value supplied to the AI provider configuration registry operation and used when producing its result.</param>
    void ApplyDetachedDraft(
        AICoreOptions target,
        AICoreOptions draft,
        IReadOnlyCollection<string> removedOllamaEndpoints,
        string? explicitOllamaPrimaryEndpoint);
}
