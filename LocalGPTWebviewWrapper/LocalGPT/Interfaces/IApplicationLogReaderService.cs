using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Interfaces
{
    public interface IApplicationLogReaderService
    {
        string DatabasePath { get; }
        Task<IReadOnlyList<ApplicationLogSummary>> GetRecentAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 20, CancellationToken cancellationToken = default);
        Task<string> BuildAiLogBriefingAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 8, CancellationToken cancellationToken = default);
    }
}
