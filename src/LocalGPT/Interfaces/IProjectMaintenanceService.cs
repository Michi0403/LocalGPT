using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for project maintenance behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IProjectMaintenanceService
{
    /// <summary>
    /// Retrieves workspace roots as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<ProjectWorkspaceRoot>> GetWorkspaceRootsAsync(Guid? projectId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists workspace root as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project workspace root produced by the operation.</returns>
    Task<ProjectWorkspaceRoot> SaveWorkspaceRootAsync(SaveProjectWorkspaceRootRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Resolves workspace as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project workspace resolution produced by the operation.</returns>
    Task<ProjectWorkspaceResolution> ResolveWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs assess workspace permissions as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceRootId">Identifier of the workspace root to use for this operation.</param>
    /// <param name="userConfirmedWriteProbe">Value indicating whether user confirmed write probe should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The workspace permission assessment produced by the operation.</returns>
    Task<WorkspacePermissionAssessment> AssessWorkspacePermissionsAsync(Guid workspaceRootId, bool userConfirmedWriteProbe, CancellationToken cancellationToken = default);

    /// <summary>Lists stored compiler and runtime toolchain profiles.</summary>
    /// <param name="cancellationToken">Cancels the database read.</param>
    /// <returns>A task that returns the stored profiles.</returns>
    Task<IReadOnlyList<ProjectCompilerInstallation>> GetCompilerInstallationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists compiler installation as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
    /// <summary>
    /// Deletes compiler installation as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="compilerId">Stored compiler identifier.</param>
    /// <param name="userConfirmed">Whether the user approved the destructive write.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns true when the profile was removed.</returns>
    Task<bool> DeleteCompilerInstallationAsync(Guid compilerId, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs scan project files as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project scan result produced by the operation.</returns>
    Task<ProjectScanResult> ScanProjectFilesAsync(Guid projectId, ScanProjectFilesRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves tracked files as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<LocalGptProjectTrackedFile>> GetTrackedFilesAsync(Guid projectId, Guid? revisionId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists tracked file pattern as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="trackedFileId">Identifier of the tracked file to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project tracked file produced by the operation.</returns>
    Task<LocalGptProjectTrackedFile> SaveTrackedFilePatternAsync(Guid trackedFileId, SaveTrackedFilePatternRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Registers revision workspace as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="sourceRootPath">Source root path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="solutionPath">Solution path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project revision produced by the operation.</returns>
    Task<LocalGptProjectRevision> RegisterRevisionWorkspaceAsync(Guid projectId, Guid revisionId, string sourceRootPath, string solutionPath, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs run build verification as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project build verification produced by the operation.</returns>
    Task<ProjectBuildVerification> RunBuildVerificationAsync(Guid projectId, RunProjectBuildVerificationRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs record council build review as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="verificationId">Identifier of the verification to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project build verification produced by the operation.</returns>
    Task<ProjectBuildVerification> RecordCouncilBuildReviewAsync(Guid verificationId, RecordCouncilBuildReviewRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Approves revision ready for test as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project build verification produced by the operation.</returns>
    Task<ProjectBuildVerification> ApproveRevisionReadyForTestAsync(Guid projectId, Guid revisionId, ApproveRevisionReadyForTestRequest request, CancellationToken cancellationToken = default);
}
