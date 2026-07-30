using LocalGPT.BusinessObjects;
using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

public interface IOrganicAddonManifestService
{
    IReadOnlyList<OrganicAddonManifest> GetManifests();
    IReadOnlyList<OneWireSkillDescriptor> GetSkillDescriptors();
    IReadOnlyList<DxAiFunctionCatalogEntry> GetCatalogEntries();
}
