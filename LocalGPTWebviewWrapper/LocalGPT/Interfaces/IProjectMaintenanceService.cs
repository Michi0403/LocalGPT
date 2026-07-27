using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IProjectMaintenanceService
{
    Task<IReadOnlyList<ProjectWorkspaceRoot>> GetWorkspaceRootsAsync(Guid? projectId = null, CancellationToken cancellationToken = default);
    Task<ProjectWorkspaceRoot> SaveWorkspaceRootAsync(SaveProjectWorkspaceRootRequest request, CancellationToken cancellationToken = default);
    Task<ProjectWorkspaceResolution> ResolveWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectCompilerInstallation>> GetCompilerInstallationsAsync(CancellationToken cancellationToken = default);
    Task<ProjectCompilerInstallation> SaveCompilerInstallationAsync(SaveProjectCompilerInstallationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectCompilerInstallation>> DiscoverCompilerInstallationsAsync(DiscoverProjectCompilersRequest request, CancellationToken cancellationToken = default);
    Task<ProjectCompilerInstallation> ValidateCompilerInstallationAsync(Guid compilerId, bool userConfirmed, CancellationToken cancellationToken = default);

    Task<ProjectScanResult> ScanProjectFilesAsync(Guid projectId, ScanProjectFilesRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalGptProjectTrackedFile>> GetTrackedFilesAsync(Guid projectId, Guid? revisionId = null, CancellationToken cancellationToken = default);
    Task<LocalGptProjectTrackedFile> SaveTrackedFilePatternAsync(Guid trackedFileId, SaveTrackedFilePatternRequest request, CancellationToken cancellationToken = default);
    Task<LocalGptProjectRevision> RegisterRevisionWorkspaceAsync(Guid projectId, Guid revisionId, string sourceRootPath, string solutionPath, bool userConfirmed, CancellationToken cancellationToken = default);

    Task<ProjectBuildVerification> RunBuildVerificationAsync(Guid projectId, RunProjectBuildVerificationRequest request, CancellationToken cancellationToken = default);
    Task<ProjectBuildVerification> RecordCouncilBuildReviewAsync(Guid verificationId, RecordCouncilBuildReviewRequest request, CancellationToken cancellationToken = default);
    Task<ProjectBuildVerification> ApproveRevisionReadyForTestAsync(Guid projectId, Guid revisionId, ApproveRevisionReadyForTestRequest request, CancellationToken cancellationToken = default);
}
