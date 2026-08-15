using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Runs the deterministic all-selected-member benchmark phase used by the maintained first-run calibration Council.
/// </summary>
public interface ICouncilBenchmarkCalibrationService
{
    /// <summary>
    /// Benchmarks every distinct benchmark-capable provider-qualified target in the request at four measured token points,
    /// persists Low/Middle/High/Expert hardware profiles, and returns bounded evidence for later social review rounds.
    /// </summary>
    /// <param name="request">Confirmed calibration request containing the exact Council model identities and runtime limits.</param>
    /// <param name="progressMessage">Optional callback that receives user-visible benchmark progress for the parent Council transcript.</param>
    /// <param name="cancellationToken">Cancels the calibration and its provider calls.</param>
    /// <returns>The calibration report, stored profiles and coverage summary.</returns>
    Task<CouncilBenchmarkCalibrationResult> RunAsync(
        CouncilBenchmarkCalibrationRequest request,
        Action<string>? progressMessage = null,
        CancellationToken cancellationToken = default);
}
