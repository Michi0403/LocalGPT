using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for application log reader behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IApplicationLogReaderService
    {
        /// <summary>
        /// Gets the database path used by this application log reader instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The database path value exposed by <see cref="IApplicationLogReaderService"/>.</value>
        string DatabasePath { get; }
        /// <summary>
        /// Retrieves recent as part of the application log reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="minimumLevel">Minimum level value supplied to the application log reader operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the application log reader operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<ApplicationLogSummary>> GetRecentAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 20, CancellationToken cancellationToken = default);
        /// <summary>
        /// Builds AI log briefing as part of the application log reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="minimumLevel">Minimum level value supplied to the application log reader operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the application log reader operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        Task<string> BuildAiLogBriefingAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 8, CancellationToken cancellationToken = default);
    }
}
