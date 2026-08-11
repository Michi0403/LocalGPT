using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Owns detached editing and durable merge semantics for configured AI provider hosts.
/// </summary>
public interface IAiProviderConfigurationRegistryService
{
    /// <summary>
    /// Creates detached draft.
    /// </summary>
    AICoreOptions CreateDetachedDraft(AICoreOptions? source);

    /// <summary>
    /// Applies detached draft.
    /// </summary>
    void ApplyDetachedDraft(
        AICoreOptions target,
        AICoreOptions draft,
        IReadOnlyCollection<string> removedOllamaEndpoints,
        string? explicitOllamaPrimaryEndpoint);
}
