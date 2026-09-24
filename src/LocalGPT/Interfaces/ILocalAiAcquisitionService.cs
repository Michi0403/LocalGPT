using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Provides LocalGPT's reviewed direct/GitHub-first model catalog while leaving model hubs as optional provider integrations.</summary>
public interface ILocalAiAcquisitionService
{
    /// <summary>Lists reviewed upstream AI projects that LocalGPT may download directly over HTTPS.</summary>
    /// <returns>The local-AI direct source catalog.</returns>
    IReadOnlyList<LocalAiKnownModelDefinition> GetKnownModels();

    /// <summary>Downloads one reviewed upstream source archive without invoking Git, GitHub CLI, or a model-hub client.</summary>
    /// <param name="sourceKey">The stable reviewed source key.</param>
    /// <param name="userConfirmed">Whether the human operator explicitly approved the network download.</param>
    /// <param name="cancellationToken">Cancellation token that stops the transfer.</param>
    /// <returns>The managed local source archive and SHA-256 digest.</returns>
    Task<LocalAiSourceDownloadResult> DownloadSourceAsync(string sourceKey, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Downloads and installs one reviewed executable model integration through the managed Python environment.</summary>
    /// <param name="request">The reviewed source key, variant, and confirmation state.</param>
    /// <param name="cancellationToken">Cancellation token that stops download or installation.</param>
    /// <returns>The registered LocalGPT specialized-model installation.</returns>
    Task<LocalAiModelInstallation> InstallKnownModelAsync(LocalAiKnownModelInstallRequest request, CancellationToken cancellationToken = default);
}
