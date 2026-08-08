using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LocalGPT.Services;

public sealed class ArtifactBuildExecutor(
    IOptionsMonitor<ArtifactBuildOptions> options,
    ILogger<ArtifactBuildExecutor> logger) : IArtifactBuildExecutor
{
    private const int MinimumTimeoutSeconds = 5;
    private const int MaximumTimeoutSeconds = 900;

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
