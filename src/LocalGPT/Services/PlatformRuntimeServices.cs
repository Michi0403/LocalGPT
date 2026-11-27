using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Runtime.InteropServices;
using System.Buffers.Binary;

namespace LocalGPT.Services;

/// <summary>Windows filesystem and platform identity implementation.</summary>
public sealed class WindowsPlatformRuntimeService : IPlatformRuntimeService
{
    /// <summary>
    /// Gets the toolchain platform value that forms part of the windows platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public ToolchainPlatformKind ToolchainPlatform => ToolchainPlatformKind.Windows;

    /// <summary>
    /// Gets the provider bootstrap token value that forms part of the windows platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public string ProviderBootstrapToken => "windows";

    /// <inheritdoc />
    public bool IsCompatiblePythonBinary(string path) => true;

    /// <inheritdoc />
    public string GetNativeExecutableName(string baseName) => baseName + ".exe";

    /// <inheritdoc />
    public bool SupportsPyTorchBackend(string backend) =>
        RuntimeInformation.OSArchitecture == Architecture.X64 && (backend switch
        {
            "cuda" or "rocm" => true,
            "xpu" => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000),
            _ => false
        });

    /// <summary>
    /// Gets the path comparer used by this windows platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <inheritdoc />
    public StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Gets the path comparison used by this windows platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <inheritdoc />
    public StringComparison PathComparison => StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Normalizes absolute path as part of the windows platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Performs paths equal as part of the windows platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Determines whether same or descendant path as part of the windows platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>Prepares native process startup for Windows command wrappers while leaving real executables unchanged.</summary>
    /// <inheritdoc />
    public (string ExecutablePath, string Arguments) PrepareToolchainProcessInvocation(string executablePath, string arguments)
    {
        try
        {
            var extension = Path.GetExtension(executablePath);
            if (!extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".bat", StringComparison.OrdinalIgnoreCase))
                return (executablePath, arguments);
            var commandProcessor = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            return (commandProcessor, $"/d /s /c \"\"{executablePath}\" {arguments}\"");
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsPlatformRuntimeService), nameof(PrepareToolchainProcessInvocation), exception);
            throw;
        }
    }

    /// <summary>
    /// Determines whether protected workspace root as part of the windows platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
    /// <summary>
    /// Stores the internal is mac OS state used by <see cref="UnixPlatformRuntimeService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    /// <summary>
    /// Stores the internal is linux state used by <see cref="UnixPlatformRuntimeService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets the toolchain platform value that forms part of the unix platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public ToolchainPlatformKind ToolchainPlatform => isMacOS
        ? ToolchainPlatformKind.MacOS
        : isLinux
            ? ToolchainPlatformKind.Linux
            : ToolchainPlatformKind.Other;

    /// <summary>
    /// Gets the provider bootstrap token value that forms part of the unix platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public string ProviderBootstrapToken => isMacOS ? "macos" : isLinux ? "linux" : "other";

    /// <inheritdoc />
    public bool IsCompatiblePythonBinary(string path)
    {
        if (!isMacOS || RuntimeInformation.OSArchitecture != Architecture.Arm64)
            return true;

        // Inspect the Mach-O headers before launching anything. An x86-only interpreter
        // can successfully answer a version probe through Rosetta and appear usable.
        const uint arm64Cpu = 0x0100000C;
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[8];
            stream.ReadExactly(header);
            uint magic = BinaryPrimitives.ReadUInt32BigEndian(header);
            if (magic == 0xCFFAEDFE)
                return BinaryPrimitives.ReadUInt32LittleEndian(header[4..]) == arm64Cpu;
            if (magic == 0xFEEDFACF)
                return BinaryPrimitives.ReadUInt32BigEndian(header[4..]) == arm64Cpu;
            if (magic is not (0xCAFEBABE or 0xCAFEBABF or 0xBEBAFECA or 0xBFBAFECA))
                return false;

            bool littleEndian = magic is 0xBEBAFECA or 0xBFBAFECA;
            int stride = magic is 0xCAFEBABF or 0xBFBAFECA ? 32 : 20;
            uint count = littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(header[4..])
                : BinaryPrimitives.ReadUInt32BigEndian(header[4..]);
            if (count == 0 || count > 64 || stream.Length < 8L + stride * count)
                return false;
            for (int index = 0; index < count; index++)
            {
                stream.Position = 8L + index * stride;
                stream.ReadExactly(header[..4]);
                uint cpu = littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(header)
                    : BinaryPrimitives.ReadUInt32BigEndian(header);
                if (cpu == arm64Cpu)
                    return true;
            }
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Trace.TraceWarning("Python architecture inspection failed: {0}", exception.GetType().Name);
            return false;
        }
    }

    /// <inheritdoc />
    public string GetNativeExecutableName(string baseName) => baseName;

    /// <inheritdoc />
    public bool SupportsPyTorchBackend(string backend) =>
        backend switch
        {
            "mps" => OperatingSystem.IsMacOSVersionAtLeast(14) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64,
            "cuda" or "rocm" or "xpu" => isLinux && RuntimeInformation.OSArchitecture == Architecture.X64,
            _ => false
        };

    /// <summary>
    /// Gets the path comparer used by this unix platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <inheritdoc />
    public StringComparer PathComparer => StringComparer.Ordinal;

    /// <summary>
    /// Gets the path comparison used by this unix platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <inheritdoc />
    public StringComparison PathComparison => StringComparison.Ordinal;

    /// <summary>
    /// Normalizes absolute path as part of the unix platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Performs paths equal as part of the unix platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Determines whether same or descendant path as part of the unix platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>Returns the configured executable unchanged because Unix tool wrappers are launched through their executable/script entry point.</summary>
    /// <inheritdoc />
    public (string ExecutablePath, string Arguments) PrepareToolchainProcessInvocation(string executablePath, string arguments)
    {
        try
        {
            return (executablePath, arguments);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixPlatformRuntimeService), nameof(PrepareToolchainProcessInvocation), exception);
            throw;
        }
    }

    /// <summary>
    /// Determines whether protected workspace root as part of the unix platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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


