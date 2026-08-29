using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Provides operating-system-specific filesystem and platform identity behavior behind one injected boundary.
/// Application services use this contract instead of branching on the host operating system directly.
/// </summary>
public interface IPlatformRuntimeService
{
    /// <summary>
    /// Gets the toolchain platform value that forms part of the platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The toolchain platform value exposed by <see cref="IPlatformRuntimeService"/>.</value>
    ToolchainPlatformKind ToolchainPlatform { get; }

    /// <summary>Gets the stable provider-bootstrap platform token: windows, linux, macos, or other.</summary>
    /// <value>The provider bootstrap token value exposed by <see cref="IPlatformRuntimeService"/>.</value>
    string ProviderBootstrapToken { get; }

    /// <summary>Gets the comparer that matches the host filesystem's path semantics.</summary>
    /// <value>The path comparer value exposed by <see cref="IPlatformRuntimeService"/>.</value>
    StringComparer PathComparer { get; }

    /// <summary>Gets the comparison that matches the host filesystem's path semantics.</summary>
    /// <value>The path comparison value exposed by <see cref="IPlatformRuntimeService"/>.</value>
    StringComparison PathComparison { get; }

    /// <summary>Normalizes a path to one absolute path without a redundant trailing separator.</summary>
    /// <param name="path">Path value supplied to the platform runtime operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string NormalizeAbsolutePath(string path);

    /// <summary>Returns whether two filesystem paths identify the same path under host path semantics.</summary>
    /// <param name="left">Left value supplied to the platform runtime operation and used when producing its result.</param>
    /// <param name="right">Right value supplied to the platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool PathsEqual(string left, string right);

    /// <summary>Returns whether the candidate is the root itself or a descendant of that root.</summary>
    /// <param name="root">Root value supplied to the platform runtime operation and used when producing its result.</param>
    /// <param name="candidate">Candidate value supplied to the platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsSameOrDescendantPath(string root, string candidate);

    /// <summary>Returns whether a workspace root is too broad or is a protected host/user location.</summary>
    /// <param name="path">Path value supplied to the platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsProtectedWorkspaceRoot(string path);
}
