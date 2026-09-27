using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Owns discovery, persistence, and normalization of operator-configurable Razor layout surfaces.</summary>
public interface ILocalGptUiLayoutService
{
    /// <summary>Returns the merged built-in and persisted layout catalog.</summary>
    Task<IReadOnlyList<LocalGptUiLayoutSurface>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Resolves one layout surface for the supplied component and section.</summary>
    Task<LocalGptUiLayoutSurface> ResolveAsync(Type componentType, string sectionName, CancellationToken cancellationToken = default);
    /// <summary>Saves one user-owned layout override.</summary>
    Task<LocalGptUiLayoutSurface> SaveAsync(LocalGptUiLayoutSurface surface, CancellationToken cancellationToken = default);
    /// <summary>Removes one persisted override so the declared component defaults become authoritative again.</summary>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Returns the durable JSON file used for layout overrides.</summary>
    string GetStoragePath();
}
