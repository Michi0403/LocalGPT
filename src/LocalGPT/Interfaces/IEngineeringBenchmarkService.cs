using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the engineering benchmark service contract.
    /// </summary>
    public interface IEngineeringBenchmarkService
    {
        /// <summary>
        /// Runs the run async operation.
        /// </summary>
        Task<EngineeringBenchmarkResult> RunAsync(
            EngineeringBenchmarkRequest request,
            CancellationToken cancellationToken = default);
    }
}
