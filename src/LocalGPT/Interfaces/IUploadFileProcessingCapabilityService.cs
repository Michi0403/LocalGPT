using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Defines deterministic routing of quarantined upload files to LocalGPT-native or connected PublisherStudio processing capabilities.</summary>
public interface IUploadFileProcessingCapabilityService
{
    /// <summary>Returns processing routes for the supplied quarantined file evidence without executing or promoting any file.</summary>
    /// <param name="files">Quarantined file evidence to route.</param>
    /// <returns>The deterministic processing routes currently available.</returns>
    IReadOnlyList<UploadFileProcessingRoute> Evaluate(IReadOnlyList<ProjectIngestionFileEvidence> files);
}
