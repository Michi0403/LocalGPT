using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ILocalPathExplorerService
{
    LocalPathBrowseResult Browse(LocalPathBrowseRequest request);
    IReadOnlyList<string> GetSuggestedRoots();
}
