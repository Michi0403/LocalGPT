using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Shared LM Studio/llmster CLI discovery across explicit platform candidate paths plus inherited PATH.</summary>
public abstract class LmStudioPlatformServiceBase : ILmStudioPlatformService
{
    /// <summary>
    /// Gets the platform name value that forms part of the lm studio platform service base state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public abstract string PlatformName { get; }

    /// <summary>Returns platform-specific absolute candidate paths before PATH entries are inspected.</summary>
    /// <returns>The collection produced by the operation.</returns>
    protected abstract IEnumerable<string> GetKnownExecutableCandidates();

    /// <summary>
    /// Gets the executable name value that forms part of the lm studio platform service base state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The executable name value exposed by <see cref="LmStudioPlatformServiceBase"/>.</value>
    protected virtual string ExecutableName => "lms";

    /// <summary>Gets the comparer used to de-duplicate executable paths on the current host filesystem.</summary>
    /// <value>The executable path comparer value exposed by <see cref="LmStudioPlatformServiceBase"/>.</value>
    protected virtual StringComparer ExecutablePathComparer => StringComparer.Ordinal;

    /// <summary>
    /// Resolves executable for <see cref="LmStudioPlatformServiceBase"/>, keeping the operation consistent with the state and invariants of the surrounding lm studio platform service base workflow.
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

    /// <summary>
    /// Performs expand home for <see cref="LmStudioPlatformServiceBase"/>, keeping the operation consistent with the state and invariants of the surrounding lm studio platform service base workflow.
    /// </summary>
    /// <param name="path">Path value supplied to the lm studio platform service base operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// <summary>
    /// Gets the platform name value that forms part of the windows lm studio platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public override string PlatformName => "Windows";
    /// <summary>
    /// Gets the executable name value that forms part of the windows lm studio platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    protected override string ExecutableName => "lms.exe";
    /// <summary>
    /// Gets the executable path comparer used by this windows lm studio platform instance to locate the associated file-system resource.
    /// </summary>
    /// <inheritdoc />
    protected override StringComparer ExecutablePathComparer => StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Retrieves known executable candidates as part of the windows lm studio platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
    /// <summary>
    /// Gets the platform name value that forms part of the mac OS lm studio platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public override string PlatformName => "macOS";
    /// <summary>
    /// Retrieves known executable candidates as part of the mac OS lm studio platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
    /// <summary>
    /// Gets the platform name value that forms part of the linux lm studio platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public override string PlatformName => "Linux";
    /// <summary>
    /// Retrieves known executable candidates as part of the linux lm studio platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
    /// <summary>
    /// Gets the platform name value that forms part of the generic lm studio platform state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public override string PlatformName => "Other";
    /// <summary>
    /// Retrieves known executable candidates as part of the generic lm studio platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
            System.Diagnostics.Trace.TraceError("Generic LM Studio candidate discovery failed: {0}", exception);
            throw;
        }
    }
}
