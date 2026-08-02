using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IProjectMaintenanceService
{
    Task<IReadOnlyList<ProjectWorkspaceRoot>> GetWorkspaceRootsAsync(Guid? projectId = null, CancellationToken cancellationToken = default);
    Task<ProjectWorkspaceRoot> SaveWorkspaceRootAsync(SaveProjectWorkspaceRootRequest request, CancellationToken cancellationToken = default);
    Task<ProjectWorkspaceResolution> ResolveWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<WorkspacePermissionAssessment> AssessWorkspacePermissionsAsync(Guid workspaceRootId, bool userConfirmedWriteProbe, CancellationToken cancellationToken = default);

    /// <summary>Lists stored compiler and runtime toolchain profiles.</summary>
    /// <param name="cancellationToken">Cancels the database read.</param>
    /// <returns>A task that returns the stored profiles.</returns>
    Task<IReadOnlyList<ProjectCompilerInstallation>> GetCompilerInstallationsAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates or updates one approval-gated compiler profile.</summary>
    /// <param name="request">Validated toolchain profile and approval state.</param>
    /// <param name="cancellationToken">Cancels the database write.</param>
    /// <returns>A task that returns the persisted profile.</returns>
    Task<ProjectCompilerInstallation> SaveCompilerInstallationAsync(SaveProjectCompilerInstallationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Discovers compiler executables from PATH, common locations and approved custom roots.</summary>
    /// <param name="request">Discovery roots, persistence choice and approval state.</param>
    /// <param name="cancellationToken">Cancels discovery and persistence.</param>
    /// <returns>A task that returns the detected profiles.</returns>
    Task<IReadOnlyList<ProjectCompilerInstallation>> DiscoverCompilerInstallationsAsync(DiscoverProjectCompilersRequest request, CancellationToken cancellationToken = default);

    /// <summary>Runs one bounded version probe for a stored compiler profile.</summary>
    /// <param name="compilerId">Stored compiler identifier.</param>
    /// <param name="userConfirmed">Whether the user approved native process execution.</param>
    /// <param name="cancellationToken">Cancels the probe and database update.</param>
    /// <returns>A task that returns the updated validation profile.</returns>
    Task<ProjectCompilerInstallation> ValidateCompilerInstallationAsync(Guid compilerId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Deletes one unreferenced compiler profile after explicit human confirmation.</summary>
    /// <param name="compilerId">Stored compiler identifier.</param>
    /// <param name="userConfirmed">Whether the user approved the destructive write.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns true when the profile was removed.</returns>
    Task<bool> DeleteCompilerInstallationAsync(Guid compilerId, bool userConfirmed, CancellationToken cancellationToken = default);

    Task<ProjectScanResult> ScanProjectFilesAsync(Guid projectId, ScanProjectFilesRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalGptProjectTrackedFile>> GetTrackedFilesAsync(Guid projectId, Guid? revisionId = null, CancellationToken cancellationToken = default);
    Task<LocalGptProjectTrackedFile> SaveTrackedFilePatternAsync(Guid trackedFileId, SaveTrackedFilePatternRequest request, CancellationToken cancellationToken = default);
    Task<LocalGptProjectRevision> RegisterRevisionWorkspaceAsync(Guid projectId, Guid revisionId, string sourceRootPath, string solutionPath, bool userConfirmed, CancellationToken cancellationToken = default);

    Task<ProjectBuildVerification> RunBuildVerificationAsync(Guid projectId, RunProjectBuildVerificationRequest request, CancellationToken cancellationToken = default);
    Task<ProjectBuildVerification> RecordCouncilBuildReviewAsync(Guid verificationId, RecordCouncilBuildReviewRequest request, CancellationToken cancellationToken = default);
    Task<ProjectBuildVerification> ApproveRevisionReadyForTestAsync(Guid projectId, Guid revisionId, ApproveRevisionReadyForTestRequest request, CancellationToken cancellationToken = default);
}
