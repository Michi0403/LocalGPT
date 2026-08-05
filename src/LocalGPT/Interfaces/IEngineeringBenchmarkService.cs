using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IEngineeringBenchmarkService
    {
        Task<EngineeringBenchmarkResult> RunAsync(
            EngineeringBenchmarkRequest request,
            CancellationToken cancellationToken = default);
    }
}
