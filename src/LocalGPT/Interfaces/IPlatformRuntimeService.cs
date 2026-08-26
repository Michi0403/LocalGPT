using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Provides operating-system-specific filesystem and platform identity behavior behind one injected boundary.
/// Application services use this contract instead of branching on the host operating system directly.
/// </summary>
public interface IPlatformRuntimeService
{
    /// <summary>Gets the toolchain platform used by knowledge-backed discovery.</summary>
    ToolchainPlatformKind ToolchainPlatform { get; }

    /// <summary>Gets the stable provider-bootstrap platform token: windows, linux, macos, or other.</summary>
    string ProviderBootstrapToken { get; }

    /// <summary>Gets the comparer that matches the host filesystem's path semantics.</summary>
    StringComparer PathComparer { get; }

    /// <summary>Gets the comparison that matches the host filesystem's path semantics.</summary>
    StringComparison PathComparison { get; }

    /// <summary>Normalizes a path to one absolute path without a redundant trailing separator.</summary>
    string NormalizeAbsolutePath(string path);

    /// <summary>Returns whether two filesystem paths identify the same path under host path semantics.</summary>
    bool PathsEqual(string left, string right);

    /// <summary>Returns whether the candidate is the root itself or a descendant of that root.</summary>
    bool IsSameOrDescendantPath(string root, string candidate);

    /// <summary>Returns whether a workspace root is too broad or is a protected host/user location.</summary>
    bool IsProtectedWorkspaceRoot(string path);
}
