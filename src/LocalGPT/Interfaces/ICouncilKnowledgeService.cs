using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface ICouncilKnowledgeService
    {
        string DatabasePath { get; }
        Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CouncilKnowledgeEntry>> GetEntriesAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default);
        Task<CouncilKnowledgeEntry> SaveEntryAsync(CouncilKnowledgeEntry entry, CancellationToken cancellationToken = default);
        Task DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Guid> SaveFromCouncilRunAsync(MultiModelCouncilResult result, CancellationToken cancellationToken = default);
        Task<string> BuildKnowledgeBriefingAsync(int take = 8, CancellationToken cancellationToken = default);
    }
}
