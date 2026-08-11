using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the local path explorer service contract.
/// </summary>
public interface ILocalPathExplorerService
{
    /// <summary>
    /// Runs the browse operation.
    /// </summary>
    LocalPathBrowseResult Browse(LocalPathBrowseRequest request);
    /// <summary>
    /// Gets suggested roots.
    /// </summary>
    IReadOnlyList<string> GetSuggestedRoots();
    /// <summary>
    /// Runs the format warnings operation.
    /// </summary>
    string FormatWarnings(IEnumerable<string> warnings);
}
