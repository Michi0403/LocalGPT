using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Owns detached editing and durable merge semantics for configured AI provider hosts.
/// </summary>
public interface IAiProviderConfigurationRegistryService
{
    AICoreOptions CreateDetachedDraft(AICoreOptions? source);

    void ApplyDetachedDraft(
        AICoreOptions target,
        AICoreOptions draft,
        IReadOnlyCollection<string> removedOllamaEndpoints,
        string? explicitOllamaPrimaryEndpoint);
}
