using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Owns discovery, persistence, and normalization of operator-configurable Razor layout surfaces.</summary>
public interface ILocalGptUiLayoutService
{
    /// <summary>Returns the merged built-in and persisted layout catalog.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<LocalGptUiLayoutSurface>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Resolves one layout surface for the supplied component and section.</summary>
    /// <param name="componentType">Component type value supplied to the local GPT UI layout operation and used when producing its result.</param>
    /// <param name="sectionName">Section name value supplied to the local GPT UI layout operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local GPT UI layout surface produced by the operation.</returns>
    Task<LocalGptUiLayoutSurface> ResolveAsync(Type componentType, string sectionName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs save as part of the local GPT UI layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="surface">Surface value supplied to the local GPT UI layout operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local GPT UI layout surface produced by the operation.</returns>
    Task<LocalGptUiLayoutSurface> SaveAsync(LocalGptUiLayoutSurface surface, CancellationToken cancellationToken = default);
    /// <summary>Removes one persisted override so the declared component defaults become authoritative again.</summary>
    /// <param name="key">Key value supplied to the local GPT UI layout operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Returns the durable JSON file used for layout overrides.</summary>
    /// <returns>The string produced by the operation.</returns>
    string GetStoragePath();
}
