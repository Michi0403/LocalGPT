using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for local path explorer behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalPathExplorerService
{
    /// <summary>
    /// Performs browse as part of the local path explorer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The local path browse result produced by the operation.</returns>
    LocalPathBrowseResult Browse(LocalPathBrowseRequest request);
    /// <summary>
    /// Retrieves suggested roots as part of the local path explorer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<string> GetSuggestedRoots();
    /// <summary>
    /// Performs format warnings as part of the local path explorer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="warnings">String dependency used by the local path explorer workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    string FormatWarnings(IEnumerable<string> warnings);
}
