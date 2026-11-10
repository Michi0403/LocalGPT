using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Owns source-provenance and archive-verification evidence for approved Council repository snapshots.</summary>
public interface ICouncilDevelopmentWorkspaceService
{
    /// <summary>Captures the original staged bytes outside the user-editable workspace.</summary>
    /// <param name="artifactRoot">Application-owned Council artifact directory.</param>
    /// <param name="workspaceName">Newly approved staged workspace identifier.</param>
    /// <param name="cancellationToken">Cancels fingerprint calculation.</param>
    /// <returns>The number of source files fingerprinted.</returns>
    Task<int> RecordStageBaselineAsync(string artifactRoot, string workspaceName, CancellationToken cancellationToken = default);

    /// <summary>Checks for actual changes and verifies that the complete ZIP matches current workspace bytes.</summary>
    /// <param name="artifactRoot">Application-owned Council artifact directory.</param>
    /// <param name="workspaceName">Existing staged workspace identifier.</param>
    /// <param name="cancellationToken">Cancels content verification.</param>
    /// <returns>Authoritative source/ZIP evidence; never a compiler-success claim.</returns>
    Task<CouncilRepositoryArtifactEvidence> VerifyStageArchiveAsync(string artifactRoot, string workspaceName, CancellationToken cancellationToken = default);
}
