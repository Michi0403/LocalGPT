using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for council knowledge behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface ICouncilKnowledgeService
    {
        /// <summary>
        /// Gets the database path used by this council knowledge instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The database path value exposed by <see cref="ICouncilKnowledgeService"/>.</value>
        string DatabasePath { get; }
        /// <summary>
        /// Ensures created as part of the council knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Retrieves entries as part of the council knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="includeArchived">Value indicating whether include archived should apply to this operation.</param>
        /// <param name="take">Take value supplied to the council knowledge operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<CouncilKnowledgeEntry>> GetEntriesAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default);
        /// <summary>
        /// Persists entry as part of the council knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="entry">Entry value supplied to the council knowledge operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The council knowledge entry produced by the operation.</returns>
        Task<CouncilKnowledgeEntry> SaveEntryAsync(CouncilKnowledgeEntry entry, CancellationToken cancellationToken = default);
        /// <summary>
        /// Deletes entry as part of the council knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="id">Identifier of the resource to use for this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Saves from council run async.
        /// </summary>
        /// <param name="result">Result value supplied to the council knowledge operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The GUID produced by the operation.</returns>
        Task<Guid> SaveFromCouncilRunAsync(MultiModelCouncilResult result, CancellationToken cancellationToken = default);
        /// <summary>
        /// Builds knowledge briefing as part of the council knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="take">Take value supplied to the council knowledge operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        Task<string> BuildKnowledgeBriefingAsync(int take = 8, CancellationToken cancellationToken = default);
    }
}
