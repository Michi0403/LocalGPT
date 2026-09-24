using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Reads and explicitly mutates process, LocalGPT application, and supported operating-system environment scopes.</summary>
public interface IToolchainEnvironmentService
{
    /// <summary>Builds the current redacted environment snapshot and applies persisted LocalGPT application overrides to the process first.</summary>
    /// <param name="cancellationToken">Cancellation token that stops snapshot construction.</param>
    /// <returns>The current environment snapshot.</returns>
    Task<ToolchainEnvironmentSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Filters environment entries for the Toolchains workbench without moving text-search policy into the UI layer.</summary>
    /// <param name="entries">Environment entries to filter.</param>
    /// <param name="searchText">Optional user-entered search text.</param>
    /// <param name="maximumResults">Maximum number of rows returned to the workbench.</param>
    /// <returns>The bounded filtered environment entries.</returns>
    IReadOnlyList<ToolchainEnvironmentEntry> FilterEntries(IReadOnlyCollection<ToolchainEnvironmentEntry> entries, string? searchText, int maximumResults = 300);

    /// <summary>Applies one explicitly confirmed environment-variable change.</summary>
    /// <param name="request">The requested name, value, scope, and removal state.</param>
    /// <param name="cancellationToken">Cancellation token that stops persistence.</param>
    /// <returns>The refreshed redacted environment snapshot.</returns>
    Task<ToolchainEnvironmentSnapshot> ChangeAsync(ToolchainEnvironmentChangeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Applies persisted LocalGPT application overrides to the current process without mutating global operating-system scopes.</summary>
    /// <param name="cancellationToken">Cancellation token that stops initialization.</param>
    /// <returns>A task that completes after the configured overrides have been applied.</returns>
    Task ApplyApplicationOverridesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Lists and downloads only reviewed, pre-seeded direct toolchain sources without invoking package managers or installers.</summary>
public interface IToolchainAcquisitionService
{
    /// <summary>Lists reviewed direct-download entries for common runtime and build toolchains.</summary>
    /// <param name="cancellationToken">Cancellation token that stops catalog access.</param>
    /// <returns>The toolchain acquisition catalog.</returns>
    Task<IReadOnlyList<ToolchainAcquisitionSource>> GetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Downloads one reviewed catalog artifact through HTTPS after exact human confirmation.</summary>
    /// <param name="request">The reviewed source key and confirmation state.</param>
    /// <param name="cancellationToken">Cancellation token that stops the network transfer.</param>
    /// <returns>The managed local file and its SHA-256 digest.</returns>
    Task<ToolchainAcquisitionDownloadResult> DownloadAsync(ToolchainAcquisitionDownloadRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Owns database-backed generic toolchain process profiles and their bounded execution.</summary>
public interface IToolchainExecutionProfileService
{
    /// <summary>Lists enabled persisted profiles, optionally filtered by capability.</summary>
    Task<IReadOnlyList<ToolchainExecutionProfile>> GetProfilesAsync(string? capabilityKey = null, CancellationToken cancellationToken = default);
    /// <summary>Returns one persisted profile by stable key.</summary>
    Task<ToolchainExecutionProfile?> GetProfileAsync(string profileKey, CancellationToken cancellationToken = default);
    /// <summary>Saves one explicitly approved persisted profile.</summary>
    Task<ToolchainExecutionProfile> SaveProfileAsync(SaveToolchainExecutionProfileRequest request, CancellationToken cancellationToken = default);
    /// <summary>Runs one profile without a command shell after required human approval.</summary>
    Task<ToolchainProcessExecutionResult> ExecuteAsync(ToolchainProcessExecutionRequest request, CancellationToken cancellationToken = default);
}
