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

    /// <summary>Gets the executable file name expected on the current operating system.</summary>
    protected virtual string ExecutableName => "ollama";

    /// <inheritdoc />
    public string? ResolveExecutable()
    {
        var candidates = new List<string>();
        candidates.AddRange(GetKnownExecutableCandidates().Where(path => !string.IsNullOrWhiteSpace(path)));

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        candidates.AddRange(path
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory, ExecutableName)));

        return candidates
            .Select(candidate => ExpandHome(candidate))
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .FirstOrDefault(File.Exists);
    }

    /// <inheritdoc />
    public virtual bool IsGuiExecutable(string executable) => false;

    /// <summary>Expands a leading home-directory marker without invoking a shell.</summary>
    /// <param name="path">Candidate path to normalize.</param>
    /// <returns>The normalized candidate path.</returns>
    private static string ExpandHome(string path)
    {
        if (!path.StartsWith("~/", StringComparison.Ordinal))
            return path;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(home) ? path : Path.Combine(home, path[2..]);
    }
}

/// <summary>Resolves Windows Ollama installations from standard per-user/system locations and PATH.</summary>
public sealed class WindowsOllamaPlatformService : OllamaPlatformServiceBase
{
    /// <inheritdoc />
    public override string PlatformName => "Windows";

    /// <inheritdoc />
    protected override string ExecutableName => "ollama.exe";

    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "Programs", "Ollama", "ollama.exe");
            yield return Path.Combine(localAppData, "Programs", "Ollama", "ollama app.exe");
            yield return Path.Combine(localAppData, "Programs", "Ollama", "ollama.app.exe");
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            yield return Path.Combine(programFiles, "Ollama", "ollama.exe");
            yield return Path.Combine(programFiles, "Ollama", "ollama app.exe");
            yield return Path.Combine(programFiles, "Ollama", "ollama.app.exe");
        }
    }

    /// <inheritdoc />
    public override bool IsGuiExecutable(string executable)
    {
        var name = new string(Path.GetFileNameWithoutExtension(executable)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
        return name == "ollamaapp";
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
        yield return "/opt/homebrew/bin/ollama";
        yield return "/usr/local/bin/ollama";
        yield return "~/.local/bin/ollama";
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
        yield return "/usr/local/bin/ollama";
        yield return "/usr/bin/ollama";
        yield return "~/.local/bin/ollama";
    }
}

/// <summary>Fallback platform service that restricts Ollama discovery to PATH on unsupported operating systems.</summary>
public sealed class GenericOllamaPlatformService : OllamaPlatformServiceBase
{
    /// <inheritdoc />
    public override string PlatformName => "Other";

    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates() => [];
}
