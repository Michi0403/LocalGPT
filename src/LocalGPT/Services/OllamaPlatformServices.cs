using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Shared executable-search helpers for operating-system-specific Ollama platform services.</summary>
public abstract class OllamaPlatformServiceBase : IOllamaPlatformService
{
    /// <summary>
    /// Gets the platform name value that forms part of the Ollama platform service base state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public abstract string PlatformName { get; }

    /// <summary>Returns platform-specific absolute candidate paths before PATH entries are inspected.</summary>
    /// <returns>Candidate executable paths ordered from most specific to least specific.</returns>
    protected abstract IEnumerable<string> GetKnownExecutableCandidates();

    /// <summary>Gets the comparer used to de-duplicate executable paths on the current host filesystem.</summary>
    /// <value>The executable path comparer value exposed by <see cref="OllamaPlatformServiceBase"/>.</value>
    protected virtual StringComparer ExecutablePathComparer => StringComparer.Ordinal;

    /// <summary>Gets the executable file name expected on the current operating system.</summary>
    /// <value>The executable name value exposed by <see cref="OllamaPlatformServiceBase"/>.</value>
    protected virtual string ExecutableName => "ollama";

    /// <summary>Resolves the documented provider-default model-store directory for this platform.</summary>
    /// <returns>The absolute model-store path, or <see langword="null"/> when this platform is unsupported.</returns>
    public abstract string? ResolveDefaultModelDirectory();

    /// <summary>Returns platform-specific mounted storage roots that can be inspected without traversing the host filesystem recursively.</summary>
    /// <returns>Mounted roots suitable for user review.</returns>
    public virtual IReadOnlyList<string> ResolveMountedStorageRoots()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(drive => drive.IsReady)
                .Select(drive => drive.RootDirectory.FullName)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(ExecutablePathComparer)
                .OrderBy(path => path, ExecutablePathComparer)
                .Take(64)
                .ToList();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Ollama mounted-storage discovery failed: {0}", exception);
            return [];
        }
    }

    /// <summary>Discovers existing Ollama model stores by checking documented, inherited, linked, and shallow mounted-volume candidates.</summary>
    /// <returns>Provider-shaped directories containing both <c>blobs</c> and <c>manifests</c>.</returns>
    public IReadOnlyList<string> DiscoverModelDirectories()
    {
        try
        {
            var discovered = new HashSet<string>(ExecutablePathComparer);
            var candidates = new List<string>();
            var defaultDirectory = ResolveDefaultModelDirectory();
            var inheritedDirectory = Environment.GetEnvironmentVariable("OLLAMA_MODELS");
            AddCandidate(candidates, defaultDirectory);
            AddCandidate(candidates, inheritedDirectory);

            if (!string.IsNullOrWhiteSpace(defaultDirectory))
            {
                try
                {
                    var info = new DirectoryInfo(defaultDirectory);
                    var target = info.Exists ? info.ResolveLinkTarget(returnFinalTarget: true) : null;
                    AddCandidate(candidates, target?.FullName);
                }
                catch
                {
                    // Link inspection is optional evidence and must not block provider management.
                }
            }

            foreach (var root in ResolveMountedStorageRoots())
            {
                AddKnownStoreShapes(candidates, root);
                try
                {
                    foreach (var child in Directory.EnumerateDirectories(root).Take(64))
                        AddKnownStoreShapes(candidates, child);
                }
                catch
                {
                    // A mounted volume can disappear or reject enumeration while the workbench is open.
                }
            }

            foreach (var candidate in candidates.Take(1024))
            {
                try
                {
                    var fullPath = Path.GetFullPath(candidate);
                    if (Directory.Exists(Path.Combine(fullPath, "blobs"))
                        && Directory.Exists(Path.Combine(fullPath, "manifests")))
                    {
                        discovered.Add(fullPath);
                    }
                }
                catch
                {
                    // Candidate paths are advisory only; invalid/inaccessible paths are skipped.
                }
            }

            return discovered.OrderBy(path => path, ExecutablePathComparer).ToList();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Ollama model-store discovery failed: {0}", exception);
            return [];
        }
    }

    /// <summary>Adds a non-empty path candidate to the bounded provider-store discovery list.</summary>
    /// <param name="candidates">Candidate collection being assembled.</param>
    /// <param name="path">Optional path to add.</param>
    private void AddCandidate(List<string> candidates, string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path))
                candidates.Add(Environment.ExpandEnvironmentVariables(path.Trim()));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceWarning("Skipping an invalid Ollama model-store path candidate: {0}", exception.Message);
        }
    }

    /// <summary>Adds common Ollama store layouts below one reviewed mounted-volume path.</summary>
    /// <param name="candidates">Candidate collection being assembled.</param>
    /// <param name="root">Mounted root or shallow child directory.</param>
    private void AddKnownStoreShapes(List<string> candidates, string root)
    {
        try
        {
            AddCandidate(candidates, root);
            AddCandidate(candidates, Path.Combine(root, "models"));
            AddCandidate(candidates, Path.Combine(root, ".ollama", "models"));
            AddCandidate(candidates, Path.Combine(root, "ollama", "models"));
            AddCandidate(candidates, Path.Combine(root, "Ollama", "models"));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceWarning("Skipping invalid Ollama model-store shapes below a mounted root: {0}", exception.Message);
        }
    }

    /// <summary>
    /// Resolves executable for <see cref="OllamaPlatformServiceBase"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama platform service base workflow.
    /// </summary>
    /// <inheritdoc />
    public string? ResolveExecutable()
    {
        try
        {
            var candidates = new List<string>();
            candidates.AddRange(GetKnownExecutableCandidates().Where(path => !string.IsNullOrWhiteSpace(path)));

            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            candidates.AddRange(path
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(directory => Path.Combine(directory, ExecutableName)));

            return candidates
                .Select(candidate => ExpandHome(candidate))
                .Distinct(ExecutablePathComparer)
                .FirstOrDefault(File.Exists);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Ollama executable discovery failed: {0}", exception);
            throw;
        }
    }

    /// <summary>
    /// Determines whether gui executable for <see cref="OllamaPlatformServiceBase"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama platform service base workflow.
    /// </summary>
    /// <inheritdoc />
    public virtual bool IsGuiExecutable(string executable)
    {
        try
        {
            return false;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Ollama GUI executable classification failed: {0}", exception);
            throw;
        }
    }

    /// <summary>Expands a leading home-directory marker without invoking a shell.</summary>
    /// <param name="path">Candidate path to normalize.</param>
    /// <returns>The normalized candidate path.</returns>
    private string ExpandHome(string path)
    {
        try
        {
            if (!path.StartsWith("~/", StringComparison.Ordinal))
                return path;
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.IsNullOrWhiteSpace(home) ? path : Path.Combine(home, path[2..]);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Ollama home-path expansion failed: {0}", exception);
            throw;
        }
    }
}

