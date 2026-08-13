namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for model capability self assessment behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IModelCapabilitySelfAssessmentService
{
    /// <summary>
    /// Performs capture and strip as part of the model capability self assessment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the model capability self assessment operation and used when producing its result.</param>
    /// <param name="visibleContent">Visible content value supplied to the model capability self assessment operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> CaptureAndStripAsync(string modelName, string visibleContent, CancellationToken cancellationToken = default);
}
