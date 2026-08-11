using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the learn base knowledge importer service contract.
    /// </summary>
    public interface ILearnBaseKnowledgeImporterService
    {
        /// <summary>
        /// Imports async.
        /// </summary>
        Task<LearnBaseImportResult> ImportAsync(
            LearnBaseImportRequest request,
            CancellationToken cancellationToken = default);
    }
}
