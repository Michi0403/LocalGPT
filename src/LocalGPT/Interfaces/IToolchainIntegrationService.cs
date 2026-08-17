using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Provides knowledge-backed compiler and runtime toolchain profiles without hardcoding platform paths in callers.</summary>
public interface IToolchainKnowledgeService
{
    /// <summary>Returns all toolchain profiles parsed from current LocalGPT knowledge articles.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<ToolchainKnowledgeProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);
    /// <summary>Returns one profile by key.</summary>
    /// <param name="key">Key value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The toolchain knowledge profile produced by the operation.</returns>
    Task<ToolchainKnowledgeProfile?> GetProfileAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Extracts a version from bounded probe output using the profile's database-backed regex rule.</summary>
    /// <param name="profileKey">Profile key value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <param name="probeOutput">Probe output value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> ExtractVersionAsync(string profileKey, string probeOutput, CancellationToken cancellationToken = default);
    /// <summary>Checks whether LocalGPT has contextual knowledge for an exact discovered toolchain version.</summary>
    /// <param name="profileKey">Profile key value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <param name="version">Version value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The toolchain version knowledge result produced by the operation.</returns>
    Task<ToolchainVersionKnowledgeResult> GetVersionKnowledgeAsync(string profileKey, string version, CancellationToken cancellationToken = default);
    /// <summary>Queues a non-blocking Human Collaboration request when an exact version/context article is missing.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The toolchain version knowledge result produced by the operation.</returns>
    Task<ToolchainVersionKnowledgeResult> RequestMissingVersionKnowledgeAsync(ToolchainKnowledgeGapRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Discovers local toolchain executables using PATH first, then knowledge-defined environment roots and platform roots.</summary>
public interface IToolchainDiscoveryService
{
    /// <summary>Returns the current operating-system family used for knowledge-root selection.</summary>
    /// <value>The current platform value exposed by <see cref="IToolchainDiscoveryService"/>.</value>
    ToolchainPlatformKind CurrentPlatform { get; }
    /// <summary>Performs bounded local discovery without network access.</summary>
    /// <param name="customRoots">String dependency used by the toolchain discovery workflow to provide the corresponding application capability.</param>
    /// <param name="maximumCandidates">Maximum candidates value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<ToolchainDiscoveryCandidate>> DiscoverAsync(IReadOnlyList<string>? customRoots = null, int maximumCandidates = 128, CancellationToken cancellationToken = default);
    /// <summary>Parses the backward-compatible JSON representation into a structured environment-variable list.</summary>
    /// <param name="environmentVariablesJson">Environment variables json value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<ToolchainEnvironmentVariableSetting> ParseEnvironmentVariables(string environmentVariablesJson);
    /// <summary>Serializes enabled structured environment variables to the JSON object consumed by native process execution.</summary>
    /// <param name="environmentVariables">Toolchain environment variable setting dependency used by the toolchain discovery workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    string SerializeEnvironmentVariables(IEnumerable<ToolchainEnvironmentVariableSetting>? environmentVariables);
}
