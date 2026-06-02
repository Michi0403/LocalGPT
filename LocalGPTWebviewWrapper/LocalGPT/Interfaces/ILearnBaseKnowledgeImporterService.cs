using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface ILearnBaseKnowledgeImporterService
    {
        Task<LearnBaseImportResult> ImportAsync(
            LearnBaseImportRequest request,
            CancellationToken cancellationToken = default);
    }
}