/// <summary>Resolves Windows Ollama installations from standard per-user/system locations and PATH.</summary>
public sealed class WindowsOllamaPlatformService : OllamaPlatformServiceBase
{
    /// <summary>
    /// Gets the executable path comparer used by this windows Ollama platform instance to locate the associated file-system resource.
    /// </summary>
    /// <inheritdoc />
    protected override StringComparer ExecutablePathComparer => StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Gets the platform name value that forms part of the windows Ollama platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public override string PlatformName => "Windows";

    /// <summary>
    /// Gets the executable name value that forms part of the windows Ollama platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    protected override string ExecutableName => "ollama.exe";

    /// <summary>
    /// Resolves default model directory as part of the windows Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public override string? ResolveDefaultModelDirectory()
    {
    try
    {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, ".ollama", "models");
    }
    catch (Exception exception)
    {
        System.Diagnostics.Trace.TraceError("WindowsOllamaPlatformService.ResolveDefaultModelDirectory failed: {0}", exception);
        throw;
    }
}

    /// <summary>Returns ready Windows drive roots so removable or secondary drives can host an Ollama model store.</summary>
    /// <returns>Ready filesystem drive roots.</returns>
    public override IReadOnlyList<string> ResolveMountedStorageRoots()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(drive => drive.IsReady)
                .Select(drive => drive.RootDirectory.FullName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(64)
                .ToList();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Windows mounted-drive discovery failed: {0}", exception);
            return [];
        }
    }

    /// <summary>
    /// Retrieves known executable candidates as part of the windows Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        try
        {
            var candidates = new List<string>();
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", "ollama.exe"));
                candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", "ollama app.exe"));
                candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", "ollama.app.exe"));
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                candidates.Add(Path.Combine(programFiles, "Ollama", "ollama.exe"));
                candidates.Add(Path.Combine(programFiles, "Ollama", "ollama app.exe"));
                candidates.Add(Path.Combine(programFiles, "Ollama", "ollama.app.exe"));
            }

            return candidates;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Windows Ollama candidate discovery failed: {0}", exception);
            throw;
        }
    }

    /// <summary>
    /// Determines whether gui executable as part of the windows Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public override bool IsGuiExecutable(string executable)
    {
        try
        {
            var name = new string(Path.GetFileNameWithoutExtension(executable)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
            return name == "ollamaapp";
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Windows Ollama GUI executable classification failed: {0}", exception);
            throw;
        }
    }
}

