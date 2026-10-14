using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Probes local Ollama OCR capability and performs OCR only for images already contained by a chat-upload workspace.</summary>
public interface IWorkspaceVisionOcrService
{
    /// <summary>
    /// Retrieves capability as part of the workspace vision OCR service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestedModel">Requested model value supplied to the workspace vision OCR operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local vision OCR capability produced by the operation.</returns>
    Task<LocalVisionOcrCapability> GetCapabilityAsync(string? requestedModel = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs recognize workspace image as part of the workspace vision OCR service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The workspace image OCR result produced by the operation.</returns>
    Task<WorkspaceImageOcrResult> RecognizeWorkspaceImageAsync(WorkspaceImageOcrRequest request, CancellationToken cancellationToken = default);
}
