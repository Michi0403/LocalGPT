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
    /// Retrieves known executable candidates as part of the mac OS Ollama platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        try
        {
            return ["/opt/homebrew/bin/ollama", "/usr/local/bin/ollama", "~/.local/bin/ollama"];
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
