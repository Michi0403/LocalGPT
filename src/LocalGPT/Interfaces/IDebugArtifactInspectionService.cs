using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IDebugArtifactInspectionService
{
    Task<DebugArtifactInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken = default);
}
