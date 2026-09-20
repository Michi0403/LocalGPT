using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Coordinates quarantine inspection, independent review and explicit promotion of uploaded projects.</summary>
public interface IProjectIngestionService
{
    Task<ProjectIngestionGateRecord> InspectAsync(string workspaceName, CancellationToken cancellationToken = default);
    Task<ProjectIngestionGateRecord?> GetAsync(string workspaceName, CancellationToken cancellationToken = default);
    Task<ProjectIngestionGateRecord> ReviewAsync(ProjectIngestionReviewRequest request, CancellationToken cancellationToken = default);
    Task<ProjectIngestionGateRecord> PromoteAsync(string workspaceName, bool userConfirmed, CancellationToken cancellationToken = default);
}

/// <summary>Reconstructs bounded file blobs and sends them through the same quarantine gate as normal uploads.</summary>
public interface IProjectBlobReconstructionService
{
    Task<ProjectBlobSessionSnapshot> StartAsync(ProjectBlobManifest manifest, CancellationToken cancellationToken = default);
    Task<ProjectBlobSessionSnapshot> AddChunkAsync(Guid sessionId, string relativePath, int chunkIndex, string base64Data, CancellationToken cancellationToken = default);
    Task<ProjectBlobSessionSnapshot> FinalizeAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

/// <summary>Provides reviewed project-type and toolchain evidence without executing source content.</summary>
public interface IProjectEvidenceClassifierService
{
    Task<(IReadOnlyList<string> ProjectKinds, IReadOnlyList<string> Toolchains)> ClassifyAsync(string rootPath, CancellationToken cancellationToken = default);
}
