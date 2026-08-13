using LocalGPT.BusinessObjects;
using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for organic addon manifest behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicAddonManifestService
{
    /// <summary>
    /// Retrieves manifests as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OrganicAddonManifest> GetManifests();
    /// <summary>
    /// Retrieves skill descriptors as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OneWireSkillDescriptor> GetSkillDescriptors();
    /// <summary>
    /// Retrieves catalog entries as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<DxAiFunctionCatalogEntry> GetCatalogEntries();
}
