using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Shared executable-search helpers for operating-system-specific Ollama platform services.</summary>
public abstract class OllamaPlatformServiceBase : IOllamaPlatformService
{
    /// <inheritdoc />
    public abstract string PlatformName { get; }

    /// <summary>Returns platform-specific absolute candidate paths before PATH entries are inspected.</summary>
    /// <returns>Candidate executable paths ordered from most specific to least specific.</returns>
    protected abstract IEnumerable<string> GetKnownExecutableCandidates();

    /// <summary>Gets the comparer used to de-duplicate executable paths on the current host filesystem.</summary>
    protected virtual StringComparer ExecutablePathComparer => StringComparer.Ordinal;

    /// <summary>Gets the executable file name expected on the current operating system.</summary>
    protected virtual string ExecutableName => "ollama";

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
    /// <inheritdoc />
    protected override StringComparer ExecutablePathComparer => StringComparer.OrdinalIgnoreCase;

    /// <inheritdoc />
    public override string PlatformName => "Windows";

    /// <inheritdoc />
    protected override string ExecutableName => "ollama.exe";

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
    /// <inheritdoc />
    public override string PlatformName => "macOS";

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
    /// <inheritdoc />
    public override string PlatformName => "Linux";

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
    /// <inheritdoc />
    public override string PlatformName => "Other";

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
