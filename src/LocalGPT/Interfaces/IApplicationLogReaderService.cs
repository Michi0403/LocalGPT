using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the application log reader service contract.
    /// </summary>
    public interface IApplicationLogReaderService
    {
        string DatabasePath { get; }
        /// <summary>
        /// Gets recent async.
        /// </summary>
        Task<IReadOnlyList<ApplicationLogSummary>> GetRecentAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 20, CancellationToken cancellationToken = default);
        /// <summary>
        /// Builds ai log briefing async.
        /// </summary>
        Task<string> BuildAiLogBriefingAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 8, CancellationToken cancellationToken = default);
    }
}
