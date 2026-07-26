namespace LocalGPT.Interfaces;

public interface IModelCapabilitySelfAssessmentService
{
    Task<string> CaptureAndStripAsync(string modelName, string visibleContent, CancellationToken cancellationToken = default);
}
