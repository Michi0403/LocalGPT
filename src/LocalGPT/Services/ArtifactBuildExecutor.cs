using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LocalGPT.Services;

/// <summary>
/// Represents an artifact build executor application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="options">Options containing the caller-supplied values that control this operation.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ArtifactBuildExecutor(
    IOptionsMonitor<ArtifactBuildOptions> options,
    ILogger<ArtifactBuildExecutor> logger) : IArtifactBuildExecutor
{
    /// <summary>
    /// Defines the minimum timeout seconds constant used by <see cref="ArtifactBuildExecutor"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MinimumTimeoutSeconds = 5;
    /// <summary>
    /// Defines the maximum timeout seconds constant used by <see cref="ArtifactBuildExecutor"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaximumTimeoutSeconds = 900;

    /// <summary>
    /// Performs build for <see cref="ArtifactBuildExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding artifact build executor workflow.
    /// </summary>
    /// <param name="targetPath">Target path value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="allowedRoot">Allowed root value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <param name="outputDirectory">Output directory value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="requestedTimeout">Requested timeout value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <returns>The artifact build execution result produced by the operation.</returns>
    public async Task<ArtifactBuildExecutionResult> BuildAsync(
        string targetPath,
        string allowedRoot,
        string configuration,
        string? outputDirectory,
        TimeSpan requestedTimeout,
        CancellationToken cancellationToken = default,
        bool userConfirmed = false)
    {
        var startedAt = DateTime.UtcNow;
        if (!userConfirmed)
            return Result("HumanConfirmationRequired", null, startedAt, string.Empty, "Fresh human confirmation is required for this exact artifact build.");

        var settings = options.CurrentValue;
        if (!settings.Enabled)
            return Result("Disabled", null, startedAt, string.Empty, "Artifact builds are disabled by configuration.");

        var root = NormalizeDirectory(allowedRoot);
        var target = Path.GetFullPath(targetPath);
        if (!IsInsideRoot(target, root))
            return Result("Denied", null, startedAt, string.Empty, "Build target is outside the allowed artifact root.");
        if (!File.Exists(target))
            return Result("TargetMissing", null, startedAt, string.Empty, "Build target does not exist.");
        if (!target.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) &&
            !target.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return Result("Denied", null, startedAt, string.Empty, "Only .sln and .csproj targets are allowed.");
        }

        string? normalizedOutput = null;
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            normalizedOutput = Path.GetFullPath(outputDirectory);
            if (!IsInsideRoot(normalizedOutput, root))
                return Result("Denied", null, startedAt, string.Empty, "Build output is outside the allowed artifact root.");
            Directory.CreateDirectory(normalizedOutput);
        }

        var configuredSeconds = Math.Clamp(settings.MaxDurationSeconds, MinimumTimeoutSeconds, MaximumTimeoutSeconds);
        var requestedSeconds = (int)Math.Ceiling(requestedTimeout.TotalSeconds);
        var timeoutSeconds = Math.Clamp(Math.Min(configuredSeconds, Math.Max(MinimumTimeoutSeconds, requestedSeconds)), MinimumTimeoutSeconds, MaximumTimeoutSeconds);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(target) ?? root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(target);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(string.IsNullOrWhiteSpace(configuration) ? "Debug" : configuration.Trim());
        if (normalizedOutput is not null)
        {
            startInfo.ArgumentList.Add("-o");
            startInfo.ArgumentList.Add(normalizedOutput);
        }
        startInfo.ArgumentList.Add("/nologo");
        startInfo.ArgumentList.Add("/p:UseSharedCompilation=false");

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        try
        {
            if (!process.Start())
                return Result("ProcessNotStarted", null, startedAt, string.Empty, "The dotnet build process could not be started.");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                KillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                return Result("TimedOut", null, startedAt, await outputTask.ConfigureAwait(false), await errorTask.ConfigureAwait(false));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                KillProcessTree(process);
                throw;
            }

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            return Result(process.ExitCode == 0 ? "BuildPassed" : "BuildFailed", process.ExitCode, startedAt, output, error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bounded artifact build failed for target {TargetName}.", Path.GetFileName(target));
            return Result("BuildCheckError", null, startedAt, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Normalizes directory for <see cref="ArtifactBuildExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding artifact build executor workflow.
    /// </summary>
    /// <param name="path">Path value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeDirectory(string path)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Allowed root is required.", nameof(path));
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ArtifactBuildExecutor)}.{nameof(NormalizeDirectory)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ArtifactBuildExecutor)}.{nameof(NormalizeDirectory)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether inside root for <see cref="ArtifactBuildExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding artifact build executor workflow.
    /// </summary>
    /// <param name="path">Path value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="root">Root value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsInsideRoot(string path, string root)
    {
    try
    {
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(path, root, comparison) || path.StartsWith(root + Path.DirectorySeparatorChar, comparison);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ArtifactBuildExecutor)}.{nameof(IsInsideRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ArtifactBuildExecutor)}.{nameof(IsInsideRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs kill process tree for <see cref="ArtifactBuildExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding artifact build executor workflow.
    /// </summary>
    /// <param name="process">Process value supplied to the artifact build executor operation and used when producing its result.</param>
    private void KillProcessTree(Process process)
    {
    try
    {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ArtifactBuildExecutor)}.{nameof(KillProcessTree)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ArtifactBuildExecutor)}.{nameof(KillProcessTree)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs result for <see cref="ArtifactBuildExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding artifact build executor workflow.
    /// </summary>
    /// <param name="status">Status value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="exitCode">Exit code value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="startedAt">Started at value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="output">Output value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <returns>The artifact build execution result produced by the operation.</returns>
    private ArtifactBuildExecutionResult Result(
        string status,
        int? exitCode,
        DateTime startedAt,
        string output,
        string error) {
    try
    {
        return new(status, exitCode, output, error, DateTime.UtcNow - startedAt);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ArtifactBuildExecutor)}.{nameof(Result)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ArtifactBuildExecutor)}.{nameof(Result)} failed.");
        throw;
    }
}
}
