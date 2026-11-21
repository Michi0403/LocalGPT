using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;

namespace LocalGPT.Services;

/// <summary>
/// Represents an artifact build executor application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="options">Options containing the caller-supplied values that control this operation.</param>
/// <param name="platform">Platform runtime service used for cross-platform process and filesystem behavior during artifact builds.</param>
/// <param name="scopeFactory">Creates an operation-scoped compiler inventory service without capturing scoped state in the singleton executor.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the artifact build executor workflow to provide the corresponding application capability.</param>
public sealed class ArtifactBuildExecutor(
    IOptionsMonitor<ArtifactBuildOptions> options,
    IPlatformRuntimeService platform,
    IServiceScopeFactory scopeFactory,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ArtifactBuildExecutor> logger) : IArtifactBuildExecutor
{

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
    /// <param name="projectId">Optional persisted project binding for generated revision workspaces.</param>
    /// <returns>The artifact build execution result produced by the operation.</returns>
    public async Task<ArtifactBuildExecutionResult> BuildAsync(
        string targetPath,
        string allowedRoot,
        string configuration,
        string? outputDirectory,
        TimeSpan requestedTimeout,
        CancellationToken cancellationToken = default,
        bool userConfirmed = false,
        Guid? projectId = null)
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
        // Resolve the exact compiler and any project-specific solution convention through
        // the existing persisted maintenance service, not PATH or an invented filename.
        // Do not hold a scoped service across an unrelated build or alter the saved default.
        IReadOnlyList<ProjectCompilerInstallation> compilers;
        IReadOnlyList<ProjectWorkspaceRoot> workspaceRoots;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var maintenance = scope.ServiceProvider.GetRequiredService<IProjectMaintenanceService>();
            compilers = await maintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false);
            workspaceRoots = await maintenance.GetWorkspaceRootsAsync(projectId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not obtain the registered compiler and workspace policy for an approved build.");
            return Result("CompilerInventoryUnavailable", null, startedAt, string.Empty,
                "The persisted compiler/workspace inventory could not be read; no executable was started.");
        }

        // A workspace owns its build-target regex and toolchain selection. A project-bound
        // generated revision may live in a separate approved artifact root, so the owning
        // project's workspace policy remains eligible without granting filesystem access
        // outside the caller's allowedRoot.
        var matchingWorkspace = workspaceRoots.Where(item => item.IsEnabled &&
                ((projectId.HasValue && item.ProjectId == projectId) ||
                 (!string.IsNullOrWhiteSpace(item.RootPath) && platform.IsSameOrDescendantPath(item.RootPath, target))))
            .OrderByDescending(item => projectId.HasValue && item.ProjectId == projectId)
            .ThenByDescending(item => !string.IsNullOrWhiteSpace(item.RootPath) && platform.IsSameOrDescendantPath(item.RootPath, target))
            .ThenByDescending(item => item.RootPath?.Length ?? 0)
            .ThenBy(item => item.Priority)
            .FirstOrDefault();
        var extension = Path.GetExtension(target);
        // The global solution pattern and MSBuild document suffix are a backwards-compatible
        // .NET fallback, NOT a universal build-target allowlist for other compilers.
        var dotNetTarget = runtimePolicy.GetPattern("runtime.project.solution-file-extension").IsMatch(target) ||
            (extension.Length > 1 && extension.EndsWith("proj", StringComparison.OrdinalIgnoreCase));
        var configuredTarget = false;
        if (matchingWorkspace?.SolutionPattern is { Length: > 0 } workspacePattern)
        {
            try
            {
                var relativeTarget = Path.GetRelativePath(root, target).Replace('\\', '/');
                var workspaceRelativeTarget = !string.IsNullOrWhiteSpace(matchingWorkspace.RootPath) &&
                    platform.IsSameOrDescendantPath(matchingWorkspace.RootPath, target)
                    ? Path.GetRelativePath(matchingWorkspace.RootPath, target).Replace('\\', '/')
                    : relativeTarget;
                var targetFileName = Path.GetFileName(target);
                configuredTarget = Regex.IsMatch(target.Replace('\\', '/'), workspacePattern,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, runtimePolicy.RegexTimeout) ||
                    Regex.IsMatch(relativeTarget, workspacePattern,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, runtimePolicy.RegexTimeout) ||
                    Regex.IsMatch(workspaceRelativeTarget, workspacePattern,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, runtimePolicy.RegexTimeout) ||
                    Regex.IsMatch(targetFileName, workspacePattern,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, runtimePolicy.RegexTimeout);
            }
            catch (Exception exception) when (exception is ArgumentException or RegexMatchTimeoutException)
            {
                logger.LogWarning(exception, "Selected workspace has an invalid or timed-out build-target path pattern.");
                return Result("Denied", null, startedAt, string.Empty,
                    "The selected workspace's build-target filename policy is invalid.");
            }
        }
        var validated = compilers.Where(item => item.IsEnabled && item.LastValidationSucceeded &&
                !string.IsNullOrWhiteSpace(item.ExecutablePath) && File.Exists(item.ExecutablePath))
            .ToList();
        ProjectCompilerInstallation? compiler;
        if (matchingWorkspace?.PreferredCompilerInstallationId is Guid preferredId)
        {
            compiler = validated.FirstOrDefault(item => item.Id == preferredId);
            if (compiler is null)
                return Result("CompilerNotValidated", null, startedAt, string.Empty,
                    "The workspace's preferred compiler is not enabled, validated, or executable. Revalidate it in project maintenance; the saved selection is unchanged.");
        }
        else if (dotNetTarget && string.IsNullOrWhiteSpace(matchingWorkspace?.BuildArguments))
        {
            // Preserve legacy .NET artifact generation with or without a default workspace.
            compiler = validated.Where(IsDotNetCompiler)
                .OrderByDescending(item => item.IsDefaultForLanguage)
                .ThenByDescending(item => Version.TryParse(item.Version, out var version) ? version : new Version(0, 0))
                .FirstOrDefault();
        }
        else
        {
            // Never guess a language from a filename or choose arbitrarily across registered
            // compilers. A single unambiguous validated installation is safe to select.
            compiler = validated.Count == 1 ? validated[0] : null;
        }
        if (compiler is null)
            return Result("CompilerNotSelected", null, startedAt, string.Empty,
                "No unambiguous validated compiler is selected. Assign a preferred compiler to the workspace or register the .NET SDK for the legacy .NET target.");

        // A validated .NET toolchain retains its historic project-document fallback,
        // even when a workspace specifies only solution filenames. Other languages may
        // only execute targets explicitly admitted by their workspace's saved regex.
        if (matchingWorkspace is not null ? !configuredTarget && !(dotNetTarget && IsDotNetCompiler(compiler)) : !dotNetTarget)
            return Result("Denied", null, startedAt, string.Empty,
                "Build target does not match the workspace's configured build-target regex or a selected .NET compiler's project document.");

        string? normalizedOutput = null;
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            normalizedOutput = Path.GetFullPath(outputDirectory);
            if (!IsInsideRoot(normalizedOutput, root))
                return Result("Denied", null, startedAt, string.Empty, "Build output is outside the allowed artifact root.");
            Directory.CreateDirectory(normalizedOutput);
        }

        var minimumTimeoutSeconds = Math.Max(1, runtimePolicy.GetInt("ArtifactBuildMinimumTimeoutSeconds"));
        var maximumTimeoutSeconds = Math.Max(minimumTimeoutSeconds, runtimePolicy.GetInt("ArtifactBuildMaximumTimeoutSeconds"));
        var configuredSeconds = settings.MaxDurationSeconds <= 0 ? maximumTimeoutSeconds : Math.Clamp(settings.MaxDurationSeconds, minimumTimeoutSeconds, maximumTimeoutSeconds);
        var requestedSeconds = requestedTimeout <= TimeSpan.Zero ? configuredSeconds : (int)Math.Min(int.MaxValue, Math.Ceiling(requestedTimeout.TotalSeconds));
        var timeoutSeconds = Math.Clamp(Math.Min(configuredSeconds, Math.Max(minimumTimeoutSeconds, requestedSeconds)), minimumTimeoutSeconds, maximumTimeoutSeconds);

        var startInfo = new ProcessStartInfo
        {
            FileName = compiler.ExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(target) ?? root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // ProjectMaintenanceService.BuildReview applies compiler settings first and the
        // workspace's persisted environment second. Honor that same user-owned order for
        // this separately approved artifact build; never mutate global process settings.
        try
        {
            foreach (var environmentJson in new[] { compiler.EnvironmentVariablesJson, matchingWorkspace?.EnvironmentVariablesJson })
            {
                if (string.IsNullOrWhiteSpace(environmentJson)) continue;
                var environment = JsonSerializer.Deserialize<Dictionary<string, string>>(environmentJson)
                    ?? new Dictionary<string, string>();
                foreach (var item in environment)
                    startInfo.Environment[item.Key] = item.Value;
            }
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Persisted compiler/workspace environment settings were invalid; build refused.");
            return Result("CompilerEnvironmentInvalid", null, startedAt, string.Empty,
                "The registered compiler/workspace environment is invalid. Correct it in project maintenance; no executable was started.");
        }

        if (!string.IsNullOrWhiteSpace(matchingWorkspace?.BuildArguments))
        {
            // The reviewed workspace argument template is authoritative for ANY language.
            // Substitute individual path/configuration values as quoted arguments; never
            // invoke a new shell or invent flags for an unknown compiler.
            var template = matchingWorkspace.BuildArguments;
            if (normalizedOutput is null && template.Contains("{outputDirectory}", StringComparison.OrdinalIgnoreCase))
                return Result("BuildArgumentsInvalid", null, startedAt, string.Empty,
                    "The workspace build arguments require an output directory, but none was supplied.");
            if (normalizedOutput is not null && !template.Contains("{outputDirectory}", StringComparison.OrdinalIgnoreCase))
                return Result("BuildArgumentsInvalid", null, startedAt, string.Empty,
                    "An output directory was requested, but the workspace build arguments do not use {outputDirectory}.");
            var arguments = ProjectBuildArgumentTemplate.Expand(template, target, configuration, normalizedOutput);
            var invocation = platform.PrepareToolchainProcessInvocation(compiler.ExecutablePath, arguments);
            startInfo.FileName = invocation.ExecutablePath;
            startInfo.Arguments = invocation.Arguments;
        }
        else if (IsDotNetCompiler(compiler) && dotNetTarget)
        {
            // Historical .NET defaults remain a compiler-specific convenience only.
            // Other language toolchains need explicit workspace build arguments.
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
        }
        else
            return Result("BuildArgumentsMissing", null, startedAt, string.Empty,
                "The selected compiler has no configured build arguments. Enter workspace build arguments in project maintenance.");

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        try
        {
            if (!process.Start())
                return Result("ProcessNotStarted", null, startedAt, string.Empty, "The selected compiler build process could not be started.");

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

    /// <summary>Recognizes the existing .NET compiler registration only for legacy .NET builds.</summary>
    private static bool IsDotNetCompiler(ProjectCompilerInstallation item) =>
        string.Equals(item.Language, "DotNet", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(item.KnowledgeProfileKey, "dotnet-sdk", StringComparison.OrdinalIgnoreCase);

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
            return platform.IsSameOrDescendantPath(root, path);
    
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
