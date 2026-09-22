using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Probes local Ollama OCR capability and performs OCR only for images already contained by a chat-upload workspace.</summary>
public interface IWorkspaceVisionOcrService
{
    Task<LocalVisionOcrCapability> GetCapabilityAsync(string? requestedModel = null, CancellationToken cancellationToken = default);
    Task<WorkspaceImageOcrResult> RecognizeWorkspaceImageAsync(WorkspaceImageOcrRequest request, CancellationToken cancellationToken = default);
}
