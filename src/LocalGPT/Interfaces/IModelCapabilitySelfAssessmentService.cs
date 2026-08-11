namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the model capability self assessment service contract.
/// </summary>
public interface IModelCapabilitySelfAssessmentService
{
    /// <summary>
    /// Runs the capture and strip async operation.
    /// </summary>
    Task<string> CaptureAndStripAsync(string modelName, string visibleContent, CancellationToken cancellationToken = default);
}