/// <summary>Resolves native macOS Ollama command-line installations from Homebrew/common user locations and PATH.</summary>
public sealed class MacOsOllamaPlatformService : OllamaPlatformServiceBase
{
    /// <summary>
    /// Gets the platform name value that forms part of the mac OS Ollama platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public override string PlatformName => "macOS";

    /// <summary>
    /// Resolves default model directory as part of the mac OS Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public override string? ResolveDefaultModelDirectory()
    {
    try
    {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, ".ollama", "models");
    }
    catch (Exception exception)
    {
        System.Diagnostics.Trace.TraceError("MacOsOllamaPlatformService.ResolveDefaultModelDirectory failed: {0}", exception);
        throw;
    }
}

    /// <summary>Returns mounted macOS volumes below <c>/Volumes</c> for explicit storage selection and bounded provider-store discovery.</summary>
    /// <returns>Currently mounted volume directories.</returns>
    public override IReadOnlyList<string> ResolveMountedStorageRoots()
    {
        try
        {
            return Directory.Exists("/Volumes")
                ? Directory.EnumerateDirectories("/Volumes").Take(64).ToList()
                : [];
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("macOS mounted-volume discovery failed: {0}", exception);
            return [];
        }
    }

    /// <summary>
    /// Retrieves known executable candidates as part of the mac OS Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        try
        {
            return
            [
                "/Applications/Ollama.app/Contents/Resources/ollama",
                "~/Applications/Ollama.app/Contents/Resources/ollama",
                "/opt/homebrew/bin/ollama",
                "/usr/local/bin/ollama",
                "~/.local/bin/ollama"
            ];
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("macOS Ollama candidate discovery failed: {0}", exception);
            throw;
        }
    }
}

/// <summary>Resolves Linux Ollama command-line installations from common system/user locations and PATH.</summary>
public sealed class LinuxOllamaPlatformService : OllamaPlatformServiceBase
{
    /// <summary>
    /// Gets the platform name value that forms part of the linux Ollama platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public override string PlatformName => "Linux";

    /// <summary>
    /// Resolves default model directory as part of the linux Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public override string? ResolveDefaultModelDirectory() {
    try
    {
        return "/usr/share/ollama/.ollama/models";
    }
    catch (Exception exception)
    {
        System.Diagnostics.Trace.TraceError("LinuxOllamaPlatformService.ResolveDefaultModelDirectory failed: {0}", exception);
        throw;
    }
}

    /// <summary>Returns common Linux user/removable mount roots without traversing the host root filesystem.</summary>
    /// <returns>Ready mounted storage directories visible below common mount parents.</returns>
    public override IReadOnlyList<string> ResolveMountedStorageRoots()
    {
        try
        {
            var roots = new HashSet<string>(StringComparer.Ordinal);
            var homeUser = Environment.UserName;
            foreach (var parent in new[] { "/mnt", "/media", string.IsNullOrWhiteSpace(homeUser) ? string.Empty : Path.Combine("/run/media", homeUser) })
            {
                if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
                    continue;
                try
                {
                    foreach (var child in Directory.EnumerateDirectories(parent).Take(64))
                        roots.Add(child);
                }
                catch
                {
                    // Individual mount parents may not be enumerable for the current service user.
                }
            }
            return roots.OrderBy(path => path, StringComparer.Ordinal).ToList();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Linux mounted-volume discovery failed: {0}", exception);
            return [];
        }
    }

    /// <summary>
    /// Retrieves known executable candidates as part of the linux Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        try
        {
            return ["/usr/local/bin/ollama", "/usr/bin/ollama", "~/.local/bin/ollama"];
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Linux Ollama candidate discovery failed: {0}", exception);
            throw;
        }
    }
}

/// <summary>Fallback platform service that restricts Ollama discovery to PATH on unsupported operating systems.</summary>
public sealed class GenericOllamaPlatformService : OllamaPlatformServiceBase
{
    /// <summary>
    /// Gets the platform name value that forms part of the generic Ollama platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public override string PlatformName => "Other";

    /// <summary>
    /// Resolves default model directory as part of the generic Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public override string? ResolveDefaultModelDirectory() {
    try
    {
        return null;
    }
    catch (Exception exception)
    {
        System.Diagnostics.Trace.TraceError("GenericOllamaPlatformService.ResolveDefaultModelDirectory failed: {0}", exception);
        throw;
    }
}

    /// <summary>
    /// Retrieves known executable candidates as part of the generic Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        try
        {
            return [];
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Generic Ollama candidate discovery failed: {0}", exception);
            throw;
        }
    }
}
