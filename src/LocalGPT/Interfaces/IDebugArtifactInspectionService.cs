using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the debug artifact inspection service contract.
/// </summary>
public interface IDebugArtifactInspectionService
{
    /// <summary>
    /// Runs the inspect async operation.
    /// </summary>
    Task<DebugArtifactInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken = default);
}
