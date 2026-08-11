using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the project maintenance service contract.
/// </summary>
public interface IProjectMaintenanceService
{
    /// <summary>
    /// Gets workspace roots async.
    /// </summary>
    Task<IReadOnlyList<ProjectWorkspaceRoot>> GetWorkspaceRootsAsync(Guid? projectId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves workspace root async.
    /// </summary>
    Task<ProjectWorkspaceRoot> SaveWorkspaceRootAsync(SaveProjectWorkspaceRootRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Resolves workspace async.
    /// </summary>
    Task<ProjectWorkspaceResolution> ResolveWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the assess workspace permissions async operation.
    /// </summary>
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

    /// <summary>
    /// Runs the scan project files async operation.
    /// </summary>
    Task<ProjectScanResult> ScanProjectFilesAsync(Guid projectId, ScanProjectFilesRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets tracked files async.
    /// </summary>
    Task<IReadOnlyList<LocalGptProjectTrackedFile>> GetTrackedFilesAsync(Guid projectId, Guid? revisionId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves tracked file pattern async.
    /// </summary>
    Task<LocalGptProjectTrackedFile> SaveTrackedFilePatternAsync(Guid trackedFileId, SaveTrackedFilePatternRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Registers revision workspace async.
    /// </summary>
    Task<LocalGptProjectRevision> RegisterRevisionWorkspaceAsync(Guid projectId, Guid revisionId, string sourceRootPath, string solutionPath, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the run build verification async operation.
    /// </summary>
    Task<ProjectBuildVerification> RunBuildVerificationAsync(Guid projectId, RunProjectBuildVerificationRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the record council build review async operation.
    /// </summary>
    Task<ProjectBuildVerification> RecordCouncilBuildReviewAsync(Guid verificationId, RecordCouncilBuildReviewRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the approve revision ready for test async operation.
    /// </summary>
    Task<ProjectBuildVerification> ApproveRevisionReadyForTestAsync(Guid projectId, Guid revisionId, ApproveRevisionReadyForTestRequest request, CancellationToken cancellationToken = default);
}
