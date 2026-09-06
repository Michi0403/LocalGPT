using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Shared LM Studio/llmster CLI discovery across explicit platform candidate paths plus inherited PATH.</summary>
public abstract class LmStudioPlatformServiceBase : ILmStudioPlatformService
{
    /// <inheritdoc />
    public abstract string PlatformName { get; }

    /// <summary>Returns platform-specific absolute candidate paths before PATH entries are inspected.</summary>
    protected abstract IEnumerable<string> GetKnownExecutableCandidates();

    /// <summary>Gets the executable name expected on the current operating system.</summary>
    protected virtual string ExecutableName => "lms";

    /// <summary>Gets the comparer used to de-duplicate executable paths on the current host filesystem.</summary>
    protected virtual StringComparer ExecutablePathComparer => StringComparer.Ordinal;

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
                .Select(ExpandHome)
                .Distinct(ExecutablePathComparer)
                .FirstOrDefault(File.Exists);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("LM Studio CLI executable discovery failed: {0}", exception);
            throw;
        }
    }

    private string ExpandHome(string path)
    {
        try
        {
            if (!path.StartsWith("~/", StringComparison.Ordinal))
                return Environment.ExpandEnvironmentVariables(path);
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.IsNullOrWhiteSpace(home) ? path : Path.Combine(home, path[2..]);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("LM Studio home-path expansion failed: {0}", exception);
            throw;
        }
    }
}

/// <summary>Resolves Windows LM Studio/llmster CLI installations from user-scoped locations and PATH.</summary>
public sealed class WindowsLmStudioPlatformService : LmStudioPlatformServiceBase
{
    /// <inheritdoc />
    public override string PlatformName => "Windows";
    /// <inheritdoc />
    protected override string ExecutableName => "lms.exe";
    /// <inheritdoc />
    protected override StringComparer ExecutablePathComparer => StringComparer.OrdinalIgnoreCase;

    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home))
                return [];
            return
            [
                Path.Combine(home, ".lmstudio", "bin", "lms.exe"),
                Path.Combine(home, ".cache", "lm-studio", "bin", "lms.exe")
            ];
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Windows LM Studio candidate discovery failed: {0}", exception);
            throw;
        }
    }
}

/// <summary>Resolves macOS LM Studio/llmster CLI installations from the documented user-scoped CLI home and common command locations.</summary>
public sealed class MacOsLmStudioPlatformService : LmStudioPlatformServiceBase
{
    /// <inheritdoc />
    public override string PlatformName => "macOS";
    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        try
        {
            return
            [
                "~/.lmstudio/bin/lms",
                "~/.local/bin/lms",
                "/usr/local/bin/lms",
                "/opt/homebrew/bin/lms"
            ];
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("macOS LM Studio candidate discovery failed: {0}", exception);
            throw;
        }
    }
}

/// <summary>Resolves Linux LM Studio/llmster CLI installations from the documented user-scoped CLI home and common command locations.</summary>
public sealed class LinuxLmStudioPlatformService : LmStudioPlatformServiceBase
{
    /// <inheritdoc />
    public override string PlatformName => "Linux";
    /// <inheritdoc />
    protected override IEnumerable<string> GetKnownExecutableCandidates()
    {
        try
        {
            return
            [
                "~/.lmstudio/bin/lms",
                "~/.local/bin/lms",
                "~/bin/lms",
                "/usr/local/bin/lms",
                "/usr/bin/lms"
            ];
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Linux LM Studio candidate discovery failed: {0}", exception);
            throw;
        }
    }
}

/// <summary>Fallback LM Studio/llmster CLI discovery that restricts lookup to PATH.</summary>
public sealed class GenericLmStudioPlatformService : LmStudioPlatformServiceBase
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
            System.Diagnostics.Trace.TraceError("Generic LM Studio candidate discovery failed: {0}", exception);
            throw;
        }
    }
}
