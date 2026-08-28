using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Runtime.InteropServices;

namespace LocalGPT.Services;

/// <summary>Windows filesystem and platform identity implementation.</summary>
public sealed class WindowsPlatformRuntimeService : IPlatformRuntimeService
{
    /// <inheritdoc />
    public ToolchainPlatformKind ToolchainPlatform => ToolchainPlatformKind.Windows;

    /// <inheritdoc />
    public string ProviderBootstrapToken => "windows";

    /// <inheritdoc />
    public StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;

    /// <inheritdoc />
    public StringComparison PathComparison => StringComparison.OrdinalIgnoreCase;

    /// <inheritdoc />
    public string NormalizeAbsolutePath(string path) 
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsPlatformRuntimeService), nameof(NormalizeAbsolutePath), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public bool PathsEqual(string left, string right) 
    {
        try
        {
            return string.Equals(NormalizeAbsolutePath(left), NormalizeAbsolutePath(right), PathComparison);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsPlatformRuntimeService), nameof(PathsEqual), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public bool IsSameOrDescendantPath(string root, string candidate)
    {
        try
        {
            var normalizedRoot = NormalizeAbsolutePath(root);
            var normalizedCandidate = NormalizeAbsolutePath(candidate);
            if (string.Equals(normalizedRoot, normalizedCandidate, PathComparison))
                return true;

            var rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
                ? normalizedRoot
                : normalizedRoot + Path.DirectorySeparatorChar;
            return normalizedCandidate.StartsWith(rootWithSeparator, PathComparison);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsPlatformRuntimeService), nameof(IsSameOrDescendantPath), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public bool IsProtectedWorkspaceRoot(string path)
    {
        try
        {
            var normalized = NormalizeAbsolutePath(path);
            var filesystemRoot = Path.GetPathRoot(normalized);
            if (!string.IsNullOrWhiteSpace(filesystemRoot) && PathsEqual(normalized, filesystemRoot))
                return true;

            var protectedRoots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            return protectedRoots
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Any(item => PathsEqual(normalized, item));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsPlatformRuntimeService), nameof(IsProtectedWorkspaceRoot), exception);
            throw;
        }
    }
}

/// <summary>Unix/macOS/Linux filesystem and platform identity implementation.</summary>
public sealed class UnixPlatformRuntimeService : IPlatformRuntimeService
{
    private readonly bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    private readonly bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <inheritdoc />
    public ToolchainPlatformKind ToolchainPlatform => isMacOS
        ? ToolchainPlatformKind.MacOS
        : isLinux
            ? ToolchainPlatformKind.Linux
            : ToolchainPlatformKind.Other;

    /// <inheritdoc />
    public string ProviderBootstrapToken => isMacOS ? "macos" : isLinux ? "linux" : "other";

    /// <inheritdoc />
    public StringComparer PathComparer => StringComparer.Ordinal;

    /// <inheritdoc />
    public StringComparison PathComparison => StringComparison.Ordinal;

    /// <inheritdoc />
    public string NormalizeAbsolutePath(string path) 
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixPlatformRuntimeService), nameof(NormalizeAbsolutePath), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public bool PathsEqual(string left, string right) 
    {
        try
        {
            return string.Equals(NormalizeAbsolutePath(left), NormalizeAbsolutePath(right), PathComparison);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixPlatformRuntimeService), nameof(PathsEqual), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public bool IsSameOrDescendantPath(string root, string candidate)
    {
        try
        {
            var normalizedRoot = NormalizeAbsolutePath(root);
            var normalizedCandidate = NormalizeAbsolutePath(candidate);
            if (string.Equals(normalizedRoot, normalizedCandidate, PathComparison))
                return true;

            var rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
                ? normalizedRoot
                : normalizedRoot + Path.DirectorySeparatorChar;
            return normalizedCandidate.StartsWith(rootWithSeparator, PathComparison);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixPlatformRuntimeService), nameof(IsSameOrDescendantPath), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public bool IsProtectedWorkspaceRoot(string path)
    {
        try
        {
            var normalized = NormalizeAbsolutePath(path);
            var filesystemRoot = Path.GetPathRoot(normalized);
            if (!string.IsNullOrWhiteSpace(filesystemRoot) && PathsEqual(normalized, filesystemRoot))
                return true;

            var protectedRoots = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "/usr",
                "/etc",
                "/bin",
                "/sbin",
                "/var"
            };
            if (isMacOS)
            {
                protectedRoots.Add("/System");
                protectedRoots.Add("/Library");
                protectedRoots.Add("/Applications");
            }

            return protectedRoots
                .Where(item => !string.IsNullOrWhiteSpace(item) && Directory.Exists(item))
                .Any(item => PathsEqual(normalized, item));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixPlatformRuntimeService), nameof(IsProtectedWorkspaceRoot), exception);
            throw;
        }
    }


}
