using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the council knowledge service contract.
    /// </summary>
    public interface ICouncilKnowledgeService
    {
        string DatabasePath { get; }
        /// <summary>
        /// Ensures created async.
        /// </summary>
        Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets entries async.
        /// </summary>
        Task<IReadOnlyList<CouncilKnowledgeEntry>> GetEntriesAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default);
        /// <summary>
        /// Saves entry async.
        /// </summary>
        Task<CouncilKnowledgeEntry> SaveEntryAsync(CouncilKnowledgeEntry entry, CancellationToken cancellationToken = default);
        /// <summary>
        /// Deletes entry async.
        /// </summary>
        Task DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Saves from council run async.
        /// </summary>
        Task<Guid> SaveFromCouncilRunAsync(MultiModelCouncilResult result, CancellationToken cancellationToken = default);
        /// <summary>
        /// Builds knowledge briefing async.
        /// </summary>
        Task<string> BuildKnowledgeBriefingAsync(int take = 8, CancellationToken cancellationToken = default);
    }
}
