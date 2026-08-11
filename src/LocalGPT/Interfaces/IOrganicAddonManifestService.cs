using LocalGPT.BusinessObjects;
using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the organic addon manifest service contract.
/// </summary>
public interface IOrganicAddonManifestService
{
    /// <summary>
    /// Gets manifests.
    /// </summary>
    IReadOnlyList<OrganicAddonManifest> GetManifests();
    /// <summary>
    /// Gets skill descriptors.
    /// </summary>
    IReadOnlyList<OneWireSkillDescriptor> GetSkillDescriptors();
    /// <summary>
    /// Gets catalog entries.
    /// </summary>
    IReadOnlyList<DxAiFunctionCatalogEntry> GetCatalogEntries();
}
